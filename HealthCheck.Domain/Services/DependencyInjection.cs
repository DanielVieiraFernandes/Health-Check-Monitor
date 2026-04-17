using HealthCheck.Framework.Services.Database;
using HealthCheck.Framework.Services.Database.MonitoredSystemService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthCheck.Framework.Services;

public static class DependencyInjection
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        //****************************************************************************************************
        // RECUPERA A STRING DE CONEXÃO DO POSTGRESQL DO ARQUIVO DE CONFIGURAÇÃO
        //****************************************************************************************************
        string? pgConnectionString = configuration.GetConnectionString("HealthCheckDB");

        //****************************************************************************************************
        // CASO A STRING DE CONEXÃO ESTEJA VAZIA OU NULA,
        // LANÇA UMA EXCEÇÃO PARA INDICAR QUE HÁ UM PROBLEMA NA CONFIGURAÇÃO
        //****************************************************************************************************
        if (string.IsNullOrWhiteSpace(pgConnectionString))
            throw new InvalidOperationException("PostgreSQL connection string is not configured.");

        services.AddScoped(c => new DatabaseService(pgConnectionString));

        services.AddScoped<MonitoredSystemService>();
    }

}
