# Regras de Notificação

O sistema de notificações alerta administradores e donos de sistemas quando eventos críticos ocorrem.

## Arquitetura de notificação

```
MonitoringServices ──→ NotificationService ──→ EmailService ──→ SMTP (Hostinger)
       │                       │                      │
       │                 ┌─────┴──────┐         ┌─────┴────────┐
       │                 │  Cooldown   │         │ Retry (3x)   │
       │                 │  (5 min)    │         │ Backoff 200ms│
       │                 └────────────┘         │ Exponencial   │
       │                                         └──────────────┘
       │
  ┌────┴─────────────────────────────────┐
  │ ISystemChecker → CheckResult         │
  │ (WebApiSystemChecker,                │
  │  FrontendSystemChecker)              │
  └──────────────────────────────────────┘
```

## Alertas implementados

### 1. Mudança de status do sistema

| Campo | Valor |
|---|---|
| **Gatilho** | Status muda de Healthy → Unhealthy/Unknown |
| **alertKey** | `status-change-{userId}-{systemId}-{status}` |
| **Destino** | Dono do sistema (email resolvido do banco) |
| **Severidade** | `Warning` (Unhealthy) / `Error` (Unknown) |
| **Cooldown** | 5 minutos por alertKey |

**Conteúdo do email:**
```
Assunto: [HC - Sistema de Monitoramento] Alerta do sistema {Nome}

O sistema monitorado "{Nome}" mudou para o status {Status}.
URL monitorada: {Url}
Horário (UTC): {timestamp}
```

### 2. Falha no processamento de URLs

| Campo | Valor |
|---|---|
| **Gatilho** | Exceção não tratada no `ExecuteMonitoring` |
| **alertKey** | `monitoring-execution-failed` |
| **Destino** | Admin (`AlertEmail` do appsettings) |
| **Severidade** | `Error` |

### 3. Falha na limpeza do banco

| Campo | Valor |
|---|---|
| **Gatilho** | Exceção no `ExecuteDBCleanup` |
| **alertKey** | `db-cleanup-failed` |
| **Destino** | Admin (`AlertEmail`) |
| **Severidade** | `Error` |

### 4. Worker interrompido por erro crítico

| Campo | Valor |
|---|---|
| **Gatilho** | Exceção crítica no loop principal do Worker |
| **alertKey** | `worker-critical-stop` |
| **Destino** | Admin (`AlertEmail`) |
| **Severidade** | `Critical` |

### 5. Configuração do Worker indisponível

| Campo | Valor |
|---|---|
| **Gatilho** | `_workerConfig == null` após tentar carregar |
| **alertKey** | `config-null` |
| **Destino** | Admin (`AlertEmail`) |
| **Severidade** | `Error` |

### 6. Worker em modo fallback de configuração

| Campo | Valor |
|---|---|
| **Gatilho** | Config carregada do appsettings (não do banco) |
| **alertKey** | `config-fallback` |
| **Destino** | Admin (`AlertEmail`) |
| **Severidade** | `Warning` |

## Mecanismo de cooldown

Cada alerta é identificado por uma `alertKey`. O `NotificationService` mantém um dicionário `ConcurrentDictionary<string, DateTime>` com a última vez que cada chave foi enviada.

- **Intervalo**: 5 minutos
- **Thread-safe**: `ConcurrentDictionary`
- **Bypass**: o parâmetro `bypassCooldown` permite ignorar o cooldown em emergências

## Fallback de resolução de email

Se o `NotificationService.NotifyUserAlertAsync` não conseguir resolver o email do usuário (ex: `UsersService` falha), o alerta é **silenciosamente ignorado** com um log de warning. O sistema não quebra — apenas registra que não foi possível notificar.

## Resiliência do EmailService

- **Retry**: 3 tentativas com backoff exponencial (200ms × attempt)
- **Conexão reutilizada**: `SmtpClient` mantido como campo, não recriado a cada envio
- **Thread-safe**: `SemaphoreSlim(1,1)` garante que apenas um email é enviado por vez
- **Erros transientes**: `SmtpCommandException`, `SmtpProtocolException`, `IOException`, `TimeoutException` disparam retry
- **Credenciais**: criptografadas com AES-GCM, descriptografadas uma única vez na construção do serviço
