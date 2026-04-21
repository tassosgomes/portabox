---
status: completed
title: apps/backoffice estrutura read-only cross-tenant + seletor de tenant + log de acesso
type: frontend
complexity: medium
dependencies:
  - task_11
  - task_12
  - task_13
---

# Task 17: apps/backoffice estrutura read-only cross-tenant + seletor de tenant + log de acesso

## Overview
Entrega a visualização de estrutura no `apps/backoffice` em modo **read-only** para operadores da plataforma, consumindo o endpoint `GET /api/v1/admin/condominios/{condominioId}/estrutura` (mesmo shape de response `Estrutura` do endpoint do síndico — ver [`api-contract.yaml`](../api-contract.yaml)). A tela reutiliza o `<Tree>` de `packages/ui` sem controles de ação, adiciona um seletor de tenant no topo e dispara um log de auditoria local quando o operador acessa a estrutura de um tenant.

> **Sem query param extra:** o endpoint admin aceita apenas `includeInactive` como query; o `condominioId` vai no path e **já representa o tenant**. Não inventar `?tenantId=...` ou similar — o contrato é autoritativo.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar rota `/tenants/:condominioId/estrutura` em `apps/backoffice`
- MUST criar `<TenantSelector>` (dropdown) listando condomínios disponíveis via endpoint existente de F01 (`GET /api/v1/admin/condominios` ou equivalente); ao selecionar, navegar para a rota acima
- MUST criar `src/features/tenants/estrutura/hooks/useEstruturaAdmin.ts` usando `useQuery` com `queryKey = queryKeys.estruturaAdmin(condominioId)` e `queryFn = () => getEstruturaAdmin(condominioId, includeInactive)`; expor também `getEstruturaAdmin` em `@portabox/api-client/src/modules/estrutura.ts` (estender task_11 se necessário)
- MUST criar `<EstruturaReadOnlyPage>` renderizando o mesmo `<Tree>` de `packages/ui` **sem** `onClick` de ação — apenas navegação de expand/collapse; nenhum botão de "adicionar" ou "inativar" visível
- MUST reutilizar o mapper `toTreeItems` de task_14 (mover para `packages/ui` ou `packages/api-client` se facilitar; alternativamente, duplicar com intenção clara)
- MUST registrar log de auditoria no backend ao acessar a estrutura: chamar endpoint `POST /api/v1/admin/audit-access` (criar endpoint stub se necessário, ou deferir para task_10 backend) — **se escopo for apertado, deferir para Phase 2 e apenas documentar no código como TODO**
- MUST suportar toggle "Mostrar inativos" (mesma UX de task_14)
- SHOULD exibir contadores rápidos no topo da árvore (blocos ativos, unidades ativas) para apoiar o operador em suporte; implementar sem adicionar endpoint — calcular em memória a partir da própria resposta
</requirements>

## Subtasks
- [x] 17.1 Estender `@portabox/api-client` com `getEstruturaAdmin(condominioId, includeInactive)`
- [x] 17.2 Criar hook `useEstruturaAdmin` com query key `estruturaAdmin`
- [x] 17.3 Criar `<TenantSelector>` consumindo endpoint de listagem de tenants de F01
- [x] 17.4 Criar `<EstruturaReadOnlyPage>` reusando `<Tree>` sem ações
- [x] 17.5 Exibir contadores (blocos ativos, unidades ativas) calculados em memória
- [x] 17.6 Instrumentar log de auditoria de acesso (ou documentar TODO se deferido) + testes

## Implementation Details
Ver TechSpec seção **Data Flow C6** para comportamento esperado. Reutilizar mapper `toTreeItems` via import cruzado entre apps (se os workspaces permitirem) ou mover para `packages/api-client` (mais correto arquiteturalmente — o mapper só depende dos types do api-client).

Sobre log de auditoria de acesso: o ADR-005 exige "Acesso registrado em log de auditoria". Em F02, backend não implementou endpoint dedicado; opções:
1. Adicionar endpoint `POST /admin/audit-access` na mesma task 09 — **escopo adicional a validar**.
2. Deferir para Phase 2 e marcar como TODO no código.
3. Reutilizar `TenantAuditEntry` com um novo `EventKind.OperatorAccessed` — fora do escopo de F02 estrutural.

Recomendação: **opção 2** — documentar TODO no `<EstruturaReadOnlyPage>` e capturar a decisão em uma entrada em Open Questions do domain doc ou do próprio TechSpec para Phase 2. Task de integration test (task_10) não cobre isto.

### Relevant Files
- `apps/backoffice/src/routes/tenants.$condominioId.estrutura.tsx` — nova rota
- `apps/backoffice/src/features/tenants/estrutura/EstruturaReadOnlyPage.tsx` — novo
- `apps/backoffice/src/features/tenants/estrutura/hooks/useEstruturaAdmin.ts` — novo
- `apps/backoffice/src/features/tenants/components/TenantSelector.tsx` — novo
- `packages/api-client/src/modules/estrutura.ts` — estender com `getEstruturaAdmin`
- `packages/ui/src/Tree/Tree.tsx` — consumido
- `apps/backoffice/src/features/tenants/estrutura/__tests__/` — testes

### Dependent Files
- Task 18 exercita fluxo backoffice no smoke E2E
- Fora de F02: `TenantSelector` e `useEstruturaAdmin` viram padrão para features futuras de backoffice (F09 dashboard, F08 dispositivos)

### Related ADRs
- [ADR-005: Escrita Exclusiva do Síndico; Backoffice Read-Only Cross-Tenant](adrs/adr-005.md) — motivação da tela
- [ADR-009: Endpoint Único Retornando Árvore Completa](adrs/adr-009.md) — consumo do endpoint admin
- [ADR-010: TanStack Query + React Context](adrs/adr-010.md) — padrão de `useQuery`

## Deliverables
- Rota `/tenants/:id/estrutura` no backoffice
- `<EstruturaReadOnlyPage>` consumindo endpoint admin
- `<TenantSelector>` funcional
- Contadores rápidos no topo da árvore
- Log de acesso (ou TODO documentado)
- Unit tests with 80%+ coverage **(REQUIRED)**
- Integration tests com MSW **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `useEstruturaAdmin` chama `getEstruturaAdmin` com `condominioId` e `includeInactive` corretos; key `estruturaAdmin` diferente de `estrutura` (isolamento de cache)
  - [x] `<TenantSelector>` lista tenants e navega para `/tenants/:id/estrutura` na seleção
  - [x] `<EstruturaReadOnlyPage>` não renderiza botões de ação (CTA "Novo bloco" ausente; menu de ações ausente)
  - [x] Contadores: "Blocos ativos" e "Unidades ativas" calculam corretamente a partir da response
  - [x] Toggle "Mostrar inativos" funciona (refetch com `includeInactive=true`)
- Integration tests:
  - [x] MSW simula `GET /admin/condominios` + `GET /admin/condominios/:id/estrutura` → tela renderiza árvore
  - [x] MSW simula 403 (operador sem role) → redireciona para página de erro
  - [x] Mudança de tenant via seletor → nova query dispara com nova key
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Operador consegue selecionar um tenant e ver a árvore completa em modo read-only
- Nenhum botão de escrita visível (validado via RTL + snapshot)
- Contadores ajudam o operador a diagnosticar problemas sem precisar pedir info ao síndico
