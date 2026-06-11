# Visão Geral da Arquitetura

Documentação arquitetural usando o modelo **C4** (Contexto → Container → Componente → Código).

## Nível 1: Diagrama de Contexto

```
┌──────────────────────────────────────────────────────────────────┐
│                        HealthCheck Monitor                       │
│                                                                  │
│  ┌────────────┐     ┌──────────────┐     ┌───────────────────┐  │
│  │ Administrador│────→│  Aplicação   │────→│  Sistemas         │  │
│  │              │     │  Web         │     │  Monitorados      │  │
│  └────────────┘     │  (Blazor)     │     │  (HTTP endpoints) │  │
│                      └──────┬───────┘     └───────────────────┘  │
│                             │                                     │
│  ┌────────────┐     ┌───────┴──────┐     ┌───────────────────┐  │
│  │ Usuário     │←────│   Worker     │────→│  SMTP (Hostinger)  │  │
│  │ (email)    │     │   (Background│     │  Envio de alertas │  │
│  └────────────┘     │    Service)  │     └───────────────────┘  │
│                      └──────┬───────┘                            │
│                             │                                     │
│                      ┌──────┴───────┐                            │
│                      │  PostgreSQL   │                            │
│                      │  (Banco)     │                            │
│                      └──────────────┘                            │
└──────────────────────────────────────────────────────────────────┘
```

**Atores externos:**
- **Administrador**: interage com a UI Blazor Server para cadastrar sistemas e visualizar dashboards
- **Usuário (dono do sistema)**: recebe alertas por email quando seus sistemas ficam indisponíveis

**Sistemas externos:**
- **Sistemas Monitorados**: endpoints HTTP que o Worker verifica periodicamente
- **SMTP (Hostinger)**: servidor de email para envio de alertas
- **PostgreSQL**: banco de dados relacional

## Nível 2: Diagrama de Container

```
┌─────────────────────────────────────────────────────────────┐
│                    HealthCheck Monitor                       │
│                                                              │
│  ┌─────────────────────┐    ┌─────────────────────────┐    │
│  │  Web Application    │    │  Worker Service          │    │
│  │  (Blazor Server)    │    │  (Background Service)    │    │
│  │                     │    │                          │    │
│  │  Porta: 5000        │    │  Processo: SHC           │    │
│  │  .NET 10            │    │  .NET 10                │    │
│  │                     │    │                          │    │
│  │  ┌───────────────┐  │    │  ┌────────────────────┐  │    │
│  │  │ Páginas       │  │    │  │ MonitoringServices │  │    │
│  │  │ - Dashboard   │  │    │  │ - ExecuteMonitoring│  │    │
│  │  │ - Sistemas    │  │    │  │ - ExecuteDBCleanup │  │    │
│  │  │ - Auditoria   │  │    │  └────────┬───────────┘  │    │
│  │  │ - Config      │  │    │           │              │    │
│  │  └───────────────┘  │    │  ┌────────┴───────────┐  │    │
│  │                     │    │  │ ISystemChecker     │  │    │
│  │  ┌───────────────┐  │    │  │ - WebApiChecker    │  │    │
│  │  │ Auth          │  │    │  │ - FrontendChecker  │  │    │
│  │  │ (Cookies)     │  │    │  └────────────────────┘  │    │
│  │  └───────────────┘  │    │                          │    │
│  │                     │    │  ┌────────────────────┐  │    │
│  │  ┌───────────────┐  │    │  │ NotificationSvc    │  │    │
│  │  │ MudBlazor     │  │    │  │ - NotifyAdmin      │  │    │
│  │  │ Syncfusion    │  │    │  │ - NotifyUser       │  │    │
│  │  └───────────────┘  │    │  └────────┬───────────┘  │    │
│  └─────────┬───────────┘    │           │              │    │
│            │                │  ┌────────┴───────────┐  │    │
│            │                │  │ EmailService       │  │    │
│            │                │  │ (MailKit + AES)    │  │    │
│            │                │  └────────────────────┘  │    │
│            │                └────────────┬─────────────┘    │
│            │                             │                   │
│            └──────────┬──────────────────┘                   │
│                       ↓                                      │
│              ┌────────────────┐                              │
│              │   PostgreSQL   │                              │
│              │                │                              │
│              │  Tabelas:      │                              │
│              │  - users       │                              │
│              │  - monitored_  │                              │
│              │    systems     │                              │
│              │  - system_     │                              │
│              │    checks     │                              │
│              │  - worker_     │                              │
│              │    config     │                              │
│              └────────────────┘                              │
│                                                              │
│  ┌────────────────────────────────────────────────────┐     │
│  │  Framework (Shared)                                │     │
│  │  - Models, Enums, Helpers                          │     │
│  │  - Services (Email, DB, Cryptography)              │     │
│  │  - Repositories (Dapper)                           │     │
│  │  - Validators (FluentValidation)                   │     │
│  └────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

## Comunicação entre containers

| De | Para | Protocolo | Detalhe |
|---|---|---|---|
| Web App | PostgreSQL | Npgsql (TCP 5432) | Dapper, queries parametrizadas |
| Worker | PostgreSQL | Npgsql (TCP 5432) | Dapper, queries parametrizadas |
| Worker | Sistemas Monitorados | HTTP/HTTPS | `HttpClient`, timeout configurável |
| Worker | SMTP (Hostinger) | SMTP (TCP 465) | MailKit, SSL, AES-GCM |

## Princípios arquiteturais

1. **Separação de responsabilidades**: Web (UI), Worker (background), Framework (compartilhado), DbUp (infra)
2. **Strategy Pattern**: checkers intercambiáveis por tipo de sistema
3. **Result Pattern**: `Result<T>` para controle de fluxo sem exceções
4. **Dapper sobre EF**: SQL puro para performance e controle
5. **Configuração dinâmica**: Worker recarrega config do banco periodicamente
6. **Resiliência**: retry, cooldown, fallback, credenciais criptografadas
