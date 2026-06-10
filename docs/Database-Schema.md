# Banco de Dados

## Visão geral

PostgreSQL como banco relacional. Acesso via **Dapper** com SQL puro e parâmetros nomeados. Inicialização via projeto `HealthCheck.DbUp`.

- **Host**: `localhost:5432`
- **Database**: `healthcheck`
- **ORM**: Dapper (micro ORM)
- **Migrations**: Scripts SQL manuais + `DatabaseService.cs` (StringBuilder)
- **Extensões**: `pg_trgm`, `unaccent`

## Diagrama Entidade-Relacionamento (ER)

```
┌──────────────────────┐
│       users          │
├──────────────────────┤
│ PK id: UUID (uuidv7) │
│    name: VARCHAR(255)│
│ UK email: VARCHAR(255)│
│    password: VARCHAR  │
│    history: TEXT      │
│    created_at: TIMESTP│
│    updated_at: TIMESTP│
└──────┬───────────────┘
       │ 1
       │
       │ N
┌──────┴──────────────────────────┐
│       monitored_systems         │
├─────────────────────────────────┤
│ PK id: UUID (uuidv7)            │
│ FK user_id → users(id)          │
│    name: VARCHAR(255)            │
│    description: TEXT             │
│    url: TEXT                     │
│    system_type: INT (1=WebApi, 2=Frontend) │
│    expected_http_status: INT?    │
│    expected_body_text: TEXT?     │
│    last_status: INT              │
│    last_checked_at: TIMESTP?     │
│    history: TEXT                 │
│    created_at: TIMESTP           │
│    updated_at: TIMESTP           │
│ UK (user_id, url)                │
└──────┬───────────────────────────┘
       │ 1
       │
       │ N
┌──────┴──────────────────────────┐
│       system_checks             │
├─────────────────────────────────┤
│ PK id: BIGSERIAL                │
│ FK user_id → users(id)          │
│ FK system_id → monitored_systems│
│    status: INT                   │
│    latency_ms: BIGINT            │
│    checked_at: TIMESTP           │
│    system_response: TEXT?        │
│    error_message: TEXT?          │
│    exception_type: VARCHAR(150)? │
│    stack_trace: TEXT?            │
└─────────────────────────────────┘

┌──────────────────────┐
│    worker_config     │
├──────────────────────┤
│ PK id: SMALLINT (1)  │
│    monitoring_interval│
│    timeout_seconds    │
│    max_concurrent     │
│    max_retries        │
│    delay_between_retry│
│    updated_at: TIMESTP│
│ FK user_uuid_last_mod │
│    → users(id)        │
└──────────────────────┘
```

## Tabelas

### users

Usuários autenticados do sistema.

| Coluna | Tipo | Constraints | Descrição |
|---|---|---|---|
| `id` | UUID | PK, uuidv7 | Identificador único |
| `name` | VARCHAR(255) | NOT NULL | Nome do usuário |
| `email` | VARCHAR(255) | UNIQUE, NOT NULL | Email (usado para login e alertas) |
| `password` | VARCHAR(255) | NOT NULL | Hash bcrypt da senha |
| `history` | TEXT | NOT NULL, DEFAULT '' | Histórico de alterações |
| `created_at` | TIMESTAMP | NOT NULL, DEFAULT NOW() | Data de criação |
| `updated_at` | TIMESTAMP | NOT NULL, DEFAULT NOW() | Data de atualização |

### monitored_systems

Sistemas registrados para monitoramento.

| Coluna | Tipo | Constraints | Descrição |
|---|---|---|---|
| `id` | UUID | PK, uuidv7 | Identificador único |
| `user_id` | UUID | FK → users(id), ON DELETE CASCADE | Dono do sistema |
| `name` | VARCHAR(255) | NOT NULL | Nome do sistema |
| `description` | TEXT | NOT NULL, DEFAULT '' | Descrição |
| `url` | TEXT | NOT NULL | URL monitorada |
| `system_type` | INT | NOT NULL, DEFAULT 1 | 1=WebApi, 2=Frontend |
| `expected_http_status` | INT | NULL | Status HTTP esperado (null=default) |
| `expected_body_text` | TEXT | NULL | Texto esperado no body (Frontend) |
| `last_status` | INT | NOT NULL, DEFAULT 1 | Último status (1=Healthy, 2=Unhealthy, 3=Unknown) |
| `last_checked_at` | TIMESTAMP | NULL | Data da última checagem |
| `history` | TEXT | NOT NULL, DEFAULT '' | Histórico de alterações |
| `created_at` | TIMESTAMP | NOT NULL, DEFAULT NOW() | Data de criação |
| `updated_at` | TIMESTAMP | NOT NULL, DEFAULT NOW() | Data de atualização |

**Unique constraint:** `(user_id, url)` — um usuário não pode cadastrar duas vezes a mesma URL.

### system_checks

Registros individuais de checagens.

| Coluna | Tipo | Constraints | Descrição |
|---|---|---|---|
| `id` | BIGSERIAL | PK | Identificador sequencial (performance) |
| `user_id` | UUID | FK → users(id) | Dono do sistema |
| `system_id` | UUID | FK → monitored_systems(id) | Sistema verificado |
| `status` | INT | NOT NULL | Status da checagem |
| `latency_ms` | BIGINT | NOT NULL | Latência em milissegundos |
| `checked_at` | TIMESTAMP | NOT NULL, DEFAULT NOW() | Momento da checagem |
| `system_response` | TEXT | NULL | Resposta do sistema (sucesso) |
| `error_message` | TEXT | NULL | Mensagem de erro (falha) |
| `exception_type` | VARCHAR(150) | NULL | Tipo da exceção |
| `stack_trace` | TEXT | NULL | Stack trace da exceção |

**Limpeza:** registros com mais de 7 dias são removidos periodicamente.

### worker_config

Configuração global do Worker. Tabela de registro único.

| Coluna | Tipo | Constraints | Descrição |
|---|---|---|---|
| `id` | SMALLINT | PK, CHECK=1 | Sempre 1 (single-row) |
| `monitoring_interval_seconds` | SMALLINT | NOT NULL, DEFAULT 30 | Intervalo entre ciclos |
| `timeout_seconds` | SMALLINT | NOT NULL, DEFAULT 10 | Timeout por checagem |
| `max_concurrent_checks` | SMALLINT | NOT NULL, DEFAULT 10 | Máximo paralelo |
| `max_retries` | SMALLINT | NOT NULL, DEFAULT 0 | Retentativas |
| `delay_between_retries_ms` | SMALLINT | NOT NULL, DEFAULT 0 | Delay entre retentativas |
| `updated_at` | TIMESTAMP | NOT NULL, DEFAULT NOW() | Última atualização |
| `user_uuid_last_modified` | UUID | FK → users(id) | Quem alterou por último |

## Extensões

| Extensão | Propósito |
|---|---|
| `pg_trgm` | Busca de similaridade textual com trigramas |
| `unaccent` | Remoção de acentos em buscas de texto |

## Inicialização

O projeto `HealthCheck.DbUp` inicializa o banco na ordem:

1. Cria extensões (`pg_trgm`, `unaccent`)
2. Cria tabela `users`
3. Cria usuário admin (`admin@gmail.com` / `123456` com bcrypt)
4. Cria tabela `monitored_systems`
5. Cria tabela `system_checks`
6. Cria tabela `worker_config` + insere valores padrão
