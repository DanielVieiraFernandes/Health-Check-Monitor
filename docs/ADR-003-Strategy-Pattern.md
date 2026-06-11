# ADR 003 — Strategy Pattern nos System Checkers

- **Status**: Aceito
- **Data**: 2026-05-15
- **Decisão de**: Daniel Vieira Fernandes

## Contexto

O HealthCheck precisava suportar diferentes tipos de sistemas (Web API, Front-end) com lógicas de verificação distintas. A abordagem inicial com `if/switch` no `MonitoringServices` não escalava bem e violava o princípio Open/Closed.

## Decisão

**Usar Strategy Pattern** via interface `ISystemChecker`, com injeção de dependência para descoberta automática de implementações.

```csharp
public interface ISystemChecker
{
    SystemType SupportedType { get; }
    Task<CheckResult> CheckAsync(MonitoredSystem system, CancellationToken ct);
}
```

## Justificativas

1. **Open/Closed**: Adicionar um novo tipo de sistema (ex: Database, TCP) requer apenas criar uma nova classe — zero alterações no `MonitoringServices`
2. **Testabilidade**: Cada checker pode ser testado isoladamente com mocks
3. **Descoberta automática**: `IEnumerable<ISystemChecker>` no DI + `FirstOrDefault(c => c.SupportedType == type)` resolve o checker correto em runtime
4. **Responsabilidade única**: Cada checker sabe verificar exatamente um tipo de sistema

## Implementação

```csharp
// Registro no DI
services.AddSingleton<ISystemChecker>(sp =>
    new WebApiSystemChecker(sp.GetRequiredService<IHttpClientFactory>(), 10));
services.AddSingleton<ISystemChecker>(sp =>
    new FrontendSystemChecker(sp.GetRequiredService<IHttpClientFactory>(), 10));

// Uso no MonitoringServices
var checker = _checkers.FirstOrDefault(c => c.SupportedType == monitoredSystem.SystemType)
    ?? _checkers.First();
var result = await checker.CheckAsync(monitoredSystem, stoppingToken);
```

## Consequências

### Positivas
- Extensível sem modificar código existente
- Testável isoladamente
- Separação clara de responsabilidades

### Negativas
- Overhead de abstração para apenas 2 tipos (justifica-se pela previsão de novos tipos)
- Fallback para o primeiro checker se nenhum corresponder (pode mascarar erros de configuração)
