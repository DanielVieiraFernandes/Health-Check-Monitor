using HealthCheck.Worker.Services;
using Xunit;
using System.Reflection;

namespace HealthCheck.Tests.Worker;

public class GetExceptionNameTests
{
    /// <summary>
    /// GetExceptionName é private static no MonitoringServices.
    /// Usamos reflexão para testar.
    /// </summary>
    private static string InvokeGetExceptionName(Exception ex, CancellationToken ct)
    {
        var method = typeof(MonitoringServices)
            .GetMethod("GetExceptionName", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (string)method.Invoke(null, new object[] { ex, ct })!;
    }

    [Fact]
    public void TaskCanceledException_SemCancellationToken_DeveRetornarTimeoutException()
    {
        var ex = new TaskCanceledException("timeout");
        var ct = CancellationToken.None;

        var result = InvokeGetExceptionName(ex, ct);
        Assert.Equal("TimeoutException", result);
    }

    [Fact]
    public void TaskCanceledException_ComCancellationToken_DeveRetornarTaskCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var ex = new TaskCanceledException("cancelado", null, cts.Token);

        var result = InvokeGetExceptionName(ex, cts.Token);
        Assert.Equal("TaskCanceledException", result);
    }

    [Fact]
    public void HttpRequestException_DeveRetornarNomeDaClasse()
    {
        var ex = new HttpRequestException("Erro de rede");
        var result = InvokeGetExceptionName(ex, CancellationToken.None);
        Assert.Equal("HttpRequestException", result);
    }

    [Fact]
    public void InvalidOperationException_DeveRetornarNomeDaClasse()
    {
        var ex = new InvalidOperationException("Erro genérico");
        var result = InvokeGetExceptionName(ex, CancellationToken.None);
        Assert.Equal("InvalidOperationException", result);
    }

    [Fact]
    public void Exception_Aninhada_DeveRetornarNomeDaClasseExterna()
    {
        var inner = new ArgumentNullException("param");
        var ex = new AggregateException("múltiplos erros", inner);

        var result = InvokeGetExceptionName(ex, CancellationToken.None);
        Assert.Equal("AggregateException", result);
    }
}
