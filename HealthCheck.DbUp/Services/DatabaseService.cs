using Dapper;
using Npgsql;
using System.Text;

namespace HealthCheck.DbUp.Services;

public class DatabaseService
{
    private NpgsqlConnection _connection { get; set; }

    public DatabaseService(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async Task CreateMonitoredSystemTable()
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Criando tabela de sistemas monitorados... ");

            StringBuilder sql = new();

            sql.Append("DROP TABLE IF EXISTS monitored_systems; ");
            sql.Append("CREATE TABLE monitored_systems ( ");
            sql.Append("id UUID PRIMARY KEY DEFAULT uuidv7(), ");
            sql.Append("name VARCHAR(255) NOT NULL, ");
            sql.Append("url TEXT NOT NULL, ");
            sql.Append("interval_in_minutes INT NOT NULL, ");
            sql.Append("last_status INT NOT NULL DEFAULT 1, ");
            sql.Append("last_checked_at TIMESTAMP WITHOUT TIME ZONE, ");
            sql.Append("created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()");
            sql.Append("); ");

            await _connection.ExecuteAsync(sql.ToString());

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Tabela de sistemas monitorados criada com sucesso! ");
            Console.WriteLine();
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Erro ao criar tabela de sistemas monitorados no sistema: {ex.Message} ");
        }
    }

    public async Task CreateTables()
    {
        await CreateMonitoredSystemTable();
    }
}
