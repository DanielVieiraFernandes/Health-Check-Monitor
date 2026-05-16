using Dapper;
using HealthCheck.Framework.Helpers;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database;
using HealthCheck.Framework.Services.Database.SystemChecksService.Filters;
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

    public async Task<List<SystemCheck>> GetAll(SearchSystemChecksFilter filters, NpgsqlConnection? connectionnAlreadyCreated = null)
    {
        var connection = connectionnAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        if (connection == null)
            throw new Exception("Falha ao criar conexão com o banco de dados.");

        DynamicParameters searchParameters = new();

        string sqlSelect = $@"SELECT checks.*, ms.name AS system_name FROM {TABLE_NAME} checks 
        INNER JOIN monitored_systems ms ON ms.id = checks.system_id WHERE checks.user_id = @UserId";

        //================================================================================================================================
        //Caso o usuário tenha inserido termo(s) de busca, adiciona na query
        //================================================================================================================================
        if (!string.IsNullOrEmpty(filters.SearchTerm))
        {
            //*********************************************************************************************************************
            //Divido o temro de busca em palavras, para permitir a busca por múltiplos termos
            //*********************************************************************************************************************
            string[] terms = filters.SearchTerm.Split(' ');
            int termCounter = 0;

            sqlSelect += " AND (";

            foreach (string term in terms)
            {
                termCounter++;

                string paramTermName = $"SearchTerm{termCounter}";
                string termValue = $"%{term}%";

                if (termCounter > 1)
                    sqlSelect += " OR ";

                sqlSelect += $@"ms.name ILIKE @{paramTermName} OR checks.system_response ILIKE @{paramTermName} 
                OR checks.error_message ILIKE @{paramTermName} OR exception_type ILIKE @{paramTermName}";

                searchParameters.Add(paramTermName, termValue);
            }

            sqlSelect += ") ";
        }


        //================================================================================================================================
        //Caso queira apenas os registros das últimas 24 horas, adiciona a condição na query limitando a quantidade
        //de registros a 1441, que é o número máximo de registros que devem ser gerados nesse período
        //================================================================================================================================
        if (filters.Last24Hours)
            sqlSelect += " AND checked_at BETWEEN(NOW() - INTERVAL '1 day') AND NOW()";

        //================================================================================================================================
        //Caso queira filtrar por um período específico, adiciona a condição na query para filtrar por esse período
        //================================================================================================================================
        if (filters.FromDate != null && filters.ToDate != null)
            sqlSelect += " AND checked_at BETWEEN @FromDate AND @ToDate";

        //================================================================================================================================
        //Caso queira filtrar por status de saúde, adiciona a condição na query para filtrar por esses status
        //================================================================================================================================
        if (filters.HealthStatusSelected != null && filters.HealthStatusSelected.Count > 0)
        {
            sqlSelect += " AND status = ANY(@HealthStatusSelectedToInt)";
            searchParameters.Add("HealthStatusSelectedToInt", filters.HealthStatusSelected.Select(e => (int)e).ToArray());
        }

        //<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>
        //O FILTRO DE LATÊNCIA É UM POUCO DIFERENTE, POIS ELE NÃO FILTRA EM SI OS REGISTROS, MAS SIM ORDENA OS REGISTROS PELA LATÊNCIA.
        //POR ISSO, ELE É TRATADO EM UM BLOCO SEPARADO DOS OUTROS FILTROS, E DEVE SEMPRE SER ADICIONADO AO FINAL DA QUERY, PARA GARANTIR
        //QUE ELE ORDENE OS REGISTROS JÁ FILTRADOS E NÃO HAJA NENHUM PROBLEMA DE SINTAXE
        //<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>

        //================================================================================================================================
        //Caso queira filtrar pela latência
        //================================================================================================================================
        if (filters.LatencyPreference != null)
        {
            //================================================================================================================================
            //Adiciona um order by para ordenar os registros pela latência
            //================================================================================================================================
            sqlSelect += " ORDER BY latency_ms ";

            //================================================================================================================================
            //Se a preferência for pela maior latência, ordena de forma decrescente
            //================================================================================================================================
            if (filters.LatencyPreference == Enums.LatencyPreference.Highest)
                sqlSelect += "DESC";
            //================================================================================================================================
            //Se a preferência for pela menor latência, ordena de forma crescente
            //================================================================================================================================
            else
                sqlSelect += "ASC";
        }
        else if (filters.Last24Hours)
            sqlSelect += " ORDER BY checked_at DESC";

        //Adiciono o resto dos parâmetros de busca, que são os mesmos para todos os filtros
        searchParameters.AddDynamicParams(filters);

        var result = await connection.QueryAsync<SystemCheck>(sqlSelect, searchParameters);

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

    public async Task<SystemCheck?> GetById(long id, NpgsqlConnection? connectionnAlreadyCreated = null)
    {
        var connection = connectionnAlreadyCreated ?? await databaseService.CreateNewPgConnection();

        if (connection == null)
            throw new Exception("Falha ao criar conexão com o banco de dados.");

        string whereClause = "id = @Id";

        string sql = QueryBuilder.BuildSelectQuery(TABLE_NAME, whereClause);

        var result = await connection.QueryFirstOrDefaultAsync<SystemCheck>(sql, new { Id = id });

        if (connectionnAlreadyCreated == null)
            await connection.DisposeAsync();

        return result;
    }
}
