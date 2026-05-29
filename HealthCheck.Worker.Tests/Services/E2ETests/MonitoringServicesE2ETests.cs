using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;
using HealthCheck.Worker.Services;
using HealthCheck.Worker.Tests.Services.E2ETests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HealthCheck.Worker.Tests.Services.E2ETests;

[Collection(WorkerE2ETestCollection.Name)]
[Trait("Category", "E2E")]
public sealed class MonitoringServicesE2ETests(WorkerE2ETestFixture fixture)
{
    [Fact]
    public async Task ExecuteMonitoring_DeveProcessar1000SistemasEmDoisCiclos()
    {
        await WorkerE2ETestDataHelper.EnsureSchemaObjectsAsync(fixture);
        await WorkerE2ETestDataHelper.ResetSchemaDataAsync(fixture);

        var userId = await WorkerE2ETestDataHelper.SeedUserAsync(fixture, fixture.RunId);

        await WorkerE2ETestDataHelper.SeedMonitoredSystemsAsync(
            fixture,
            userId,
            fixture.RunId,
            1000,
            i => $"https://example.com/?e2e={fixture.RunId}&i={i}",
            lastStatus: HealthStatus.Healthy);

        var config = new WorkerConfig
        {
            MonitoringIntervalSeconds = 3600,
            TimeoutSeconds = 10,
            MaxConcurrentChecks = 30,
            MaxRetries = 1,
            DelayBetweenRetriesMs = 0
        };

        using var scope = fixture.CreateScope();
        var monitoring = scope.ServiceProvider.GetRequiredService<MonitoringServices>();

        await monitoring.ExecuteMonitoring(CancellationToken.None, config);
        await monitoring.ExecuteMonitoring(CancellationToken.None, config);

        var checksCount = await WorkerE2ETestDataHelper.CountChecksByRunIdAsync(fixture, fixture.RunId);
        Assert.Equal(1000, checksCount);
    }

    [Fact]
    public async Task ExecuteMonitoring_QuandoEndpointRetornaErro_DevePersistirStatusUnhealthy()
    {
        await WorkerE2ETestDataHelper.EnsureSchemaObjectsAsync(fixture);
        await WorkerE2ETestDataHelper.ResetSchemaDataAsync(fixture);

        var userId = await WorkerE2ETestDataHelper.SeedUserAsync(fixture, fixture.RunId);

        await WorkerE2ETestDataHelper.SeedMonitoredSystemsAsync(
            fixture,
            userId,
            fixture.RunId,
            1,
            _ => "https://example.com/healthcheck-e2e-not-found",
            lastStatus: HealthStatus.Unhealthy);

        var config = new WorkerConfig
        {
            MonitoringIntervalSeconds = 300,
            TimeoutSeconds = 10,
            MaxConcurrentChecks = 2,
            MaxRetries = 1,
            DelayBetweenRetriesMs = 0
        };

        using var scope = fixture.CreateScope();
        var monitoring = scope.ServiceProvider.GetRequiredService<MonitoringServices>();

        await monitoring.ExecuteMonitoring(CancellationToken.None, config);

        var unhealthyCount = await WorkerE2ETestDataHelper.CountSystemsByStatusAsync(fixture, fixture.RunId, HealthStatus.Unhealthy);
        var checksCount = await WorkerE2ETestDataHelper.CountChecksByRunIdAsync(fixture, fixture.RunId);

        Assert.Equal(1, unhealthyCount);
        Assert.Equal(1, checksCount);
    }

    [Fact]
    public async Task ExecuteMonitoring_QuandoFalhaConexao_DevePersistirStatusUnknown()
    {
        await WorkerE2ETestDataHelper.EnsureSchemaObjectsAsync(fixture);
        await WorkerE2ETestDataHelper.ResetSchemaDataAsync(fixture);

        var userId = await WorkerE2ETestDataHelper.SeedUserAsync(fixture, fixture.RunId);

        await WorkerE2ETestDataHelper.SeedMonitoredSystemsAsync(
            fixture,
            userId,
            fixture.RunId,
            1,
            _ => "https://203.0.113.1",
            lastStatus: HealthStatus.Unknown);

        var config = new WorkerConfig
        {
            MonitoringIntervalSeconds = 300,
            TimeoutSeconds = 2,
            MaxConcurrentChecks = 1,
            MaxRetries = 1,
            DelayBetweenRetriesMs = 0
        };

        using var scope = fixture.CreateScope();
        var monitoring = scope.ServiceProvider.GetRequiredService<MonitoringServices>();

        await monitoring.ExecuteMonitoring(CancellationToken.None, config);

        var unknownCount = await WorkerE2ETestDataHelper.CountSystemsByStatusAsync(fixture, fixture.RunId, HealthStatus.Unknown);
        var checksCount = await WorkerE2ETestDataHelper.CountChecksByRunIdAsync(fixture, fixture.RunId);

        Assert.Equal(1, unknownCount);
        Assert.Equal(1, checksCount);

        await using var conn = await fixture.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT exception_type
FROM system_checks c
INNER JOIN monitored_systems ms ON ms.id = c.system_id
WHERE ms.name ILIKE @RunName
ORDER BY c.checked_at DESC
LIMIT 1;";
        cmd.Parameters.AddWithValue("RunName", $"%E2E-{fixture.RunId}-%");

        var exceptionType = Convert.ToString(await cmd.ExecuteScalarAsync());

        Assert.False(string.IsNullOrWhiteSpace(exceptionType));
    }
}
