namespace HealthCheck.Worker.Tests.Services.E2ETests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class WorkerE2ETestCollection : ICollectionFixture<WorkerE2ETestFixture>
{
    public const string Name = "Worker E2E Collection";
}
