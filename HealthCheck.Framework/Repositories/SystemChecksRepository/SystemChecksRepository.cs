using Dapper;
using HealthCheck.Framework.Helpers;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database;
using Npgsql;

namespace HealthCheck.Framework.Repositories.SystemChecksRepository;

public class SystemChecksRepository(DatabaseService databaseService) : ISystemChecksRepository
{
    private const string TABLE_NAME = "system_checks";

    public async Task Create(SystemCheck systemCheck, NpgsqlConnection? connectionnAlreadyCreated = null)
    {
        var connection = connectionnAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        if (connection == null)
            throw new Exception("Falha ao criar conexão com o banco de dados.");

        string sql = QueryBuilder.BuildInsertQuery(systemCheck, systemCheck.GetIgnoreAttributes(), TABLE_NAME);

        await connection.ExecuteAsync(sql, systemCheck);

        if (connectionnAlreadyCreated == null)
            await connection.DisposeAsync();
    }

    public Task Delete(long id, NpgsqlConnection? connectionnAlreadyCreated = null)
    {
        throw new NotImplementedException();
    }

    //public async Task Delete(long id, NpgsqlConnection? connectionnAlreadyCreated = null)
    //{
    //    var connection = connectionnAlreadyCreated ?? await databaseService.CreateNewPgConnection();

    //    if (connection == null)
    //        throw new Exception("Falha ao criar conexão com o banco de dados.");

    //    string whereClause = "id = @Id";

    //    string sql = QueryBuilder.BuildDeleteQuery(TABLE_NAME, whereClause);

    //    var result = await connection.ExecuteAsync(sql, new { Id = id });

    //    if (result == 0)
    //        throw new Exception($"Nenhum registro encontrado para o id: {id}");
    //}

    public async Task<List<SystemCheck>> GetAll(Guid userId, bool last24Hours, NpgsqlConnection? connectionnAlreadyCreated = null)
    {
        var connection = connectionnAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        if (connection == null)
            throw new Exception("Falha ao criar conexão com o banco de dados.");

        string whereClause = "user_id = @UserId";

        //================================================================================================================================
        //Caso queira apenas os registros das últimas 24 horas, adiciona a condição na query limitando a quantidade
        //de registros a 1441, que é o número máximo de registros que devem ser gerados nesse período
        //================================================================================================================================
        if (last24Hours)
            whereClause += " AND checked_at BETWEEN(NOW() - INTERVAL '1 day') AND NOW() ORDER BY checked_at ASC LIMIT 1441";

        string sql = QueryBuilder.BuildSelectQuery(TABLE_NAME, whereClause);

        var result = await connection.QueryAsync<SystemCheck>(sql, new { UserId = userId });

        if (connectionnAlreadyCreated == null)
            await connection.DisposeAsync();

        if (result == null || !result.Any())
            return [];

        return [.. result];
    }

    public async Task<List<SystemCheck>> GetAllBySystemId(Guid systemId, NpgsqlConnection? connectionnAlreadyCreated = null)
    {
        var connection = connectionnAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        if (connection == null)
            throw new Exception("Falha ao criar conexão com o banco de dados.");

        string whereClause = "system_id = @SystemId";

        string sql = QueryBuilder.BuildSelectQuery(TABLE_NAME, whereClause);

        var result = await connection.QueryAsync<SystemCheck>(sql, new { SystemId = systemId });

        if (connectionnAlreadyCreated == null)
            await connection.DisposeAsync();

        if (result == null || !result.Any())
            return [];

        return [.. result];
    }
}
