# Estrutura do Projeto

## Solution

A solution contém 5 projetos com responsabilidades bem definidas.

```
HealthCheck/
├── HealthCheck.Web/              # 🌐 Interface Blazor Server
├── HealthCheck.Worker/           # ⚙️ Serviço de monitoramento background
├── HealthCheck.Framework/        # 🧩 Código compartilhado
├── HealthCheck.DbUp/             # 🛠️ Inicialização do banco
└── HealthCheck.Worker.Tests/     # 🧪 Testes unitários
```

## HealthCheck.Web

Aplicação Blazor Server. Interface do usuário.

```
HealthCheck.Web/
├── Components/
│   ├── Layout/
│   │   └── MainLayout.razor          # Layout com drawer + tema
│   ├── Pages/
│   │   ├── Dashboard/                # Dashboard principal
│   │   ├── MonitoredSystems/         # CRUD de sistemas (cards + diálogo)
│   │   │   ├── MonitoredSystems.razor
│   │   │   ├── MonitoredSystems.razor.css
│   │   │   └── EditMonitoredSystem.razor
│   │   ├── Audit/                    # Auditoria de checagens
│   │   ├── Config/                   # Configuração do Worker
│   │   └── Login/                    # Autenticação
│   └── Shared/                       # Componentes reutilizáveis
├── Services/
│   └── Authentication/               # AuthService, UserSessionInfo
├── wwwroot/
│   └── app.css                       # CSS global (MudBlazor)
└── Program.cs                        # Entry point + DI
```

**Responsabilidades:**
- Renderizar UI com MudBlazor + Syncfusion
- Gerenciar autenticação (cookies)
- Chamar serviços do Framework para CRUD
- Exibir dashboard, auditoria e configurações

## HealthCheck.Worker

Serviço Windows em background. Monitoramento contínuo.

```
HealthCheck.Worker/
├── Services/
│   ├── MonitoringServices.cs         # Orquestração do monitoramento
│   ├── NotificationService.cs        # Envio de alertas por email
│   └── SystemCheckers/
│       ├── ISystemChecker.cs         # Interface Strategy Pattern
│       ├── WebApiSystemChecker.cs    # Checker para Web API
│       └── FrontendSystemChecker.cs  # Checker para Front-end
├── Worker.cs                         # BackgroundService loop
├── DependencyInjection.cs            # Registro de serviços do Worker
└── Program.cs                        # Entry point + Serilog
```

**Responsabilidades:**
- Buscar sistemas pendentes do banco
- Executar checagens HTTP em paralelo
- Persistir resultados (SystemCheck)
- Notificar mudanças de status
- Limpar dados antigos (>7 dias)
- Recarregar configuração dinamicamente

## HealthCheck.Framework

Núcleo compartilhado. Usado por Web e Worker.

```
HealthCheck.Framework/
├── Enums/
│   ├── HealthStatus.cs               # Healthy, Unhealthy, Unknown, All
│   ├── SystemType.cs                 # WebApi, Frontend
│   └── LatencyPreference.cs          # Preferência de latência
├── Models/
│   ├── User.cs                       # Entidade usuário
│   ├── MonitoredSystem.cs            # Entidade sistema monitorado
│   ├── SystemCheck.cs                # Registro de checagem
│   └── WorkerConfig.cs               # Configuração do Worker
├── Services/
│   ├── Cryptography/
│   │   ├── BcryptPasswordEncrypter.cs # Hash de senhas
│   │   ├── ISMTPCredentialProvider.cs # Interface criptografia SMTP
│   │   └── SMTPCredentialProvider.cs  # AES-GCM para credenciais
│   ├── Database/
│   │   ├── MonitoredSystemService/    # CRUD de sistemas
│   │   ├── SystemChecksService/       # CRUD de checagens + limpeza
│   │   ├── UsersService/              # CRUD de usuários + auth
│   │   ├── WorkerConfigService/       # CRUD de config do Worker
│   │   └── DatabaseService.cs         # Gerenciamento de conexão
│   ├── Email/
│   │   ├── EmailService.cs            # Envio SMTP via MailKit
│   │   └── Models/                    # EmailBody, EmailCredentials, EmailAttachment
│   └── DependencyInjection.cs         # Registro de serviços do Framework
├── Repositories/
│   ├── MonitoredSystemRepository/     # Queries Dapper
│   ├── SystemChecksRepository/        # Queries Dapper
│   └── UsersRepository/               # Queries Dapper
└── Helpers/
    └── QueryBuilder.cs                # Builder de queries dinâmicas
```

**Responsabilidades:**
- Modelos de domínio e enums
- Acesso a dados (Dapper + PostgreSQL)
- Validação (FluentValidation)
- Criptografia (bcrypt, AES-GCM)
- Envio de email (MailKit)
- Padrão Result<T> para controle de fluxo

## HealthCheck.DbUp

Utilitário de inicialização do banco.

```
HealthCheck.DbUp/
└── Services/
    └── DatabaseService.cs             # CREATE TABLE via StringBuilder
```

**Responsabilidades:**
- Criar extensões PostgreSQL
- Criar tabelas (users, monitored_systems, system_checks, worker_config)
- Inserir dados seed (usuário admin)

## HealthCheck.Worker.Tests

Testes unitários com xUnit.

```
HealthCheck.Worker.Tests/
├── Utils/
│   └── ResultTests.cs
├── Models/
│   └── WorkerConfigTests.cs
├── Validators/
│   ├── CreateMonitoredSystemValidatorTests.cs
│   └── WorkerConfigValidatorTests.cs
├── Cryptography/
│   └── BcryptPasswordEncrypterTests.cs
└── Worker/
    └── GetExceptionNameTests.cs
```
