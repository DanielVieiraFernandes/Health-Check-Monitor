using HealthCheck.Framework.Services.Email;
using HealthCheck.Worker.Services;
using HealthCheck.Worker.Tests.Services.E2ETests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HealthCheck.Worker.Tests.Services.E2ETests;

[Collection(WorkerE2ETestCollection.Name)]
[Trait("Category", "E2E")]
public sealed class NotificationServiceE2ETests(WorkerE2ETestFixture fixture)
{
    [Fact]
    public async Task NotifyAdminAlertAsync_DeveEnviarEmailRealComSmtp()
    {
        await WorkerE2ETestDataHelper.EnsureSchemaObjectsAsync(fixture);

        using var scope = fixture.CreateScope();
        var notification = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var ex = await Record.ExceptionAsync(() =>
            notification.NotifyAdminAlertAsync(
                alertKey: $"e2e-admin-{fixture.RunId}-{Guid.NewGuid():N}",
                title: "[E2E] Alerta administrativo",
                summary: "Disparo E2E do NotificationService com SMTP real.",
                severity: LogLevel.Warning,
                exception: null,
                ct: CancellationToken.None,
                bypassCooldown: true));

        Assert.Null(ex);
    }

    [Fact]
    public async Task NotifyUserAlertAsync_ComUsuarioReal_DeveEnviarEmailRealComSmtp()
    {
        await WorkerE2ETestDataHelper.EnsureSchemaObjectsAsync(fixture);
        await WorkerE2ETestDataHelper.ResetSchemaDataAsync(fixture);

        var alertEmail = fixture.Configuration["EmailSettings:AlertEmail"]
            ?? throw new InvalidOperationException("AlertEmail não configurado para E2E.");

        var userId = await WorkerE2ETestDataHelper.SeedUserAsync(fixture, fixture.RunId, alertEmail);

        using var scope = fixture.CreateScope();
        var notification = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var ex = await Record.ExceptionAsync(() =>
            notification.NotifyUserAlertAsync(
                alertKey: $"e2e-user-{fixture.RunId}-{Guid.NewGuid():N}",
                userId: userId,
                userName: "Usuário E2E",
                title: "[E2E] Alerta usuário",
                summary: "Disparo E2E de alerta para usuário com SMTP real.",
                severity: LogLevel.Error,
                ct: CancellationToken.None,
                bypassCooldown: true));

        Assert.Null(ex);
    }

    [Fact]
    public async Task EmailService_SendSystemEmail_DeveRetornarSucessoComSmtpReal()
    {
        await WorkerE2ETestDataHelper.EnsureSchemaObjectsAsync(fixture);

        var alertEmail = fixture.Configuration["EmailSettings:AlertEmail"]
            ?? throw new InvalidOperationException("AlertEmail não configurado para E2E.");

        using var scope = fixture.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

        var result = await emailService.SendSystemEmail(
            new HealthCheck.Framework.Services.Email.Models.EmailBody
            {
                To = alertEmail,
                Name = "Admin E2E",
                Subject = "[E2E] Teste de envio SMTP real",
                Body = "Teste E2E real do EmailService.",
                IsHtml = false
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
