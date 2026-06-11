# Serviço Worker

Serviço Windows em background responsável pelo monitoramento contínuo dos sistemas cadastrados.

- **Projeto**: `HealthCheck.Worker`
- **Tipo**: `BackgroundService` (.NET Hosted Service)
- **Nome do serviço Windows**: `SHC` (System Health Checker)

## Ciclo de execução

```
┌─────────────────────────────────────────────┐
│                 Worker Loop                  │
│                                              │
│  ┌──────────────────────┐                    │
│  │ RefreshConfigIfNeeded │ ← a cada 1 min    │
│  │ (banco ou fallback)  │                    │
│  └─────────┬────────────┘                    │
│            ↓                                  │
│  ┌──────────────────────┐                    │
│  │ ExecuteMonitoring     │ ← em paralelo     │
│  │ ┌──────────────────┐ │                    │
│  │ │ ISystemChecker   │ │ ← Strategy Pattern │
│  │ │ - WebApiChecker  │ │                    │
│  │ │ - FrontendChecker│ │                    │
│  │ └──────────────────┘ │                    │
│  └─────────┬────────────┘                    │
│            ↓                                  │
│  ┌──────────────────────┐                    │
│  │ ExecuteDBCleanup      │ ← a cada 7 dias   │
│  │ (limpa checks antigos)│                    │
│  └─────────┬────────────┘                    │
│            ↓                                  │
│  ┌──────────────────────┐                    │
│  │ await Task.Delay(N s) │ ← intervalo       │
│  │ (MonitoringInterval)  │    configurável    │
│  └──────────────────────┘                    │
└─────────────────────────────────────────────┘
```

## MonitoringServices

Serviço principal de monitoramento. Executado como Singleton.

### ExecuteMonitoring

1. Busca sistemas pendentes do banco (`GetPendingMonitoredSystemsAsync`)
2. Para cada sistema, em paralelo (limitado por `MaxConcurrentChecks`):
   - Valida a URL (proteção SSRF)
   - Seleciona o `ISystemChecker` adequado pelo `SystemType`
   - Executa `CheckAsync` com timeout configurável
   - Se status mudou para Unhealthy/Unknown → notifica dono
   - Persiste `SystemCheck` + atualiza status no banco
3. Em caso de falha geral → notifica admin

### ExecuteDBCleanup

Executado a cada 7 dias. Remove `SystemCheck` com mais de 7 dias via `SystemChecksService.CleanOldChecks()`.

## Strategy Pattern: ISystemChecker

```
ISystemChecker (interface)
├── SupportedType: SystemType
└── CheckAsync(MonitoredSystem, CancellationToken) → CheckResult

CheckResult
├── Status: HealthStatus
├── LatencyMs: long
├── Response: string?
├── ErrorMessage: string?
├── ExceptionType: string?
└── StackTrace: string?
```

### WebApiSystemChecker
- Faz HTTP GET
- Avalia status code contra `ExpectedHttpStatus` (default: 2xx)
- Não verifica conteúdo do body

### FrontendSystemChecker
- Faz HTTP GET
- Avalia status code contra `ExpectedHttpStatus` (default: 2xx)
- Se `ExpectedBodyText` configurado, verifica se o body contém o texto

## NotificationService

Serviço de notificação. Singleton.

- `NotifyAdminAlertAsync`: envia para o email admin (`AlertEmail`)
- `NotifyUserAlertAsync`: resolve email do usuário no banco e envia
- Cooldown: 5 minutos por `alertKey`
- Usa `EmailService` para envio via SMTP

## Configuração dinâmica

O Worker recarrega sua configuração a cada 1 minuto:

1. Tenta carregar do banco (`WorkerConfigService.Get()`)
2. Se falhar, tenta fallback do `appsettings.json`
3. Se fallback funcionar, notifica admin que está em modo fallback
4. Se tudo falhar, aguarda 10s e tenta novamente
5. Após 3 falhas consecutivas, interrompe o Worker

## Parâmetros de configuração

| Parâmetro | Padrão | Range | Descrição |
|---|---|---|---|
| `MonitoringIntervalSeconds` | 30 | 15-30 | Intervalo entre ciclos |
| `TimeoutSeconds` | 10 | 5-30 | Timeout por checagem HTTP |
| `MaxConcurrentChecks` | 10 | 5-20 | Máximo de checagens paralelas |
| `MaxRetries` | 0 | 0-5 | Retentativas por sistema |
| `DelayBetweenRetriesMs` | 0 | 0-5000 | Delay entre retentativas |
