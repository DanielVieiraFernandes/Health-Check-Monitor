using System.Diagnostics;
using System.Net.Sockets;
using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;

namespace HealthCheck.Worker.Services.SystemCheckers;

public class NoSqlDatabaseSystemChecker : ISystemChecker
{
    public SystemType SupportedType => SystemType.NoSqlDatabase;

    public async Task<CheckResult> CheckAsync(MonitoredSystem system, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var parts = system.Url.Split(':');
            var host = parts[0].Trim();
            var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 6379;

            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, ct);
            sw.Stop();

            return new CheckResult
            {
                Status = HealthStatus.Healthy,
                LatencyMs = sw.ElapsedMilliseconds,
                Response = $"TCP conectado a {host}:{port}"
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
                ExceptionType = ex.GetType().Name,
                StackTrace = ex.StackTrace
            };
        }
    }
}
