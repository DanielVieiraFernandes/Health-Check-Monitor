using HealthCheck.Framework.Models;
using HealthCheck.Framework.Repositories.UsersRepository;
using HealthCheck.Framework.Services.Cryptography;
using HealthCheck.Framework.Services.Database.UsersService;
using HealthCheck.Framework.Services.Email;
using HealthCheck.Framework.Services.Email.Models;
using HealthCheck.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace HealthCheck.Worker.Tests.Services.UnitTests;

public class NotificationServiceTests
{
    [Fact]
    public async Task NotifyAdminAlertAsync_DeveAplicarCooldownEEnviarUmaUnicaVez()
    {
        var (sut, emailServiceMock) = BuildSut();

        await sut.NotifyAdminAlertAsync("alerta-x", "Título", "Resumo", LogLevel.Warning, null, CancellationToken.None);
        await sut.NotifyAdminAlertAsync("alerta-x", "Título", "Resumo", LogLevel.Warning, null, CancellationToken.None);

        emailServiceMock.Verify(x => x.SendSystemEmail(It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyAdminAlertAsync_ComBypassCooldown_DeveEnviarSempre()
    {
        var (sut, emailServiceMock) = BuildSut();

        await sut.NotifyAdminAlertAsync("alerta-y", "Título", "Resumo", LogLevel.Warning, null, CancellationToken.None, bypassCooldown: true);
        await sut.NotifyAdminAlertAsync("alerta-y", "Título", "Resumo", LogLevel.Warning, null, CancellationToken.None, bypassCooldown: true);

        emailServiceMock.Verify(x => x.SendSystemEmail(It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task NotifyUserAlertAsync_QuandoUsuarioExiste_DeveEnviarEmail()
    {
        var userId = Guid.NewGuid();
        var (sut, emailServiceMock) = BuildSut(userId, "cliente@healthcheck.local");

        await sut.NotifyUserAlertAsync(
            alertKey: "status-change",
            userId: userId,
            userName: "Cliente",
            title: "Mudança de status",
            summary: "Sistema ficou indisponível",
            severity: LogLevel.Error,
            ct: CancellationToken.None,
            bypassCooldown: true);

        emailServiceMock.Verify(x => x.SendSystemEmail(It.Is<EmailBody>(m => m.To == "cliente@healthcheck.local"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyUserAlertAsync_QuandoUsuarioSemEmail_NaoDeveEnviar()
    {
        var userId = Guid.NewGuid();
        var (sut, emailServiceMock) = BuildSut(userId, null);

        await sut.NotifyUserAlertAsync(
            alertKey: "status-change",
            userId: userId,
            userName: "Cliente",
            title: "Mudança de status",
            summary: "Sistema ficou indisponível",
            severity: LogLevel.Error,
            ct: CancellationToken.None,
            bypassCooldown: true);

        emailServiceMock.Verify(x => x.SendSystemEmail(It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (NotificationService Sut, Mock<EmailService> EmailMock) BuildSut(Guid? userId = null, string? userEmail = "user@healthcheck.local")
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:AlertEmail"] = "admin@healthcheck.local"
            })
            .Build());

        var usersRepositoryMock = new Mock<IUsersRepository>();
        usersRepositoryMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<NpgsqlConnection?>()))
            .ReturnsAsync((Guid id, NpgsqlConnection? _) =>
            {
                if (userId.HasValue && id == userId.Value)
                {
                    return new User
                    {
                        Id = id,
                        Name = "Cliente",
                        Email = userEmail ?? string.Empty,
                        Password = "x"
                    };
                }

                return null;
            });

        services.AddSingleton(usersRepositoryMock.Object);

        var passwordEncrypterMock = new Mock<IPasswordEncrypter>();
        passwordEncrypterMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(x => x);
        passwordEncrypterMock.Setup(x => x.Compare(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        services.AddSingleton(passwordEncrypterMock.Object);

        services.AddScoped<UsersService>();

        var smtpCredentialProviderMock = new Mock<ISMTPCredentialProvider>();
        smtpCredentialProviderMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(x => x);
        smtpCredentialProviderMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(x => x);

        var emailServiceMock = new Mock<EmailService>(
            new EmailCredentials
            {
                Host = "smtp.local",
                Port = 25,
                EnableSSL = false,
                Email = "noreply@healthcheck.local",
                Password = "123"
            },
            smtpCredentialProviderMock.Object);

        emailServiceMock.Setup(x => x.SendSystemEmail(It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<object>.AsSuccess(new { }));

        services.AddScoped(_ => emailServiceMock.Object);
        services.AddSingleton<NotificationService>();

        var provider = services.BuildServiceProvider();
        var sut = provider.GetRequiredService<NotificationService>();

        return (sut, emailServiceMock);
    }
}
