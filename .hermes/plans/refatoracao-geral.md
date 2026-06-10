# Plano de Refatoração — HealthCheck

> **Para o Hermes:** Usar `subagent-driven-development` para implementar cada item.

**Branch base:** `develop`  
**Branch de trabalho:** `refactor/plano-geral`  
**Início:** 10/06/2026

---

## Itens pendentes

### 1. Logging centralizado — Serilog + Seq

**Status:** ✅ Concluído — branch `feature/serilog-seq`, commit `dbe9aee`

**Objetivo:** Unificar logs do Worker e Web num dashboard com busca e filtros.

**O que fazer:**
- Subir container Seq: `docker run -d --name seq -p 5341:80 -e ACCEPT_EULA=Y datalust/seq`
- Adicionar pacote `Serilog.Sinks.Seq` no Worker e Web
- Configurar sink Seq no `appsettings.json` (manter file sink como fallback)
- Remover `RecordLog.cs` custom da Web? (avaliar — pode manter como fallback)

**Arquivos:**
- `HealthCheck.Worker/appsettings.json`
- `HealthCheck.Web/appsettings.json`
- `HealthCheck.Worker/Program.cs`
- `HealthCheck.Web/Program.cs`

---

### 2. Auditoria de mudanças — tabela `audit_log`

**Status:** ⬜ Pendente

**Objetivo:** Substituir campo `history TEXT` por tabela estruturada e consultável.

**O que fazer:**
- Criar migration SQL + `DatabaseService.CreateAuditLogTable()` no DbUp
- Criar modelo `AuditEntry` no Framework
- Criar `AuditRepository` com Dapper
- Alterar `MonitoredSystemService.UpdateAsync()` — comparar old/new e logar
- Alterar `MonitoredSystemService.CreateAsync()` — logar criação
- Alterar `MonitoredSystemService.DeleteAsync()` — logar exclusão
- Remover campo `history` do modelo `MonitoredSystem` e da tabela (ALTER TABLE DROP)
- Limpar trechos de código que concatenam string no `history`

**Schema:**
```sql
CREATE TABLE audit_log (
    id BIGSERIAL PRIMARY KEY,
    entity_type VARCHAR(50) NOT NULL,
    entity_id UUID NOT NULL,
    user_id UUID NOT NULL REFERENCES users(id),
    action VARCHAR(10) NOT NULL,      -- CREATE, UPDATE, DELETE
    changes JSONB NOT NULL DEFAULT '{}',
    changed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_audit_entity ON audit_log(entity_type, entity_id);
CREATE INDEX idx_audit_date ON audit_log(changed_at);
```

**Arquivos:**
- `HealthCheck.DbUp/Services/DatabaseService.cs`
- `HealthCheck.DbUp/Scripts/S00X_audit_log.sql`
- `HealthCheck.Framework/Models/AuditEntry.cs` (novo)
- `HealthCheck.Framework/Repositories/AuditRepository/` (novo)
- `HealthCheck.Framework/Services/Database/MonitoredSystemService/MonitoredSystemService.cs`
- `HealthCheck.Framework/Models/MonitoredSystem.cs`
- Migration SQL para ALTER TABLE DROP history

**Não auditar:**
- `users` (quase nunca muda)
- `worker_config` (registro único, baixa criticidade)
- `system_checks` (imutável, só INSERT + DELETE periódico)

---

### 3. 

---

## Notas

- 
