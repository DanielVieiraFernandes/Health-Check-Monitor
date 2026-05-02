using HealthCheck.Framework.Services.Database;
using HealthCheck.Framework.Services.Database.MonitoredSystemService;
using HealthCheck.Framework.Services.Database.UsersService;
using HealthCheck.Framework.Services.Cryptography;
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

        //****************************************************************************************************
        // ADICIONA UMA INSTÂNCIA PERSONALIZADA DE DatabaseService AO CONTÊINER DE INJEÇÃO DE DEPENDÊNCIA
        //****************************************************************************************************
        services.AddScoped(c => new DatabaseService(pgConnectionString));

        //****************************************************************************************************
        // SERVIÇOS
        //****************************************************************************************************
        services.AddScoped<MonitoredSystemService>();
        services.AddScoped<UsersService>();
        services.AddScoped<IPasswordEncrypter, BcryptPasswordEncrypter>();
    }

}
