---
status: completed
title: Setup TanStack Query em apps/sindico e apps/backoffice
type: frontend
complexity: low
dependencies:
  - task_11
---

# Task 13: Setup TanStack Query em apps/sindico e apps/backoffice

## Overview
Configura `@tanstack/react-query` nos dois frontends (`apps/sindico` e `apps/backoffice`), incluindo `QueryClient`, `QueryClientProvider`, opções default (staleTime 30s, retry 1) e Devtools em dev. Configura também `@portabox/api-client` via `configure(...)` com o getter de auth token apropriado para cada app.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST instalar `@tanstack/react-query` e `@tanstack/react-query-devtools` em `apps/sindico` e `apps/backoffice`
- MUST criar `src/providers/QueryProvider.tsx` em cada app com `QueryClient` configurado (`defaultOptions.queries.staleTime = 30_000`, `retry = 1`), `QueryClientProvider` e `ReactQueryDevtools` condicional a `import.meta.env.DEV`
- MUST chamar `@portabox/api-client` `configure({ baseUrl, getAuthToken })` no bootstrap de cada app — `baseUrl` vem de `import.meta.env.VITE_API_URL` (ou equivalente); `getAuthToken` lê de `localStorage` conforme padrão de F01
- MUST montar o `QueryProvider` no `main.tsx` de cada app envolvendo o `<App />`
- MUST adicionar variáveis de ambiente no `.env.local` de cada app (commitado como `.env.local.example`)
- SHOULD criar `src/lib/queryClient.ts` exportando a instância do `QueryClient` caso futuramente seja necessário acessar fora do React tree (ex.: em loaders de rotas)
</requirements>

## Subtasks
- [x] 13.1 Instalar dependências TanStack Query em ambos os apps
- [x] 13.2 Criar `QueryProvider` em `apps/sindico/src/providers/`
- [x] 13.3 Criar `QueryProvider` em `apps/backoffice/src/providers/`
- [x] 13.4 Chamar `configure` de `@portabox/api-client` no bootstrap de cada app
- [x] 13.5 Montar `QueryProvider` no `main.tsx` de cada app
- [x] 13.6 Adicionar `.env.local.example` documentando `VITE_API_URL`

## Implementation Details
Ver ADR-010 (Implementation Notes) para exemplo de setup do `QueryClient` e Provider. Os dois apps têm setup idêntico; considerar extrair para um helper compartilhado em `packages/ui` se houver interesse — mas por YAGNI, manter duplicado nesta task e consolidar em Phase 2 se outras features replicarem.

Contrato de `configure` do `@portabox/api-client`:
```typescript
configure({
  baseUrl: import.meta.env.VITE_API_URL!,
  getAuthToken: () => localStorage.getItem('portabox.token'),
});
```

O nome da key `portabox.token` deve alinhar com F01 (confirmar em `apps/sindico`/`apps/backoffice` se já há convenção).

### Relevant Files
- `apps/sindico/package.json` — adicionar deps
- `apps/sindico/src/providers/QueryProvider.tsx` — novo
- `apps/sindico/src/main.tsx` — montar provider
- `apps/sindico/.env.local.example` — documentar `VITE_API_URL`
- `apps/backoffice/package.json` — adicionar deps
- `apps/backoffice/src/providers/QueryProvider.tsx` — novo
- `apps/backoffice/src/main.tsx` — montar provider
- `apps/backoffice/.env.local.example` — documentar `VITE_API_URL`

### Dependent Files
- Todas as features frontend subsequentes (task_14+) consomem `useQuery`/`useMutation` deste setup
- `@portabox/api-client` precisa ter `configure` disponível (task_11)

### Related ADRs
- [ADR-010: Baseline Frontend — TanStack Query + React Context](adrs/adr-010.md) — motivação e configuração recomendada

## Deliverables
- `QueryProvider` montado em ambos os apps
- `configure` de `@portabox/api-client` chamado no bootstrap
- Devtools disponíveis em dev
- `.env.local.example` documentando variáveis necessárias
- Unit tests with 80%+ coverage **(REQUIRED)** — Vitest + Testing Library verificando que provider fornece contexto
- Integration tests — cobertos indiretamente em task_14+ ao consumir `useQuery`

## Tests
- Unit tests:
  - [x] `<QueryProvider>` de cada app provê `QueryClient` para `useQueryClient()` ao filho
  - [x] `QueryClient` tem `defaultOptions.queries.staleTime === 30000` e `retry === 1`
  - [x] Devtools **não** é montado quando `import.meta.env.DEV === false`
  - [x] `configure` é chamado uma única vez no bootstrap (sem side effects em re-render)
- Integration tests:
  - [x] Montar `<QueryProvider>` + componente de teste que usa `useQuery` contra mock → resolve com cache esperado
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- `useQuery`/`useMutation` funcionam em qualquer componente abaixo do `<QueryProvider>`
- Devtools aparece apenas em dev (visível ao rodar `pnpm dev`)
- Nenhuma divergência de setup entre os dois apps
