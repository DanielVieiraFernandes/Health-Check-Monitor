using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;
using HealthCheck.Worker.Services.SystemCheckers;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace HealthCheck.Worker.Tests.Services.SystemCheckers;

public class WebApiSystemCheckerTests
{
    [Fact]
    public async Task CheckAsync_200_Default_Healthy()
    {
        var checker = new WebApiSystemChecker(CreateFactory(HttpStatusCode.OK, "ok"), 10);
        var system = new MonitoredSystem { Url = "http://test.com" };

        var result = await checker.CheckAsync(system, CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckAsync_200_Expected201_Unhealthy()
    {
        var checker = new WebApiSystemChecker(CreateFactory(HttpStatusCode.OK, "ok"), 10);
        var system = new MonitoredSystem { Url = "http://test.com", ExpectedHttpStatus = HttpStatusCode.Created };

        var result = await checker.CheckAsync(system, CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("200", result.ErrorMessage);
        Assert.Contains("201", result.ErrorMessage);
    }

    [Fact]
    public async Task CheckAsync_Timeout_Unknown()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("timeout"));

        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient("HealthCheck")).Returns(new HttpClient(handler.Object));
        var checker = new WebApiSystemChecker(mock.Object, 1);
        var system = new MonitoredSystem { Url = "http://test.com" };

        var result = await checker.CheckAsync(system, CancellationToken.None);

        Assert.Equal(HealthStatus.Unknown, result.Status);
        Assert.Equal("TimeoutException", result.ExceptionType);
    }

    private static IHttpClientFactory CreateFactory(HttpStatusCode code, string body)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = code, Content = new StringContent(body) });
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient("HealthCheck")).Returns(new HttpClient(handler.Object));
        return mock.Object;
    }
}
