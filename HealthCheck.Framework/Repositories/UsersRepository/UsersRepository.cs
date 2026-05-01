using Dapper;
using HealthCheck.Framework.Helpers;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database;
using Npgsql;

namespace HealthCheck.Framework.Repositories.UsersRepository;

public class UsersRepository(DatabaseService databaseService) : IUsersRepository
{
    public const string TABLE_NAME = "users";

    public async Task<User?> Create(User user, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        var connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        if (connection == null)
            throw new Exception("Failed to create a database connection.");

        string sqlInsert = QueryBuilder.BuildInsertQuery(user, [nameof(User.Id)], TABLE_NAME);

        var result = await connection.QueryFirstOrDefaultAsync<User?>(sqlInsert, user);

        return result;
    }

    public async Task<User?> GetById(Guid id, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        var connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        if (connection == null)
            throw new Exception("Failed to create a database connection.");

        string sqlSelect = QueryBuilder.BuildSelectQuery(TABLE_NAME, $"id = @Id");

        var result = await connection.QueryFirstOrDefaultAsync<User?>(sqlSelect, new { Id = id });

        return result;
    }

    public async Task<User?> GetByEmail(string email, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        var connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        if (connection == null)
            throw new Exception("Failed to create a database connection.");

        string sqlSelect = QueryBuilder.BuildSelectQuery(TABLE_NAME, "email = @Email");

        var result = await connection.QueryFirstOrDefaultAsync<User?>(sqlSelect, new { Email = email });

        return result;
    }

    public Task Update(User user, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        throw new NotImplementedException();
    }
}
