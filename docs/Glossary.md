# Glossário

Terminologia do domínio HealthCheck Monitor.

## Entidades

| Termo | Definição |
|---|---|
| **MonitoredSystem** | Um endpoint HTTP registrado para monitoramento. Possui nome, URL, tipo e status atual. |
| **SystemCheck** | Registro individual de uma checagem realizada. Contém status, latência, resposta e detalhes de erro. |
| **User** | Usuário autenticado do sistema. Responsável por cadastrar e gerenciar sistemas monitorados. |
| **WorkerConfig** | Configuração global do serviço de monitoramento: intervalo, timeout, concorrência, retentativas. |

## Tipos de Sistema

| Termo | Definição |
|---|---|
| **WebApi** | Sistema do tipo API REST/HTTP. Verificado por requisição GET e validação de status code. |
| **Frontend** | Sistema do tipo aplicação web (HTML). Verificado por requisição GET, status code e conteúdo do body. |
| **SystemType** | Enum que classifica o tipo de sistema (WebApi=1, Frontend=2). |

## Status de Saúde

| Termo | Definição |
|---|---|
| **Healthy** | O sistema respondeu com sucesso (HTTP 2xx) dentro do timeout. |
| **Unhealthy** | O sistema respondeu com erro (HTTP 4xx/5xx) ou status inesperado. |
| **Unknown** | Não foi possível determinar o status: timeout, DNS falhou, URL bloqueada ou exceção. |
| **HealthStatus** | Enum que classifica o status de saúde (Healthy=1, Unhealthy=2, Unknown=3, All=4). |

## Notificações

| Termo | Definição |
|---|---|
| **alertKey** | Chave única que identifica um tipo de alerta. Usada para controle de cooldown. |
| **Cooldown** | Período mínimo (5 minutos) entre envios do mesmo alertKey. Evita spam. |
| **AlertEmail** | Email do administrador que recebe alertas críticos do sistema. Configurado no appsettings. |
| **NotificationService** | Serviço responsável por orquestrar o envio de alertas por email. |
| **EmailService** | Serviço de envio de emails via SMTP (MailKit) com retry e criptografia. |

## Worker

| Termo | Definição |
|---|---|
| **ISystemChecker** | Interface para checkers de sistema (Strategy pattern). Cada implementação sabe verificar um SystemType. |
| **CheckResult** | Resultado de uma checagem: Status, LatencyMs, Response, ErrorMessage, ExceptionType, StackTrace. |
| **SSRF** | Server-Side Request Forgery. O worker bloqueia URLs internas/privadas para prevenir esse ataque. |
| **ExecuteMonitoring** | Método principal do worker que busca sistemas pendentes e executa as checagens. |
| **ExecuteDBCleanup** | Método que remove SystemChecks com mais de 7 dias para manter o banco enxuto. |

## Banco de Dados

| Termo | Definição |
|---|---|
| **DbUp** | Projeto utilitário para inicialização do banco (tabelas + seed data). |
| **uuidv7** | Tipo de UUID ordenado temporalmente, usado como PK nas tabelas principais. |
| **pg_trgm** | Extensão PostgreSQL para busca de similaridade de texto (trigramas). |
| **unaccent** | Extensão PostgreSQL para remoção de acentos em buscas de texto. |

## Padrões e Práticas

| Termo | Definição |
|---|---|
| **Dapper** | Micro ORM usado para acesso a dados. Executa SQL puro com parâmetros nomeados. |
| **Result\<T\>** | Padrão de retorno que encapsula sucesso ou falha, evitando exceções para controle de fluxo. |
| **Strategy Pattern** | Padrão usado nos checkers: cada `ISystemChecker` sabe verificar um tipo de sistema. |
| **AES-GCM** | Algoritmo de criptografia usado para proteger credenciais SMTP. |
| **Conventional Commits** | Padrão de mensagens de commit: `tipo(escopo): descrição`. Commits em português. |
