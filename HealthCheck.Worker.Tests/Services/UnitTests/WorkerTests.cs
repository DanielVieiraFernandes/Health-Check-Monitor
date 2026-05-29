using HealthCheck.Framework.Models;
using HealthCheck.Framework.Repositories.MonitoredSystemRepository;
using HealthCheck.Framework.Repositories.SystemChecksRepository;
using HealthCheck.Framework.Repositories.UsersRepository;
using HealthCheck.Framework.Repositories.WorkerConfigRepository;
using HealthCheck.Framework.Services.Cryptography;
using HealthCheck.Framework.Services.Database.MonitoredSystemService;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.DTOS;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.Filters;
using HealthCheck.Framework.Services.Database.SystemChecksService;
using HealthCheck.Framework.Services.Database.SystemChecksService.Filters;
using HealthCheck.Framework.Services.Database.UsersService;
using HealthCheck.Framework.Services.Database.WorkerConfigService;
using HealthCheck.Framework.Services.Email;
using HealthCheck.Framework.Services.Email.Models;
using HealthCheck.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using System.Reflection;

namespace HealthCheck.Worker.Tests.Services.UnitTests;

public class WorkerTests
{
    [Fact]
    public async Task RefreshConfigIfNeeded_QuandoRepositorioFalha_DeveUsarFallbackDoAppsettings()
    {
        var worker = BuildWorker(throwOnConfigGet: true, includeFallbackSettings: true);

        await InvokeRefreshConfigIfNeeded(worker, force: true, CancellationToken.None);

        var config = GetPrivateField<WorkerConfig?>(worker, "_workerConfig");

        Assert.NotNull(config);
        Assert.Equal(1, config!.MonitoringIntervalSeconds);
        Assert.Equal(2, config.MaxConcurrentChecks);
    }

    [Fact]
    public async Task RefreshConfigIfNeeded_QuandoRepositorioFalhaESemFallback_DeveManterConfigNula()
    {
        var worker = BuildWorker(throwOnConfigGet: true, includeFallbackSettings: false);

        await InvokeRefreshConfigIfNeeded(worker, force: true, CancellationToken.None);

        var config = GetPrivateField<WorkerConfig?>(worker, "_workerConfig");

        Assert.Null(config);
    }

    [Fact]
    public async Task ExecuteAsync_QuandoRefreshConfigLockMaiorQueDois_DeveLancarInvalidOperationException()
    {
        var worker = BuildWorker(throwOnConfigGet: false, includeFallbackSettings: true);

        SetPrivateField(worker, "_refreshConfigLock", (byte)3);

        var ex = await Record.ExceptionAsync(() => InvokeExecuteAsync(worker, CancellationToken.None));

        Assert.NotNull(ex);
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("Falha ao obter a configuração do worker", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_EmCicloNormalComCancelamento_NaoDeveLancarErroCritico()
    {
        var worker = BuildWorker(throwOnConfigGet: false, includeFallbackSettings: true);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var ex = await Record.ExceptionAsync(() => InvokeExecuteAsync(worker, cts.Token));

        Assert.True(ex is OperationCanceledException or TaskCanceledException);
    }

    private static Worker BuildWorker(bool throwOnConfigGet, bool includeFallbackSettings)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        var configData = new Dictionary<string, string?>
        {
            ["EmailSettings:AlertEmail"] = "admin@healthcheck.local"
        };

        if (includeFallbackSettings)
        {
            configData["Settings:MonitoringIntervalSeconds"] = "1";
            configData["Settings:TimeoutSeconds"] = "1";
            configData["Settings:MaxConcurrentChecks"] = "2";
            configData["Settings:MaxRetries"] = "1";
            configData["Settings:DelayBetweenRetriesMs"] = "0";
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddSingleton<IWorkerConfigRepository>(new InMemoryWorkerConfigRepository(throwOnConfigGet));
        services.AddScoped<WorkerConfigService>();

        services.AddSingleton<IMonitoredSystemRepository>(new InMemoryMonitoredSystemRepository([]));
        services.AddSingleton<ISystemChecksRepository>(new InMemorySystemChecksRepository());
        services.AddScoped<MonitoredSystemService>();
        services.AddScoped<SystemChecksService>();

        services.AddSingleton<IUsersRepository>(new InMemoryUsersRepository());

        var passwordEncrypter = new Mock<IPasswordEncrypter>();
        passwordEncrypter.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(x => x);
        passwordEncrypter.Setup(x => x.Compare(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        services.AddSingleton(passwordEncrypter.Object);
        services.AddScoped<UsersService>();

        var smtpCredentials = new Mock<ISMTPCredentialProvider>();
        smtpCredentials.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(x => x);
        smtpCredentials.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(x => x);

        var emailService = new Mock<EmailService>(
            new EmailCredentials
            {
                Host = "smtp.local",
                Port = 25,
                EnableSSL = false,
                Email = "noreply@healthcheck.local",
                Password = "123"
            },
            smtpCredentials.Object);

        emailService.Setup(x => x.SendSystemEmail(It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<object>.AsSuccess(new { }));

        services.AddScoped(_ => emailService.Object);

        services.AddSingleton<NotificationService>();

        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(new SuccessHttpHandler()));
        services.AddSingleton(httpFactoryMock.Object);

        services.AddSingleton<MonitoringServices>();
        services.AddSingleton<Worker>();

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<Worker>();
    }

    private static async Task InvokeRefreshConfigIfNeeded(Worker worker, bool force, CancellationToken ct)
    {
        var method = typeof(Worker)
            .GetMethod("RefreshConfigIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var task = (Task?)method!.Invoke(worker, [ct, force]);
        Assert.NotNull(task);

        await task!;
    }

    private static async Task InvokeExecuteAsync(Worker worker, CancellationToken ct)
    {
        var method = typeof(Worker)
            .GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var task = (Task?)method!.Invoke(worker, [ct]);
        Assert.NotNull(task);

        await task!;
    }

    private static T GetPrivateField<T>(Worker worker, string fieldName)
    {
        var field = typeof(Worker).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        return (T)field!.GetValue(worker)!;
    }

    private static void SetPrivateField(Worker worker, string fieldName, object value)
    {
        var field = typeof(Worker).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        field!.SetValue(worker, value);
    }

    private sealed class SuccessHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }

    private sealed class InMemoryWorkerConfigRepository(bool throwOnGet) : IWorkerConfigRepository
    {
        public Task<WorkerConfig> Get()
        {
            if (throwOnGet)
                throw new InvalidOperationException("falha simulada ao recuperar config");

            return Task.FromResult(new WorkerConfig
            {
                MonitoringIntervalSeconds = 1,
                TimeoutSeconds = 1,
                MaxConcurrentChecks = 2,
                MaxRetries = 1,
                DelayBetweenRetriesMs = 0
            });
        }

        public Task<WorkerConfig> Update(WorkerConfig workerConfig) => Task.FromResult(workerConfig);
    }

    private sealed class InMemoryMonitoredSystemRepository(List<MonitoredSystem> initial) : IMonitoredSystemRepository
    {
        private readonly List<MonitoredSystem> _systems = initial;

        public Task<MonitoredSystem> Create(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.FromResult(monitoredSystem);

        public Task Update(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.CompletedTask;

        public Task UpdateStatus(UpdateMonitoredSystemStatusDTO update, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.CompletedTask;

        public Task Delete(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.CompletedTask;

        public Task<IList<MonitoredSystem>> GetAll(SearchFiltersMonitoredSystems? searchFiltersMonitoredSystems = null, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.FromResult((IList<MonitoredSystem>)_systems);

        public Task<MonitoredSystem?> GetById(Guid id, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.FromResult(_systems.FirstOrDefault(x => x.Id == id));

        public Task<MonitoredSystem?> GetByUrl(string url, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.FromResult(_systems.FirstOrDefault(x => x.Url == url));

        public Task<List<MonitoredSystem>> GetPending(NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.FromResult(_systems);
    }

    private sealed class InMemorySystemChecksRepository : ISystemChecksRepository
    {
        public Task Create(SystemCheck systemCheck, NpgsqlConnection? connectionnAlreadyCreated = null) => Task.CompletedTask;
        public Task<List<SystemCheck>> GetAll(SearchSystemChecksFilter filters, NpgsqlConnection? connectionnAlreadyCreated = null) => Task.FromResult(new List<SystemCheck>());
        public Task<SystemCheck?> GetById(long id, NpgsqlConnection? connectionnAlreadyCreated = null) => Task.FromResult<SystemCheck?>(null);
        public Task<List<SystemCheck>> GetAllBySystemId(Guid systemId, NpgsqlConnection? connectionnAlreadyCreated = null) => Task.FromResult(new List<SystemCheck>());
        public Task<SystemCheck?> GetLastBySystemId(Guid systemId, NpgsqlConnection? connectionnAlreadyCreated = null) => Task.FromResult<SystemCheck?>(null);
        public Task Clean(NpgsqlConnection? connectionnAlreadyCreated = null) => Task.CompletedTask;
        public Task Delete(long id, NpgsqlConnection? connectionnAlreadyCreated = null) => Task.CompletedTask;
    }

    private sealed class InMemoryUsersRepository : IUsersRepository
    {
        public Task<User?> Create(User user, NpgsqlConnection? connectionAlreadyCreated = null) => Task.FromResult<User?>(user);
        public Task<User?> GetById(Guid id, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.FromResult<User?>(new User { Id = id, Name = "Usuário", Email = "user@healthcheck.local", Password = "x" });
        public Task<User?> GetByEmail(string email, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.FromResult<User?>(new User { Id = Guid.NewGuid(), Name = "Usuário", Email = email, Password = "x" });
        public Task Update(User user, NpgsqlConnection? connectionAlreadyCreated = null) => Task.CompletedTask;
    }
}
