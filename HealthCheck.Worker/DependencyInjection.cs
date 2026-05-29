using HealthCheck.Worker.Services;

namespace HealthCheck.Worker;

public static class DependencyInjection
{
    public static void AddWorkerServices(this IServiceCollection services)
    {
        services.AddSingleton<MonitoringServices>();
        services.AddSingleton<NotificationService>();
    }

}
