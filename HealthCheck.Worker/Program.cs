using HealthCheck.Framework.Repositories;
using HealthCheck.Framework.Services;
using HealthCheck.Framework.Services.Cryptography;
using HealthCheck.Worker;
using Serilog;
using System.Net;
using System.Net.Mail;
using System.Text;


//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//Tive que fazer essas alterações pois os logs não estavam sendo gravados na pasta do executável devido a restrições
//de permissão do Windows, especialmente quando o serviço é executado com privilégios limitados. Ao definir um diretório
//de logs em ProgramData, garantimos que o serviço tenha permissão para criar e gravar arquivos de log, evitando falhas
//relacionadas a permissões.
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

//=======================================================================================================================================
//Define um diretório padrão e gravável para serviços do Windows (ProgramData)
//para evitar falhas de permissão ao escrever logs na pasta do executável.
//=======================================================================================================================================
var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "HealthCheck", "Logs");

//=======================================================================================================================================
//Garante que a pasta exista antes do Serilog tentar criar o arquivo.
//=======================================================================================================================================
Directory.CreateDirectory(logDirectory);

var builder = Host.CreateApplicationBuilder(args);

//=======================================================================================================================================
//Sobrescreve o caminho do sink de arquivo definido no appsettings para usar o diretório acima.
//=======================================================================================================================================
builder.Configuration["Serilog:WriteTo:0:Args:path"] = Path.Combine(logDirectory, "worker-log-.txt");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("App", "Worker")
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services.AddHostedService<Worker>();

//=======================================================================================================================================
//ADICIONA TODAS AS DEPENDÊNCIAS EXTERNAS DO WORKER
//=======================================================================================================================================
builder.Services.AddFrameworkServices(builder.Configuration);
builder.Services.AddFrameworkRepositories();

//=======================================================================================================================================
//ADICIONA AS DEPENDÊNCIAS DO WORKER
//=======================================================================================================================================
builder.Services.AddWorkerDependencies(builder.Configuration);

builder.Services.AddHttpClient();
builder.Services.AddWindowsService(options =>
{
    //System Health Checker
    options.ServiceName = "SHC";
});

var host = builder.Build();
var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
Exception? fatalException = null;
var shutdownReason = "A aplicação foi encerrada.";

AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
{
    if (eventArgs.ExceptionObject is Exception ex)
    {
        fatalException = ex;
        shutdownReason = "A aplicação foi encerrada por exceção não tratada (AppDomain).";
        Log.Fatal(ex, "Exceção não tratada capturada no domínio da aplicação.");
        return;
    }

    shutdownReason = "A aplicação foi encerrada por falha crítica não tratada.";
    Log.Fatal("Falha crítica não tratada capturada no domínio da aplicação.");
};

TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
{
    fatalException = eventArgs.Exception;
    shutdownReason = "A aplicação registrou exceção não observada em tarefa assíncrona.";
    Log.Fatal(eventArgs.Exception, "Exceção não observada capturada pelo agendador de tarefas.");
    eventArgs.SetObserved();
};

applicationLifetime.ApplicationStopping.Register(() =>
{
    if (fatalException is null)
        shutdownReason = "A aplicação recebeu solicitação de parada.";

    Log.Warning("▶ Shutdown | Motivo={Reason}", shutdownReason);
});

try
{
    host.Run();
}
//=======================================================================================================================================
// CAPTURA QUALQUER EXCEÇÃO DURANTE A EXECUÇÃO DO SERVIÇO E REGISTRA NO LOG, GARANTINDO QUE FALHAS SEJAM DOCUMENTADAS PARA ANÁLISE FUTURA
//=======================================================================================================================================
catch (Exception ex)
{
    fatalException = ex;
    shutdownReason = "A aplicação foi encerrada por exceção durante a execução do host.";
    Log.Fatal(ex, "O serviço falhou durante a execução do host.");
}
finally
{
    TrySendShutdownEmail(builder.Configuration, shutdownReason, fatalException);
    Log.CloseAndFlush();
}

static void TrySendShutdownEmail(IConfiguration configuration, string reason, Exception? exception)
{
    var emailSettings = configuration.GetSection("EmailSettings");
    var smtpSection = emailSettings.GetSection("SMTPSettings");

    var smtpHost = smtpSection["Host"];
    var from = smtpSection["Email"];
    var to = emailSettings["ShutdownAlertEmail"];

    if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
    {
        Log.Warning("▶ Shutdown | Status=EmailNaoEnviado Reason=ConfiguracoesAusentes");
        return;
    }

    try
    {
        var smtpPort = smtpSection.GetValue("Port", 587);
        var useSsl = smtpSection.GetValue("EnableSSL", true);

        var provider = new SMTPCredentialProvider(configuration);
        var password = provider.Decrypt(configuration["SmtpHPassword"]!);

        using var message = new MailMessage(from, to)
        {
            Subject = "[HealthCheck Worker] Aplicação encerrada",
            Body = BuildShutdownEmailBody(reason, exception),
            IsBodyHtml = false
        };

        using var smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = useSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(from, password)
        };

        smtpClient.Send(message);
        Log.Information("▶ Shutdown | Status=EmailEnviado To={To}", to);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "▶ Shutdown | Status=EmailFalhou");
    }
}

static string BuildShutdownEmailBody(string reason, Exception? exception)
{
    var body = new StringBuilder()
        .AppendLine("O HealthCheck Worker foi encerrado.")
        .AppendLine()
        .AppendLine($"Data/Hora (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}")
        .AppendLine($"Máquina: {Environment.MachineName}")
        .AppendLine($"Motivo: {reason}");

    if (exception is not null)
    {
        body.AppendLine()
            .AppendLine("Detalhes da exceção:")
            .AppendLine(exception.ToString());
    }

    return body.ToString();
}
