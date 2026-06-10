using Microsoft.Extensions.Logging;

namespace HealthCheck.Framework.Utils;

public static class RecordLog
{
    private static ILogger _logger = null!;

    public static void Initialize(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("RecordLog");
    }

    public static void RecordExceptionLog(Exception ex)
    {
        _logger.LogError(ex, "▶ Exceção | ExceptionType={ExceptionType} Source={Source}",
            ex.GetType().FullName, ex.Source);
    }
}
