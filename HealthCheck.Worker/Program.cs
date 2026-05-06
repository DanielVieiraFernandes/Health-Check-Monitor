using HealthCheck.Framework.Repositories.MonitoredSystemRepository;
using HealthCheck.Framework.Services.Database;
using HealthCheck.Framework.Services.Database.MonitoredSystemService;
using HealthCheck.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services.AddHostedService<Worker>();

var connectionString = builder.Configuration.GetConnectionString("HealthCheckDb");

if (string.IsNullOrEmpty(connectionString))
{
    Log.Fatal("A string de conexão para o banco de dados não foi configurada. Verifique as configurações e tente novamente.");
    return;
}

// Ativa o mapeamento de nomes com underscores para propriedades em C# (ex.: "last_checked_at" -> "LastCheckedAt")
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddScoped(config => new DatabaseService(connectionString));

builder.Services.AddScoped<MonitoredSystemService>();
builder.Services.AddScoped<IMonitoredSystemRepository, MonitoredSystemRepository>();
builder.Services.AddHttpClient();
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Servico foda!";
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
