using HealthCheck.Framework.Models;
using Npgsql;

namespace HealthCheck.Framework.Repositories.SystemChecksRepository;

public interface ISystemChecksRepository
{
    Task Create(SystemCheck systemCheck, NpgsqlConnection? connectionnAlreadyCreated = null);
    Task<List<SystemCheck>> GetAll(Guid userId, bool last24Hours, NpgsqlConnection? connectionnAlreadyCreated = null);
    Task<List<SystemCheck>> GetAllBySystemId(Guid systemId, NpgsqlConnection? connectionnAlreadyCreated = null);
    Task Delete(long id, NpgsqlConnection? connectionnAlreadyCreated = null);
}
