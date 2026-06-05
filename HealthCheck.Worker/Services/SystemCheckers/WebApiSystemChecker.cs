using System.Diagnostics;
using System.Net;
using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;

namespace HealthCheck.Worker.Services.SystemCheckers;

public class WebApiSystemChecker(IHttpClientFactory httpClientFactory, int timeoutSeconds) : ISystemChecker
{
    public SystemType SupportedType => SystemType.WebApi;

    public async Task<CheckResult> CheckAsync(MonitoredSystem system, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = httpClientFactory.CreateClient("HealthCheck");
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            using var response = await client.GetAsync(system.Url, ct);
            sw.Stop();

            var expectedCode = system.ExpectedHttpStatus ?? 200;
            var body = await response.Content.ReadAsStringAsync(ct);
            var isHealthy = (int)response.StatusCode == expectedCode;

            return new CheckResult
            {
                Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                LatencyMs = sw.ElapsedMilliseconds,
                Response = Truncate(body, 500),
                ErrorMessage = isHealthy ? null
                    : $"HTTP {(int)response.StatusCode} — esperado: {expectedCode}"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckResult
            {
                Status = HealthStatus.Unknown,
                LatencyMs = sw.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                ExceptionType = GetExceptionName(ex),
                StackTrace = ex.StackTrace
            };
        }
    }

    private static string Truncate(string v, int m) => v.Length <= m ? v : v[..m];
    private static string GetExceptionName(Exception ex) =>
        ex is TaskCanceledException ? nameof(TimeoutException) : ex.GetType().Name;
}
