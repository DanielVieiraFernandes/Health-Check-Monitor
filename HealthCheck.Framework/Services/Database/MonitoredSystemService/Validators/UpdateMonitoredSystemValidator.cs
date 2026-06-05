using FluentValidation;
using HealthCheck.Framework.Enums;
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

        RuleFor(x => x.SystemType)
            .IsInEnum()
            .WithMessage("Tipo de sistema inválido.");

        RuleFor(x => x.ExpectedHttpStatus)
            .Must(code => code is null || code == -1 || (code >= 100 && code <= 599))
            .WithMessage("Status HTTP deve estar entre 100 e 599.");

        RuleFor(x => x.ExpectedBodyText)
            .MaximumLength(500)
            .WithMessage("Texto esperado no body deve ter no máximo 500 caracteres.");
    }
}
