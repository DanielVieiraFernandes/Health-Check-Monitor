# ADR 001 — Dapper sobre Entity Framework Core

- **Status**: Aceito
- **Data**: 2025-11-15
- **Decisão de**: Daniel Vieira Fernandes

## Contexto

Precisávamos escolher um ORM para acesso a dados no PostgreSQL. As opções eram Entity Framework Core (ORM completo) e Dapper (micro ORM).

## Decisão

**Usar Dapper** como biblioteca de acesso a dados, com SQL puro e parâmetros nomeados.

## Justificativas

1. **Performance**: Dapper é significativamente mais rápido que EF Core para queries, especialmente em cenários de leitura intensiva como o dashboard e a auditoria
2. **Controle**: SQL puro permite otimizações específicas (índices, CTEs, window functions) que seriam difíceis ou impossíveis com LINQ
3. **Simplicidade**: O domínio do HealthCheck tem queries relativamente simples (CRUD + filtros), não se beneficiando do change tracking do EF
4. **Conexão explícita**: `NpgsqlConnection` gerenciado manualmente, compatível com o padrão de injeção de dependência e escopos do Worker
5. **Evita abstrações desnecessárias**: Não precisamos de migrations automáticas (usamos DbUp com scripts SQL explícitos)

## Consequências

### Positivas
- Queries otimizadas manualmente
- Mapeamento explícito e previsível
- Menos overhead de runtime
- Facilidade para usar recursos nativos do PostgreSQL (uuidv7, pg_trgm)

### Negativas
- Sem change tracking (não necessário para o caso de uso)
- Mais código boilerplate para CRUD simples
- Sem migrations automáticas (resolvido com DbUp + scripts manuais)

## Regras estabelecidas

- **Nunca usar `SELECT *`**: sempre listar colunas explicitamente
- **Parâmetros nomeados**: `@param` em vez de concatenação
- **Mapeamento underscore**: `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true`
