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

            await CreateTestUser();
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
    public async Task CreateSystemChecksTable()
    {
        //**********************************************************************************************************************
        //ESSA TABELA É RESPONSÁVEL POR ARMAZENAR OS REGISTROS DE CHECAGENS REALIZADAS NOS SISTEMAS MONITORADOS, INCLUINDO O
        //STATUS, LATÊNCIA E DETALHES DE ERROS, SE HOUVER. ELA É FUNDAMENTAL PARA O HISTÓRICO DE MONITORAMENTO E ANÁLISE DE
        //DESEMPENHO DOS SISTEMAS.
        //**********************************************************************************************************************

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //TODO:A idéia é que haja uma rotina para que possa ser executada uma limpeza periódica dessa tabela, para evitar o acúmulo
        //excessivo de registros, já que cada checagem realizada gera um novo registro nessa tabela, e dependendo da frequência
        //das checagens, isso pode resultar em um grande volume de dados ao longo do tempo. 
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        //TODO: Definir a frequência de limpeza dos registros
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //OBS: Por enquanto, tenho em mente que para definir os dados do dashboard, iremos utilizar os registros dessa tabela de uma
        //semana e então realizar ir realizando a limpeza dos registros de mais de duas semanas, para sempre manter um histórico
        //de uma semana atrás. Muito provavelmente, será possível deixar um histórico maior, mas isso vai depender do volume de
        //dados gerados e de alguns testes de performance, para garantir que a consulta dos dados do dashboard continue rápida e
        //eficiente mesmo com um volume maior de registros.
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        try
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Criando tabela de checagens de sistemas... ");

            StringBuilder sql = new();

            sql.Append("DROP TABLE IF EXISTS system_checks CASCADE;");
            sql.Append("CREATE TABLE system_checks ( ");
            //*****************************************************************************************************************************
            //Estou usando BIGSERIAL para o campo id pois não há necessidade de usar UUID aqui, já que não há problema de qualquer
            //usuário saber a quantidade de registros de checagens realizadas, e principalmente, como será uma tabela com muitos
            //registros, o uso de BIGSERIAL é mais eficiente em termos de performance e armazenamento do que UUID, que é mais pesado
            //e complexo.
            //*****************************************************************************************************************************
            sql.Append("id BIGSERIAL PRIMARY KEY, ");
            sql.Append("user_id UUID REFERENCES users(id) ON DELETE CASCADE, ");
            sql.Append("system_id UUID REFERENCES monitored_systems(id) ON DELETE CASCADE, ");
            sql.Append("status INT NOT NULL, ");
            sql.Append("latency_ms INT NOT NULL, ");
            sql.Append("checked_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),");

            //******************************************************************************************************************
            //Campos opcionais para armazenar detalhes de erros
            //******************************************************************************************************************

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //Mensagem de erro, especificando o erro ocorrido durante a checagem do sistema.
            //Útil para diagnóstico e análise de falhas.
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            sql.Append("message TEXT DEFAULT NULL, ");

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //Tipo de exceção, armazenando o tipo específico de erro que ocorreu, como
            //"HttpRequestException" ou "TimeoutException".
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            sql.Append("exception_type VARCHAR(150) DEFAULT NULL, ");

            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //Stack trace, armazenando a pilha de chamadas no momento do erro,
            //o que pode ajudar a identificar a origem do problema.
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            sql.Append("stack_trace TEXT DEFAULT NULL ");
            sql.Append("); ");

            await connection.ExecuteAsync(sql.ToString());

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Tabela de checagens de sistemas criada com sucesso! ");
            Console.WriteLine();
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Erro ao criar tabela de 'checagem de sistemas' no sistema: {ex.Message} ");
        }
    }
    public async Task CreateTables()
    {
        await CreateUsersTable();
        await CreateMonitoredSystemTable();
        await CreateSystemChecksTable();
    }
}
