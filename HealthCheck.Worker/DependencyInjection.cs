using HealthCheck.Framework.Repositories.MonitoredSystemRepository;
using HealthCheck.Framework.Services.Database;
using HealthCheck.Framework.Services.Database.MonitoredSystemService;
using HealthCheck.Worker.Services;
using Serilog;

namespace HealthCheck.Worker;

public static class DependencyInjection
{
    public static void AddWorkerDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        AddRepositories(services);
        AddFrameworkServices(services, configuration);
        AddWorkerServices(services);
    }

    private static void AddWorkerServices(IServiceCollection services)
    {
        services.AddSingleton<MonitoringServices>();
    }

    private static void AddFrameworkServices(IServiceCollection services, IConfiguration configuration)
    {
        //Recupera a string de conexão do banco de dados Postgre
        var connectionString = configuration.GetConnectionString("HealthCheckDb");

        if (string.IsNullOrEmpty(connectionString))
        {
            Log.Fatal("A string de conexão para o banco de dados não foi configurada. Verifique as configurações e tente novamente.");
            return;
        }

        services.AddScoped(config => new DatabaseService(connectionString));
        services.AddScoped<MonitoredSystemService>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        //Ativa o mapeamento de nomes com underscores para propriedades em C# (ex.: "last_checked_at" -> "LastCheckedAt")
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddScoped<IMonitoredSystemRepository, MonitoredSystemRepository>();
    }
}
