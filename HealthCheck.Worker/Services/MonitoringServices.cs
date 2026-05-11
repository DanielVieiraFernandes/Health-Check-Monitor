using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.DTOS;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.Validators;
using System.Net;

namespace HealthCheck.Worker.Services;

public class MonitoringServices
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly uint _healthCheckTimeoutSeconds;

    public MonitoringServices(ILogger<Worker> logger,
                              IServiceScopeFactory scopeFactory,
                              IHttpClientFactory httpClientFactory,
                              IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;

        _healthCheckTimeoutSeconds = configuration.GetValue<uint>("Settings:HealthCheckTimeoutSeconds");
    }

    /// <summary>
    /// Executa o monitoramento dos sistemas pendentes de verificação.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public async Task ExecutarMonitoramento(CancellationToken stoppingToken)
    {
        try
        {
            //Crie um escopo para serviços Scoped (ex.: repositórios/serviços de banco).
            using var scope = _scopeFactory.CreateScope();
            var monitoredSystemService = scope.ServiceProvider
                .GetRequiredService<HealthCheck.Framework.Services.Database.MonitoredSystemService.MonitoredSystemService>();

            var resultPending = await monitoredSystemService.GetPendingMonitoredSystemsAsync();

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

            //Paralelismo controlado para evitar sobrecarga (10 requisições simultâneas).
            var semaphore = new SemaphoreSlim(10);

            //Processa cada sistema monitorado pendente em paralelo, respeitando o limite de concorrência.
            var tasks = pendentes.Select(async monitoredSystem =>
            {
                await semaphore.WaitAsync(stoppingToken);

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
                        return;
                    }

                    //Crio um client para cada requisição para evitar problemas de concorrência
                    //e garantir que cada verificação seja independente.
                    var client = _httpClientFactory.CreateClient();

                    //Configuro um timeout de segundos para não esperar por
                    //muito tempo uma resposta do sistema
                    client.Timeout = TimeSpan.FromSeconds(_healthCheckTimeoutSeconds);

                    //Realizo a requisição HTTP para a URL do sistema monitorado.
                    using var response = await client.GetAsync(monitoredSystem.Url, stoppingToken);

                    //Atualizo o status do sistema monitorado com base no código de resposta.
                    currentStatus = response.StatusCode == HttpStatusCode.OK ? HealthStatus.Healthy : HealthStatus.Unhealthy;
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
                }
                finally
                {
                    //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
                    //TODO: Caso o status do sistema monitorado tenha mudado para Unhealthy ou Unknown, devo enviar um alerta (ex.: email, webhook, etc.)
                    // para notificar os responsáveis sobre a indisponibilidade do sistema.
                    //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

                    try
                    {
                        UpdateMonitoredSystemStatusDTO updateMonitoredSystemStatus = new()
                        {
                            Id = monitoredSystem.Id,
                            LastCheckedAt = DateTime.Now,
                            Status = currentStatus
                        };

                        await monitoredSystemService.UpdateMonitoredSystemStatus(updateMonitoredSystemStatus);
                    }
                    catch (Exception ex)
                    {
                        //GRAVA O LOG DE FALHA AO ATUALIZAR O SISTEMA MONITORADO, INFORMANDO O ID DO SISTEMA MONITORADO E A URL.
                        _logger.LogError(ex, "Falha ao atualizar o sistema monitorado. SistemaId: {SystemId}, Url: {Url}", monitoredSystem.Id, monitoredSystem.Url);
                    }

                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar URLs pendentes no worker.");
        }
    }
}

