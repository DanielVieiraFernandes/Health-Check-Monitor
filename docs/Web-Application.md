# Aplicação Web (Blazor Server)

## Visão geral

Aplicação Blazor Server renderizada no servidor com SignalR para UI interativa. Usa MudBlazor como framework de componentes principais e Syncfusion para componentes específicos (diálogos, grade).

- **Projeto**: `HealthCheck.Web`
- **Porta**: 5000 (desenvolvimento)
- **Render mode**: Blazor Server (pré-renderizado)
- **Autenticação**: ASP.NET Core Cookies

## Estrutura de páginas

```
/                       → Redireciona para /dashboard ou /login
/login                  → Login (email + senha)
/dashboard              → Dashboard com indicadores de disponibilidade
/sistemas               → CRUD de sistemas monitorados (cards)
/auditoria              → Histórico de checagens com filtros
/configuracoes          → Configuração do Worker
```

## Componentes principais

| Componente | Caminho | Responsabilidade |
|---|---|---|
| `MonitoredSystems` | `Pages/MonitoredSystems/` | Lista de sistemas em cards, com busca, filtros e diálogo de edição |
| `EditMonitoredSystem` | `Pages/MonitoredSystems/` | Formulário de cadastro/edição com validação FluentValidation |
| `Dashboard` | `Pages/Dashboard/` | Painel com indicadores, gráficos e alertas |
| `Audit` | `Pages/Audit/` | Tabela de histórico de checagens com filtros avançados |
| `Config` | `Pages/Config/` | Tela de configuração do Worker (intervalo, timeout, etc.) |
| `Login` | `Pages/Login/` | Formulário de autenticação |
| `MainLayout` | `Layout/` | Layout principal com drawer de navegação e tema escuro/claro |

## Fluxo de autenticação

```
1. Usuário acessa /login
2. Preenche email + senha
3. AuthenticationService.SignInAsync()
   → UsersService.AuthenticateUser()
   → BcryptPasswordEncrypter.Compare()
   → Se OK: cria ClaimsPrincipal + cookie
4. Redireciona para /dashboard
5. Sessão mantida via cookie com expiração configurável
```

## Injeção de dependência

A Web registra seus serviços via `Program.cs`:

```csharp
builder.Services.AddSingleton<LoadingState>();       // Estado global de carregamento
builder.Services.AddScoped<AuthenticationService>(); // Autenticação
builder.Services.AddScoped<UserSessionInfo>();       // Sessão do usuário (cascading)
builder.Services.AddFrameworkServices(config);       // Framework (DB, repositórios, validação)
```

## Tema

Suporte a tema claro e escuro via `IsDarkMode` cascading parameter. O CSS está em:
- `wwwroot/app.css` — estilos globais (incluindo `.system-url`, `.theme-dark`)
- `Componente.razor.css` — CSS isolado do Blazor (apenas para elementos do próprio componente)

**Regra**: estilos para componentes MudBlazor SEMPRE em `app.css` (global), nunca em `.razor.css` (isolado).

## Tratamento de erros

- `ErrorBoundary` captura exceções não tratadas na UI
- Stack trace real vai para arquivo: `LOGS/Exceptions/ExceptionLog_yyyy-MM-dd.txt`
- `LoadingState` gerencia estados de carregamento globais
