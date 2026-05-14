using System.Reflection;
using System.Text;

namespace HealthCheck.Framework.Utils;

public static class RecordLog
{
    public static void RecordExceptionLog(Exception ex)
    {
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Obtém o caminho completo do diretório de logs de exceções
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        var fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "LOGS", "Exceptions");

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Verifica a existência do diretório de logs e cria se não existir
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Gera um nome de arquivo único para o log de exceção
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        var fileName = $"ExceptionLog_{DateTime.Now:yyyy-MM-dd}.txt";
        var filePath = Path.Combine(fullPath, fileName);

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Gera a estrutura do log de exceção
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        var logBuilder = new StringBuilder();
        var exception = ex;
        var level = 0;

        logBuilder.AppendLine("");
        logBuilder.AppendLine("====================================================================");
        logBuilder.AppendLine("LOG DE EXCEÇÃO");
        logBuilder.AppendLine("====================================================================");
        logBuilder.AppendLine($"Data/hora........: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        logBuilder.AppendLine($"Máquina..........: {Environment.MachineName}");
        logBuilder.AppendLine($"Ambiente.........: {Environment.OSVersion}");
        logBuilder.AppendLine($"Assembly.........: {Assembly.GetExecutingAssembly().GetName().Name}");
        logBuilder.AppendLine($"Arquivo de Log...: {fileName}");
        logBuilder.AppendLine();

        while (exception is not null)
        {
            logBuilder.AppendLine($"------------------------- NÍVEL DE EXCEÇÃO {level} -------------------------");
            logBuilder.AppendLine($"Tipo.............: {exception.GetType().FullName}");
            logBuilder.AppendLine($"Mensagem.........: {exception.Message}");
            logBuilder.AppendLine($"Fonte............: {exception.Source}");
            logBuilder.AppendLine($"Método Alvo......: {exception.TargetSite}");
            logBuilder.AppendLine("Rastreamento......:");
            logBuilder.AppendLine(exception.StackTrace ?? "(sem rastreamento)");
            logBuilder.AppendLine();

            exception = exception.InnerException;
            level++;
        }

        logBuilder.AppendLine("====================================================================");
        logBuilder.AppendLine("FIM DO LOG DE EXCEÇÃO");
        logBuilder.AppendLine("====================================================================");
        logBuilder.AppendLine("");

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Abre o arquivo de log para escrita e grava o conteúdo do log de exceção
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        File.AppendAllText(filePath, logBuilder.ToString());
    }
}
