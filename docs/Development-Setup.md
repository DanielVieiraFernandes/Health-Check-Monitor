# Setup do Ambiente de Desenvolvimento

## Pré-requisitos

| Ferramenta | Versão | Instalação |
|---|---|---|
| .NET SDK | 10.0 | `winget install Microsoft.DotNet.SDK.10` |
| PostgreSQL | 16+ | `winget install PostgreSQL.PostgreSQL` |
| Node.js | 24+ | `winget install OpenJS.NodeJS` |
| Git | qualquer | `winget install Git.Git` |
| IDE | — | Visual Studio 2022+ ou Rider |

## Variáveis de ambiente

Criar arquivo `.env` na raiz do projeto ou no Worker:

```env
# SMTP
SmtpHPassword=sua_senha_smtp_aqui
```

## Passo a passo

### 1. Clonar o repositório

```bash
git clone https://github.com/DanielVieiraFernandes/Health-Check-Monitor.git
cd Health-Check-Monitor
git checkout develop
```

### 2. Restaurar dependências

```bash
dotnet restore
```

### 3. Criar o banco de dados

```sql
CREATE DATABASE healthcheck;
```

Atualizar connection string em `HealthCheck.DbUp/appsettings.json` e `HealthCheck.Web/appsettings.Development.json`.

### 4. Inicializar o banco

```bash
cd HealthCheck.DbUp
dotnet run
```

Isso cria as extensões (`pg_trgm`, `unaccent`), tabelas (`users`, `monitored_systems`, `system_checks`, `worker_config`) e o usuário admin (`admin@gmail.com` / `123456`).

### 5. Build

```bash
dotnet build
```

### 6. Rodar a aplicação Web

```bash
cd HealthCheck.Web
dotnet run --urls http://localhost:5000
```

Acessar: `http://localhost:5000`

### 7. Rodar o Worker

```bash
cd HealthCheck.Worker
dotnet run
```

### 8. Rodar os testes

```bash
dotnet test HealthCheck.Worker.Tests
```

## Estrutura de pastas esperada

```
C:\Desenvolvimento\Aplicacoes\HealthCheck\
├── HealthCheck.Web\                 # Aplicação Blazor Server
│   ├── Components\                  # Páginas e componentes Razor
│   ├── wwwroot\                     # CSS, JS, assets estáticos
│   └── Services\                    # Auth, UserSession
├── HealthCheck.Worker\              # Worker Service
│   ├── Services\                    # Monitoring, Notification, Checkers
│   └── DependencyInjection.cs       # DI do Worker
├── HealthCheck.Framework\           # Código compartilhado
│   ├── Models\                      # User, MonitoredSystem, SystemCheck, WorkerConfig
│   ├── Enums\                       # SystemType, HealthStatus
│   ├── Services\                    # Email, Cryptography, Database
│   ├── Repositories\                # Dapper repositories
│   └── Helpers\                     # QueryBuilder, extensões
├── HealthCheck.DbUp\                # Inicialização do banco
│   └── Services\DatabaseService.cs  # Criação de tabelas
├── HealthCheck.Worker.Tests\        # Testes unitários
├── docs\                            # Documentação
└── scripts\                         # Scripts auxiliares
```
