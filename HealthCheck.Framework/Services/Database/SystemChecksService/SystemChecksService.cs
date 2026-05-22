using FluentValidation.Results;
using HealthCheck.Framework.Helpers;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Repositories.SystemChecksRepository;
using HealthCheck.Framework.Services.Database.SystemChecksService.Filters;
using HealthCheck.Framework.Services.Database.SystemChecksService.Validators;
using System.Net;

namespace HealthCheck.Framework.Services.Database.SystemChecksService;

public class SystemChecksService(ISystemChecksRepository systemChecksRepository)
{
    public async Task<Result<object>> CreateCheck(SystemCheck systemCheck)
    {
        await systemChecksRepository.Create(systemCheck);

        return Result<object>.AsSuccess(new { });
    }

    //public async Task<Result<object>> DeleteCheck(SystemCheck systemCheck)
    //{
    //    await systemChecksRepository.Create(systemCheck);

    //    return Result<object>.AsSuccess(new { });
    //}

    public async Task<Result<List<SystemCheck>>> GetAllChecks(SearchSystemChecksFilter filters)
    {
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        //Realizo as normalizações necessárias antes de validar os filtros
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        filters.SearchTerm = filters.SearchTerm.NormalizeWhiteSpaces(removeAccent: true, removeMultipleSpaces: true);

        SearchSystemChecksFilterValidator validator = new();

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        //Valida os filtros fornecidos pelo usuário
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        var validationResult = validator.Validate(filters);

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        //Caso a validação falhe, retorno um resultado de falha contendo os erros de validação
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        if (!validationResult.IsValid)
        {
            Failure failure = new(HttpStatusCode.BadRequest, validationResult);

            return Result<List<SystemCheck>>.AsFailure(failure);
        }

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        //Se a validação passar, sigo para buscar os dados no repositório 
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

        var systemChecks = await systemChecksRepository.GetAll(filters);

        return Result<List<SystemCheck>>.AsSuccess(systemChecks);
    }

    public async Task<Result<List<SystemCheck>>> GetAllChecksBySystemId(Guid systemId)
    {
        var systemChecks = await systemChecksRepository.GetAllBySystemId(systemId);

        return Result<List<SystemCheck>>.AsSuccess(systemChecks);
    }

    public async Task<Result<SystemCheck?>> GetLastBySystemId(Guid systemId)
    {
        var systemCheck = await systemChecksRepository.GetLastBySystemId(systemId);

        if (systemCheck == null)
        {
            ValidationFailure validationFailure = new("SystemCheck", "Recurso não encontrado.");

            return Result<SystemCheck?>.AsFailure(new Failure(HttpStatusCode.NotFound, new() { Errors = [validationFailure] }));
        }

        return Result<SystemCheck?>.AsSuccess(systemCheck);
    }

    public async Task<Result<SystemCheck?>> GetCheckById(long id)
    {
        var systemCheck = await systemChecksRepository.GetById(id);

        return Result<SystemCheck?>.AsSuccess(systemCheck);
    }

    public async Task<Result<object>> CleanOldChecks()
    {
        await systemChecksRepository.Clean();

        return Result<object>.AsSuccess(new { });
    }

}