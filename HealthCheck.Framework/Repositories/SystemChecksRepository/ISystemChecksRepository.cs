using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database.SystemChecksService.Filters;
using Npgsql;

namespace HealthCheck.Framework.Repositories.SystemChecksRepository;

public interface ISystemChecksRepository
{
    Task Create(SystemCheck systemCheck, NpgsqlConnection? connectionnAlreadyCreated = null);
    Task<List<SystemCheck>> GetAll(SearchSystemChecksFilter filters, NpgsqlConnection? connectionnAlreadyCreated = null);
    Task<SystemCheck?> GetById(long id, NpgsqlConnection? connectionnAlreadyCreated = null);
    Task<List<SystemCheck>> GetAllBySystemId(Guid systemId, NpgsqlConnection? connectionnAlreadyCreated = null);
    Task Delete(long id, NpgsqlConnection? connectionnAlreadyCreated = null);
}
