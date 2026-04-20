using FluentValidation;
using HealthCheck.Framework.Models;

namespace HealthCheck.Framework.Services.Database.MonitoredSystemService.Validators;

public class CreateMonitoredSystemValidator : AbstractValidator<MonitoredSystem>
{
    public CreateMonitoredSystemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(255).WithMessage("O nome não pode exceder 255 caracteres.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("A URL é obrigatória.")
            .MaximumLength(2048).WithMessage("A URL não pode exceder 2048 caracteres.")
            .Must(url => !url.Any(char.IsWhiteSpace)).WithMessage("A URL não pode conter espaços em branco.")
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("A URL deve ser válida.");

        RuleFor(x => x.IntervalInMinutes)
            .GreaterThanOrEqualTo(480).WithMessage("O intervalo deve ser maior ou igual a 480 minutos (8 horas).")
            .LessThanOrEqualTo(1440).WithMessage("O intervalo não pode exceder 1440 minutos (24 horas).");
    }
}
