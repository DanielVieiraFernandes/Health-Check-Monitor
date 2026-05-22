using HealthCheck.Framework.Models;

namespace HealthCheck.Framework.Repositories.WorkerConfigRepository;

public interface IWorkerConfigRepository
{
    Task<WorkerConfig> Get();
    Task<WorkerConfig> Update(WorkerConfig workerConfig);
}
