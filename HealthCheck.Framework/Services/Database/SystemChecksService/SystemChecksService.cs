using HealthCheck.Framework.Models;
using HealthCheck.Framework.Repositories.SystemChecksRepository;
using HealthCheck.Framework.Services.Database.SystemChecksService.Filters;

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
        var systemChecks = await systemChecksRepository.GetAll(filters);

        return Result<List<SystemCheck>>.AsSuccess(systemChecks);
    }

    public async Task<Result<List<SystemCheck>>> GetAllChecksBySystemId(Guid systemId)
    {
        var systemChecks = await systemChecksRepository.GetAllBySystemId(systemId);

        return Result<List<SystemCheck>>.AsSuccess(systemChecks);
    }
}
