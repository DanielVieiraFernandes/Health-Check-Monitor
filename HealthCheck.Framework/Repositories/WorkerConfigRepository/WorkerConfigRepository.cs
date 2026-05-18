using Dapper;
using HealthCheck.Framework.Helpers;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database;

namespace HealthCheck.Framework.Repositories.WorkerConfigRepository;

public class WorkerConfigRepository(DatabaseService databaseService) : IWorkerConfigRepository
{
    private const string TABLE_NAME = "worker_config";

    public async Task<WorkerConfig> Get()
    {
        await using var connection = await databaseService.CreateNewPgConnection();

        string sql = $"SELECT * FROM {TABLE_NAME}";

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        //Sempre irei assumir que existe uma configuração no banco de dados, caso contrário, o sistema não tem como funcionar.
        //Caso não exista, o sistema deve lançar uma exceção, para que o time de desenvolvimento possa corrigir o problema.
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        return await connection.QueryFirstAsync<WorkerConfig>(sql);
    }

    public async Task<WorkerConfig> Update(WorkerConfig workerConfig)
    {
        await using var connection = await databaseService.CreateNewPgConnection();

        string sql = QueryBuilder.BuildUpdateQuery<WorkerConfig>(TABLE_NAME, "1=1", []);

        sql += " RETURNING *";

        return await connection.QueryFirstAsync<WorkerConfig>(sql, workerConfig);
    }
}
