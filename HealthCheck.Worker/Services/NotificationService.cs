using HealthCheck.Framework.Services.Database.UsersService;
using HealthCheck.Framework.Services.Email;
using HealthCheck.Framework.Services.Email.Models;
using System.Collections.Concurrent;
using System.Reflection;

namespace HealthCheck.Worker.Services;

public class NotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _alertEmail;
    private readonly string _projectName;
    private readonly ConcurrentDictionary<string, DateTime> _lastAlertEmailByKey = new();
    private static readonly TimeSpan _alertEmailCooldown = TimeSpan.FromMinutes(5);

    public NotificationService(
        ILogger<NotificationService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        //*******************************************************************************************************
        //TENTA RECUPERAR O EMAIL DE ALERTA DO appsettings.json
        //*******************************************************************************************************
        var emailFromConfig = configuration.GetValue<string?>("EmailSettings:AlertEmail");

        //*******************************************************************************************************
        //CASO O EMAIL DE ALERTA NÃO ESTEJA CONFIGURADO, LANÇA UMA EXCEÇÃO PARA
        //INDICAR QUE HÁ UM PROBLEMA NA CONFIGURAÇÃO
        //*******************************************************************************************************
        _alertEmail = emailFromConfig ?? throw new InvalidOperationException("Alert email is not configured.");

        //*******************************************************************************************************
        //RECUPERA O NOME DO PROJETO PARA USAR NO ASSUNTO DO EMAIL
        //*******************************************************************************************************
        _projectName = Assembly.GetExecutingAssembly().GetName().Name ?? "Worker";
    }

    public async Task NotifyAdminAlertAsync(
        string alertKey,
        string title,
        string summary,
        LogLevel severity,
        Exception? exception,
        CancellationToken ct,
        bool bypassCooldown = false)
    {
        await SendAlertEmailAsync(
            alertKey: alertKey,
            to: _alertEmail,
            name: "Responsável pela aplicação",
            subject: $"[{_projectName}] {title}",
            summary: summary,
            severity: severity,
            exception: exception,
            ct: ct,
            bypassCooldown: bypassCooldown);
    }

    public async Task NotifyUserAlertAsync(
        string alertKey,
        Guid userId,
        string userName,
        string title,
        string summary,
        LogLevel severity,
        CancellationToken ct,
        bool bypassCooldown = false)
    {
        var userEmail = await ResolveUserEmailAsync(userId, ct);

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            _logger.LogWarning("Não foi possível identificar o e-mail do usuário {UserId} para envio de alerta.", userId);
            return;
        }

        await SendAlertEmailAsync(
            alertKey: alertKey,
            to: userEmail,
            name: userName,
            subject: $"[HC - Sistema de Monitoramento] {title}",
            summary: summary,
            severity: severity,
            exception: null,
            ct: ct,
            bypassCooldown: bypassCooldown);
    }

    private async Task SendAlertEmailAsync(
        string alertKey,
        string to,
        string name,
        string subject,
        string summary,
        LogLevel severity,
        Exception? exception,
        CancellationToken ct,
        bool bypassCooldown)
    {
        if (!bypassCooldown &&
            _lastAlertEmailByKey.TryGetValue(alertKey, out var lastSentAt) &&
            DateTime.UtcNow - lastSentAt < _alertEmailCooldown)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            var safeExceptionSummary = exception is null
                ? "Sem detalhes de exceção para este alerta."
                : $"{exception.GetType().Name}: {exception.Message}";

            var emailBody = new EmailBody
            {
                To = to,
                Name = name,
                Subject = subject,
                IsHtml = false,
                Body =
$"Resumo do aviso:\n{summary}\n\n" +
$"Nível: {severity}\n" +
$"Data/Hora (UTC): {DateTime.UtcNow:dd-MM-yy HH:mm:ss}\n" +
$"Máquina: {Environment.MachineName}\n" +
$"Aplicação: {_projectName}\n\n" +
$"Detalhe técnico resumido:\n{safeExceptionSummary}"
            };

            var result = await emailService.SendSystemEmail(emailBody, ct);

            if (result.IsFailure)
            {
                _logger.LogWarning("Falha ao enviar alerta por e-mail. Erros: {Errors}", string.Join(" | ", result.Failure!.Errors.Select(e => e.ErrorMessage)));
                return;
            }

            _lastAlertEmailByKey[alertKey] = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha inesperada ao montar/enviar alerta por e-mail.");
        }
    }

    private async Task<string?> ResolveUserEmailAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var usersService = scope.ServiceProvider.GetRequiredService<UsersService>();
            var userResult = await usersService.GetUserById(userId);

            if (userResult.IsSuccess && !string.IsNullOrWhiteSpace(userResult.Success?.Email))
                return userResult.Success.Email;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao recuperar e-mail do usuário {UserId} para envio de alerta.", userId);
        }

        return null;
    }
}
