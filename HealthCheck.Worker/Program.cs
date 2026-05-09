using HealthCheck.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services.AddHostedService<Worker>();

//Adiciona todas as dependências externas do Worker
builder.Services.AddWorkerDependencies(builder.Configuration);

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
