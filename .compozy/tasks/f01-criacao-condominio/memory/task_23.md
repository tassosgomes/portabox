# Task Memory: task_23.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

Task 23 implemented and verified complete: backoffice SPA screens — list, details, activate, resend magic link, opt-in document upload/download.

## Important Decisions

- `validateFile` extracted to `uploadUtils.ts` (separate from `UploadOptInDocument.tsx`) to satisfy `react-refresh/only-export-components` ESLint rule.
- `apiClient.postFormData` added to `client.ts` — skips `Content-Type: application/json` header when body is `FormData`.
- Loading state in list/details pages uses `aria-live="polite"` (not `role="status"`) to avoid conflict with success toast that also uses `role="status"`.
- React Compiler pattern for effects: initialize `loading: true` in useState; never call setState synchronously in effect bodies; only inside `.then`/`.catch`.

## Learnings

- `eslint-plugin-react-hooks` v7.1.1 includes React Compiler rules. `react-hooks/set-state-in-effect` forbids any synchronous `setState` inside an effect or `useCallback` body — even `setLoading(true)`. All setState must be inside async callbacks.
- MSW + void API: `apiClient.post<void>` calling `res.json()` throws if server returns 200 with null body. Fix: use `HttpResponse.json(null, { status: 200 })` in MSW handlers.
- `userEvent.upload` respects `accept` attribute and filters out disallowed MIME types before firing change event — cannot test MIME type rejection via DOM interaction; must unit-test `validateFile` directly.
- When component file exports both a component and a utility function, move the utility to a separate file (e.g., `uploadUtils.ts`) to satisfy `react-refresh/only-export-components`.

## Files / Surfaces

- `src/shared/api/client.ts` — added FormData support + `postFormData` method
- `src/features/condominios/types.ts` — added list/details/document types
- `src/features/condominios/api.ts` — added 6 new API functions
- `src/features/condominios/uploadUtils.ts` — new: validateFile, MAX_UPLOAD_SIZE_BYTES
- `src/features/condominios/components/StatusBadge.tsx` + test
- `src/features/condominios/components/ActivateTenantAction.tsx` + test
- `src/features/condominios/components/ResendMagicLinkAction.tsx` + test
- `src/features/condominios/components/UploadOptInDocument.tsx` + test
- `src/features/condominios/pages/ListaCondominiosPage.tsx` + test
- `src/features/condominios/pages/DetalhesCondominioPage.tsx` + test
- `src/app/routes.tsx` — replaced placeholder with real pages

## Errors / Corrections

- Removed synchronous `setLoading(true)` from effect/callback bodies (React Compiler rule).
- Fixed `role="status"` conflict between loading text and success toast.
- Fixed MSW void response causing JSON parse error.

## Ready for Next Run

Task 23 complete. 113 tests pass, 0 ESLint errors.
