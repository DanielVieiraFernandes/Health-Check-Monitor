using HealthCheck.Framework.Models;
using Xunit;

namespace HealthCheck.Tests.Models;

public class WorkerConfigTests
{
    [Fact]
    public void ValoresPadrao_DevemSerAplicadosNaCriacao()
    {
        var config = new WorkerConfig();

        Assert.Equal(30, config.MonitoringIntervalSeconds);
        Assert.Equal(10, config.TimeoutSeconds);
        Assert.Equal(10, config.MaxConcurrentChecks);
        Assert.Equal(0, config.MaxRetries);
        Assert.Equal(0, config.DelayBetweenRetriesMs);
    }

    [Fact]
    public void Propriedades_DevemSerEditaveis()
    {
        var config = new WorkerConfig
        {
            MonitoringIntervalSeconds = 60,
            TimeoutSeconds = 15,
            MaxConcurrentChecks = 5,
            MaxRetries = 3,
            DelayBetweenRetriesMs = 500,
            UserUUIDLastModified = Guid.NewGuid()
        };

        Assert.Equal(60, config.MonitoringIntervalSeconds);
        Assert.Equal(15, config.TimeoutSeconds);
        Assert.Equal(5, config.MaxConcurrentChecks);
        Assert.Equal(3, config.MaxRetries);
        Assert.Equal(500, config.DelayBetweenRetriesMs);
        Assert.NotEqual(Guid.Empty, config.UserUUIDLastModified);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void MonitoringIntervalSeconds_NegativoOuZero_DeveSerAceitoPeloModelo(int valor)
    {
        // O modelo aceita qualquer int — a validação é feita pelo FluentValidation
        var config = new WorkerConfig { MonitoringIntervalSeconds = valor };
        Assert.Equal(valor, config.MonitoringIntervalSeconds);
    }

    [Fact]
    public void UpdatedAt_DeveSerDataAtualNaCriacao()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var config = new WorkerConfig();
        var after = DateTime.Now.AddSeconds(1);

        Assert.True(config.UpdatedAt >= before && config.UpdatedAt <= after);
    }
}
