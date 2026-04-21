# Task Memory: task_14.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Entregar a leitura da estrutura no `apps/sindico` com rota protegida, hook `useEstrutura`, mapper para `TreeItem[]`, estados de loading/error/empty/success e testes Vitest/MSW.

## Important Decisions

- O app ainda nao expoe `condominioId` de forma estavel no contexto do sindico; a implementacao vai aceitar `tenantId` opcional vindo do auth e fallback por `:condominioId` na rota para manter a task funcional sem expandir escopo para refatorar auth inteira.
- Como nao existe infraestrutura compartilhada de toast no `apps/sindico`, erros nao-401/404 serao exibidos em um banner/toast local da propria `EstruturaPage`, mantendo o comportamento pedido sem introduzir provider global fora de escopo.
- O hook `useEstrutura` manteve a query key canonica `queryKeys.estrutura(condominioId)` e faz `refetch` explicito quando `includeInactive` muda, para respeitar o requisito da task sem alterar o helper compartilhado de query keys criado na task 11.

## Learnings

- Os arquivos `AGENTS.md` e `CLAUDE.md` pedidos pela task nao existem no estado atual do repositorio; a execucao segue o task spec, `_techspec.md`, `_tasks.md` e ADRs 009/010 como fontes de verdade.
- O `packages/api-client` atual nao envia `credentials` por padrao, enquanto a autenticacao real do backend usa cookie de sessao (`IdentityConstants.ApplicationScheme`); a feature precisa alinhar isso para conseguir consumir `GET /estrutura` autenticado.
- A suite existente do `apps/sindico` tinha duas lacunas de baseline que precisaram ser corrigidas junto da task para manter o app validavel: alias de rota `/setup-password` e login sem `noRedirectOn401`, que disparava navegacao do jsdom em testes.

## Files / Surfaces

- `apps/sindico/src/app/routes.tsx`
- `apps/sindico/src/features/auth/AuthContext.tsx`
- `apps/sindico/src/features/auth/context.ts`
- `apps/sindico/src/features/auth/useAuthContext.ts`
- `packages/api-client/src/http.ts`
- `apps/sindico/src/routes/estrutura.tsx`
- `apps/sindico/src/features/estrutura/EstruturaPage.tsx`
- `apps/sindico/src/features/estrutura/components/EmptyState.tsx`
- `apps/sindico/src/features/estrutura/hooks/useEstrutura.ts`
- `apps/sindico/src/features/estrutura/mappers/toTreeItems.ts`
- `apps/sindico/src/features/estrutura/__tests__/`

## Errors / Corrections

- Correcao durante validacao: remover `setState` dentro de `useEffect` em `EstruturaPage` para satisfazer a regra `react-hooks/set-state-in-effect`; a mensagem de erro derivada da API agora e calculada no render, preservando apenas feedback local no estado.
- Correcao durante validacao: mover `AuthContext` para `features/auth/context.ts` e o hook para `useAuthContext.ts` para resolver o warning de fast-refresh e o erro de import nao usado durante `build`/`lint`.

## Ready for Next Run

- Task pronta para follow-ups de mutacao (tasks 15/16) sobre a mesma `EstruturaPage`; o CTA do empty state hoje apenas exibe mensagem local e deve ser ligado a um modal/mutation real na proxima etapa.
