# Tipos de Sistema + Resposta Esperada — Plano de Implementação v5

> **Para o Hermes:** Usar a skill `subagent-driven-development` para implementar tarefa por tarefa via OpenCode.

**Goal:** Adicionar suporte a 2 tipos de sistema (Web API, Front-end) com resposta esperada configurável por tipo, refinando a verificação do worker para cada cenário.

**Architecture:** Enum `SystemType` (2 valores), campos `SystemType` + `ExpectedHttpStatus` + `ExpectedBodyText` no modelo, padrão Strategy com 2 checkers no worker, UI com dropdown de tipo + dropdown de status code (com opção customizada) + campo de texto condicional.

**Tech Stack:** .NET 10, Dapper, PostgreSQL, MudBlazor, xUnit

---

## Visão geral por tipo

### Web API
| Campo | Tipo | Descrição |
|---|---|---|
| URL | string | Endpoint HTTP (ex: `https://api.exemplo.com/health`) |
| Status esperado | dropdown | 200, 201, 204, 301, 302, 400, 401, 403, 404, 500, 503 + customizado |
| Worker | — | HTTP GET → valida status code |

### Front-end
| Campo | Tipo | Descrição |
|---|---|---|
| URL | string | URL da página (ex: `https://painel.exemplo.com`) |
| Status esperado | dropdown | Mesmos códigos (default: 200) + customizado |
| Texto no body | text (opcional) | Ex: "Painel", "Login" |
| Worker | — | HTTP GET → valida code + texto no body |

---

## Task 0: Branch (já criada)

```
feature/tipos-sistema-resposta-esperada
```

---

## Task 1: Criar enum SystemType

**Files:** Create `HealthCheck.Framework/Enums/SystemType.cs`

```csharp
using System.ComponentModel;

namespace HealthCheck.Framework.Enums;

public enum SystemType
{
    [Description("Web API")]
    WebApi = 1,

    [Description("Front-end")]
    Frontend = 2
}
```

**Verification:** `dotnet build HealthCheck.Framework`

---

## Task 2: Adicionar campos no modelo MonitoredSystem

**Files:** Modify `HealthCheck.Framework/Models/MonitoredSystem.cs`

Adicionar **antes** do bloco `ignoreAttributes`:

```csharp
public SystemType SystemType { get; set; } = SystemType.WebApi;

/// <summary>Código HTTP esperado. Null = usa default do tipo.</summary>
public HttpStatusCode? ExpectedHttpStatus { get; set; }

/// <summary>Texto esperado no body (Front-end). Null = não verifica.</summary>
public string? ExpectedBodyText { get; set; }
```

**NÃO** adicionar ao `ignoreAttributes`.

**Verification:** `dotnet build HealthCheck.Framework`

---

## Task 3: Criar migration SQL

**Files:** Create `HealthCheck.DbUp/Scripts/S002_AddSystemTypeColumns.sql`

```sql
ALTER TABLE monitored_systems
ADD COLUMN IF NOT EXISTS system_type INTEGER NOT NULL DEFAULT 1;

ALTER TABLE monitored_systems
ADD COLUMN IF NOT EXISTS expected_http_status INTEGER;

ALTER TABLE monitored_systems
ADD COLUMN IF NOT EXISTS expected_body_text TEXT;

COMMENT ON COLUMN monitored_systems.system_type IS '1=Web API, 2=Front-end';
COMMENT ON COLUMN monitored_systems.expected_http_status IS 'HTTP status code esperado';
COMMENT ON COLUMN monitored_systems.expected_body_text IS 'Texto esperado no body (Front-end)';
```

**Pitfall:** Confirmar `HealthCheck.DbUp.csproj` tem `<EmbeddedResource Include="Scripts\*.sql" />`
**Pitfall:** Atualizar `DatabaseService.CreateMonitoredSystemTable()` no `HealthCheck.DbUp/Services/DatabaseService.cs` com as 3 colunas novas.

**Verification:** `dotnet build HealthCheck.DbUp`

---

## Task 4: Criar ISystemChecker + CheckResult

**Files:** Create `HealthCheck.Worker/Services/SystemCheckers/ISystemChecker.cs`

```csharp
using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;

namespace HealthCheck.Worker.Services.SystemCheckers;

public interface ISystemChecker
{
    SystemType SupportedType { get; }
    Task<CheckResult> CheckAsync(MonitoredSystem system, CancellationToken ct);
}

public record CheckResult
{
    public HealthStatus Status { get; init; }
    public long LatencyMs { get; init; }
    public string? Response { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ExceptionType { get; init; }
    public string? StackTrace { get; init; }
}
```

**Verification:** `dotnet build HealthCheck.Worker`

---

## Task 5: Implementar WebApiSystemChecker

**Files:** Create `HealthCheck.Worker/Services/SystemCheckers/WebApiSystemChecker.cs`

```csharp
using System.Diagnostics;
using System.Net;
using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;

namespace HealthCheck.Worker.Services.SystemCheckers;

public class WebApiSystemChecker(IHttpClientFactory httpClientFactory, int timeoutSeconds) : ISystemChecker
{
    public SystemType SupportedType => SystemType.WebApi;

    public async Task<CheckResult> CheckAsync(MonitoredSystem system, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = httpClientFactory.CreateClient("HealthCheck");
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            using var response = await client.GetAsync(system.Url, ct);
            sw.Stop();

            var expectedCode = system.ExpectedHttpStatus ?? HttpStatusCode.OK;
            var body = await response.Content.ReadAsStringAsync(ct);
            var isHealthy = response.StatusCode == expectedCode;

            return new CheckResult
            {
                Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                LatencyMs = sw.ElapsedMilliseconds,
                Response = Truncate(body, 500),
                ErrorMessage = isHealthy ? null
                    : $"HTTP {(int)response.StatusCode} — esperado: {(int)expectedCode}"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckResult
            {
                Status = HealthStatus.Unknown,
                LatencyMs = sw.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                ExceptionType = GetExceptionName(ex),
                StackTrace = ex.StackTrace
            };
        }
    }

    private static string Truncate(string v, int m) => v.Length <= m ? v : v[..m];
    private static string GetExceptionName(Exception ex) =>
        ex is TaskCanceledException ? nameof(TimeoutException) : ex.GetType().Name;
}
```

**Verification:** `dotnet build HealthCheck.Worker`

---

## Task 6: Implementar FrontendSystemChecker

**Files:** Create `HealthCheck.Worker/Services/SystemCheckers/FrontendSystemChecker.cs`

```csharp
using System.Diagnostics;
using System.Net;
using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;

namespace HealthCheck.Worker.Services.SystemCheckers;

public class FrontendSystemChecker(IHttpClientFactory httpClientFactory, int timeoutSeconds) : ISystemChecker
{
    public SystemType SupportedType => SystemType.Frontend;

    public async Task<CheckResult> CheckAsync(MonitoredSystem system, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var client = httpClientFactory.CreateClient("HealthCheck");
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            using var response = await client.GetAsync(system.Url, ct);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync(ct);
            var expectedCode = system.ExpectedHttpStatus ?? HttpStatusCode.OK;
            var codeOk = response.StatusCode == expectedCode;

            var expectedText = system.ExpectedBodyText;
            var textOk = string.IsNullOrWhiteSpace(expectedText)
                || body.Contains(expectedText, StringComparison.OrdinalIgnoreCase);

            var isHealthy = codeOk && textOk;
            string? errorMessage = null;
            if (!codeOk)
                errorMessage = $"HTTP {(int)response.StatusCode} — esperado: {(int)expectedCode}";
            else if (!textOk)
                errorMessage = $"HTTP {(int)expectedCode} OK — texto \"{expectedText}\" não encontrado ({body.Length} bytes)";

            return new CheckResult
            {
                Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                LatencyMs = sw.ElapsedMilliseconds,
                Response = Truncate(body, 500),
                ErrorMessage = errorMessage
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CheckResult
            {
                Status = HealthStatus.Unknown,
                LatencyMs = sw.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                ExceptionType = GetExceptionName(ex),
                StackTrace = ex.StackTrace
            };
        }
    }

    private static string Truncate(string v, int m) => v.Length <= m ? v : v[..m];
    private static string GetExceptionName(Exception ex) =>
        ex is TaskCanceledException ? nameof(TimeoutException) : ex.GetType().Name;
}
```

**Verification:** `dotnet build HealthCheck.Worker`

---

## Task 7: Refatorar MonitoringServices para Strategy

**Files:** Modify `HealthCheck.Worker/Services/MonitoringServices.cs` + `HealthCheck.Worker/DependencyInjection.cs`

### 7a. DependencyInjection.cs

```csharp
// System Checkers (Strategy por tipo)
services.AddSingleton<ISystemChecker>(sp =>
    new WebApiSystemChecker(sp.GetRequiredService<IHttpClientFactory>(), 10));
services.AddSingleton<ISystemChecker>(sp =>
    new FrontendSystemChecker(sp.GetRequiredService<IHttpClientFactory>(), 10));
```

### 7b. MonitoringServices.cs

1. Adicionar campo: `private readonly IEnumerable<ISystemChecker> _checkers;`
2. **Substituir** bloco HTTP Get:

ANTES:
```csharp
var client = _httpClientFactory.CreateClient();
client.Timeout = TimeSpan.FromSeconds(workerConfig.TimeoutSeconds);
stopwatch.Start();
using var response = await client.GetAsync(monitoredSystem.Url, stoppingToken);
stopwatch.Stop();
currentStatus = response.StatusCode == HttpStatusCode.OK ? HealthStatus.Healthy : HealthStatus.Unhealthy;
var responseBody = await response.Content.ReadAsStringAsync();
if (!string.IsNullOrEmpty(responseBody))
    systemCheck.SystemResponse = responseBody;
systemCheck.LatencyMs = stopwatch.ElapsedMilliseconds;
```

DEPOIS:
```csharp
var checker = _checkers.FirstOrDefault(c => c.SupportedType == monitoredSystem.SystemType)
    ?? _checkers.First();

var result = await checker.CheckAsync(monitoredSystem, stoppingToken);

currentStatus = result.Status;
systemCheck.LatencyMs = result.LatencyMs;
systemCheck.SystemResponse = result.Response;
systemCheck.ErrorMessage = result.ErrorMessage;
systemCheck.ExceptionType = result.ExceptionType;
systemCheck.StackTrace = result.StackTrace;
```

3. Remover `Stopwatch stopwatch = new();`
4. Remover `using System.Net;` se não for mais usado

**Verification:** `dotnet build HealthCheck.Worker`

---

## Task 8: Atualizar UI — formulário de cadastro/edição

**Files:** Modify `HealthCheck.Web/Components/Pages/MonitoredSystems/EditMonitoredSystem.razor`

### Template — adicionar após o campo URL

```razor
@* ── Tipo de sistema ── *@
<MudSelect @bind-Value="monitoredSystem.SystemType"
           Label="Tipo de sistema"
           Variant="Variant.Outlined"
           T="SystemType"
           ToStringFunc="@(t => t.GetDescription())"
           Immediate="true">
    <MudSelectItem Value="SystemType.WebApi">Web API</MudSelectItem>
    <MudSelectItem Value="SystemType.Frontend">Front-end</MudSelectItem>
</MudSelect>

@* ── Status HTTP esperado (Web API + Front-end) ── *@
@if (monitoredSystem.SystemType is SystemType.WebApi or SystemType.Frontend)
{
    <MudSelect @bind-Value="monitoredSystem.ExpectedHttpStatus"
               Label="Status HTTP esperado"
               Variant="Variant.Outlined"
               T="HttpStatusCode?"
               ToStringFunc="@(c => c is null ? "200 (padrão)" : $"{(int)c} {c}")"
               Immediate="true">
        <MudSelectItem Value="(HttpStatusCode?)null">200 OK (padrão)</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.OK">200 OK</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.Created">201 Created</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.NoContent">204 No Content</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.MovedPermanently">301 Moved Permanently</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.Found">302 Found</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.BadRequest">400 Bad Request</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.Unauthorized">401 Unauthorized</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.Forbidden">403 Forbidden</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.NotFound">404 Not Found</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.InternalServerError">500 Internal Server Error</MudSelectItem>
        <MudSelectItem Value="HttpStatusCode.ServiceUnavailable">503 Service Unavailable</MudSelectItem>
    </MudSelect>

    <MudSwitch @bind-Checked="useCustomHttpStatus" Label="Status code customizado" Color="Color.Info" />

    @if (useCustomHttpStatus)
    {
        <MudTextField @bind-Value="customHttpStatusText"
                      Label="Digite o status code"
                      Variant="Variant.Outlined"
                      Placeholder="418"
                      MaxLength="3"
                      Immediate="true" />
    }
}

@* ── Texto esperado no body (Front-end) ── *@
@if (monitoredSystem.SystemType == SystemType.Frontend)
{
    <MudTextField @bind-Value="monitoredSystem.ExpectedBodyText"
                  Label="Texto esperado no body (opcional)"
                  Variant="Variant.Outlined"
                  Placeholder="Painel"
                  HelperText="Se preenchido, verifica se a resposta contém este texto"
                  MaxLength="500"
                  Counter="500"
                  Clearable />
}
```

### @code — adicionar

```csharp
private bool useCustomHttpStatus;
private string customHttpStatusText = "";

public HttpStatusCode? GetEffectiveHttpStatus()
{
    if (useCustomHttpStatus && int.TryParse(customHttpStatusText, out var code) && Enum.IsDefined(typeof(HttpStatusCode), code))
        return (HttpStatusCode)code;
    return monitoredSystem.ExpectedHttpStatus;
}
```

**Pitfall:** O componente pai (`MonitoredSystems.razor`) precisa chamar `GetEffectiveHttpStatus()` ao montar o DTO antes de salvar.

**Verification:** `dotnet build HealthCheck.Web`

---

## Task 9: Atualizar validação FluentValidation

**Files:** Modify:
- `HealthCheck.Framework/Services/Database/MonitoredSystemService/Validators/CreateMonitoredSystemValidator.cs`
- `HealthCheck.Framework/Services/Database/MonitoredSystemService/Validators/UpdateMonitoredSystemValidator.cs`

```csharp
RuleFor(x => x.SystemType)
    .IsInEnum()
    .WithMessage("Tipo de sistema inválido.");

RuleFor(x => x.ExpectedHttpStatus)
    .Must(code => code is null || Enum.IsDefined(typeof(HttpStatusCode), (int)code!))
    .WithMessage("Status HTTP deve estar entre 100 e 599.");

RuleFor(x => x.ExpectedBodyText)
    .MaximumLength(500)
    .WithMessage("Texto esperado no body deve ter no máximo 500 caracteres.");
```

**Verification:** `dotnet test HealthCheck.Worker.Tests`

---

## Task 10: Testes unitários

**Files:** Create `HealthCheck.Worker.Tests/Services/SystemCheckers/WebApiSystemCheckerTests.cs`

```csharp
using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;
using HealthCheck.Worker.Services.SystemCheckers;
using Moq;
using Moq.Protected;
using System.Net;

namespace HealthCheck.Worker.Tests.Services.SystemCheckers;

public class WebApiSystemCheckerTests
{
    [Fact] public async Task CheckAsync_200_Default_Healthy() { ... }
    [Fact] public async Task CheckAsync_200_Expected201_Unhealthy() { ... }
    [Fact] public async Task CheckAsync_Timeout_Unknown() { ... }
}
```

**Verification:** `dotnet test HealthCheck.Worker.Tests --filter WebApiSystemChecker`

---

## Resumo de arquivos

| Arquivo | Ação |
|---|---|
| `Framework/Enums/SystemType.cs` | Criar (2 valores: WebApi, Frontend) |
| `Framework/Models/MonitoredSystem.cs` | Modificar (+3 props) |
| `DbUp/Scripts/S002_AddSystemTypeColumns.sql` | Criar |
| `DbUp/Services/DatabaseService.cs` | Modificar (colunas no CREATE TABLE) |
| `Worker/.../SystemCheckers/ISystemChecker.cs` | Criar |
| `Worker/.../SystemCheckers/WebApiSystemChecker.cs` | Criar |
| `Worker/.../SystemCheckers/FrontendSystemChecker.cs` | Criar |
| `Worker/Services/MonitoringServices.cs` | Modificar |
| `Worker/DependencyInjection.cs` | Modificar |
| `Web/.../EditMonitoredSystem.razor` | Modificar |
| `Framework/.../Validators/CreateMonitoredSystemValidator.cs` | Modificar |
| `Framework/.../Validators/UpdateMonitoredSystemValidator.cs` | Modificar |
| `Worker.Tests/.../WebApiSystemCheckerTests.cs` | Criar |

---

## Pitfalls

1. **DbUp:** Confirmar `HealthCheck.DbUp.csproj` tem `<EmbeddedResource Include="Scripts\*.sql" />` e o `DatabaseService.CreateMonitoredSystemTable()` inclui as 3 colunas novas.
2. **QueryBuilder reflection:** INSERT/UPDATE pegam novos campos automaticamente
3. **Dapper SELECT *:** Mapeia novas colunas automaticamente
4. **UI status customizado:** `MudSwitch` ativa/desativa campo de status code livre. Pai chama `GetEffectiveHttpStatus()` antes de salvar.
