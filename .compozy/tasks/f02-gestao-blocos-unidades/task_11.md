---
status: pending
title: packages/api-client baseline (http + queryKeys + ApiError + módulos tipados)
type: frontend
complexity: medium
dependencies:
  - task_09
---

# Task 11: packages/api-client baseline (http + queryKeys + ApiError + módulos tipados)

## Overview
Cria o pacote compartilhado `packages/api-client` que vira o cliente HTTP tipado único consumido por `apps/sindico` e `apps/backoffice`. Inaugura a convenção de query keys hierárquicas (`queryKeys.estrutura(condominioId)`) adotada como baseline para todas as features (ADR-010). É a primeira materialização do padrão TanStack Query no projeto.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar pacote `packages/api-client/` com `package.json` privado (scope `@portabox/api-client` se convenção monorepo de F01 usar scope)
- MUST criar `src/http.ts` com função `apiFetch<T>(path, init?)` que: monta URL base via env var; injeta header `Authorization` se token disponível; trata `Content-Type: application/json`; converte response não-ok em `ApiError`; retorna `T` parseado ou `undefined` em 204
- MUST criar `src/errors.ts` com classe `ApiError` contendo `status`, `title`, `detail` (vindo de ProblemDetails), `fieldErrors?: Record<string, string[]>` (para 400)
- MUST criar `src/queryKeys.ts` exportando helper `queryKeys = { estrutura: (id: string) => ['estrutura', id] as const, estruturaAdmin: (id: string) => ['estrutura-admin', id] as const }`
- MUST criar módulos tipados:
  - `src/modules/estrutura.ts` — `getEstrutura(condominioId, includeInactive): Promise<Estrutura>`
  - `src/modules/blocos.ts` — `criarBloco`, `renomearBloco`, `inativarBloco`, `reativarBloco`
  - `src/modules/unidades.ts` — `criarUnidade`, `inativarUnidade`, `reativarUnidade`
- MUST criar types `src/types.ts` espelhando os DTOs do backend (`Estrutura`, `BlocoNode`, `AndarNode`, `UnidadeLeaf`, `BlocoDto`, `UnidadeDto`) — por ora **digitados manualmente**; geração automática via OpenAPI fica para Phase 2
- MUST adicionar ao workspace `pnpm-workspace.yaml` (se ainda não listado) e aos `package.json` de `apps/sindico` e `apps/backoffice` como dependência (`"@portabox/api-client": "workspace:*"`)
- SHOULD configurar `tsconfig.json` do pacote emitindo `.d.ts` e usando TypeScript strict mode
</requirements>

## Subtasks
- [ ] 11.1 Criar `packages/api-client/` com `package.json`, `tsconfig.json` e entrypoint `src/index.ts`
- [ ] 11.2 Implementar `http.ts` + `errors.ts` com tratamento de ProblemDetails
- [ ] 11.3 Implementar `queryKeys.ts` com helpers hierárquicos
- [ ] 11.4 Implementar módulos `estrutura`, `blocos`, `unidades` com funções tipadas
- [ ] 11.5 Criar `types.ts` com DTOs espelhados do backend
- [ ] 11.6 Adicionar dependência workspace em `apps/sindico` e `apps/backoffice`; escrever tests unitários

## Implementation Details
Ver TechSpec seção **Integration Points** e ADR-010 Implementation Notes para exemplos de código do `apiFetch` e do `queryKeys`. `ApiError` deve sintetizar `ProblemDetails` (`type`, `title`, `detail`, `status`, `instance`, `errors?`) em uma classe JS utilizável em try/catch.

O endpoint de auth ainda não está fechado em F02; assumir que o token é lido de `localStorage` via função injetável `getAuthToken()` exposta pelo módulo (app define a função no bootstrap). Isso desacopla o pacote de qualquer implementação de auth específica.

Exemplo de estrutura mínima:
```typescript
// packages/api-client/src/http.ts
export function configure(opts: { baseUrl: string; getAuthToken: () => string | null }) { ... }
export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> { ... }
```

### Relevant Files
- `packages/api-client/package.json` — novo
- `packages/api-client/tsconfig.json` — novo
- `packages/api-client/src/index.ts` — re-exporta tudo
- `packages/api-client/src/http.ts` — novo
- `packages/api-client/src/errors.ts` — novo
- `packages/api-client/src/queryKeys.ts` — novo
- `packages/api-client/src/types.ts` — novo
- `packages/api-client/src/modules/estrutura.ts` — novo
- `packages/api-client/src/modules/blocos.ts` — novo
- `packages/api-client/src/modules/unidades.ts` — novo
- `apps/sindico/package.json` — adicionar dependência
- `apps/backoffice/package.json` — adicionar dependência
- `pnpm-workspace.yaml` ou equivalente — já deve listar `packages/*`

### Dependent Files
- `apps/sindico/src/features/estrutura/hooks/*` (task_14+) consomem este pacote
- `apps/backoffice/src/features/tenants/.../estrutura/hooks/*` (task_17) idem
- Tests: Vitest unit tests do pacote

### Related ADRs
- [ADR-009: Endpoint Único Retornando Árvore Completa](adrs/adr-009.md) — define o contrato HTTP consumido
- [ADR-010: Baseline Frontend — TanStack Query + React Context](adrs/adr-010.md) — motivação dos query keys e do pacote dedicado

## Deliverables
- Pacote `@portabox/api-client` instalável e tipado
- `queryKeys` helper exportado e adotado
- Módulos `estrutura`/`blocos`/`unidades` com funções tipadas
- Apps consomem o pacote via workspace
- Unit tests with 80%+ coverage **(REQUIRED)** (Vitest)
- Integration tests para pacote isolado (`apiFetch` contra mock server) **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] `apiFetch` chama `fetch` com URL absoluta (`baseUrl + path`) e header `Authorization` quando token existe
  - [ ] `apiFetch` em response 204 retorna `undefined` sem parse
  - [ ] `apiFetch` em response 4xx/5xx lança `ApiError` com campos de ProblemDetails
  - [ ] `ApiError` serializa corretamente `fieldErrors` de um payload de validação (400)
  - [ ] `queryKeys.estrutura(id)` retorna `['estrutura', id]` tipado como tuple literal
  - [ ] `getEstrutura(condominioId, true)` chama `apiFetch` com query string `?includeInactive=true`
  - [ ] `criarBloco({ condominioId, nome })` chama `POST /condominios/{id}/blocos` com JSON correto
- Integration tests:
  - [ ] Test contra mock server (MSW ou `fetch-mock`) simulando 201 Created → retorna `BlocoDto`
  - [ ] Test contra mock server simulando 409 Conflict → lança `ApiError` com `status=409`
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- `apps/sindico` e `apps/backoffice` importam `@portabox/api-client` sem erros de tipo
- `queryKeys` convention documentada no README do pacote e pronta para adoção em F03+
- `ApiError` surface permite tratamento amigável de 400/409/422 no frontend
