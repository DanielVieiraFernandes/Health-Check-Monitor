using HealthCheck.Framework.Repositories;
using HealthCheck.Framework.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HealthCheck.Worker.Tests.Services.E2ETests.Infrastructure;

public sealed class WorkerE2ETestFixture : IAsyncLifetime, IAsyncDisposable
{
    public const string SchemaName = "hc_worker_e2e";

    private ServiceProvider? _rootProvider;

    public string RunId { get; } = Guid.NewGuid().ToString("N");
    public IConfiguration Configuration { get; private set; } = default!;
    public string ConnectionString { get; private set; } = string.Empty;

    public IServiceProvider Services => _rootProvider
        ?? throw new InvalidOperationException("Fixture ainda não inicializada.");

    public async Task InitializeAsync()
    {
        Configuration = BuildConfiguration();

        var baseConnectionString =
            Configuration.GetConnectionString("HealthCheckDB")
            ?? Configuration.GetConnectionString("HealthCheckDb")
            ?? throw new InvalidOperationException("Connection string HealthCheckDb não encontrada.");

        var csb = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            SearchPath = $"{SchemaName},public"
        };

        ConnectionString = csb.ConnectionString;

        if (string.IsNullOrWhiteSpace(Configuration["SmtpHPassword"]))
            throw new InvalidOperationException("SmtpHPassword não configurado para testes E2E reais.");

        var configurationForDi = new ConfigurationBuilder()
            .AddConfiguration(Configuration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HealthCheckDB"] = ConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configurationForDi);
        services.AddLogging();
        services.AddHttpClient();

        services.AddFrameworkServices(configurationForDi);
        services.AddFrameworkRepositories();
        services.AddWorkerServices();
        services.AddSingleton<HealthCheck.Worker.Worker>();

        _rootProvider = services.BuildServiceProvider();

        await using var conn = await OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS {SchemaName};";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public IServiceScope CreateScope() => Services.CreateScope();

    public Task DisposeAsync()
    {
        _rootProvider?.Dispose();
        return Task.CompletedTask;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }

    private static IConfiguration BuildConfiguration()
    {
        var workerProjectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../HealthCheck.Worker"));

        return new ConfigurationBuilder()
            .SetBasePath(workerProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
