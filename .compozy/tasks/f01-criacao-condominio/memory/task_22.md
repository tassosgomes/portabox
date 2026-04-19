# Task Memory: task_22.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

Implement the 3-step wizard + review + submit flow at `/condominios/novo` in `apps/backoffice`. Integrates with `POST /api/v1/admin/condominios`. Status: **complete**.

## Important Decisions

- Used `useState` (no `react-hook-form`) — library is not installed; manual state management is "similar" per task spec.
- Each step component manages its own local state initialized from parent on mount (conditional rendering unmounts/remounts naturally, preserving parent's copy of data).
- On success: `navigate('/condominios/{id}', { state: { successMessage } })` — toast message delivered via `location.state` so the details page (task_23) can display it without the wizard having to linger.
- Step 4 (review) passes `currentStep={4}` to StepIndicator — all 3 step items render as completed (stepNumber < 4).
- CNPJ/CPF masking: display value uses `formatCnpj(form.cnpj)` with `onChange` storing raw input (mask chars included); formatters strip non-digits before reformatting — circular but correct.
- `ghost` variant used for back/secondary CTAs (navy outline), `primary` for advance/submit (orange pill).
- Address fields in step 1 are optional in validation (DB schema has no NOT NULL on address columns); only `nomeFantasia` + valid `cnpj` are required.
- 409 error body expected shape: `{ extensions: { nomeExistente: string, criadoEm: string } }`.

## Learnings

- `fireEvent.change` (dynamic import from `@testing-library/react`) needed for `<input type="date">` because `userEvent.type` doesn't work with date inputs in jsdom.
- `const TODAY = ...` declared in test file but unused (removed after `tsc -b` caught it) — TS checks test files during build because `tsconfig.json` does not exclude them.

## Files / Surfaces

- `src/features/condominios/types.ts` — WizardData, ProblemDetails, response types
- `src/features/condominios/validation.ts` — formatCnpj, validateCnpj, formatCpf, validateCpf, formatCep, validateEmail, validateE164, isDateNotFuture
- `src/features/condominios/api.ts` — buildPayload, createCondominio (wraps apiClient.post)
- `src/features/condominios/components/StepDadosCondominio.tsx` — step 1 form
- `src/features/condominios/components/StepOptIn.tsx` — step 2 form (LGPD)
- `src/features/condominios/components/StepSindico.tsx` — step 3 form
- `src/features/condominios/components/Step.module.css` — shared step layout (row, actions, etc.)
- `src/features/condominios/components/Revisao.tsx` — review screen + submit
- `src/features/condominios/components/Revisao.module.css`
- `src/features/condominios/pages/NovoCondominioPage.tsx` — wizard orchestrator
- `src/features/condominios/pages/NovoCondominioPage.module.css`
- `src/app/routes.tsx` — added `condominios/novo` route
- `src/features/condominios/validation.test.ts` — 33 unit tests
- `src/features/condominios/pages/NovoCondominioPage.test.tsx` — 14 integration tests with MSW

## Errors / Corrections

- Removed unused `TODAY` const from test file; `tsc -b` includes test files and fails on unused vars.

## Ready for Next Run

- task_23 (lista, detalhes, ativação) should read `location.state.successMessage` on the details/list page to show the toast that task_22 passes via navigate state.
- All 69 tests pass; coverage 97.57% statements / 85.71% branches.
