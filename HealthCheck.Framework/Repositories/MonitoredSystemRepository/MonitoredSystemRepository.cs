using Dapper;
using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Helpers;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.Filters;
using Npgsql;

namespace HealthCheck.Framework.Repositories.MonitoredSystemRepository;

public class MonitoredSystemRepository(DatabaseService databaseService) : IMonitoredSystemRepository
{
    private const string TABLE_NAME = "monitored_systems";

    public async Task<MonitoredSystem> Create(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        List<string> ignoredAttr = [nameof(MonitoredSystem.Id)];

        string sql = QueryBuilder.BuildInsertQuery(monitoredSystem, ignoredAttr, TABLE_NAME, true);

        NpgsqlConnection connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

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

    public async Task<IList<MonitoredSystem>> GetAll(SearchFiltersMonitoredSystems? searchFiltersMonitoredSystems = null, Guid? userId = null, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        string sql = QueryBuilder.BuildSelectQuery(TABLE_NAME);

        DynamicParameters? parameters = null;

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //TODO: Adicionar filtros para tornar a busca mais flexível.
        // Por exemplo, filtros para buscar apenas os sistemas monitorados que estão com status de saúde "Ruim" ou "Crítico",
        // ou filtros para buscar os sistemas monitorados que estão com a última verificação feita há mais de X horas, etc.
        // Esses filtros serão passados através do objeto SearchFiltersMonitoredSystems, que será recebido como parâmetro nesse método.
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //********************************************************************************
        // Caso o usuário tenha informado algum filtro de busca ou o sistema esteja
        // buscando os sistemas de um usuário específico, aplico os filtros na query
        //********************************************************************************
        if (searchFiltersMonitoredSystems != null || userId is not null)
        {
            parameters = new();

            sql += " WHERE 1=1 ";

            //********************************************************************************
            // Se especifiquei um userId, busco apenas os sistemas monitorados daquele usuário
            //********************************************************************************
            if (userId != null)
            {
                sql += " AND user_id = @UserId";
                parameters.Add("UserId", userId);
            }

            if (searchFiltersMonitoredSystems != null)
            {
                //********************************************************************************
                // Se o usuário informou um termo de busca
                //********************************************************************************
                if (!string.IsNullOrEmpty(searchFiltersMonitoredSystems.SearchTerm))
                {
                    //*************************************************************************************************************
                    // Crio uma lista de termos, separando o termo de busca por espaço. Assim, se o usuário buscar por
                    // "Sistema de Pagamento", eu vou buscar por "Sistema", "de" e "Pagamento"
                    //*************************************************************************************************************
                    var terms = searchFiltersMonitoredSystems.SearchTerm.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                    // Índice para criar parâmetros únicos para cada termo
                    int tIndex = 0;

                    foreach (var item in terms)
                    {
                        //*******************************************************************************************************************************
                        // Cada iteração, eu adiciono uma condição na query para buscar o termo no nome, url ou descrição do sistema monitorado
                        // e adiciono um parâmetro para o termo, usando o índice para garantir que cada parâmetro seja único
                        //*******************************************************************************************************************************
                        sql += $" AND (name ILIKE @Term{tIndex} OR url ILIKE @Term{tIndex} OR description ILIKE @Term{tIndex})";
                        parameters.Add($"Term{tIndex}", $"%{item}%");
                        tIndex++;
                    }
                }
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------
        // Por enquanto, deixarei fixo a ordenação pela data de última verificação, em ordem decrescente.
        // No entanto, futuramente, irei adicionar um filtro para o usuário escolher a ordenação que preferir
        //------------------------------------------------------------------------------------------------------------------------------------------
        sql += " ORDER BY last_checked_at DESC";

        NpgsqlConnection connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        var result = await connection.QueryAsync<MonitoredSystem>(sql, parameters);

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

    public async Task<MonitoredSystem?> GetByUrl(string url, NpgsqlConnection? connectionAlreadyCreated = null)
    {
        string whereClause = $"url = @Url";

        string sql = QueryBuilder.BuildSelectQuery(TABLE_NAME, whereClause);

        NpgsqlConnection connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        var result = await connection.QueryFirstOrDefaultAsync<MonitoredSystem?>(sql, new { Url = url });

        //********************************************************************************
        // Caso eu tenha criado a conexão aqui, eu fecho ela
        //********************************************************************************
        if (connectionAlreadyCreated == null)
            await connection.DisposeAsync();

        return result;
    }

    /// <summary>
    /// Recupera os sistemas monitorados que estão pendentes de verificação, ou seja, aqueles que ainda não foram 
    /// verificados ou que estão com a última verificação feita há mais de X horas (dependendo da frequência 
    /// de monitoramento configurada para cada sistema monitorado). <br/>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<List<MonitoredSystem>> GetPending(NpgsqlConnection? connectionAlreadyCreated = null)
    {
        //********************************************************************************************
        //SE O SISTEMA NUNCA FOI CHECADO, ENTÃO, A URL SERÁ RETORNADA PARA CHECAGEM.
        //SE O SISTEMA FOI CHECADO, MAS O INTERVALO DESDE A ÚLTIMA CHECAGEM FOR MAIOR OU
        //IGUAL AO CONFIGURADO,ENTÃO, A URL SERÁ RETORNADA PARA CHECAGEM.
        //********************************************************************************************
        string sql = $@"SELECT * FROM {TABLE_NAME} 
                    WHERE last_status = {(int)HealthStatus.Unknown} OR (last_checked_at IS NULL 
                       OR last_checked_at <= (NOW() - (INTERVAL '1 minute' * interval_in_minutes)))";

        NpgsqlConnection connection = connectionAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        var result = await connection.QueryAsync<MonitoredSystem>(sql);

        if (result == null)
            return [];

        //********************************************************************************
        // Caso eu tenha criado a conexão aqui, eu fecho ela
        //********************************************************************************
        if (connectionAlreadyCreated == null)
            await connection.DisposeAsync();

        return [.. result];
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
