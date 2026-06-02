# Plano: Refatoração de Layout Responsivo (Mobile)

**Branch:** `fix/responsive-layout`  
**Data:** 2026-06-02  
**Objetivo:** Tornar o layout do HealthCheck Web totalmente responsivo para mobile.

---

## Diagnóstico Atual

### Problemas identificados

| # | Arquivo | Problema | Impacto Mobile |
|---|---------|----------|----------------|
| 1 | `MainLayout.razor` | Drawer fixo `Variant="Mini"` + `ClipMode="Always"` | Ocupa espaço horizontal mesmo em telas pequenas, empurrando conteúdo |
| 2 | `MainLayout.razor` | AppBar com título "Monitoramento Web" + ícones fixos | Título pode truncar; ícones acumulados |
| 3 | `MainLayout.razor.css` | Apenas 1 media query (780px) para esconder subtítulo | Sem ajustes para drawer, padding, fontes em mobile |
| 4 | `Dashboard.razor` | MudGrid já responsivo (`xs="12"`) mas cards podem ficar apertados | OK no geral, mas o hero com Row pode quebrar em telas < 400px |
| 5 | `MonitoredSystems.razor` | Row com TextField + 2 botões na search-row | Pode overflow horizontal em telas estreitas |
| 6 | `app.css` | Sem regras mobile-first | Body sem padding adjustments |
| 7 | `wwwroot/app.css` + Bootstrap | Bootstrap CSS carregado mas subutilizado | Peso extra desnecessário para responsividade que o MudBlazor já provê |

### O que já funciona bem
- `meta viewport` em `App.razor` já configurado corretamente
- MudGrid com breakpoints responsivos em todas as páginas
- MudBlazor já é um framework responsivo por natureza

---

## Abordagem

**Estratégia:** MudBlazor-first. Aproveitar os componentes responsivos nativos do MudBlazor em vez de escrever CSS customizado do zero. Adaptar o drawer para ser `Responsive` no MudBlazor, ajustar a AppBar, e adicionar media queries pontuais.

**Não vamos:**
- Reescrever o layout do zero
- Trocar MudBlazor por outro framework
- Remover Bootstrap (dependência do template Blazor)

---

## Passo a Passo

### 1. `MainLayout.razor` — Drawer Responsivo

**Arquivo:** `HealthCheck.Web/Components/Layout/MainLayout.razor`

- [ ] Adicionar `Breakpoint` ao `MudDrawer` para que abaixo de `Breakpoint.Sm` ele vire `Temporary` (overlay)
- [ ] Adicionar `MudDrawerContainer` como wrapper para suportar drawer temporary
- [ ] Adicionar `MudOverlay` para fechar drawer mobile ao tocar fora
- [ ] No `@code`, adicionar lógica: em telas pequenas, `DrawerToggle()` abre/fecha drawer temporary

### 2. `MainLayout.razor` — AppBar Simplificada no Mobile

**Arquivo:** `HealthCheck.Web/Components/Layout/MainLayout.razor`

- [ ] Esconder título "Monitoramento Web" em telas < 600px (usar `Class="d-none d-sm-flex"` do Bootstrap)
- [ ] Mostrar ícone pequeno ou texto abreviado no mobile
- [ ] Agrupar ícones (logout, tema) para não sobrecarregar a AppBar

### 3. `MainLayout.razor.css` — Media Queries

**Arquivo:** `HealthCheck.Web/Components/Layout/MainLayout.razor.css`

- [ ] Adicionar breakpoint `@media (max-width: 600px)` para:
  - Reduzir padding do `hc-content-wrapper`
  - Esconder elementos da AppBar
  - Ajustar drawer overlay
- [ ] Garantir que o conteúdo principal não tenha scroll horizontal

### 4. `MonitoredSystems.razor` — Search Row Responsiva

**Arquivo:** `HealthCheck.Web/Components/Pages/MonitoredSystems/MonitoredSystems.razor`

- [ ] A search-row (`MudStack Row`) deve empilhar verticalmente em telas pequenas
- [ ] Mudar para `Wrap="true"` ou usar `MudGrid` com breakpoints

### 5. `app.css` — Ajustes Globais

**Arquivo:** `HealthCheck.Web/wwwroot/app.css`

- [ ] Adicionar `max-width: 100vw; overflow-x: hidden;` no body para evitar scroll horizontal
- [ ] Garantir que imagens e tabelas sejam `max-width: 100%`

### 6. Páginas — Verificação

- [ ] `Dashboard.razor`: OK (já usa MudGrid responsivo), mas ajustar hero stack para mobile
- [ ] `Audit.razor`: Verificar tabelas/data grids
- [ ] `Configurations.razor`: Verificar formulários
- [ ] `Login.razor`: Já é simples, provavelmente OK

---

## Arquivos que Serão Modificados

1. `HealthCheck.Web/Components/Layout/MainLayout.razor` — Drawer + AppBar responsivos
2. `HealthCheck.Web/Components/Layout/MainLayout.razor.css` — Media queries mobile
3. `HealthCheck.Web/Components/Pages/MonitoredSystems/MonitoredSystems.razor` — Search row
4. `HealthCheck.Web/wwwroot/app.css` — Ajustes globais
5. Possivelmente: `Dashboard.razor`, `Audit.razor`, `Configurations.razor`

---

## Validação

- [ ] `dotnet build` passa sem erros
- [ ] Testes existentes continuam passando
- [ ] Verificar visualmente (abrir no browser em viewport 375px, 768px, 1024px+)
- [ ] Drawer abre/fecha corretamente em mobile e desktop
- [ ] Sem scroll horizontal em nenhuma página
- [ ] Conteúdo legível em todas as resoluções

---

## Risco

- **Baixo**: Mudanças são majoritariamente de CSS e atributos Blazor, sem alterar lógica de negócio
- O Syncfusion Blazor pode ter comportamentos próprios em mobile — verificar após deploy
