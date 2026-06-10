using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database.MonitoredSystemService;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.DTOS;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.Validators;
using HealthCheck.Framework.Services.Database.SystemChecksService;
using HealthCheck.Worker.Services.SystemCheckers;

namespace HealthCheck.Worker.Services;

public class MonitoringServices
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEnumerable<ISystemChecker> _checkers;
    private readonly NotificationService _notificationService;
    private MonitoredSystemService? _monitoredSystemService = null;
    private SystemChecksService? _systemCheckService = null;
    private DateTime _cleaningDBAt = DateTime.Now;
    private const byte CLEANING_INTERVAL_DAYS = 7;

    public MonitoringServices(ILogger<Worker> logger,
                              IServiceScopeFactory scopeFactory,
                              IHttpClientFactory httpClientFactory,
                              IEnumerable<ISystemChecker> checkers,
                              NotificationService notificationService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _checkers = checkers;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Executa o monitoramento dos sistemas pendentes de verificação.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <param name="workerConfig"></param>
    /// <returns></returns>
    public async Task ExecuteMonitoring(CancellationToken stoppingToken, WorkerConfig workerConfig)
    {
        try
        {
            //Crie um escopo para serviços Scoped (ex.: repositórios/serviços de banco).
            using var scope = _scopeFactory.CreateScope();
            _monitoredSystemService = scope.ServiceProvider
                .GetRequiredService<HealthCheck.Framework.Services.Database.MonitoredSystemService.MonitoredSystemService>();

            var resultPending = await _monitoredSystemService.GetPendingMonitoredSystemsAsync();

            //Caso haja falha ao obter os sistemas pendentes, registre o erro e retorne para agendar a próxima verificação.
            if (resultPending.IsFailure)
            {
                var failure = resultPending.Failure;
                var errors = string.Join("\n - ", failure?.Errors.Select(e => e.ErrorMessage) ?? ["!!!Motivo desconhecido!!!"]);
                _logger.LogWarning("Falha ao obter os sistemas pendentes. Status: {StatusCode}. Motivo: {Reason}", failure?.StatusCode, errors);
                return;
            }

            var pendentes = resultPending.Success!;

            //Caso não haja sistemas monitorados pendentes para verificação, registra o aviso e retorna para a próxima execução agendada
            if (pendentes.Count == 0)
            {
                _logger.LogInformation("Nenhum sistema monitorado pendente para verificação.");
                return;
            }

            //Cria uma instância do serviço de verificações de sistemas para registrar os resultados das verificações após
            //processar cada sistema monitorado pendente.
            _systemCheckService = scope.ServiceProvider
                .GetRequiredService<HealthCheck.Framework.Services.Database.SystemChecksService.SystemChecksService>();


            //Paralelismo controlado para evitar sobrecarga 
            var semaphore = new SemaphoreSlim(workerConfig.MaxConcurrentChecks);

            //Processa cada sistema monitorado pendente em paralelo, respeitando o limite de concorrência.
            var tasks = pendentes.Select(async monitoredSystem =>
            {
                await semaphore.WaitAsync(stoppingToken);

                SystemCheck systemCheck = new()
                {
                    UserId = monitoredSystem.UserId,
                    SystemId = monitoredSystem.Id,
                };

                HealthStatus currentStatus = monitoredSystem.LastStatus;

                try
                {
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //VALIDA A URL E BLOQUEIA ENDPOINTS INTERNOS OU NÃO PERMITIDOS (SSRF), GARANTINDO QUE O WORKER SOMENTE
                    //ACESSE DESTINOS EXTERNOS SEGUROS PARA MONITORAMENTO.
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    var validation = await MonitoredSystemUrlSafetyValidator.IsAllowedAsync(monitoredSystem.Url);

                    if (!validation)
                    {
                        currentStatus = HealthStatus.Unknown;
                        _logger.LogWarning("URL bloqueada para verificação. SistemaId: {SystemId}, Url: {Url}, Motivo: Não passou na validação de destinos seguros", monitoredSystem.Id, monitoredSystem.Url);
                        systemCheck.ErrorMessage = $"A URL: \"{monitoredSystem.Url}\" foi bloqueada para verificação. Não passou na validação de destinos seguros.";
                        return;
                    }

                    //Usa o dispatcher de ISystemChecker para delegar a verificação ao checker apropriado
                    var checker = _checkers.FirstOrDefault(c => c.SupportedType == monitoredSystem.SystemType)
                        ?? _checkers.First();

                    var result = await checker.CheckAsync(monitoredSystem, stoppingToken);

                    currentStatus = result.Status;
                    systemCheck.LatencyMs = result.LatencyMs;
                    systemCheck.SystemResponse = result.Response;
                    systemCheck.ErrorMessage = result.ErrorMessage;
                    systemCheck.ExceptionType = result.ExceptionType;
                    systemCheck.StackTrace = result.StackTrace;
                }
                catch (Exception ex)
                {
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //CASO NÃO HAJA RESPOSTA NA REQUISIÇÃO OU HAJA QUALQUER FALHA DURANTE O PROCESSAMENTO, ATUALIZA O SISTEMA
                    //MONITORADO COMO TENDO O STATUS DESCONHECIDO E GRAVA O LOG DE FALHA AO PROCESSAR A URL PENDENTE, INFORMANDO
                    //O ID DO SISTEMA MONITORADO E A URL.
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    currentStatus = HealthStatus.Unknown;
                    _logger.LogWarning(ex, "Falha ao processar a URL pendente. SistemaId: {SystemId}, Url: {Url}", monitoredSystem.Id, monitoredSystem.Url);

                    systemCheck.ErrorMessage = $"Falha ao processar a URL: \"{monitoredSystem.Url}\". Motivo: {ex.Message}";
                    systemCheck.ExceptionType = GetExceptionName(ex, stoppingToken);
                    systemCheck.StackTrace = ex.StackTrace;
                }
                finally
                {
                    systemCheck.Status = currentStatus;

                    if (monitoredSystem.LastStatus != currentStatus &&
                        (currentStatus == HealthStatus.Unhealthy || currentStatus == HealthStatus.Unknown))
                    {
                        await NotifySystemOwnerStatusAlertAsync(monitoredSystem, currentStatus, stoppingToken);
                    }

                    //*************************************************************************************************************************************************************************
                    //Tenta atualizar as informações do sistema monitorado
                    //registrando a checagem realizada e atualizando o status do sistema monitorado no banco de dados
                    //*************************************************************************************************************************************************************************
                    var resultUpdate = await TryUpdateInformationCheck(monitoredSystem, systemCheck, currentStatus, workerConfig);

                    if (!resultUpdate)
                        _logger.LogError("Falha ao atualizar as informações do sistema monitorado. SistemaId: {SystemId}, Url: {Url}", monitoredSystem.Id, monitoredSystem.Url);

                    semaphore.Release();
                }
            });

            //O Task.WhenAll faz com que o método espere a conclusão de todas as tarefas de monitoramento em paralelo antes de prosseguir.
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar URLs pendentes no worker.");
            await _notificationService.NotifyAdminAlertAsync(
                alertKey: "monitoring-execution-failed",
                title: "Falha no processamento de URLs pendentes",
                summary: "O Worker não conseguiu concluir o processamento do lote de URLs pendentes no ciclo atual.",
                severity: LogLevel.Error,
                exception: ex,
                ct: stoppingToken);
        }
    }

    public async Task ExecuteDBCleanup(CancellationToken stoppingToken)
    {
        //***********************************************************************************************************************
        //A cada X dias, o worker deve realizar uma limpeza dos dados antigos de checagens no banco de dados para evitar acúmulo
        //excessivo de dados e garantir a performance do sistema. Os registros serão mantidos por no máximo 7 dias e depois
        //serão excluídos permanentemente.
        //***********************************************************************************************************************
        if (_cleaningDBAt.AddDays(CLEANING_INTERVAL_DAYS).Day >= DateTime.Now.Day)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var systemChecksService = scope.ServiceProvider
                    .GetRequiredService<HealthCheck.Framework.Services.Database.SystemChecksService.SystemChecksService>();

                await systemChecksService.CleanOldChecks();

                _cleaningDBAt = DateTime.Now;

                _logger.LogInformation("Limpeza de dados antigos realizada com sucesso.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao realizar a limpeza de dados antigos.");
                await _notificationService.NotifyAdminAlertAsync(
                    alertKey: "db-cleanup-failed",
                    title: "Falha na limpeza de dados antigos",
                    summary: "O Worker não conseguiu concluir a limpeza periódica dos registros antigos de monitoramento.",
                    severity: LogLevel.Error,
                    exception: ex,
                    ct: stoppingToken);
            }
        }
    }

    private async Task NotifySystemOwnerStatusAlertAsync(
        MonitoredSystem monitoredSystem,
        HealthStatus currentStatus,
        CancellationToken ct)
    {
        var summary =
$"O sistema monitorado \"{monitoredSystem.Name}\" mudou para o status {currentStatus}.\n" +
$"URL monitorada: {monitoredSystem.Url}\n" +
$"Horário (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}.";

        await _notificationService.NotifyUserAlertAsync(
            alertKey: $"status-change-{monitoredSystem.UserId}-{monitoredSystem.Id}-{currentStatus}",
            userId: monitoredSystem.UserId,
            userName: monitoredSystem.Name,
            title: $"Alerta do sistema {monitoredSystem.Name}",
            summary: summary,
            severity: currentStatus == HealthStatus.Unhealthy ? LogLevel.Warning : LogLevel.Error,
            ct: ct);
    }

    private async Task<bool> TryUpdateInformationCheck(MonitoredSystem monitoredSystem,
                                                       SystemCheck systemCheck,
                                                       HealthStatus currentStatus,
                                                       WorkerConfig workerConfig)
    {
        byte retryCount = 0;
        bool isSuccess = true;

        do
        {
            retryCount++;

            //--------------------------------------------------------------------------------------------------------------------
            //Tenta registrar a checagem realizada no banco de dados
            //--------------------------------------------------------------------------------------------------------------------
            try
            {
                //Registro a checagem realizada no banco de dados
                await _systemCheckService!.CreateCheck(systemCheck);
                isSuccess = true;
            }
            //--------------------------------------------------------------------------------------------------------------------
            //Caso não consiga, registra o erro no log, informando o ID do sistema monitorado e a URL, mas continua a execução
            //para tentar atualizar o status do sistema monitorado e processar os demais sistemas pendentes.
            //--------------------------------------------------------------------------------------------------------------------
            catch (Exception ex)
            {
                //GRAVA O LOG DE FALHA AO TENTAR REGISTRAR A CHECAGEM REALIZADA NO BANCO DE DADOS, INFORMANDO O ID DO SISTEMA MONITORADO E A URL.
                _logger.LogError(ex, "Falha ao tentar registrar a checagem realizada no banco de dados. SistemaId: {SystemId}, Url: {Url}", monitoredSystem.Id, monitoredSystem.Url);
                isSuccess = false;
            }


            //========================================================================================================================================
            //Somente se o registro da checagem no banco de dados for bem sucedido, tenta atualizar o status do sistema monitorado.
            //========================================================================================================================================
            if (isSuccess)
            {
                //--------------------------------------------------------------------------------------------------------------------
                //Tenta atualizar o status do sistema monitorado no banco de dados 
                //--------------------------------------------------------------------------------------------------------------------
                try
                {
                    UpdateMonitoredSystemStatusDTO updateMonitoredSystemStatus = new()
                    {
                        Id = monitoredSystem.Id,
                        LastCheckedAt = DateTime.Now,
                        Status = currentStatus
                    };

                    var result = await _monitoredSystemService!.UpdateMonitoredSystemStatus(updateMonitoredSystemStatus);

                    if (result.IsFailure)
                    {
                        var failure = result.Failure;
                        var errors = string.Join("\n - ", failure?.Errors.Select(e => e.ErrorMessage) ?? ["!!!Motivo desconhecido!!!"]);
                        _logger.LogError("Falha ao atualizar o sistema monitorado. SistemaId: {SystemId}, Url: {Url}, Status: {StatusCode}, Motivo: {Reason}",
                                         monitoredSystem.Id, monitoredSystem.Url, failure?.StatusCode, errors);
                        isSuccess = false;
                        continue;
                    }

                    isSuccess = true;
                }
                //--------------------------------------------------------------------------------------------------------------------
                //Caso não consiga, registra o erro no log, informando o ID do sistema monitorado e a URL, mas continua a execução
                //--------------------------------------------------------------------------------------------------------------------
                catch (Exception ex)
                {
                    //GRAVA O LOG DE FALHA AO ATUALIZAR O SISTEMA MONITORADO, INFORMANDO O ID DO SISTEMA MONITORADO E A URL.
                    _logger.LogError(ex, "Falha ao atualizar o sistema monitorado. SistemaId: {SystemId}, Url: {Url}", monitoredSystem.Id, monitoredSystem.Url);
                    isSuccess = false;
                }

            }

            await Task.Delay(workerConfig.DelayBetweenRetriesMs);

        } while (retryCount < workerConfig.MaxRetries);

        return isSuccess;
    }

    private static string GetExceptionName(Exception ex, CancellationToken stoppingToken)
    {
        if (ex is TaskCanceledException && !stoppingToken.IsCancellationRequested)
        {
            return nameof(TimeoutException);
        }

        return ex.GetType().Name;
    }
}

