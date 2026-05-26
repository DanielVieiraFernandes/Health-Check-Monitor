using HealthCheck.DbUp.Services;
using Npgsql;

NpgsqlConnection connection = new("Host=127.0.0.1;Port=5432;Database=healthcheck;Username=postgres;Password=c6628901d4");

await connection.OpenAsync();

DatabaseService databaseService = new(connection);
await databaseService.InitDatabase();