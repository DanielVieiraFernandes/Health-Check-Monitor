using Dapper;
using HealthCheck.Framework.Helpers;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Cryptography;
using Npgsql;
using System.Text;

namespace HealthCheck.DbUp.Services;

public class DatabaseService(NpgsqlConnection connection)
{
    public async Task CreateMonitoredSystemTable()
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Criando tabela de sistemas monitorados... ");

            StringBuilder sql = new();

            sql.Append("DROP TABLE IF EXISTS monitored_systems CASCADE;");
            sql.Append("CREATE TABLE monitored_systems ( ");
            sql.Append("id UUID PRIMARY KEY DEFAULT uuidv7(), ");
            sql.Append("user_id UUID REFERENCES users(id) ON DELETE CASCADE, ");
            sql.Append("name VARCHAR(255) NOT NULL, ");
            sql.Append("description TEXT NOT NULL DEFAULT '', ");
            sql.Append("url TEXT UNIQUE NOT NULL, ");
            sql.Append("last_status INT NOT NULL DEFAULT 1, ");
            sql.Append("last_checked_at TIMESTAMP WITHOUT TIME ZONE, ");
            sql.Append("history TEXT NOT NULL DEFAULT '', ");
            sql.Append("created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),");
            sql.Append("updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()");
            sql.Append("); ");

            await connection.ExecuteAsync(sql.ToString());

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
    public async Task CreateUsersTable()
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Criando tabela de usuários... ");

            StringBuilder sql = new();

            sql.Append("DROP TABLE IF EXISTS users CASCADE; ");
            sql.Append("CREATE TABLE users ( ");
            sql.Append("id UUID PRIMARY KEY DEFAULT uuidv7(), ");
            sql.Append("name VARCHAR(255) NOT NULL, ");
            sql.Append("email VARCHAR(255) UNIQUE NOT NULL, ");
            sql.Append("password VARCHAR(255) NOT NULL, ");
            sql.Append("history TEXT NOT NULL DEFAULT '', ");
            sql.Append("created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(), ");
            sql.Append("updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()");
            sql.Append("); ");

            await connection.ExecuteAsync(sql.ToString());

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Tabela de usuários criada com sucesso! ");
            Console.WriteLine();
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Erro ao criar tabela de usuários no sistema: {ex.Message} ");
        }
    }

    public async Task CreateTestUser()
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Criando usuário de teste... ");

            User user = new()
            {
                Name = "Admin",
                Email = "admin@gmail.com",
                Password = "123456",
            };

            BcryptPasswordEncrypter passwordEncrypter = new();

            user.Password = passwordEncrypter.Encrypt(user.Password);

            string sql = QueryBuilder.BuildInsertQuery(user, [nameof(User.Id)], "users");

            await connection.ExecuteAsync(sql, user);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Tabela de usuários criada com sucesso! ");
            Console.WriteLine();
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Erro ao criar tabela de usuários no sistema: {ex.Message} ");
        }
    }

    public async Task CreateTables()
    {
        await CreateUsersTable();
        await CreateMonitoredSystemTable();

        await CreateTestUser();
    }
}
