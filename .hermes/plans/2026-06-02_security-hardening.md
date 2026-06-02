# Plano: Hardening de Segurança — Aplicação + VPS

**Branch:** `fix/responsive-layout` (aproveitar branch atual)  
**Data:** 2026-06-02  
**Objetivo:** Blindar aplicação e VPS contra ataques, invasões e travamentos.

---

## Diagnóstico

| # | Problema | Risco | Camada |
|---|----------|-------|--------|
| 1 | UFW desativado | Todas as portas expostas | VPS |
| 2 | Sem fail2ban | Brute force SSH sem proteção | VPS |
| 3 | Sem Nginx/rate limit | Sem proteção contra DDoS e request flood | VPS |
| 4 | App sem rate limiting | Endpoints vulneráveis a abuso | Aplicação |
| 5 | Sem security headers | XSS, clickjacking, MIME sniffing | Aplicação |
| 6 | rp_filter em loose mode | Spoofing de IP possível | VPS |
| 7 | PostgreSQL em localhost apenas | ✅ OK | VPS |
| 8 | Kestrel sem limites configurados | Pode travar com muitas conexões | Aplicação |

---

## Parte 1: VPS (Infra — Hermes executa direto)

### 1.1 UFW Firewall
```bash
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp     # SSH
ufw allow 5000/tcp   # HealthCheck Web
ufw --force enable
```

### 1.2 Fail2ban (SSH brute force)
```bash
apt-get install -y fail2ban
# Configurar jail para SSH: max 3 tentativas, ban 10min
```

### 1.3 sysctl hardening
```bash
# Anti-spoofing
net.ipv4.conf.all.rp_filter = 1
net.ipv4.conf.default.rp_filter = 1
# SYN flood
net.ipv4.tcp_syncookies = 1
net.ipv4.tcp_max_syn_backlog = 2048
# Ignorar ICMP redirects
net.ipv4.conf.all.accept_redirects = 0
# Proteção contra time-wait assassination
net.ipv4.tcp_rfc1337 = 1
```

### 1.4 SSH hardening
- Desabilitar login root por senha (só chave)
- Porta SSH mantida em 22 (não vamos security by obscurity)

---

## Parte 2: Aplicação .NET (OpenCode implementa)

### 2.1 Rate Limiting (ASP.NET Core 10 built-in)
Arquivo: `HealthCheck.Web/Program.cs`

- Adicionar `builder.Services.AddRateLimiter()` com política fixa:
  - 30 requisições/minuto por IP para endpoints gerais
  - 5 requisições/minuto para `/auth/login` (anti brute-force)
- Adicionar `app.UseRateLimiter()` no pipeline

### 2.2 Security Headers Middleware
Arquivo: `HealthCheck.Web/Middleware/SecurityHeadersMiddleware.cs` (novo)

Headers:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Content-Security-Policy: default-src 'self'`
- Remover header `Server: Kestrel`

Registrar em `Program.cs`: `app.UseMiddleware<SecurityHeadersMiddleware>()`

### 2.3 Kestrel Limits
Arquivo: `HealthCheck.Web/Program.cs` ou `appsettings.json`

- `MaxConcurrentConnections`: 100
- `MaxConcurrentUpgradedConnections`: 100
- `MaxRequestBodySize`: 1MB
- `RequestHeadersTimeout`: 30s
- `KeepAliveTimeout`: 60s

### 2.4 Login anti brute-force
Arquivo: `HealthCheck.Web/Services/Authentication/AuthenticationService.cs`  
Arquivo: `HealthCheck.Web/Controllers/AuthController.cs`

- Adicionar lockout após 5 tentativas falhas (15 min)
- Registrar tentativas por IP no banco ou em memória

---

## Arquivos Afetados

### Aplicação (.NET)
1. `HealthCheck.Web/Program.cs` — Rate limiting + Kestrel limits + SecurityHeaders
2. `HealthCheck.Web/Middleware/SecurityHeadersMiddleware.cs` — NOVO
3. `HealthCheck.Web/Controllers/AuthController.cs` — Anti brute-force login
4. `HealthCheck.Web/Services/Authentication/AuthenticationService.cs` — Lockout logic
5. `HealthCheck.Web/appsettings.json` — Kestrel config

### VPS
1. `/etc/ufw/user.rules` — Firewall
2. `/etc/fail2ban/jail.local` — Fail2ban
3. `/etc/sysctl.d/99-security.conf` — Kernel hardening
4. `/etc/ssh/sshd_config` — SSH hardening

---

## Validação

- [ ] `dotnet build` sem erros
- [ ] UFW ativo com regras corretas
- [ ] Fail2ban rodando e monitorando SSH
- [ ] Rate limiting responde 429 após exceder limite
- [ ] Security headers presentes nas responses
- [ ] Login bloqueia após 5 tentativas falhas
- [ ] Serviço reinicia sem erros

---

## Risco

- **Médio**: Mudanças de infra no VPS (UFW, fail2ban) são seguras mas requerem cuidado para não se trancar fora do SSH
- Alterações no pipeline HTTP podem quebrar funcionalidade — testar após deploy
