# ADR 002 — Blazor Server sobre Blazor WASM

- **Status**: Aceito
- **Data**: 2025-10-20
- **Decisão de**: Daniel Vieira Fernandes

## Contexto

Precisávamos escolher o modelo de renderização para a interface web em .NET. As opções eram Blazor Server (renderização no servidor via SignalR) e Blazor WebAssembly (renderização no navegador).

## Decisão

**Usar Blazor Server** como modelo de renderização.

## Justificativas

1. **Tempo de carga inicial**: Blazor Server carrega instantaneamente (não precisa baixar o runtime .NET no browser)
2. **Acesso direto ao banco**: Componentes Blazor Server podem acessar serviços e banco diretamente via DI, sem necessidade de API HTTP intermediária
3. **Segurança**: Lógica de negócio e acesso a dados ficam no servidor, nunca expostos ao cliente
4. **Público interno**: O HealthCheck é uma ferramenta de operações, usada por um número limitado de administradores — a latência do SignalR não é um problema
5. **Produtividade**: Ciclo de desenvolvimento mais rápido sem a complexidade de uma camada API separada

## Consequências

### Positivas
- Desenvolvimento mais rápido (sem API layer)
- Carga inicial instantânea
- Segurança simplificada (dados nunca saem do servidor)
- Compatibilidade total com componentes MudBlazor e Syncfusion

### Negativas
- Cada usuário consome recursos no servidor (circuito SignalR)
- Latência de rede afeta a interatividade
- Não funciona offline
- Conexão persistente necessária

## Mitigações

- O público é pequeno (administradores), então o consumo de recursos é baixo
- Uso de `LoadingState` para feedback visual durante operações lentas
- `ErrorBoundary` para capturar falhas de circuito
