# Convenções de Código

## Dapper

### Regra suprema: NUNCA usar SELECT *

```csharp
// ❌ ERRADO
var sql = "SELECT * FROM monitored_systems WHERE id = @Id";

// ✅ CORRETO
var sql = "SELECT id, user_id, name, description, url, system_type,
                  last_status, last_checked_at, created_at, updated_at
           FROM monitored_systems WHERE id = @Id";
```

### Parâmetros nomeados

```csharp
// ✅ CORRETO — sempre @param
var result = await connection.QueryAsync<User>(sql, new { Email = email });

// ❌ ERRADO — nunca concatenar strings
var sql = $"SELECT * FROM users WHERE email = '{email}'";
```

### Mapeamento underscore → PascalCase

```csharp
// Ativado uma vez no startup
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
// Agora "last_checked_at" no DB → "LastCheckedAt" no C#
```

## FluentValidation

### IsInEnum() em tipos nullable

```csharp
// ❌ ERRADO — ArgumentException em HttpStatusCode?
RuleFor(x => x.ExpectedHttpStatus).IsInEnum();

// ✅ CORRETO — validação explícita
RuleFor(x => x.ExpectedHttpStatus)
    .Must(v => !v.HasValue || Enum.IsDefined(typeof(HttpStatusCode), (int)v.Value))
    .When(x => x.SystemType is SystemType.WebApi or SystemType.Frontend);
```

## MudBlazor

### MudSwitch: usar @bind-Value (não @bind-Checked)

```razor
<!-- ✅ CORRETO — MudBlazor 9.x -->
<MudSwitch @bind-Value="useCustomHttpStatus" Label="..." Color="Color.Info" />

<!-- ❌ ERRADO -->
<MudSwitch @bind-Checked="useCustomHttpStatus" T="bool" />
```

### MudTextField: contador precisa de Immediate="true"

```razor
<!-- ✅ CORRETO — contador atualiza durante digitação -->
<MudTextField @bind-Value="text" Counter="500" MaxLength="500" Immediate="true" />

<!-- ❌ ERRADO — contador só atualiza no blur -->
<MudTextField @bind-Value="text" Counter="500" MaxLength="500" />
```

### MudSelect com tipos nullable

```razor
<!-- ✅ CORRETO — usar T="int" com bridge -->
<MudSelect @bind-Value="ExpectedHttpStatusCodeInt" T="int">
    <MudSelectItem Value="0">Escolha</MudSelectItem>
    <MudSelectItem Value="200">200 OK</MudSelectItem>
</MudSelect>
```

```csharp
private int ExpectedHttpStatusCodeInt
{
    get => monitoredSystem.ExpectedHttpStatus is null ? 0 : (int)monitoredSystem.ExpectedHttpStatus.Value;
    set => monitoredSystem.ExpectedHttpStatus = value == 0 ? null : (HttpStatusCode)value;
}
```

### CSS: MudBlazor em global, nunca isolado

```css
/* ✅ CORRETO — wwwroot/app.css (global) */
.mud-drawer { position: fixed; }

/* ❌ ERRADO — Componente.razor.css (isolado pelo Blazor) */
.mud-drawer { position: fixed; } /* Nunca aplicado! */
```

### Razor: @code.ToString() conflita com diretiva @code

```razor
<!-- ❌ ERRADO -->
<MudSelectItem>@code.ToString()</MudSelectItem>

<!-- ✅ CORRETO — parênteses desambiguam -->
<MudSelectItem>@(code.ToString())</MudSelectItem>
```

## Git

### Branches

```
main                          # Produção
develop                       # Desenvolvimento
feature/nome-curto            # Nova funcionalidade
fix/nome-curto                # Correção de bug
```

### Commits (Conventional Commits em português)

```bash
feat(web): adiciona cards com ícones de tipo de sistema
fix(worker): corrige timeout em checagens concorrentes
test: adiciona testes para validação de status HTTP
docs: atualiza documentação de arquitetura
refactor(web): substitui MudLink por tag a nativa
chore: atualiza dependências
```

### Fluxo de trabalho

```bash
git checkout develop
git pull origin develop
git checkout -b feature/nova-funcionalidade
# ... implementar ...
git add .
git commit -m "feat(escopo): descrição em português"
git push -u origin HEAD
# Abrir PR para develop
```

## Proibido: editar código diretamente

Código do HealthCheck (.cs, .razor, .sql, .csproj) **nunca** deve ser editado diretamente com patch/write_file. Sempre delegar ao OpenCode:

```bash
opencode run "instruções detalhadas aqui"
```

Scripts auxiliares (.js, .md) e arquivos de configuração podem ser editados diretamente.
