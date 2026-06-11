using HealthCheck.Framework.Repositories.MonitoredSystemRepository;
using HealthCheck.Framework.Repositories.SystemChecksRepository;
using HealthCheck.Framework.Repositories.WorkerConfigRepository;
using HealthCheck.Framework.Services.Cryptography;
using HealthCheck.Framework.Services.Database;
using HealthCheck.Framework.Services.Database.MonitoredSystemService;
using HealthCheck.Framework.Services.Database.SystemChecksService;
using HealthCheck.Framework.Services.Database.UsersService;
using HealthCheck.Framework.Services.Database.WorkerConfigService;
using HealthCheck.Framework.Services.Email;
using HealthCheck.Framework.Services.Email.Models;
using HealthCheck.Worker.Services;
using HealthCheck.Worker.Services.SystemCheckers;
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
        services.AddSingleton<NotificationService>();

        services.AddSingleton<ISystemChecker>(sp =>
            new WebApiSystemChecker(sp.GetRequiredService<IHttpClientFactory>(), 10));
        services.AddSingleton<ISystemChecker>(sp =>
            new FrontendSystemChecker(sp.GetRequiredService<IHttpClientFactory>(), 10));
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
        services.AddScoped<SystemChecksService>();
        services.AddScoped<WorkerConfigService>();
        services.AddScoped<UsersService>();
        services.AddScoped<ISMTPCredentialProvider, SMTPCredentialProvider>();

        //******************************************************************************************************
        //Recupero as credenciais de email no appsettings.json
        //******************************************************************************************************
        var emailCredentials = configuration.GetSection("EmailSettings:SMTPSettings").Get<EmailCredentials>();

        //******************************************************************************************************
        //Recupero a senha das variáveis de ambiente
        //******************************************************************************************************
        var password = configuration["SmtpHPassword"];

        //******************************************************************************************************
        //Caso não seja possível recuperar as credenciais de email ou a senha, lanço uma exceção
        //******************************************************************************************************
        if (emailCredentials == null || password == null)
            throw new InvalidOperationException("Configurações de e-mail incompletas.");

        emailCredentials.Password = password;

        services.AddScoped(serviceProvider =>
        {
            var smtpCredentialProvider = serviceProvider.GetRequiredService<ISMTPCredentialProvider>();
            return new EmailService(emailCredentials, smtpCredentialProvider);
        });
    }

    private static void AddRepositories(IServiceCollection services)
    {
        //Ativa o mapeamento de nomes com underscores para propriedades em C# (ex.: "last_checked_at" -> "LastCheckedAt")
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddScoped<IMonitoredSystemRepository, MonitoredSystemRepository>();
        services.AddScoped<ISystemChecksRepository, SystemChecksRepository>();
        services.AddScoped<ISystemChecksRepository, SystemChecksRepository>();
        services.AddScoped<IWorkerConfigRepository, WorkerConfigRepository>();
    }
}
