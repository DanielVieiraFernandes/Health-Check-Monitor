using FluentValidation;
using HealthCheck.Framework.Models;

namespace HealthCheck.Framework.Services.Database.MonitoredSystemService.Validators;

public class UpdateMonitoredSystemValidator : AbstractValidator<MonitoredSystem>
{
    public UpdateMonitoredSystemValidator()
    {
        RuleFor(x => x.Name)
          .NotEmpty().WithMessage("O nome é obrigatório.")
          .MaximumLength(255).WithMessage("O nome não pode exceder 255 caracteres.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("A URL é obrigatória.")
            .MaximumLength(2048).WithMessage("A URL não pode exceder 2048 caracteres.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("A URL deve ser válida.");
    }
}
