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
            .Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("A URL deve ser válida.")
            .MustAsync(async (url, cancellationToken) => await MonitoredSystemUrlSafetyValidator.IsAllowedAsync(url))
            .WithMessage("A URL informada não é permitida.");
    }
}
