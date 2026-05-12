using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.DTOS;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.Filters;
using Npgsql;

namespace HealthCheck.Framework.Repositories.MonitoredSystemRepository;

public interface IMonitoredSystemRepository
{
    Task<MonitoredSystem> Create(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null);
    Task Update(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null);
    Task UpdateStatus(UpdateMonitoredSystemStatusDTO update, NpgsqlConnection? connectionAlreadyCreated = null);
    Task Delete(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null);
    Task<IList<MonitoredSystem>> GetAll(SearchFiltersMonitoredSystems? searchFiltersMonitoredSystems = null,
                                        NpgsqlConnection? connectionAlreadyCreated = null);
    Task<MonitoredSystem?> GetById(Guid id, NpgsqlConnection? connectionAlreadyCreated = null);
    Task<MonitoredSystem?> GetByUrl(string url, NpgsqlConnection? connectionAlreadyCreated = null);
    Task<List<MonitoredSystem>> GetPending(NpgsqlConnection? connectionAlreadyCreated = null);
}
