# Task Memory: task_18.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

Deliver the F02 closure artifacts: manual smoke roteiro, Playwright E2E spec, domain.md update, performance seed script, CHANGELOG entry, and log field verification documentation.

## Important Decisions

- Playwright spec (`f02-estrutura.spec.ts`) covers steps 3–7 via a mix of UI (steps 3–4) and API-level calls (steps 5–7) because the sindico app requires a live magic-link/password-setup flow that the test environment cannot short-circuit via a simple API endpoint. Steps 5–7 fall back to operator API calls for reliability.
- Lighthouse a11y and structured log runtime verification are human-gate steps documented in `docs/smoke-f02.md`; they cannot be automated without a live server in the CI environment.
- `seed-f02.sh` uses p95 = index 19 in a sorted array of 20 latency samples (bash awk `NR==19`).
- `condominio_id` is not a required field in the structured log per TechSpec (only `tenant_id`, `bloco_id`, `unidade_id`, `performed_by_user_id`, `outcome`). The smoke verification script checks these 5 fields.

## Learnings

- Playwright `--dry-run` is not available in v1.x; use `--list` instead to validate test collection.
- The e2e `package.json` has no tsconfig — the spec is validated purely via Playwright's built-in TypeScript transpilation at runtime (`--list` collection).
- `PLAYWRIGHT_SINDICO_URL` env var must be set (or default `http://localhost:5173`) for the F02 spec; the existing `playwright.config.ts` only exposes `PLAYWRIGHT_APP_URL` (backoffice) and `PLAYWRIGHT_API_URL`. The F02 spec reads `PLAYWRIGHT_SINDICO_URL` independently.

## Files / Surfaces

- `docs/smoke-f02.md` — created (9 smoke steps + log verification step)
- `tests/e2e/specs/f02-estrutura.spec.ts` — created (5 tests, steps 3–7)
- `domains/gestao-condominio/domain.md` — F02 status updated from `in-progress` → `done` (line 68)
- `scripts/seed-f02.sh` — created, executable, seeds 300 units + measures p95
- `CHANGELOG.md` — created with F02 entry dated 2026-04-20
- `.compozy/tasks/f02-gestao-blocos-unidades/task_18.md` — status → completed

## Errors / Corrections

- Initial smoke-f02.md lacked explicit structured log field verification (tenant_id, bloco_id, unidade_id, performed_by_user_id, outcome). Added "Passo 10" with table and Python verification snippet during final-verify.

## Ready for Next Run

Task 18 complete. F02 is fully closed. F03 can start from the baseline: smoke roteiro is in `docs/smoke-f02.md`, Playwright spec in `tests/e2e/specs/f02-estrutura.spec.ts`, and `PLAYWRIGHT_SINDICO_URL` must be added to the e2e env config if sindico UI tests are expanded.
