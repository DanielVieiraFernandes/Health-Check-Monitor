using FluentValidation;
using Xunit;
using FluentValidation.TestHelper;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.Validators;

namespace HealthCheck.Tests.Validators;

public class CreateMonitoredSystemValidatorTests
{
    private readonly CreateMonitoredSystemValidator _validator = new();

    [Fact]
    public async Task NomeVazio_DeveFalharValidacao()
    {
        var system = new MonitoredSystem { Name = "", Url = "https://example.com" };
        var result = await _validator.TestValidateAsync(system);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task UrlInvalida_DeveFalharValidacao()
    {
        var system = new MonitoredSystem { Name = "Sistema Teste", Url = "nao-e-uma-url" };
        var result = await _validator.TestValidateAsync(system);
        result.ShouldHaveValidationErrorFor(x => x.Url);
    }

    [Fact]
    public async Task UrlComEspacos_DeveFalharValidacao()
    {
        var system = new MonitoredSystem { Name = "Sistema", Url = "https://exemplo .com/health" };
        var result = await _validator.TestValidateAsync(system);
        result.ShouldHaveValidationErrorFor(x => x.Url);
    }

    [Fact]
    public async Task DadosValidos_DevePassarValidacao()
    {
        var system = new MonitoredSystem
        {
            Name = "Sistema de Produção",
            Url = "https://api.exemplo.com/health"
        };
        var result = await _validator.TestValidateAsync(system);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task NomeMuitoLongo_DeveFalharValidacao()
    {
        var system = new MonitoredSystem
        {
            Name = new string('A', 256),
            Url = "https://example.com"
        };
        var result = await _validator.TestValidateAsync(system);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
