using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.DTOS;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.Validators;
using System.Net;

namespace HealthCheck.Worker.Services;

public class MonitoringServices(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory)
{
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
            using var scope = scopeFactory.CreateScope();
            var monitoredSystemService = scope.ServiceProvider
                .GetRequiredService<HealthCheck.Framework.Services.Database.MonitoredSystemService.MonitoredSystemService>();

            var resultPending = await monitoredSystemService.GetPendingMonitoredSystemsAsync();

            //Caso haja falha ao obter os sistemas pendentes, registre o erro e retorne para agendar a próxima verificação.
            if (resultPending.IsFailure)
            {
                var failure = resultPending.Failure;
                var firstError = failure?.Errors.FirstOrDefault()?.ErrorMessage ?? "Sem detalhes";
                logger.LogWarning("Falha ao obter os sistemas pendentes. Status: {StatusCode}. Motivo: {Reason}", failure?.StatusCode, firstError);
                return;
            }

            var pendentes = resultPending.Success!;

            if (pendentes.Count == 0)
            {
                logger.LogInformation("Nenhum sistema monitorado pendente para verificação.");
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
                        logger.LogWarning("URL bloqueada para verificação. SistemaId: {SystemId}, Url: {Url}, Motivo: Não passou na validação de destinos seguros", monitoredSystem.Id, monitoredSystem.Url);
                        return;
                    }

                    //Crio um client para cada requisição para evitar problemas de concorrência
                    //e garantir que cada verificação seja independente.
                    var client = httpClientFactory.CreateClient();

                    //Configuro um timeout de 10 segundos para não esperar por
                    //muito tempo uma resposta do sistema
                    client.Timeout = TimeSpan.FromSeconds(10);

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
                    logger.LogWarning(ex, "Falha ao processar a URL pendente. SistemaId: {SystemId}, Url: {Url}", monitoredSystem.Id, monitoredSystem.Url);
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
                        logger.LogError(ex, "Falha ao atualizar o sistema monitorado. SistemaId: {SystemId}, Url: {Url}", monitoredSystem.Id, monitoredSystem.Url);
                    }

                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao processar URLs pendentes no worker.");
        }
    }
}

