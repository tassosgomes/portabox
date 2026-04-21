---
status: completed
title: apps/sindico EstruturaPage + hook useEstrutura (leitura da árvore)
type: frontend
complexity: medium
dependencies:
  - task_11
  - task_12
  - task_13
---

# Task 14: apps/sindico EstruturaPage + hook useEstrutura (leitura da árvore)

## Overview
Entrega a página de estrutura do síndico em `apps/sindico` com leitura da árvore via TanStack Query: `<EstruturaPage>` consumindo `<Tree>` de `packages/ui` e `<EmptyState>` quando o condomínio não tem bloco. A tela ainda não tem mutações (virão em task_15/16); seu foco é estabelecer a estrutura de rotas, layout, estado de loading/erro e o hook `useEstrutura` reutilizado por task_15/16.

> **Types via contrato:** todos os tipos de resposta (`Estrutura`, `BlocoNode`, `AndarNode`, `UnidadeLeaf`) vêm de `@portabox/api-client` que os deriva automaticamente do [`api-contract.yaml`](../api-contract.yaml) via `openapi-typescript` (task_11). **Nunca** redeclarar types localmente.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar rota `/estrutura` em `apps/sindico/src/routes/` (ou padrão equivalente estabelecido por F01 task_24)
- MUST criar `src/features/estrutura/hooks/useEstrutura.ts` exportando `useEstrutura(condominioId: string, includeInactive: boolean)` que usa `useQuery` com key `queryKeys.estrutura(condominioId)` e `queryFn = () => getEstrutura(condominioId, includeInactive)`
- MUST criar `src/features/estrutura/EstruturaPage.tsx` com:
  - Leitura do `condominioId` do contexto do síndico logado (ou do path se rota for parametrizada)
  - Toggle "Mostrar inativos" (default `false`)
  - Estado de loading (skeleton/spinner), erro (mensagem + retry), vazio (empty state com CTA "Cadastrar primeiro bloco")
  - Renderização via `<Tree>` transformando `Estrutura` em `TreeItem[]`
- MUST criar `src/features/estrutura/components/EmptyState.tsx` com copy didático e botão que chamará (via callback exposto) a criação do primeiro bloco (mutação real vem em task_15)
- MUST criar helper `src/features/estrutura/mappers/toTreeItems.ts` que converte `Estrutura` (type importado de `@portabox/api-client`) em `TreeItem[]` com labels e badges corretos (ex.: "Bloco A · 24 unidades")
- MUST lidar com erros da API (401 → redirect login; 404 → página dedicada; outros → toast)
- SHOULD lazy-load a rota via `React.lazy` se o padrão estabelecido por F01 for esse
</requirements>

## Subtasks
- [x] 14.1 Criar hook `useEstrutura` com TanStack Query e query key hierárquica
- [x] 14.2 Criar mapper `toTreeItems` transformando `Estrutura` → `TreeItem[]`
- [x] 14.3 Implementar `<EstruturaPage>` com loading/erro/empty/success states
- [x] 14.4 Implementar `<EmptyState>` com copy didático e CTA
- [x] 14.5 Registrar rota `/estrutura` no router de `apps/sindico`
- [x] 14.6 Escrever testes Vitest + React Testing Library cobrindo os 4 estados

## Implementation Details
Ver TechSpec seção **User Experience → Jornada do Síndico** para comportamento esperado dos estados. A rotina de criar primeiro bloco (tocada pelo CTA do empty state) fica como *prop callback* no `<EmptyState>` — task_15 injeta a mutação real.

Mapper `toTreeItems`:
- Nível raiz: `Condomínio` (label `NomeFantasia`).
- Filhos do condomínio: `BlocoNode[]` — cada bloco vira um `TreeItem` com `children` sendo os andares.
- Filhos do bloco: `AndarNode[]` — cada andar vira `TreeItem` com label `Andar ${n}` e `children` sendo as unidades.
- Folhas: `UnidadeLeaf[]` — cada unidade vira `TreeItem` sem `children`, com badge `Ativo` ou `Inativo`.

Badge de contagem: calcular em memória `blocoAtivo.unidades.length` e exibir no label do bloco.

### Relevant Files
- `apps/sindico/src/routes/estrutura.tsx` — novo (ou equivalente em React Router / TanStack Router)
- `apps/sindico/src/features/estrutura/EstruturaPage.tsx` — novo
- `apps/sindico/src/features/estrutura/components/EmptyState.tsx` — novo
- `apps/sindico/src/features/estrutura/hooks/useEstrutura.ts` — novo
- `apps/sindico/src/features/estrutura/mappers/toTreeItems.ts` — novo
- `apps/sindico/src/features/estrutura/__tests__/*` — testes (novos)
- `packages/ui/src/Tree/Tree.tsx` — consumido
- `@portabox/api-client/src/modules/estrutura.ts` — consumido

### Dependent Files
- Tasks 15, 16 estendem esta página com mutações de Bloco e Unidade
- Task 17 reutiliza `toTreeItems` no backoffice (se estrutura do mapper permitir; senão, cria irmão read-only)
- Task 18 exercita a página no smoke E2E

### Related ADRs
- [ADR-009: Endpoint Único Retornando Árvore Completa](adrs/adr-009.md) — contrato de `getEstrutura` consumido
- [ADR-010: Baseline Frontend — TanStack Query + React Context](adrs/adr-010.md) — `useQuery` + query keys

## Deliverables
- Rota `/estrutura` funcional em `apps/sindico`
- Hook `useEstrutura` reutilizável
- `<EstruturaPage>` com 4 estados (loading, error, empty, success)
- Mapper `toTreeItems` testado
- Unit tests with 80%+ coverage **(REQUIRED)** (Vitest + Testing Library)
- Integration tests com MSW mocando a API **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `useEstrutura` chama `getEstrutura` com `includeInactive` correto e usa query key `queryKeys.estrutura(id)`
  - [x] `toTreeItems` converte uma `Estrutura` com 2 blocos (1 ativo, 1 inativo) em `TreeItem[]` com `state` correto
  - [x] `toTreeItems` calcula badge "N unidades ativas" corretamente
  - [x] `<EstruturaPage>` renderiza skeleton enquanto query está pending
  - [x] `<EstruturaPage>` renderiza `<EmptyState>` quando tree.blocos.length === 0
  - [x] `<EstruturaPage>` renderiza `<Tree>` com items corretos no estado success
  - [x] Toggle "Mostrar inativos" altera `includeInactive` e dispara refetch
  - [x] Erro 401 redireciona para login (mock do auth guard)
  - [x] Erro 404 renderiza página dedicada com copy amigável
- Integration tests:
  - [x] MSW simula `GET /estrutura` com 201 sucesso → UI renderiza árvore
  - [x] MSW simula `GET /estrutura` com 500 → UI mostra erro + botão retry
  - [x] Retry button dispara novo fetch e limpa erro
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Síndico consegue navegar para `/estrutura`, ver árvore (ou empty state), expandir e colapsar blocos
- Toggle de inativos funciona sem flicker (graças a staleTime do TanStack Query)
- Skeleton aparece por < 300ms na primeira carga em rede normal (medido informalmente)
