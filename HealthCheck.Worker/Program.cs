using HealthCheck.Framework.Repositories;
using HealthCheck.Framework.Services;
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

    Log.Warning("Processo de encerramento do Worker iniciado.");
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
    Log.Fatal(ex, "O serviço falhou ao iniciar.");
}
finally
{
    TrySendShutdownEmail(builder.Configuration, shutdownReason, fatalException);
    Log.CloseAndFlush();
}

static void TrySendShutdownEmail(IConfiguration configuration, string reason, Exception? exception)
{
    var section = configuration.GetSection("ShutdownEmailNotification");

    if (!section.GetValue<bool>("Enabled"))
        return;

    var smtpHost = section["SmtpHost"];
    var from = section["From"];
    var to = section["To"];

    if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
    {
        Log.Warning("Notificação por e-mail de encerramento não enviada. Configurações obrigatórias ausentes.");
        return;
    }

    try
    {
        var smtpPort = section.GetValue("SmtpPort", 587);
        var useSsl = section.GetValue("UseSsl", true);
        var userName = section["UserName"];
        var password = section["Password"];

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
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
            smtpClient.Credentials = new NetworkCredential(userName, password);

        smtpClient.Send(message);
        Log.Information("E-mail de encerramento da aplicação enviado para {To}.", to);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Falha ao enviar e-mail de encerramento da aplicação.");
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
