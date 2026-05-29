using HealthCheck.Framework.Repositories;
using HealthCheck.Framework.Services;
using HealthCheck.Worker;
using Serilog;

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
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services.AddHostedService<Worker>();

//Adiciona todas as dependências externas do Worker
builder.Services.AddFrameworkServices(builder.Configuration);
builder.Services.AddFrameworkRepositories();

//Adiciona as dependências específicas do Worker
builder.Services.AddWorkerServices();


builder.Services.AddHttpClient();
builder.Services.AddWindowsService(options =>
{
    //System Health Checker
    options.ServiceName = "SHC";
});

var host = builder.Build();

try
{
    host.Run();
}
finally
{
    Log.CloseAndFlush();
}
