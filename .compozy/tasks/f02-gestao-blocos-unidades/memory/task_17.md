# Task Memory: task_17.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

Task 17 foi entregue integralmente: rota `/tenants/:condominioId/estrutura`, `<EstruturaReadOnlyPage>`, `<TenantSelector>`, `useEstruturaAdmin`, `toReadOnlyTreeItems`, contadores em memória, TODO de audit-access para Phase 2. Todos os 7 testes passam; cobertura 96%/84%/82%.

## Important Decisions

- Mapper `toReadOnlyTreeItems` foi duplicado localmente em `apps/backoffice` (não importado do sindico) para evitar acoplamento com menus de ação do síndico. Comentário no arquivo documenta a intenção.
- Log de auditoria deferito para Phase 2 com TODO no `EstruturaReadOnlyPage.tsx` linha 75.

## Learnings

- Testes de integração MSW no backoffice requerem `configure({ baseUrl: 'http://localhost/api/v1' })` no `beforeAll` porque o `@portabox/api-client` não é configurado automaticamente em testes (sem chamar `configureApiClient()` do bootstrap). Sem isso, o cliente cai para `VITE_API_BASE_URL = http://localhost/api` e perde o `/v1` do path, gerando mismatch com handlers MSW.
- `screen.getByText('Residencial Sol')` falha por ambiguidade quando o mesmo texto aparece tanto no `<h2>` da tree card quanto nas `<option>` do TenantSelector. Usar `screen.getByRole('heading', { name: 'Residencial Sol', level: 2 })` resolve.
- Assertions síncronas após `waitFor(heading)` falham para conteúdo de dados async. Mover todas as assertions para dentro do `waitFor` é o padrão correto.

## Files / Surfaces

- `apps/backoffice/src/features/tenants/estrutura/EstruturaReadOnlyPage.integration.test.tsx` — corrigido: configure() em beforeAll + waitFor completo + getByRole heading level 2

## Errors / Corrections

- 3 integration test failures → causa raiz: URL mismatch (api-client sem configure) + timing (assertions fora do waitFor) + ambiguidade de texto.

## Ready for Next Run

Task 17 concluída. Task 18 (Smoke E2E + hardening final) é a próxima.
