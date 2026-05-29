using HealthCheck.Framework.Repositories.MonitoredSystemRepository;
using HealthCheck.Framework.Repositories.SystemChecksRepository;
using HealthCheck.Framework.Repositories.UsersRepository;
using HealthCheck.Framework.Repositories.WorkerConfigRepository;
using Microsoft.Extensions.DependencyInjection;

namespace HealthCheck.Framework.Repositories;

public static class DependencyInjection
{

    public static void AddFrameworkRepositories(this IServiceCollection services)
    {
        // FAZ COM QUE O DAPPER CONSIGA MAPEAR AS COLUNAS COM UNDERLINE PARA PROPRIEDADES COM PASCAL/CAMEL CASE
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddScoped<IMonitoredSystemRepository, MonitoredSystemRepository.MonitoredSystemRepository>();
        services.AddScoped<IUsersRepository, UsersRepository.UsersRepository>();
        services.AddScoped<ISystemChecksRepository, SystemChecksRepository.SystemChecksRepository>();
        services.AddScoped<IWorkerConfigRepository, WorkerConfigRepository.WorkerConfigRepository>();
    }

}
