# HealthCheck Monitor — Documentação

Bem-vindo à documentação completa do **HealthCheck Monitor**, um sistema de monitoramento de disponibilidade para serviços Web API e Front-end.

## Navegação

### Domínio
- [Visão Geral do Domínio](Domain-Overview) — conceitos, personas e regras de negócio
- [Tipos de Sistema](System-Types) — Web API, Front-end e como cada um é verificado
- [Ciclo de Vida dos Status](Health-Status-Lifecycle) — máquina de estados Healthy / Unhealthy / Unknown
- [Regras de Notificação](Notification-Rules) — quando e como os alertas são disparados
- [Glossário](Glossary) — terminologia do domínio

### Arquitetura
- [Visão Geral da Arquitetura](Architecture-Overview) — C4 Model (Contexto + Container)
- [Aplicação Web](Web-Application) — Blazor Server, componentes, autenticação
- [Serviço Worker](Worker-Service) — monitoramento em background, checkers, notificações
- [Banco de Dados](Database-Schema) — diagrama ER, tabelas, extensões

### Decisões de Arquitetura (ADR)
- [ADR 001 — Dapper sobre Entity Framework](ADR-001-Dapper)
- [ADR 002 — Blazor Server sobre Blazor WASM](ADR-002-Blazor-Server)
- [ADR 003 — Strategy Pattern nos Checkers](ADR-003-Strategy-Pattern)
- [ADR 004 — AES-GCM para Credenciais SMTP](ADR-004-AES-GCM)

### Desenvolvimento
- [Setup do Ambiente](Development-Setup) — ferramentas, dependências, primeiro build
- [Estrutura do Projeto](Project-Structure) — organização da solution e responsabilidades
- [Convenções de Código](Coding-Conventions) — Dapper, FluentValidation, MudBlazor, commits
- [Estratégia de Testes](Testing) — xUnit, TDD, cobertura

---

## Stack

| Camada | Tecnologia |
|---|---|
| Runtime | .NET 10 |
| Frontend | Blazor Server + MudBlazor + Syncfusion |
| Background | Worker Service |
| Banco | PostgreSQL + Dapper |
| Validação | FluentValidation |
| Autenticação | ASP.NET Core Cookies |
| Email | MailKit + AES-GCM |
| Testes | xUnit + Moq |

## Projetos

| Projeto | Responsabilidade |
|---|---|
| `HealthCheck.Web` | Interface Blazor Server (porta 5000) |
| `HealthCheck.Worker` | Serviço de monitoramento em background |
| `HealthCheck.Framework` | Modelos, enums, serviços compartilhados |
| `HealthCheck.DbUp` | Inicialização e migração do banco |
