using FluentValidation;
using HealthCheck.Framework.Models;

namespace HealthCheck.Framework.Services.Database.WorkerConfigService.Validators;

internal class WorkerConfigValidator : AbstractValidator<WorkerConfig>
{

    public WorkerConfigValidator()
    {
        RuleFor(x => x.MonitoringIntervalSeconds).GreaterThanOrEqualTo(15).WithMessage("O intervalo de monitoramento deve ser maior ou igual a 15 segundos.")
            .LessThanOrEqualTo(30).WithMessage("O intervalo de monitoramento deve ser menor ou igual a 30 segundos.");
        RuleFor(x => x.TimeoutSeconds).GreaterThanOrEqualTo(5).WithMessage("O tempo limite deve ser maior ou igual a 5 segundos.")
            .LessThanOrEqualTo(30).WithMessage("O tempo limite deve ser menor ou igual a 30 segundos.");
        RuleFor(x => x.MaxConcurrentChecks).GreaterThanOrEqualTo(5).WithMessage("O número máximo de verificações concorrentes deve ser maior ou igual a 5.")
            .LessThanOrEqualTo(20).WithMessage("O número máximo de verificações concorrentes deve ser menor ou igual a 20.");
        RuleFor(x => x.MaxRetries).GreaterThanOrEqualTo(0).WithMessage("O número máximo de tentativas deve ser maior ou igual a 0.")
            .LessThanOrEqualTo(5).WithMessage("O número máximo de tentativas deve ser menor ou igual a 5.");
        RuleFor(x => x.DelayBetweenRetriesMs).GreaterThanOrEqualTo(0).WithMessage("O atraso entre as tentativas deve ser maior ou igual a 0 ms.")
            .LessThanOrEqualTo(5000).WithMessage("O atraso entre as tentativas deve ser menor ou igual a 5000 ms.");
    }
}
