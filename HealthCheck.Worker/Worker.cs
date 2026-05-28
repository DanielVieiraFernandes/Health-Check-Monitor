using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database.WorkerConfigService;
using HealthCheck.Worker.Services;

namespace HealthCheck.Worker;

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: TESTAR O WORKER EM AMBIENTE DE DESENVOLVIMENTO E HOMOLOGAÇÃO, SIMULANDO CENÁRIOS DE FALHA E SUCESSO,
//PARA GARANTIR QUE O COMPORTAMENTO ESTEJA DE ACORDO COM AS EXPECTATIVAS E QUE OS LOGS ESTEJAM REGISTRANDO AS INFORMAÇÕES CORRETAMENTE.
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: TESTAR O WORKER COM USUÁRIOS REAIS E SISTEMAS REAIS PARA VALIDAR O FUNCIONAMENTO EM CENÁRIOS DO MUNDO REAL
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: DECIDIR SOBRE COMO FUNCIONARÁ O PROCESSO DE IMPLANTAÇÃO E DISTRIBUIÇÃO DO SISTEMA
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationService _notificationService;
    private WorkerConfig? _workerConfig = new();
    private DateTime _nextConfigRefreshAt = DateTime.MinValue;
    private byte _refreshConfigLock = 0;
    private static readonly TimeSpan _configRefreshInterval = TimeSpan.FromMinutes(1);

    public Worker(ILogger<Worker> logger,
                  IConfiguration configuration,
                  IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;

        using var scope = _scopeFactory.CreateScope();
        _notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //Ao iniciar o worker, força a atualização da configuração para garantir que esteja utilizando os parâmetros mais recentes
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        await RefreshConfigIfNeeded(stoppingToken, force: true);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //Caso haja falha ao obter a configuração do worker durante mais de 3 ciclos, interrompe a execução do worker
                //para evitar loop de erros e sobrecarga no sistema
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                if (_refreshConfigLock > 2)
                    throw new InvalidOperationException("Falha ao obter a configuração do worker após várias tentativas.");


                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //A cada ciclo, verifica se é necessário atualizar a configuração do worker, garantindo que mudanças sejam aplicadas
                //sem necessidade de reiniciar o serviço.
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                await RefreshConfigIfNeeded(stoppingToken, force: false);

                if (_workerConfig == null)
                {
                    //Aguarda um tempo antes de tentar novamente para evitar loop de erros
                    _logger.LogError("Config do worker não está disponível. Verifique os logs anteriores para identificar falhas na obtenção da configuração.");

                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //Notifica os administradores sobre a falha na obtenção da configuração,
                    //caso ainda não tenha sido enviado um email de alerta recentemente
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    await _notificationService.NotifyAdminAlertAsync(
                        alertKey: "config-null",
                        title: "Configuração do Worker indisponível",
                        summary: "O Worker não conseguiu carregar configurações válidas e fará uma nova tentativa em 10 segundos.",
                        severity: LogLevel.Error,
                        exception: null,
                        ct: stoppingToken);

                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    _refreshConfigLock++;
                    continue;
                }

                _refreshConfigLock = 0;

                //============================================================================================
                // Executa o monitoramento dos sistemas pendentes de verificação.
                //============================================================================================
                using var scope = _scopeFactory.CreateScope();
                var monitoringServices = scope.ServiceProvider.GetRequiredService<MonitoringServices>();

                await monitoringServices.ExecuteMonitoring(stoppingToken, _workerConfig);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                //============================================================================================
                //Executa a limpeza dos registros de monitoramento antigos para evitar acúmulo excessivo
                //de dados no banco
                //============================================================================================
                await monitoringServices.ExecuteDBCleanup(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Erro crítico no worker. A execução será interrompida.");

                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //Envia um email de alerta para o administrador informando sobre o erro crítico que causou a interrupção do worker
                //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                await _notificationService.NotifyAdminAlertAsync(
                    alertKey: "worker-critical-stop",
                    title: "Worker interrompido por erro crítico",
                    summary: "A execução principal do Worker foi encerrada por uma falha crítica.",
                    severity: LogLevel.Critical,
                    exception: ex,
                    ct: stoppingToken);

                throw;
            }

            //Aguarda os segundos parametrizados antes de iniciar o próximo ciclo de monitoramento
            await Task.Delay(TimeSpan.FromSeconds(_workerConfig!.MonitoringIntervalSeconds), stoppingToken);
        }
    }

    private async Task RefreshConfigIfNeeded(CancellationToken ct, bool force = false)
    {
        if (!force && DateTime.UtcNow < _nextConfigRefreshAt && _workerConfig != null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var workerConfigService = scope.ServiceProvider.GetRequiredService<WorkerConfigService>();

            var result = await workerConfigService.Get();

            _workerConfig = result.Success!;
            _logger.LogInformation("Config do worker atualizada. Intervalo={Intervalo}s", _workerConfig.MonitoringIntervalSeconds);
        }
        catch (Exception)
        {
            WorkerConfig? config;

            //Busca a seção de configurações 
            var settingsSection = _configuration.GetSection("Settings");

            //Declara as chaves obrigatórias para validar a existência e integridade da configuração no appsettings.json
            string[] requiredSettingKeys =
            [
                nameof(WorkerConfig.MonitoringIntervalSeconds),
                nameof(WorkerConfig.TimeoutSeconds),
                nameof(WorkerConfig.MaxConcurrentChecks),
                nameof(WorkerConfig.MaxRetries),
                nameof(WorkerConfig.DelayBetweenRetriesMs)
            ];

            //Verifica se a seção de configurações existe, e, caso não exista,
            //define a configuração como nula para forçar um erro
            if (!settingsSection.Exists())
            {
                config = null;
            }
            //Caso a seção exista
            else
            {
                bool hasAllRequiredSettings = true;

                //Verifica se todas as chaves obrigatórias estão presentes e possuem valores válidos (não nulos ou vazios)
                foreach (var key in requiredSettingKeys)
                {
                    //Caso não encontre a chave ou o valor seja nulo, vazio ou composto apenas por espaços em branco,
                    //considera que a configuração é inválida
                    if (string.IsNullOrWhiteSpace(settingsSection[key]))
                    {
                        hasAllRequiredSettings = false;
                        break;
                    }
                }

                //Se a configuração for considerada inválida por falta de chaves obrigatórias ou valores inválidos,
                //define a configuração como nula para forçar um erro
                if (!hasAllRequiredSettings)
                {
                    config = null;
                }
                else
                {
                    //Caso a configuração seja considerada válida, tenta fazer o bind dos valores para o
                    //objeto de configuração do worker.
                    try
                    {
                        config = settingsSection.Get<WorkerConfig?>();
                    }
                    //Se não conseguir fazer o bind por algum motivo, define a configuração como nula para forçar um erro
                    catch
                    {
                        config = null;
                    }
                }
            }

            _workerConfig = config;

            //Se a configuração for carregada com sucesso do arquivo de configuração, mesmo que tenha ocorrido um erro ao tentar carregar do serviço,
            //registra um alerta para o administrador informando sobre a falha na atualização da configuração
            if (_workerConfig != null)
            {
                _logger.LogInformation("Config do worker atualizada. Intervalo={Intervalo}s", _workerConfig.MonitoringIntervalSeconds);

                _logger.LogWarning("Config do worker atualizada por fallback, verificar o status do banco de dados para entender o motivo.");
                await _notificationService.NotifyAdminAlertAsync(
                    alertKey: "config-fallback",
                    title: "Worker em modo fallback de configuração",
                    summary: "A configuração foi carregada via appsettings porque houve falha na leitura do banco de dados.",
                    severity: LogLevel.Warning,
                    exception: null,
                    ct: ct);

            }

        }
        finally
        {
            _nextConfigRefreshAt = DateTime.UtcNow.Add(_configRefreshInterval);
        }
    }

}
