using HealthCheck.Framework.Services.Cryptography;
using HealthCheck.Framework.Services.Database;
using HealthCheck.Framework.Services.Database.MonitoredSystemService;
using HealthCheck.Framework.Services.Database.SystemChecksService;
using HealthCheck.Framework.Services.Database.UsersService;
using HealthCheck.Framework.Services.Database.WorkerConfigService;
using HealthCheck.Framework.Services.Email;
using HealthCheck.Framework.Services.Email.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthCheck.Framework.Services;

public static class DependencyInjection
{
    public static void AddFrameworkServices(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabaseServices(services, configuration);
        AddCryptographyServices(services);
        AddEmailService(services, configuration);
    }

    private static void AddDatabaseServices(IServiceCollection services, IConfiguration configuration)
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

        services.AddScoped<MonitoredSystemService>();
        services.AddScoped<UsersService>();
        services.AddScoped<SystemChecksService>();
        services.AddScoped<WorkerConfigService>();
    }

    private static void AddCryptographyServices(IServiceCollection services)
    {
        services.AddScoped<IPasswordEncrypter, BcryptPasswordEncrypter>();
        services.AddScoped<ISMTPCredentialProvider, SMTPCredentialProvider>();
    }

    private static void AddEmailService(IServiceCollection services, IConfiguration configuration)
    {
        //******************************************************************************************************
        //Recupero as credenciais de email no appsettings.json
        //******************************************************************************************************
        var emailCredentials = configuration.GetSection("EmailSettings:SMTPSettings").Get<EmailCredentials>();

        //******************************************************************************************************
        //Recupero a senha das variáveis de ambiente
        //******************************************************************************************************
        var password = configuration["SmtpHPassword"];

        //******************************************************************************************************
        //Caso não seja possível recuperar as credenciais de email ou a senha, lanço uma exceção para impedir o 
        //início da aplicação e informo o ocorrido
        //******************************************************************************************************
        if (emailCredentials == null || password == null)
            throw new InvalidOperationException("Configurações de e-mail incompletas.");

        //******************************************************************************************************
        //Atribuo a senha, pois é a única que não salvo no appsettings.json por questões de segurança
        //******************************************************************************************************
        emailCredentials.Password = password;

        //******************************************************************************************************
        //Adiciono na injeção de dependência o serviço de email com as credenciais recuperadas
        //******************************************************************************************************
        services.AddScoped(serviceProvider =>
        {
            var smtpCredentialProvider = serviceProvider.GetRequiredService<ISMTPCredentialProvider>();
            return new EmailService(emailCredentials, smtpCredentialProvider);
        });
    }

}
