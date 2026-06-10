# Visão Geral do Domínio

## O que o HealthCheck Monitor faz?

O HealthCheck Monitor é um sistema de **observabilidade operacional** que registra serviços e verifica sua disponibilidade em intervalos configuráveis. Ele responde a uma pergunta fundamental: *"Meus sistemas estão no ar?"*

## Problema resolvido

Em vez de descobrir que um sistema caiu porque um cliente reclamou, o HealthCheck:
1. **Verifica proativamente** endpoints HTTP em intervalos regulares
2. **Classifica** cada sistema como Saudável, Não Saudável ou Desconhecido
3. **Notifica** os responsáveis quando algo sai do ar
4. **Audita** todo o histórico de checagens para análise posterior

## Personas

| Persona | Descrição | Funcionalidades principais |
|---|---|---|
| **Administrador** | Responsável pela operação dos sistemas | Dashboard, configuração do worker, cadastro de sistemas |
| **Usuário monitorado** | Dono de um sistema específico | Recebe alertas quando seu sistema muda de status |

## Entidades principais

```
User (1) ──── (N) MonitoredSystem ──── (N) SystemCheck
                         │
                    SystemType (WebApi | Frontend)
                    HealthStatus (Healthy | Unhealthy | Unknown)
```

- **User**: Quem cadastra e monitora sistemas. Autenticado por email/senha com cookies.
- **MonitoredSystem**: Um endpoint HTTP a ser monitorado. Tem nome, URL, tipo (Web API ou Front-end), status atual e parâmetros de verificação.
- **SystemCheck**: Registro individual de uma checagem. Contém status, latência, resposta e detalhes de erro.
- **WorkerConfig**: Configuração global do worker — intervalo, timeout, concorrência, retentativas.

## Fluxo principal

```
1. Admin cadastra sistema (URL + tipo)
2. Worker busca sistemas pendentes a cada N segundos
3. Worker executa checagem HTTP (GET)
4. Worker classifica status (Healthy/Unhealthy/Unknown)
5. Worker persiste SystemCheck no banco
6. Se status mudou para ruim → NotificationService envia alerta
7. Dashboard reflete status atualizado
8. Worker limpa registros antigos (>7 dias)
```

## Regras de negócio

### Verificação de sistemas
- Cada sistema é checado conforme seu `SystemType`: Web API usa um checker, Front-end usa outro
- URLs internas/privadas são bloqueadas (proteção SSRF)
- O worker processa sistemas em paralelo, com limite configurável de concorrência
- Timeout por checagem é configurável (padrão 10s)

### Status
- `Healthy`: resposta HTTP 2xx dentro do timeout
- `Unhealthy`: resposta HTTP com erro (4xx/5xx)
- `Unknown`: timeout, DNS falhou, URL bloqueada, ou qualquer exceção

### Notificações
- Um alerta é disparado quando o status muda de Healthy → Unhealthy/Unknown
- Cooldown de 5 minutos por alerta para evitar spam
- Admin recebe alertas de falha do próprio worker
- Dono do sistema recebe alertas de mudança de status do seu sistema

### Limpeza
- SystemChecks com mais de 7 dias são removidos periodicamente
- Mantém o banco enxuto para performance das consultas do dashboard
