# Task Memory: task_11.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Criar o pacote compartilhado `packages/api-client` com `apiFetch`, `ApiError`, `queryKeys`, tipos derivados do contrato OpenAPI e modulos tipados de `estrutura`, `blocos` e `unidades`.
- Integrar o pacote ao workspace e adicionar cobertura de testes unitarios e de integracao com Prism, incluindo validacao de geracao deterministica de `generated.ts`.

## Important Decisions

- O pacote usa `configure({ baseUrl, getAuthToken })` como API primaria, mas `apiFetch` tambem aceita fallback por `import.meta.env.VITE_API_BASE_URL` e `process.env.PORTABOX_API_BASE_URL` para facilitar testes e bootstrap futuro dos apps.
- O header `Accept` padrao foi definido como `application/json, application/problem+json` para suportar payloads RFC 7807 corretamente, inclusive nos mocks do Prism.
- O CI frontend passou a verificar drift do contrato com `pnpm --filter @portabox/api-client check:generated` e tambem build/test do pacote antes dos apps.

## Learnings

- O Prism respeita `Prefer: code=409`, mas so escolhe a resposta `application/problem+json` quando o `Accept` do request permite esse content type.
- A validacao ampla do task continua bloqueada por erro pre-existente em `apps/backoffice/src/features/condominios/pages/ListaCondominiosPage.tsx` (`formatCnpj` unused) durante `pnpm --filter @portabox/backoffice build`.

## Files / Surfaces

- `packages/api-client/package.json`
- `packages/api-client/README.md`
- `packages/api-client/tsconfig.json`
- `packages/api-client/tsconfig.build.json`
- `packages/api-client/vitest.config.ts`
- `packages/api-client/scripts/generate-types.sh`
- `packages/api-client/src/index.ts`
- `packages/api-client/src/generated.ts`
- `packages/api-client/src/http.ts`
- `packages/api-client/src/errors.ts`
- `packages/api-client/src/queryKeys.ts`
- `packages/api-client/src/types.ts`
- `packages/api-client/src/modules/blocos.ts`
- `packages/api-client/src/modules/estrutura.ts`
- `packages/api-client/src/modules/unidades.ts`
- `packages/api-client/tests/*.test.ts`
- `apps/sindico/package.json`
- `apps/backoffice/package.json`
- `package.json`
- `.github/workflows/ci.yml`

## Errors / Corrections

- `generate:types` falhou inicialmente por permissao de execucao do shell script; o script npm foi ajustado para chamar `bash ./scripts/generate-types.sh`.
- O pos-processamento do `generated.ts` falhou na primeira versao porque os argumentos para `node` estavam na ordem errada; corrigido com `node - ... <<'EOF'`.
- Os testes de integracao com Prism falharam ate aplicar auth mock, alinhar `baseUrl` sem duplicar `/api/v1`, aceitar `application/problem+json` e fixar `@faker-js/faker@5.5.3` no workspace.

## Ready for Next Run

- O pacote `@portabox/api-client` esta implementado, `build`, `test` e `check:generated` do proprio pacote passam.
- Para concluir formalmente a task, falta rerodar a verificacao ampla depois que o erro pre-existente de `formatCnpj` unused em `apps/backoffice` for resolvido ou isolado pelo responsavel da area.
