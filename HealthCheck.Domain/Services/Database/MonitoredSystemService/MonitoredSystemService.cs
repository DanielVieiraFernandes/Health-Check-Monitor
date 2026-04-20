using FluentValidation.Results;
using HealthCheck.Framework.Helpers;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Repositories.MonitoredSystemRepository;
using System.Net;

namespace HealthCheck.Framework.Services.Database.MonitoredSystemService;

public class MonitoredSystemService(IMonitoredSystemRepository monitoredSystemRepository)
{
    public async Task<Result<MonitoredSystem>> CreateMonitoredSystem(MonitoredSystem monitoredSystem)
    {
        NormalizeMonitoredSystem(monitoredSystem);

        CreateMonitoredSystemValidator validator = new();

        var validationResult = validator.Validate(monitoredSystem);

        if (!validationResult.IsValid)
        {
            Failure failure = new(HttpStatusCode.BadRequest, validationResult);

            return Result<MonitoredSystem>.AsFailure(failure);
        }

        monitoredSystem = await monitoredSystemRepository.Create(monitoredSystem);

        return Result<MonitoredSystem>.AsSuccess(monitoredSystem);
    }

    public async Task<Result<MonitoredSystem>> GetMonitoredSystemById(Guid id)
    {
        var monitoredSystem = await monitoredSystemRepository.GetById(id);

        if (monitoredSystem == null)
            return Result<MonitoredSystem>.AsFailure(new Failure(HttpStatusCode.NotFound, BuildValidationResult("Sistema monitorado não encontrado")));

        return Result<MonitoredSystem>.AsSuccess(monitoredSystem);
    }

    public async Task<Result<IList<MonitoredSystem>>> GetAllMonitoredSystems(SearchFiltersMonitoredSystems? searchFiltersMonitoredSystems = null)
    {
        if (searchFiltersMonitoredSystems != null)
        {
            NormalizeSearchFilters(searchFiltersMonitoredSystems);

            SearchFiltersMonitoredSystemsValidator validator = new();

            var validationResult = validator.Validate(searchFiltersMonitoredSystems);

            if (!validationResult.IsValid)
                return Result<IList<MonitoredSystem>>.AsFailure(new Failure(HttpStatusCode.BadRequest, validationResult));
        }

        var monitoredSystems = await monitoredSystemRepository.GetAll(searchFiltersMonitoredSystems);

        return Result<IList<MonitoredSystem>>.AsSuccess(monitoredSystems);
    }

    public async Task<Result<object>> UpdateMonitoredSystem(MonitoredSystem monitoredSystem)
    {
        NormalizeMonitoredSystem(monitoredSystem);

        CreateMonitoredSystemValidator validator = new();

        var validationResult = validator.Validate(monitoredSystem);

        if (!validationResult.IsValid)
            return Result<object>.AsFailure(new Failure(HttpStatusCode.BadRequest, validationResult));

        await monitoredSystemRepository.Update(monitoredSystem);

        return Result<object>.AsSuccess(new { });
    }
    public async Task<Result<object>> DeleteMonitoredSystem(Guid id)
    {
        var monitoredSystem = await monitoredSystemRepository.GetById(id);

        // TODO: Futuramente, irei validar se quem está tentando deletar o sistema monitorado é o dono do mesmo,
        // ou seja, se ele tem permissão para deletar o sistema monitorado
        if (monitoredSystem == null)
            return Result<object>.AsFailure(new(HttpStatusCode.BadRequest, BuildValidationResult("Recurso não encontrado")));

        await monitoredSystemRepository.Delete(monitoredSystem);

        return Result<object>.AsSuccess(new { });
    }

    private static ValidationResult BuildValidationResult(params string[] messages)
    {
        var errors = messages.Select(message => new ValidationFailure(string.Empty, message)).ToList();
        return new ValidationResult(errors);
    }

    private static void NormalizeMonitoredSystem(MonitoredSystem monitoredSystem)
    {
        monitoredSystem.Name = monitoredSystem.Name.NormalizeWhiteSpaces();
        monitoredSystem.Url = monitoredSystem.Url.NormalizeWhiteSpaces();
    }

    private static void NormalizeSearchFilters(SearchFiltersMonitoredSystems searchFiltersMonitoredSystems)
    {
        searchFiltersMonitoredSystems.SearchTerm = searchFiltersMonitoredSystems.SearchTerm.NormalizeWhiteSpaces();
    }
}
