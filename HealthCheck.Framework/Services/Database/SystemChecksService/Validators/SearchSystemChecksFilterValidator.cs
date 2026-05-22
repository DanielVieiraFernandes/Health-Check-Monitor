using FluentValidation;
using HealthCheck.Framework.Services.Database.SystemChecksService.Filters;

namespace HealthCheck.Framework.Services.Database.SystemChecksService.Validators;

public class SearchSystemChecksFilterValidator : AbstractValidator<SearchSystemChecksFilter>
{

    public SearchSystemChecksFilterValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Usuário não informado.");
        RuleFor(x => x.SearchTerm).MaximumLength(255).WithMessage("O termo de busca não pode ter mais de 255 caracteres.");
        RuleFor(x => x.FromDate).LessThanOrEqualTo(x => x.ToDate).When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("A data de início deve ser menor ou igual à data de término.");
        //==================================================================================================================================
        //O RuleForEach percorre cada item da coleção aplicando as validações implementadas
        //==================================================================================================================================
        RuleForEach(x => x.HealthStatusSelected)
            //Verifica se é um valor válido para o enumerador HealthStatus
            .IsInEnum()
            .WithMessage("O status selecionado ('{PropertyValue}') não é um valor válido para HealthStatus.")
            //O .When garante que a validação só ocorra se a lista não for nula e tiver itens
            .When(x => x.HealthStatusSelected != null && x.HealthStatusSelected.Count > 0);
        RuleFor(x => x.LatencyPreference).IsInEnum().When(x => x.LatencyPreference.HasValue)
            .WithMessage("A preferência de latência selecionada ('{PropertyValue}') não é um valor válido para LatencyPreference.");
    }
}


