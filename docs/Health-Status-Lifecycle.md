# Ciclo de Vida dos Status

Cada sistema monitorado tem um `HealthStatus` que evolui conforme os resultados das checagens.

## Máquina de estados

```
                    ┌──────────────┐
         ┌─────────→│   HEALTHY    │←─────────┐
         │          │  ("Saudável") │          │
         │          └──────┬───────┘          │
         │                 │                  │
         │     HTTP 4xx/5xx│                  │
         │      ou timeout │                  │
         │                 ↓                  │
         │          ┌──────────────┐    HTTP  │
         │          │  UNHEALTHY   │    2xx   │
         │          │("Não saudável")│─────────┘
         │          └──────────────┘
         │
         │          ┌──────────────┐
         └──────────│   UNKNOWN    │
           HTTP 2xx │("Desconhecido")│
                    └──────────────┘
                        ↑       ↑
                        │       │
                   timeout    exceção
                   DNS fail   URL bloqueada
```

## Estados

### Healthy (1) — "Saudável"

O sistema respondeu corretamente à checagem.

**Critérios:**
- HTTP 2xx (ou `ExpectedHttpStatus` customizado)
- Dentro do timeout
- (Front-end) Body contém `ExpectedBodyText`, se configurado

**Transições possíveis:**
- → **Unhealthy**: próximo check retorna erro HTTP (4xx/5xx)
- → **Unknown**: próximo check sofre timeout, DNS falha ou exceção

### Unhealthy (2) — "Não saudável"

O sistema respondeu, mas com erro.

**Critérios:**
- HTTP 4xx ou 5xx (ou status diferente do esperado)

**Transições possíveis:**
- → **Healthy**: próximo check retorna 2xx
- → **Unknown**: próximo check sofre timeout ou exceção

### Unknown (3) — "Desconhecido"

Não foi possível determinar o status do sistema.

**Critérios:**
- Timeout na requisição
- Falha de DNS
- URL bloqueada pela validação de segurança (SSRF)
- Qualquer exceção não tratada durante a checagem

**Transições possíveis:**
- → **Healthy**: próximo check retorna 2xx
- → **Unhealthy**: próximo check retorna erro HTTP

### All (4)

Valor especial usado apenas em filtros de busca na UI. Representa "todos os status". Não é atribuído a sistemas.

## Gatilho de notificação

A notificação é disparada quando:
```
Status anterior != Status atual
  E
Status atual == Unhealthy OU Status atual == Unknown
```

Ou seja: notifica na **transição** para um estado ruim, não em toda checagem.

Se o sistema já estava Unhealthy e continua Unhealthy, **não** notifica novamente (o cooldown de 5 minutos também previne spam).

## Estado inicial

Sistemas recém-cadastrados começam com `LastStatus = Unknown` até a primeira checagem.
