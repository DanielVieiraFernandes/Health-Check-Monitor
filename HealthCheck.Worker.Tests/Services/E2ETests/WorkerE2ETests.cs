using HealthCheck.Framework.Models;
using HealthCheck.Worker;
using HealthCheck.Worker.Tests.Services.E2ETests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace HealthCheck.Worker.Tests.Services.E2ETests;

[Collection(WorkerE2ETestCollection.Name)]
[Trait("Category", "E2E")]
public sealed class WorkerE2ETests(WorkerE2ETestFixture fixture)
{
    [Fact]
    public async Task ExecuteAsync_QuandoConfigValida_NaoDeveLancarErroCriticoEmCicloCurto()
    {
        await WorkerE2ETestDataHelper.EnsureSchemaObjectsAsync(fixture);
        await WorkerE2ETestDataHelper.ResetSchemaDataAsync(fixture);

        var userId = await WorkerE2ETestDataHelper.SeedUserAsync(fixture, fixture.RunId);

        await WorkerE2ETestDataHelper.SeedWorkerConfigAsync(fixture, new WorkerConfig
        {
            MonitoringIntervalSeconds = 1,
            TimeoutSeconds = 3,
            MaxConcurrentChecks = 2,
            MaxRetries = 1,
            DelayBetweenRetriesMs = 0,
            UserUUIDLastModified = userId
        });

        await WorkerE2ETestDataHelper.SeedMonitoredSystemsAsync(
            fixture,
            userId,
            fixture.RunId,
            2,
            i => $"https://example.com/?e2e={fixture.RunId}&i={i}");

        using var scope = fixture.CreateScope();
        var worker = scope.ServiceProvider.GetRequiredService<Worker>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        var ex = await Record.ExceptionAsync(() => InvokeExecuteAsync(worker, cts.Token));

        Assert.True(ex is OperationCanceledException or TaskCanceledException);
    }

    [Fact]
    public async Task RefreshConfigIfNeeded_QuandoWorkerConfigAusente_DeveManterConfigNula()
    {
        await WorkerE2ETestDataHelper.EnsureSchemaObjectsAsync(fixture);
        await WorkerE2ETestDataHelper.ResetSchemaDataAsync(fixture);

        using var scope = fixture.CreateScope();
        var worker = scope.ServiceProvider.GetRequiredService<Worker>();

        await InvokeRefreshConfigIfNeeded(worker, force: true, CancellationToken.None);

        var config = GetPrivateField<WorkerConfig?>(worker, "_workerConfig");

        Assert.Null(config);
    }

    private static async Task InvokeRefreshConfigIfNeeded(Worker worker, bool force, CancellationToken ct)
    {
        var method = typeof(Worker)
            .GetMethod("RefreshConfigIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Método RefreshConfigIfNeeded não encontrado.");

        var task = (Task?)method.Invoke(worker, [ct, force])
            ?? throw new InvalidOperationException("Falha ao executar RefreshConfigIfNeeded.");

        await task;
    }

    private static async Task InvokeExecuteAsync(Worker worker, CancellationToken ct)
    {
        var method = typeof(Worker)
            .GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Método ExecuteAsync não encontrado.");

        var task = (Task?)method.Invoke(worker, [ct])
            ?? throw new InvalidOperationException("Falha ao executar ExecuteAsync.");

        await task;
    }

    private static T GetPrivateField<T>(Worker worker, string fieldName)
    {
        var field = typeof(Worker).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Campo privado {fieldName} não encontrado.");

        return (T)field.GetValue(worker)!;
    }
}
