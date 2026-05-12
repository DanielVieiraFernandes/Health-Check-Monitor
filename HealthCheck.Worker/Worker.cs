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
    private readonly MonitoringServices _monitoringServices;

    private readonly int _healthCheckIntervalSeconds;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, MonitoringServices monitoringServices)
    {
        _logger = logger;
        _configuration = configuration;
        _monitoringServices = monitoringServices;

        _healthCheckIntervalSeconds = configuration.GetValue<int>("Settings:HealthCheckIntervalSeconds");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //============================================================================================
                // Executa o monitoramento dos sistemas pendentes de verificação.
                //============================================================================================
                await _monitoringServices.ExecutarMonitoramento(stoppingToken);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar o ciclo do worker.");
            }

            //Aguarda os segundos parametrizados antes de iniciar o próximo ciclo de monitoramento
            await Task.Delay(TimeSpan.FromSeconds(_healthCheckIntervalSeconds), stoppingToken);
        }
    }


}
