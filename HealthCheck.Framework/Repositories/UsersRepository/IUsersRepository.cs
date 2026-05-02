using HealthCheck.Framework.Models;
using Npgsql;

namespace HealthCheck.Framework.Repositories.UsersRepository;

public interface IUsersRepository
{
    Task<User?> Create(User user, NpgsqlConnection? connectionAlreadyCreated = null);
    Task<User?> GetById(Guid id, NpgsqlConnection? connectionAlreadyCreated = null);
    Task<User?> GetByEmail(string email, NpgsqlConnection? connectionAlreadyCreated = null);
    Task Update(User user, NpgsqlConnection? connectionAlreadyCreated = null);
}
