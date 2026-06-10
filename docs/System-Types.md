# Tipos de Sistema

Cada sistema monitorado possui um `SystemType` que determina **como** ele é verificado. O tipo define qual `ISystemChecker` será usado e quais validações adicionais são aplicadas.

## Tipos disponíveis

| Tipo | Valor | Descrição | Checker | Porta padrão |
|---|---|---|---|---|
| `WebApi` | 1 | API REST/HTTP | `WebApiSystemChecker` | Qualquer |
| `Frontend` | 2 | Aplicação web (HTML) | `FrontendSystemChecker` | Qualquer |

## Como cada tipo é verificado

### Web API (`WebApiSystemChecker`)

Faz uma requisição HTTP GET e avalia:
1. **Status Code**: por padrão espera 2xx. Pode ser customizado com `ExpectedHttpStatus`.
2. **Latência**: registrada em milissegundos.
3. **Timeout**: configurado globalmente no `WorkerConfig.TimeoutSeconds`.

```
GET https://api.exemplo.com/health
  → 200 OK em 45ms → Healthy
  → 500 Internal Server Error → Unhealthy
  → Timeout → Unknown
```

### Front-end (`FrontendSystemChecker`)

Faz uma requisição HTTP GET e avalia:
1. **Status Code**: por padrão espera 2xx. Pode ser customizado com `ExpectedHttpStatus`.
2. **Conteúdo do body**: pode verificar se o HTML contém um texto esperado (`ExpectedBodyText`).
3. **Latência**: registrada em milissegundos.
4. **Timeout**: configurado globalmente.

```
GET https://meusite.com.br
  → 200 OK, body contém "Bem-vindo" → Healthy
  → 200 OK, mas body NÃO contém "Bem-vindo" → Unhealthy
  → Timeout → Unknown
```

## Strategy Pattern

A seleção do checker é feita por injeção de dependência:

```csharp
// MonitoringServices.cs
var checker = _checkers.FirstOrDefault(c => c.SupportedType == monitoredSystem.SystemType)
    ?? _checkers.First();
var result = await checker.CheckAsync(monitoredSystem, stoppingToken);
```

Cada `ISystemChecker` expõe qual `SystemType` ele suporta. Se nenhum checker corresponder ao tipo, o primeiro disponível é usado como fallback.

## Adicionando um novo tipo

Para adicionar um novo tipo de sistema (ex: `Database`, `TCP`):

1. Adicionar valor ao enum `SystemType`
2. Criar novo checker implementando `ISystemChecker`
3. Registrar no `DependencyInjection.AddWorkerServices()`
4. Adicionar migration SQL (ALTER TABLE) + atualizar `DatabaseService.CreateMonitoredSystemTable()`
