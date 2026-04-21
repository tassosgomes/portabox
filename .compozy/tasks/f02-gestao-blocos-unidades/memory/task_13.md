# Task Memory: task_13.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Configurar TanStack Query em `apps/sindico` e `apps/backoffice` com `QueryClient`, provider, devtools em dev e bootstrap de `@portabox/api-client` para as proximas features de F02.
- Documentar `VITE_API_URL` via `.env.local.example` e garantir cobertura de testes para provider e bootstrap nos dois apps.

## Important Decisions

- O setup foi mantido duplicado por app (`src/lib/queryClient.ts`, `src/providers/QueryProvider.tsx`, `src/bootstrap.ts`) conforme o task spec e ADR-010; nenhuma extracao compartilhada foi criada nesta task.
- O bootstrap do `@portabox/api-client` ficou em `src/bootstrap.ts` para permitir teste direto de `configure(...)` sem depender de render do React.
- Os clientes HTTP locais de autenticacao (`src/shared/api/client.ts`) nao foram migrados para `@portabox/api-client`; a task ficou restrita ao baseline TanStack Query e ao bootstrap para futuras features.

## Learnings

- Os testes e builds do `backoffice` estavam bloqueados por fixtures desatualizadas nas suites de detalhes/listagem de condominios; foi necessario alinhar os fixtures ao shape atual (`cnpjMasked`, `documentos`, `sindicoSenhaDefinida`, `totalCount`) para obter verificacao fresca do workspace.
- O `sindico` continua emitindo um warning de navegacao do JSDOM em um teste legado de login invalido, mas a suite passa e a mudanca desta task nao altera esse comportamento.

## Files / Surfaces

- `apps/sindico/package.json`
- `apps/sindico/.env.local.example`
- `apps/sindico/src/main.tsx`
- `apps/sindico/src/vite-env.d.ts`
- `apps/sindico/src/bootstrap.ts`
- `apps/sindico/src/bootstrap.test.ts`
- `apps/sindico/src/lib/queryClient.ts`
- `apps/sindico/src/providers/QueryProvider.tsx`
- `apps/sindico/src/providers/QueryProvider.test.tsx`
- `apps/sindico/vitest.config.ts`
- `apps/backoffice/package.json`
- `apps/backoffice/.env.local.example`
- `apps/backoffice/src/main.tsx`
- `apps/backoffice/src/vite-env.d.ts`
- `apps/backoffice/src/bootstrap.ts`
- `apps/backoffice/src/bootstrap.test.ts`
- `apps/backoffice/src/lib/queryClient.ts`
- `apps/backoffice/src/providers/QueryProvider.tsx`
- `apps/backoffice/src/providers/QueryProvider.test.tsx`
- `apps/backoffice/vitest.config.ts`
- `apps/backoffice/src/features/condominios/pages/ListaCondominiosPage.tsx`
- `apps/backoffice/src/features/condominios/pages/ListaCondominiosPage.test.tsx`
- `apps/backoffice/src/features/condominios/pages/DetalhesCondominioPage.test.tsx`
- `pnpm-lock.yaml`

## Errors / Corrections

- `build` falhou inicialmente nos dois apps por causa de `options?.getAuthToken()` nos testes de bootstrap; corrigido guardando a referencia em variavel opcional antes da assercao.
- A suite completa do `backoffice` falhou por inconsistencias preexistentes entre fixtures de teste e o shape atual das paginas de condominios; os testes foram atualizados para refletir o contrato atual sem alterar o comportamento da feature.

## Ready for Next Run

- TanStack Query esta montado nos dois apps e `@portabox/api-client` ja fica configurado no bootstrap via `VITE_API_URL`.
- `pnpm --filter @portabox/sindico test`, `pnpm --filter @portabox/backoffice test`, `pnpm --filter @portabox/sindico build` e `pnpm --filter @portabox/backoffice build` passaram apos as mudancas.
