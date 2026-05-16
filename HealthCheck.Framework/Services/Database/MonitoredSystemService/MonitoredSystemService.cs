using FluentValidation.Results;
using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Helpers;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Repositories.MonitoredSystemRepository;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.DTOS;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.Filters;
using HealthCheck.Framework.Services.Database.MonitoredSystemService.Validators;
using HealthCheck.Framework.Services.Database.Resources;
using System.Globalization;
using System.Net;

namespace HealthCheck.Framework.Services.Database.MonitoredSystemService;

public class MonitoredSystemService(IMonitoredSystemRepository monitoredSystemRepository)
{
    public async Task<Result<MonitoredSystem>> CreateMonitoredSystem(MonitoredSystem monitoredSystem)
    {
        NormalizeMonitoredSystem(monitoredSystem);

        CreateMonitoredSystemValidator validator = new();

        var validationResult = await validator.ValidateAsync(monitoredSystem);

        if (!validationResult.IsValid)
        {
            Failure failure = new(HttpStatusCode.BadRequest, validationResult);

            return Result<MonitoredSystem>.AsFailure(failure);
        }

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //VALIDO A EXISTÊNCIA DE UM SISTEMA MONITORADO COM A MESMA URL, PARA EVITAR DUPLICIDADE
        //DE REGISTROS E GARANTIR A INTEGRIDADE DOS DADOS
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        var existingMonitoredSystem = await monitoredSystemRepository.GetByUrl(monitoredSystem.Url);

        if (existingMonitoredSystem != null)
            return Result<MonitoredSystem>.AsFailure(new Failure(HttpStatusCode.Conflict, BuildValidationResult("Já existe um sistema monitorado com a mesma URL")));

        monitoredSystem = await monitoredSystemRepository.Create(monitoredSystem);

        return Result<MonitoredSystem>.AsSuccess(monitoredSystem);
    }

    public async Task<Result<MonitoredSystem>> GetMonitoredSystemById(Guid id)
    {
        var monitoredSystem = await monitoredSystemRepository.GetById(id);

        if (monitoredSystem == null)
            return Result<MonitoredSystem>.AsFailure(new Failure(HttpStatusCode.NotFound, BuildValidationResult("Sistema monitorado não encontrado")));

        return Result<MonitoredSystem>.AsSuccess(monitoredSystem);
    }

    /// <summary>
    /// Recupera todos os sistemas monitorados, aplicando os filtros de pesquisa fornecidos, e opcionalmente <br/>
    /// filtrando por usuário, para obter uma lista de sistemas monitorados que correspondam aos critérios de pesquisa <br/>
    /// especificados, permitindo ao usuário visualizar e gerenciar seus sistemas monitorados de forma eficiente e personalizada, <br/>
    /// com base em suas preferências e necessidades específicas
    /// </summary>
    /// <param name="searchFiltersMonitoredSystems">Filtros de pesquisa para os sistemas monitorados</param>
    /// <param name="userId">ID do usuário para filtrar os sistemas monitorados</param>
    /// <returns>Lista de sistemas monitorados do tipo <see cref="MonitoredSystem"/> que correspondem aos critérios de pesquisa</returns>
    public async Task<Result<IList<MonitoredSystem>>> GetAllMonitoredSystems(SearchFiltersMonitoredSystems? searchFiltersMonitoredSystems = null)
    {
        if (searchFiltersMonitoredSystems != null)
        {
            NormalizeSearchFilters(searchFiltersMonitoredSystems);

            SearchFiltersMonitoredSystemsValidator validator = new();

            var validationResult = validator.Validate(searchFiltersMonitoredSystems);

            if (!validationResult.IsValid)
                return Result<IList<MonitoredSystem>>.AsFailure(new Failure(HttpStatusCode.BadRequest, validationResult));
        }

        var monitoredSystems = await monitoredSystemRepository.GetAll(searchFiltersMonitoredSystems);

        return Result<IList<MonitoredSystem>>.AsSuccess(monitoredSystems);
    }

    public async Task<Result<object>> UpdateMonitoredSystem(MonitoredSystem monitoredSystem, MonitoredSystem monitoredSystemClone, string changeBy)
    {
        NormalizeMonitoredSystem(monitoredSystem);

        CreateMonitoredSystemValidator validator = new();

        var validationResult = await validator.ValidateAsync(monitoredSystem);

        if (!validationResult.IsValid)
            return Result<object>.AsFailure(new Failure(HttpStatusCode.BadRequest, validationResult));


        monitoredSystem.UpdatedAt = DateTime.Now;

        //**************************************************************************************************************************
        //CASO A URL TENHA SIDO ALTERADA, RESETO O STATUS E A DATA DA ÚLTIMA VERIFICAÇÃO,
        //PARA QUE O WORKER REALIZE UMA NOVA VERIFICAÇÃO E ATUALIZE O STATUS DO SISTEMA
        //**************************************************************************************************************************
        if (monitoredSystem.Url != monitoredSystemClone.Url)
        {
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //VALIDO A EXISTÊNCIA DE UM SISTEMA MONITORADO COM A MESMA URL, PARA EVITAR DUPLICIDADE
            //DE REGISTROS E GARANTIR A INTEGRIDADE DOS DADOS
            //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            var existingMonitoredSystem = await monitoredSystemRepository.GetByUrl(monitoredSystem.Url);

            if (existingMonitoredSystem != null)
                return Result<object>.AsFailure(new Failure(HttpStatusCode.BadRequest, BuildValidationResult("Já existe um sistema monitorado com a mesma URL")));

            monitoredSystem.LastCheckedAt = null;
            monitoredSystem.LastStatus = HealthStatus.Unknown;

            monitoredSystem.RemoveIgnoreAttribute(nameof(MonitoredSystem.LastCheckedAt));
            monitoredSystem.RemoveIgnoreAttribute(nameof(MonitoredSystem.LastStatus));
        }

        //===========================================================================================================
        // Compara as diferenças entre o objeto original e o objeto atualizado, para obter uma descrição das mudanças
        // realizadas, e assim, gerar um histórico de mudanças mais detalhado e informativo para o usuário, indicando
        // quais campos foram alterados, para obter um melhor controle e rastreabilidade das modificações feitas no
        // sistema monitorado
        //===========================================================================================================
        string differences = ServicesResources.CompareObjects(monitoredSystem, monitoredSystemClone, monitoredSystem.GetIgnoreAttributes());

        //===========================================================================================================
        // Se houver diferenças, adiciona uma entrada ao histórico do sistema monitorado,
        // indicando as mudanças realizadas
        //===========================================================================================================
        if (!string.IsNullOrWhiteSpace(differences))
        {
            var updatedAt = monitoredSystem.UpdatedAt.ToString("g", CultureInfo.CurrentCulture);
            var historyEntry = $@"========================================
Alterações registradas
========================================
{differences.Trim()}
----------------------------------------
Alterado por último em: {updatedAt}
Responsável: {changeBy}
========================================";

            monitoredSystem.History += $"{historyEntry}\n";
        }

        await monitoredSystemRepository.Update(monitoredSystem);

        return Result<object>.AsSuccess(new { });
    }

    /// <summary>
    /// Atualiza o status de um sistema monitorado, com base nos resultados da verificação de saúde realizada pelo worker
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    public async Task<Result<object>> UpdateMonitoredSystemStatus(UpdateMonitoredSystemStatusDTO update)
    {
        var monitoredSystem = await monitoredSystemRepository.GetById(update.Id);

        if (monitoredSystem == null)
            return Result<object>.AsFailure(new Failure(HttpStatusCode.BadRequest, BuildValidationResult("Sistema monitorado não encontrado")));

        var monitoredSystemClone = monitoredSystem.Clone();

        monitoredSystem.UpdatedAt = DateTime.Now;

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-
        // Realizo o update do status e da data da última verificação
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-
        monitoredSystem.LastCheckedAt = update.LastCheckedAt;
        monitoredSystem.LastStatus = update.Status;

        List<string> ignoreAttributes = monitoredSystem.GetType()
            .GetProperties()
            .Where(p => p.Name != nameof(MonitoredSystem.LastStatus) && p.Name != nameof(MonitoredSystem.LastCheckedAt))
            .Select(p => p.Name).ToList();

        //===========================================================================================================
        // Compara as diferenças entre o objeto original e o objeto atualizado, para obter uma descrição das mudanças
        // realizadas, e assim, gerar um histórico de mudanças mais detalhado e informativo para o usuário, indicando
        // quais campos foram alterados, para obter um melhor controle e rastreabilidade das modificações feitas no
        // sistema monitorado
        //===========================================================================================================
        string differences = ServicesResources.CompareObjects(monitoredSystem, monitoredSystemClone, ignoreAttributes);

        //===========================================================================================================
        // Se houver diferenças, adiciona uma entrada ao histórico do sistema monitorado,
        // indicando as mudanças realizadas
        //===========================================================================================================
        if (!string.IsNullOrWhiteSpace(differences))
        {
            var updatedAt = monitoredSystem.UpdatedAt.ToString("g", CultureInfo.CurrentCulture);
            var historyEntry = $@"========================================
Alterações registradas
========================================
{differences.Trim()}
----------------------------------------
Alterado por último em: {updatedAt}
Responsável: Worker de verificação de saúde
========================================";

            update.History += $"{historyEntry}\n";
        }

        await monitoredSystemRepository.UpdateStatus(update);

        return Result<object>.AsSuccess(new { });
    }

    public async Task<Result<object>> DeleteMonitoredSystem(Guid id, Guid userId)
    {
        var monitoredSystem = await monitoredSystemRepository.GetById(id);

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Caso não encontre o sistema monitorado ou o sistema monitorado não pertenca ao usuário
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        if (monitoredSystem == null || monitoredSystem.UserId != userId)
            return Result<object>.AsFailure(new(HttpStatusCode.BadRequest, BuildValidationResult("Recurso não encontrado")));

        await monitoredSystemRepository.Delete(monitoredSystem);

        return Result<object>.AsSuccess(new { });
    }

    /// <summary>
    /// Recupera os sistemas monitorados que estão pendentes de verificação, ou seja, aqueles que ainda não foram
    /// verificados ou atualizados recentemente
    /// </summary>
    /// <returns></returns>
    public async Task<Result<List<MonitoredSystem>>> GetPendingMonitoredSystemsAsync()
    {
        var pendingMonitoredSystems = await monitoredSystemRepository.GetPending();

        return Result<List<MonitoredSystem>>.AsSuccess(pendingMonitoredSystems);
    }

    private static ValidationResult BuildValidationResult(params string[] messages)
    {
        var errors = messages.Select(message => new ValidationFailure(string.Empty, message)).ToList();
        return new ValidationResult(errors);
    }

    private static void NormalizeMonitoredSystem(MonitoredSystem monitoredSystem)
    {
        monitoredSystem.Name = monitoredSystem.Name.NormalizeWhiteSpaces(removeMultipleSpaces: true);
        monitoredSystem.Url = monitoredSystem.Url.NormalizeWhiteSpaces(removeMultipleSpaces: true);
    }

    private static void NormalizeSearchFilters(SearchFiltersMonitoredSystems searchFiltersMonitoredSystems)
    {
        searchFiltersMonitoredSystems.SearchTerm = searchFiltersMonitoredSystems.SearchTerm.NormalizeWhiteSpaces(removeAccent: true, removeMultipleSpaces: true);
    }
}
