using HealthCheck.Framework.Models;
using Npgsql;

namespace HealthCheck.Framework.Repositories.MonitoredSystemRepository;

public interface IMonitoredSystemRepository
{
    Task<MonitoredSystem> Create(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null);
    Task Update(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null);
    Task Delete(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null);
    Task<IList<MonitoredSystem>> GetAll(NpgsqlConnection? connectionAlreadyCreated = null);
    Task<MonitoredSystem?> GetById(Guid id, NpgsqlConnection? connectionAlreadyCreated = null);
}
