# ADR 004 — AES-GCM para Credenciais SMTP

- **Status**: Aceito
- **Data**: 2026-05-26
- **Decisão de**: Daniel Vieira Fernandes

## Contexto

O Worker precisa armazenar credenciais SMTP (email + senha) para envio de alertas. Armazenar em texto puro no `appsettings.json` ou no banco seria um risco de segurança.

## Decisão

**Criptografar credenciais SMTP com AES-GCM** (Advanced Encryption Standard — Galois/Counter Mode).

## Arquitetura

```
┌────────────────────┐     ┌──────────────────────┐
│  appsettings.json  │     │  SMTPCredentialProv. │
│                    │     │  (ISMTPCredentialProv)│
│  EmailSettings:    │     │                      │
│    SMTPSettings:   │     │  Decrypt(ciphertext) │
│      Host: ...     │────→│    → plaintext       │
│      Port: 465     │     │                      │
│      Email: AES... │     │  Encrypt(plaintext)  │
│      EnableSSL:tr  │     │    → ciphertext      │
└────────────────────┘     └──────────────────────┘
         │                           │
         │                           │
         ▼                           ▼
┌────────────────────┐     ┌──────────────────────┐
│  SmtpHPassword     │     │  EmailService        │
│  (env var / .env)  │     │                      │
│                    │     │  _email = Decrypt()  │
│  Senha em texto    │     │  _password = senha   │
│  puro (único       │     │                      │
│  secreto não       │     │  Descriptografa uma  │
│  criptografado     │     │  única vez na        │
│  no JSON)          │     │  construção          │
└────────────────────┘     └──────────────────────┘
```

## Por que AES-GCM?

1. **Autenticado**: GCM fornece tanto confidencialidade quanto integridade (detecta adulteração)
2. **Performance**: Acelerado por hardware (AES-NI) em CPUs modernas
3. **Padrão da indústria**: TLS 1.3, AWS KMS, Google Cloud KMS
4. **Nativo no .NET**: `System.Security.Cryptography.AesGcm`

## Separação de segredos

| Onde | O quê | Formato |
|---|---|---|
| `appsettings.json` | Email (Host, Porta, SSL) | Texto puro |
| `appsettings.json` | Email (usuário SMTP) | **Criptografado AES-GCM** |
| Variável de ambiente `.env` | Senha SMTP (`SmtpHPassword`) | Texto puro |

Apenas a senha fica fora do JSON versionado porque é o segredo mais sensível e muda com frequência. O email criptografado pode ser versionado com segurança.

## Consequências

### Positivas
- Credenciais protegidas em repouso no código fonte
- Detecção de adulteração (GCM)
- Performance nativa (AES-NI)

### Negativas
- Complexidade adicional de configuração
- Se a chave AES for perdida, as credenciais são irrecuperáveis
- Senha ainda em texto puro no `.env` (trade-off: pior seria versionar no git)
