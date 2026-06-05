using System.Diagnostics;
using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;
using Npgsql;

namespace HealthCheck.Worker.Services.SystemCheckers;

public class SqlDatabaseSystemChecker : ISystemChecker
{
    public SystemType SupportedType => SystemType.SqlDatabase;

    public async Task<CheckResult> CheckAsync(MonitoredSystem system, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new NpgsqlConnection(system.Url);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT 1 AS ok", conn);
            var result = await cmd.ExecuteScalarAsync(ct);
            sw.Stop();

            return new CheckResult
            {
                Status = HealthStatus.Healthy,
                LatencyMs = sw.ElapsedMilliseconds,
                Response = $"Conectado — SELECT 1 = {result}"
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
                ExceptionType = ex is PostgresException pe ? pe.SqlState : ex.GetType().Name,
                StackTrace = ex.StackTrace
            };
        }
    }
}
