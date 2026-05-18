using HealthCheck.Framework.Models;
using HealthCheck.Framework.Repositories.WorkerConfigRepository;
using HealthCheck.Framework.Services.Database.WorkerConfigService.Validators;
using System.Net;

namespace HealthCheck.Framework.Services.Database.WorkerConfigService;

public class WorkerConfigService(IWorkerConfigRepository workerConfigRepository)
{

    public async Task<Result<WorkerConfig>> Get()
    {
        var workerConfig = await workerConfigRepository.Get();

        if (workerConfig == null)
            throw new Exception("WorkerConfig not found");

        return Result<WorkerConfig>.AsSuccess(workerConfig);
    }

    public async Task<Result<WorkerConfig>> Update(WorkerConfig workerConfig)
    {
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //VALIDA OS DADOS DE CONFIGURAÇÃO DO WORKER
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        WorkerConfigValidator validator = new();

        var validationResult = await validator.ValidateAsync(workerConfig);

        if (!validationResult.IsValid)
        {
            Failure failure = new(HttpStatusCode.BadRequest, validationResult);

            return Result<WorkerConfig>.AsFailure(failure);
        }

        workerConfig.UpdatedAt = DateTime.Now;

        var updatedWorkerConfig = await workerConfigRepository.Update(workerConfig);

        if (updatedWorkerConfig == null)
            throw new Exception("Failed to update WorkerConfig");

        return Result<WorkerConfig>.AsSuccess(updatedWorkerConfig);
    }

}
