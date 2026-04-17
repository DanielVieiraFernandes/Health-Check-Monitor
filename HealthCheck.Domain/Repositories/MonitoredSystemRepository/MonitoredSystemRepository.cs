using Dapper;
using HealthCheck.Framework.Helpers;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database;
using Npgsql;

namespace HealthCheck.Framework.Repositories.MonitoredSystemRepository;

public class MonitoredSystemRepository(DatabaseService databaseService) : IMonitoredSystemRepository
{
    private const string TABLE_NAME = "monitored_systems";

    public async Task<MonitoredSystem> Create(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        List<string> ignoredAttr = [nameof(MonitoredSystem.Id)];

        string sql = QueryBuilder.BuildInsertQuery(monitoredSystem, ignoredAttr, TABLE_NAME);

        NpgsqlConnection connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        sql += " RETURNING *";

        var result = await connection.QueryFirstAsync<MonitoredSystem>(sql, monitoredSystem);

        if (result == null)
            throw new Exception("Não foi possível criar o MonitoredSystem");

        //********************************************************************************
        // Caso eu tenha criado a conexão aqui, eu fecho ela
        //********************************************************************************
        if (connectionAlreadyCreated == null)
            await connection.DisposeAsync();

        return result;
    }

    public async Task Delete(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        string whereClause = $"id = @Id";

        string sql = QueryBuilder.BuildDeleteQuery(TABLE_NAME, whereClause);

        NpgsqlConnection connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        await connection.ExecuteAsync(sql, monitoredSystem);

        //********************************************************************************
        // Caso eu tenha criado a conexão aqui, eu fecho ela
        //********************************************************************************
        if (connectionAlreadyCreated == null)
            await connection.DisposeAsync();
    }

    public async Task<IList<MonitoredSystem>> GetAll(NpgsqlConnection? connectionAlreadyCreated = null)
    {
        string sql = QueryBuilder.BuildSelectQuery(TABLE_NAME);

        NpgsqlConnection connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        var result = await connection.QueryAsync<MonitoredSystem>(sql);

        //********************************************************************************
        // Caso eu tenha criado a conexão aqui, eu fecho ela
        //********************************************************************************
        if (connectionAlreadyCreated == null)
            await connection.DisposeAsync();

        return [.. result];
    }

    public async Task<MonitoredSystem?> GetById(Guid id, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        string whereClause = $"id = @Id";

        string sql = QueryBuilder.BuildSelectQuery(TABLE_NAME, whereClause);

        NpgsqlConnection connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        var result = await connection.QueryFirstAsync<MonitoredSystem?>(sql, new { Id = id });

        //********************************************************************************
        // Caso eu tenha criado a conexão aqui, eu fecho ela
        //********************************************************************************
        if (connectionAlreadyCreated == null)
            await connection.DisposeAsync();

        return result;
    }

    public async Task Update(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        List<string> ignoredAttr = [nameof(MonitoredSystem.Id)];

        string whereClause = $"id = @Id";

        string sql = QueryBuilder.BuildUpdateQuery(monitoredSystem, ignoredAttr, TABLE_NAME, whereClause);

        NpgsqlConnection connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        await connection.ExecuteAsync(sql, monitoredSystem);

        //********************************************************************************
        // Caso eu tenha criado a conexão aqui, eu fecho ela
        //********************************************************************************
        if (connectionAlreadyCreated == null)
            await connection.DisposeAsync();
    }
}
