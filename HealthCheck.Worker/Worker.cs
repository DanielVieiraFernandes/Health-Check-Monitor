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
    private readonly MonitoringServices _monitoringServices;
    private WorkerConfig? _workerConfig = new();
    private DateTime _nextConfigRefreshAt = DateTime.MinValue;
    private byte _refreshConfigLock = 0;
    private static readonly TimeSpan _configRefreshInterval = TimeSpan.FromMinutes(1);

    public Worker(ILogger<Worker> logger,
                  IConfiguration configuration,
                  IServiceScopeFactory scopeFactory,
                  MonitoringServices monitoringServices)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _monitoringServices = monitoringServices;
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
                if (_refreshConfigLock > 3)
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
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                    _refreshConfigLock++;
                    continue;
                }

                _refreshConfigLock = 0;

                //============================================================================================
                // Executa o monitoramento dos sistemas pendentes de verificação.
                //============================================================================================
                await _monitoringServices.ExecutarMonitoramento(stoppingToken, _workerConfig);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Erro crítico no worker. A execução será interrompida.");
                break;
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

            //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
            //Caso haja falha ao obter a configuração do banco, faz um fallback para a configuração presente no appsettings.json
            //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
            if (result.IsFailure)
            {
                WorkerConfig? config;
                var settingsSection = _configuration.GetSection("Settings");
                string[] requiredSettingKeys =
                [
                    nameof(WorkerConfig.MonitoringIntervalSeconds),
                nameof(WorkerConfig.TimeoutSeconds),
                nameof(WorkerConfig.MaxConcurrentChecks),
                nameof(WorkerConfig.MaxRetries),
                nameof(WorkerConfig.DelayBetweenRetriesMs)
                ];

                if (!settingsSection.Exists())
                {
                    config = null;
                }
                else
                {
                    bool hasAllRequiredSettings = true;

                    foreach (var key in requiredSettingKeys)
                    {
                        if (string.IsNullOrWhiteSpace(settingsSection[key]))
                        {
                            hasAllRequiredSettings = false;
                            break;
                        }
                    }

                    if (!hasAllRequiredSettings)
                    {
                        config = null;
                    }
                    else
                    {
                        try
                        {
                            config = settingsSection.Get<WorkerConfig?>();
                        }
                        catch
                        {
                            config = null;
                        }
                    }
                }

                _workerConfig = config;

                if (config == null)
                    throw new InvalidOperationException("Não foi possível carregar a configuração do worker.");

                return;
            }

            _workerConfig = result.Success!;
            _logger.LogInformation("Config do worker atualizada. Intervalo={Intervalo}s", _workerConfig.MonitoringIntervalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao atualizar config do banco. Mantendo última config em memória.");
        }
        finally
        {
            _nextConfigRefreshAt = DateTime.UtcNow.Add(_configRefreshInterval);
        }
    }


}
