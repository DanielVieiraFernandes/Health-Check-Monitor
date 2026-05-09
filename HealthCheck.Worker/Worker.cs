using HealthCheck.Worker.Services;

namespace HealthCheck.Worker;


// - 1
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: REVISAR E REFATORAR O CÓDIGO PARA MELHORAR A CLAREZA, MANUTENIBILIDADE E DESEMPENHO, SE NECESSÁRIO. GARANTIR QUE O CÓDIGO
//ESTEJA BEM ORGANIZADO, COM NOMES DE VARIÁVEIS E MÉTODOS DESCRITIVOS, E QUE AS RESPONSABILIDADES ESTEJAM CLARAMENTE DEFINIDAS
//ENTRE AS CLASSES E MÉTODOS.
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

// - 2
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: GRAVAR LOG MAIS DETALHADO DOS ERROS E EXCEÇÕES. TUDO DEVE ESTAR MUITO BEM MAPEADO PARA QUE SEJA POSSÍVEL IDENTIFICAR COM
//MAIOR CLAREZA O QUE ESTÁ ACONTECENDO EM CADA FALHA, PARA FACILITAR A MANUTENÇÃO E RESOLUÇÃO DE PROBLEMAS FUTUROS.
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

// - 3
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: IMPLEMENTAR MÉTRICAS PARA MONITORAR O DESEMPENHO DO WORKER
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

// - 4
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: TESTAR O WORKER EM AMBIENTE DE DESENVOLVIMENTO E HOMOLOGAÇÃO, SIMULANDO CENÁRIOS DE FALHA E SUCESSO,
//PARA GARANTIR QUE O COMPORTAMENTO ESTEJA DE ACORDO COM AS EXPECTATIVAS E QUE OS LOGS ESTEJAM REGISTRANDO AS INFORMAÇÕES CORRETAMENTE.
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

// - 5
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: TESTAR O WORKER COM USUÁRIOS REAIS E SISTEMAS REAIS PARA VALIDAR O FUNCIONAMENTO EM CENÁRIOS DO MUNDO REAL
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

// - 6
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//TODO: DECIDIR SOBRE COMO FUNCIONARÁ O PROCESSO DE IMPLANTAÇÃO E DISTRIBUIÇÃO DO SISTEMA
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

public class Worker(ILogger<Worker> logger, MonitoringServices monitoringServices) : BackgroundService
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
                await monitoringServices.ExecutarMonitoramento(stoppingToken);

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


}
