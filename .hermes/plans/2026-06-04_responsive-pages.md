# Plano: Responsividade Completa do HealthCheck

**Data:** 2026-06-04
**Branch:** `fix/responsive-layout` (já existe, VPS está nela)
**Objetivo:** Tornar responsivas as páginas que ainda não foram adaptadas e subir na VPS.

---

## Contexto

O `fix/responsive-layout` já tem:
- Drawer responsivo (Mini em desktop, Overlay em mobile <600px)
- Dashboard com hero flex-wrap
- MonitoredSystems com search-row flex-wrap
- app.css com prevenção de scroll horizontal

**Falta:** Configurations, Audit, EditMonitoredSystem (diálogo), Login e diálogos auxiliares.

## Páginas a ajustar

### 1. Configurations.razor
- **Hero row:** `MudStack Row=true` não quebra no mobile → título + subtítulo ficam lado a lado
- **Botões de ação:** `Restaurar padrão` + `Salvar` em `Justify.FlexEnd` podem colidir em telas estreitas
- **Ação:** Adicionar `flex-wrap` no hero e garantir que botões empilhem no mobile

### 2. Audit.razor
- **Hero row:** Título + última atualização em Row que não quebra → adicionar wrap
- **Search bar:** Campo + botão em Row sem wrap → adicionar flex-wrap
- **Botões de ação:** Filtros + Exportar + Ver detalhes em Row → empilhar no mobile
- **Syncfusion Grid:** Já existente, verificar se causa overflow horizontal

### 3. EditMonitoredSystem.razor  
- Formulário em diálogo — os campos já usam `MudItem xs=12` no Grid
- Counter (255, 2048, 1500) pode causar overflow em telas estreitas
- **Ação:** Verificar se os TextFields com Counter quebram em mobile

### 4. Login.razor
- Página não autenticada — mais crítica para primeira impressão mobile
- **Ação:** Verificar se card de login escala corretamente, se campos/textos são legíveis

### 5. Diálogos compartilhados
- `ConfirmDeleteMonitoredSystemDialog.razor`
- `ErrorReportDialog.razor`
- `AuditCheckDialogContent.razor`
- **Ação:** Garantir que modais não estouram em mobile (max-width, scroll interno)

## Abordagem técnica

### CSS global (app.css)
Adicionar media queries se necessário para:
- `.page-hero .mud-stack` → `flex-wrap: wrap` no mobile
- `.audit-search-bar`, `.search-row` → `flex-wrap: wrap`
- `.actions-footer` → botões ocupam 100% no mobile

### Razor inline
- Onde `MudStack Row=true` não tiver flex-wrap, adicionar `Class="responsive-row"`
- A classe `responsive-row` aplica `flex-wrap: wrap; gap: 8px` via CSS

## Passos

1. Criar/atualizar branch `fix/responsive-layout` a partir do último commit
2. Delegar ao OpenCode implementar os ajustes CSS + Razor
3. `dotnet test` — 38 testes devem continuar passando
4. `dotnet build` — sem erros
5. Iniciar HealthCheck.Web e rodar visual-verify.js em todas as páginas
6. Analisar screenshots mobile vs desktop
7. Commit + push
8. Deploy na VPS: pull → publish → systemctl restart

## Arquivos a modificar

| Arquivo | Mudança |
|---------|---------|
| `HealthCheck.Web/wwwroot/css/app.css` | Media queries para responsive-row, hero wrap, search wrap |
| `Components/Pages/Configurations/Configurations.razor` | Hero row com Class responsiva, botões com wrap |
| `Components/Pages/Audit/Audit.razor` | Hero row, search bar, botões com wrap |
| `Components/Pages/Login/Login.razor` | Verificar e ajustar scaling |
| `Components/Pages/MonitoredSystems/EditMonitoredSystem.razor` | Verificar Counter overflow |

## Validação

- 38 testes xUnit passando
- Visual verify: `/`, `/login`, `/dashboard`, `/auditoria`, `/sistemas`, `/configuracoes`
- 5 viewports: mobile-sm, mobile-lg, tablet, laptop, desktop
- Análise visual confirmando que não há overflow horizontal, textos legíveis, botões acessíveis
