using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Helpers;
using System.Net;

namespace HealthCheck.Worker;

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: GRAVAR LOG MAIS DETALHADO DOS ERROS E EXCEÇÕES. TUDO DEVE ESTAR MUITO BEM MAPEADO PARA QUE SEJA POSSÍVEL IDENTIFICAR COM
//MAIOR CLAREZA O QUE ESTÁ ACONTECENDO EM CADA FALHA, PARA FACILITAR A MANUTENÇÃO E RESOLUÇÃO DE PROBLEMAS FUTUROS.
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: IMPLEMENTAR MÉTRICAS PARA MONITORAR O DESEMPENHO DO WORKER
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

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

public class Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //============================================================================================
                // Executa o monitoramento dos sistemas pendentes de verificação.
                //============================================================================================
                await ExecutarMonitoramento(stoppingToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao processar o ciclo do worker.");
            }

            //Aguarda 30 segundos antes de iniciar o próximo ciclo de monitoramento
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
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

            //Paralelismo controlado para evitar sobrecarga (5 requisições simultâneas).
            var semaphore = new SemaphoreSlim(5);

            //Processa cada sistema monitorado pendente em paralelo, respeitando o limite de concorrência.
            var tasks = pendentes.Select(async monitoredSystem =>
            {
                await semaphore.WaitAsync(stoppingToken);

                //Clone do objeto para comparação e gravação de histórico
                var monitoredSystemClone = monitoredSystem.Clone();

                try
                {
                    //Crio um client para cada requisição para evitar problemas de concorrência
                    //e garantir que cada verificação seja independente.
                    var client = httpClientFactory.CreateClient();

                    //Configuro um timeout de 20 segundos para não esperar por
                    //muito tempo uma resposta do sistema
                    client.Timeout = TimeSpan.FromSeconds(20);

                    //Realizo a requisição HTTP para a URL do sistema monitorado.
                    using var response = await client.GetAsync(monitoredSystem.Url, stoppingToken);

                    //Atualizo o status do sistema monitorado com base no código de resposta.
                    monitoredSystem.LastStatus = response.StatusCode == HttpStatusCode.OK ? HealthStatus.Healthy : HealthStatus.Unhealthy;
                }
                catch (Exception ex)
                {
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //CASO NÃO HAJA RESPOSTA NA REQUISIÇÃO OU HAJA QUALQUER FALHA DURANTE O PROCESSAMENTO, 
                    //ATUALIZA O SISTEMA MONITORADO COMO TENDO O STATUS DESCONHECIDO E GRAVA O LOG DE FALHA AO PROCESSAR A URL PENDENTE,
                    //INFORMANDO O ID DO SISTEMA MONITORADO E A URL.
                    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    monitoredSystem.LastStatus = HealthStatus.Unknown;
                    logger.LogWarning(ex, "Falha ao processar a URL pendente. SistemaId: {SystemId}, Url: {Url}", monitoredSystem.Id, monitoredSystem.Url);
                }
                finally
                {
                    monitoredSystem.LastCheckedAt = DateTime.Now;

                    //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
                    //TODO: Caso o status do sistema monitorado tenha mudado para Unhealthy ou Unknown, devo enviar um alerta (ex.: email, webhook, etc.)
                    // para notificar os responsáveis sobre a indisponibilidade do sistema.
                    //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

                    try
                    {
                        await monitoredSystemService.UpdateMonitoredSystem(monitoredSystem, monitoredSystemClone);
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
