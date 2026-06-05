using System.Diagnostics;
using System.Net;
using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;

namespace HealthCheck.Worker.Services.SystemCheckers;

public class FrontendSystemChecker(IHttpClientFactory httpClientFactory, int timeoutSeconds) : ISystemChecker
{
    public SystemType SupportedType => SystemType.Frontend;

    public async Task<CheckResult> CheckAsync(MonitoredSystem system, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = httpClientFactory.CreateClient("HealthCheck");
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            using var response = await client.GetAsync(system.Url, ct);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync(ct);
            var expectedCode = system.ExpectedHttpStatus ?? 200;
            var codeOk = (int)response.StatusCode == expectedCode;

            var expectedText = system.ExpectedBodyText;
            var textOk = string.IsNullOrWhiteSpace(expectedText)
                || body.Contains(expectedText, StringComparison.OrdinalIgnoreCase);

            var isHealthy = codeOk && textOk;
            string? errorMessage = null;
            if (!codeOk)
                errorMessage = $"HTTP {(int)response.StatusCode} — esperado: {expectedCode}";
            else if (!textOk)
                errorMessage = $"HTTP {expectedCode} OK — texto \"{expectedText}\" não encontrado ({body.Length} bytes)";

            return new CheckResult
            {
                Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                LatencyMs = sw.ElapsedMilliseconds,
                Response = Truncate(body, 500),
                ErrorMessage = errorMessage
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
