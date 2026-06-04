using HealthCheck.Framework.Models;
using Xunit;
using HealthCheck.Framework.Services.Database.WorkerConfigService.Validators;
using FluentValidation.TestHelper;

namespace HealthCheck.Tests.Validators;

public class WorkerConfigValidatorTests
{
    private readonly WorkerConfigValidator _validator = new();

    [Fact]
    public void ConfigValida_DentroDosRanges_DevePassar()
    {
        var config = new WorkerConfig
        {
            MonitoringIntervalSeconds = 30,
            TimeoutSeconds = 10,
            MaxConcurrentChecks = 10,
            MaxRetries = 3,
            DelayBetweenRetriesMs = 1000
        };
        var result = _validator.TestValidate(config);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MonitoringIntervalSeconds_AbaixoDe15_DeveFalhar()
    {
        var config = new WorkerConfig { MonitoringIntervalSeconds = 10 };
        var result = _validator.TestValidate(config);
        result.ShouldHaveValidationErrorFor(x => x.MonitoringIntervalSeconds);
    }

    [Fact]
    public void MonitoringIntervalSeconds_AcimaDe30_DeveFalhar()
    {
        var config = new WorkerConfig { MonitoringIntervalSeconds = 60 };
        var result = _validator.TestValidate(config);
        result.ShouldHaveValidationErrorFor(x => x.MonitoringIntervalSeconds);
    }

    [Fact]
    public void TimeoutSeconds_AbaixoDe5_DeveFalhar()
    {
        var config = new WorkerConfig { TimeoutSeconds = 3 };
        var result = _validator.TestValidate(config);
        result.ShouldHaveValidationErrorFor(x => x.TimeoutSeconds);
    }

    [Fact]
    public void MaxConcurrentChecks_AbaixoDe5_DeveFalhar()
    {
        var config = new WorkerConfig { MaxConcurrentChecks = 2 };
        var result = _validator.TestValidate(config);
        result.ShouldHaveValidationErrorFor(x => x.MaxConcurrentChecks);
    }

    [Fact]
    public void MaxConcurrentChecks_AcimaDe20_DeveFalhar()
    {
        var config = new WorkerConfig { MaxConcurrentChecks = 25 };
        var result = _validator.TestValidate(config);
        result.ShouldHaveValidationErrorFor(x => x.MaxConcurrentChecks);
    }

    [Fact]
    public void MaxRetries_Negativo_DeveFalhar()
    {
        var config = new WorkerConfig { MaxRetries = -1 };
        var result = _validator.TestValidate(config);
        result.ShouldHaveValidationErrorFor(x => x.MaxRetries);
    }

    [Fact]
    public void DelayBetweenRetriesMs_AcimaDe5000_DeveFalhar()
    {
        var config = new WorkerConfig { DelayBetweenRetriesMs = 6000 };
        var result = _validator.TestValidate(config);
        result.ShouldHaveValidationErrorFor(x => x.DelayBetweenRetriesMs);
    }
}
