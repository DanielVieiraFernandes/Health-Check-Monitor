using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Repositories.MonitoredSystemRepository;
using HealthCheck.Framework.Repositories.SystemChecksRepository;
using HealthCheck.Framework.Repositories.UsersRepository;
using HealthCheck.Framework.Services.Cryptography;
using HealthCheck.Framework.Services.Database.MonitoredSystemService;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.DTOS;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.Filters;
using HealthCheck.Framework.Services.Database.SystemChecksService;
using HealthCheck.Framework.Services.Database.SystemChecksService.Filters;
using HealthCheck.Framework.Services.Email;
using HealthCheck.Framework.Services.Email.Models;
using HealthCheck.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace HealthCheck.Worker.Tests.Services.UnitTests;

public class MonitoringServicesTests
{
    [Fact]
    public async Task ExecuteMonitoring_DeveProcessar1000SistemasSemFalhaCritica()
    {
        var systems = Enumerable.Range(1, 1000)
            .Select(i => new MonitoredSystem
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Name = $"Sistema {i}",
                Url = "https://example.com",
                LastStatus = HealthStatus.Healthy
            }).ToList();

        var monitoredRepo = new InMemoryMonitoredSystemRepository(systems);
        var checksRepo = new InMemorySystemChecksRepository();
        var sut = BuildSut(monitoredRepo, checksRepo, _ => new HttpResponseMessage(HttpStatusCode.OK));

        var config = new WorkerConfig
        {
            TimeoutSeconds = 5,
            MaxConcurrentChecks = 1,
            MaxRetries = 1,
            DelayBetweenRetriesMs = 0
        };

        var sw = Stopwatch.StartNew();
        await sut.ExecuteMonitoring(CancellationToken.None, config);
        sw.Stop();

        Assert.Equal(1000, checksRepo.CreatedChecks.Count);
        Assert.Equal(1000, monitoredRepo.UpdatedStatusesCount);
        Assert.All(checksRepo.CreatedChecks, c => Assert.Equal(HealthStatus.Healthy, c.Status));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task ExecuteMonitoring_QuandoStatusHttpNaoOk_DeveMarcarUnhealthy()
    {
        var monitored = new MonitoredSystem
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Sistema API",
            Url = "https://example.com/api",
            LastStatus = HealthStatus.Unhealthy
        };

        var monitoredRepo = new InMemoryMonitoredSystemRepository([monitored]);
        var checksRepo = new InMemorySystemChecksRepository();
        var sut = BuildSut(monitoredRepo, checksRepo, _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var config = new WorkerConfig { TimeoutSeconds = 5, MaxConcurrentChecks = 4, MaxRetries = 1 };

        await sut.ExecuteMonitoring(CancellationToken.None, config);

        var check = Assert.Single(checksRepo.CreatedChecks);
        Assert.Equal(HealthStatus.Unhealthy, check.Status);
        Assert.Equal(HealthStatus.Unhealthy, monitoredRepo.LastUpdatedStatus);
    }

    [Fact]
    public async Task ExecuteMonitoring_QuandoHttpLancaExcecao_DeveMarcarUnknownERegistrarErro()
    {
        var monitored = new MonitoredSystem
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Sistema Externo",
            Url = "https://example.com/timeout",
            LastStatus = HealthStatus.Unknown
        };

        var monitoredRepo = new InMemoryMonitoredSystemRepository([monitored]);
        var checksRepo = new InMemorySystemChecksRepository();
        var sut = BuildSut(monitoredRepo, checksRepo, _ => throw new HttpRequestException("falha simulada"));

        var config = new WorkerConfig { TimeoutSeconds = 1, MaxConcurrentChecks = 2, MaxRetries = 1 };

        await sut.ExecuteMonitoring(CancellationToken.None, config);

        var check = Assert.Single(checksRepo.CreatedChecks);
        Assert.Equal(HealthStatus.Unknown, check.Status);
        Assert.Contains("falha simulada", check.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(nameof(HttpRequestException), check.ExceptionType);
    }

    [Fact]
    public async Task ExecuteMonitoring_QuandoFalhaAoRecuperarPendentes_NaoDeveLancarExcecao()
    {
        var monitoredRepo = new InMemoryMonitoredSystemRepository([])
        {
            ThrowOnGetPending = true
        };

        var checksRepo = new InMemorySystemChecksRepository();
        var sut = BuildSut(monitoredRepo, checksRepo, _ => new HttpResponseMessage(HttpStatusCode.OK));

        var config = new WorkerConfig { TimeoutSeconds = 5, MaxConcurrentChecks = 4, MaxRetries = 1 };

        var ex = await Record.ExceptionAsync(() => sut.ExecuteMonitoring(CancellationToken.None, config));

        Assert.Null(ex);
        Assert.Empty(checksRepo.CreatedChecks);
        Assert.Equal(0, monitoredRepo.UpdatedStatusesCount);
    }

    private static MonitoringServices BuildSut(
        InMemoryMonitoredSystemRepository monitoredRepo,
        InMemorySystemChecksRepository checksRepo,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var serviceProvider = BuildServiceProvider(monitoredRepo, checksRepo, responseFactory);
        var logger = serviceProvider.GetRequiredService<ILogger<Worker>>();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var notificationService = serviceProvider.GetRequiredService<NotificationService>();

        return new MonitoringServices(logger, scopeFactory, httpClientFactory, notificationService);
    }

    private static ServiceProvider BuildServiceProvider(
        InMemoryMonitoredSystemRepository monitoredRepo,
        InMemorySystemChecksRepository checksRepo,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IMonitoredSystemRepository>(monitoredRepo);
        services.AddSingleton<ISystemChecksRepository>(checksRepo);
        services.AddScoped<MonitoredSystemService>();
        services.AddScoped<SystemChecksService>();

        var usersRepoMock = new Mock<IUsersRepository>();
        usersRepoMock.Setup(x => x.GetById(It.IsAny<Guid>(), It.IsAny<NpgsqlConnection?>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Name = "Usuário", Email = "user@example.com", Password = "x" });
        services.AddSingleton(usersRepoMock.Object);

        var passwordEncrypterMock = new Mock<IPasswordEncrypter>();
        passwordEncrypterMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(x => x);
        passwordEncrypterMock.Setup(x => x.Compare(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        services.AddSingleton(passwordEncrypterMock.Object);

        services.AddScoped<Framework.Services.Database.UsersService.UsersService>();

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

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:AlertEmail"] = "admin@healthcheck.local"
            })
            .Build());

        services.AddSingleton<NotificationService>();

        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(new DelegatingHandlerStub((request, _) => Task.FromResult(responseFactory(request)))));
        services.AddSingleton(httpFactoryMock.Object);

        return services.BuildServiceProvider();
    }

    private sealed class DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }

    private sealed class InMemoryMonitoredSystemRepository(List<MonitoredSystem> initial) : IMonitoredSystemRepository
    {
        private readonly ConcurrentDictionary<Guid, MonitoredSystem> _storage = new(initial.ToDictionary(x => x.Id, x => x));
        private int _updatedStatusesCount;
        private HealthStatus? _lastUpdatedStatus;

        public bool ThrowOnGetPending { get; set; }
        public int UpdatedStatusesCount => _updatedStatusesCount;
        public HealthStatus? LastUpdatedStatus => _lastUpdatedStatus;

        public Task<MonitoredSystem> Create(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.FromResult(monitoredSystem);

        public Task Update(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.CompletedTask;

        public Task Delete(MonitoredSystem monitoredSystem, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.CompletedTask;

        public Task<IList<MonitoredSystem>> GetAll(SearchFiltersMonitoredSystems? searchFiltersMonitoredSystems = null, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.FromResult((IList<MonitoredSystem>)_storage.Values.ToList());

        public Task<MonitoredSystem?> GetById(Guid id, NpgsqlConnection? connectionAlreadyCreated = null)
        {
            _storage.TryGetValue(id, out var monitoredSystem);
            return Task.FromResult(monitoredSystem);
        }

        public Task<MonitoredSystem?> GetByUrl(string url, NpgsqlConnection? connectionAlreadyCreated = null)
            => Task.FromResult(_storage.Values.FirstOrDefault(x => x.Url == url));

        public Task<List<MonitoredSystem>> GetPending(NpgsqlConnection? connectionAlreadyCreated = null)
        {
            if (ThrowOnGetPending)
                throw new InvalidOperationException("falha simulada ao recuperar pendentes");

            return Task.FromResult(_storage.Values.ToList());
        }

        public Task UpdateStatus(UpdateMonitoredSystemStatusDTO update, NpgsqlConnection? connectionAlreadyCreated = null)
        {
            if (_storage.TryGetValue(update.Id, out var monitoredSystem))
            {
                monitoredSystem.LastCheckedAt = update.LastCheckedAt;
                monitoredSystem.LastStatus = update.Status;
            }

            _lastUpdatedStatus = update.Status;
            Interlocked.Increment(ref _updatedStatusesCount);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySystemChecksRepository : ISystemChecksRepository
    {
        private readonly ConcurrentBag<SystemCheck> _createdChecks = [];
        public IReadOnlyCollection<SystemCheck> CreatedChecks => _createdChecks.ToArray();

        public Task Create(SystemCheck systemCheck, NpgsqlConnection? connectionnAlreadyCreated = null)
        {
            _createdChecks.Add(systemCheck);
            return Task.CompletedTask;
        }

        public Task<List<SystemCheck>> GetAll(SearchSystemChecksFilter filters, NpgsqlConnection? connectionnAlreadyCreated = null)
            => Task.FromResult(_createdChecks.ToList());

        public Task<SystemCheck?> GetById(long id, NpgsqlConnection? connectionnAlreadyCreated = null)
            => Task.FromResult(_createdChecks.FirstOrDefault(x => x.Id == id));

        public Task<List<SystemCheck>> GetAllBySystemId(Guid systemId, NpgsqlConnection? connectionnAlreadyCreated = null)
            => Task.FromResult(_createdChecks.Where(x => x.SystemId == systemId).ToList());

        public Task<SystemCheck?> GetLastBySystemId(Guid systemId, NpgsqlConnection? connectionnAlreadyCreated = null)
            => Task.FromResult(_createdChecks.LastOrDefault(x => x.SystemId == systemId));

        public Task Clean(NpgsqlConnection? connectionnAlreadyCreated = null)
            => Task.CompletedTask;

        public Task Delete(long id, NpgsqlConnection? connectionnAlreadyCreated = null)
            => Task.CompletedTask;
    }
}
