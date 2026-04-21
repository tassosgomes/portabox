# Task Memory: task_16.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Implementar mutações de Unidade no `apps/sindico` seguindo o baseline de Bloco: hooks de TanStack Query, formulário com zod/react-hook-form, modais de confirmação e cobertura Vitest + MSW.

## Important Decisions

- A página mantém `explicitlySelectedBlocoId` como estado e deriva `selectedBlocoId` via `useMemo` a partir de `data` e `explicitlySelectedBlocoId` — eliminando a necessidade de um `useEffect` que chamava `setState` diretamente (violaria a regra `react-hooks/set-state-in-effect`).
- `UnidadeForm` usa estado `shouldFocusAndar` + `useEffect` para focar o campo de andar após reset do formulário no modo rajada, em vez de acessar `andarInputRef.current` diretamente dentro de `handleValidSubmit` (violaria a regra `react-hooks/refs`).

## Learnings

- A regra ESLint `react-hooks/refs` flageia qualquer função passada em JSX que acessa `ref.current` no corpo da função, mesmo que o acesso só ocorra em evento de submit. Solução: usar estado + useEffect para deslocar o acesso ao ref para fora do caminho de render.
- A regra `react-hooks/set-state-in-effect` bloqueia padrão `useEffect -> setState`. Substituir por `useMemo` que deriva o valor é a abordagem aprovada.

## Files / Surfaces

- `apps/sindico/src/features/estrutura/hooks/useCriarUnidade.ts` — criado
- `apps/sindico/src/features/estrutura/hooks/useInativarUnidade.ts` — criado
- `apps/sindico/src/features/estrutura/hooks/useReativarUnidade.ts` — criado
- `apps/sindico/src/features/estrutura/components/UnidadeForm.tsx` — criado + lint fix
- `apps/sindico/src/features/estrutura/components/unidadeFormSchema.ts` — criado
- `apps/sindico/src/features/estrutura/components/UnidadeActionsMenu.tsx` — criado
- `apps/sindico/src/features/estrutura/EstruturaPage.tsx` — estendido + lint fix
- `apps/sindico/src/features/estrutura/__tests__/useCriarUnidade.test.tsx` — criado
- `apps/sindico/src/features/estrutura/__tests__/unidadeMutations.test.tsx` — criado
- `apps/sindico/src/features/estrutura/__tests__/UnidadeForm.test.tsx` — criado
- `apps/sindico/src/features/estrutura/__tests__/EstruturaPage.integration.test.tsx` — estendido com casos de Unidade

## Errors / Corrections

- Lint errors encontrados ao rodar verificação final: dois erros de lint corrigidos antes de marcar como concluído.

## Ready for Next Run

- Task 16 concluída. Próxima dependente: task_18 (Smoke E2E piloto).
