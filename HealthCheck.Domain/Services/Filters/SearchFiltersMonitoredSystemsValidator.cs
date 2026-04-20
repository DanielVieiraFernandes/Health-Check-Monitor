using FluentValidation;

namespace HealthCheck.Framework.Services.Filters;

public class SearchFiltersMonitoredSystemsValidator : AbstractValidator<SearchFiltersMonitoredSystems>
{
    public SearchFiltersMonitoredSystemsValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(5000).WithMessage("O termo de busca não pode exceder 5000 caracteres.");
    }
}
