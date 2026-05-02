using Npgsql;

namespace HealthCheck.Framework.Services.Database;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<NpgsqlConnection> CreateNewPgConnection()
    {
        NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();

        return connection;
    }
}
