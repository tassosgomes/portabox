# Task Memory: task_21.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

Implement backoffice SPA chassis: cookie-based login, main layout (topbar + sidebar), typed API client, auth context/hook, and `<RequireOperator />` route guard. No F01-specific features yet — only the frame.

## Important Decisions

- `apiClient.get('/v1/auth/me', { noRedirectOn401: true })` used in AuthContext for session check — without this flag, the 401 interceptor would redirect before setting `isAuthenticated: false`.
- Integration tests set `VITE_API_BASE_URL` via vitest.config.ts `define` to `http://localhost/api` so MSW (`msw/node`) can intercept absolute URLs correctly. Without this, relative URLs (`/api/...`) don't resolve to absolute in Node.js environment and MSW handlers don't match.
- `vitest.config.ts` kept separate from `vite.config.ts` (per shared learning for tasks 21–25) and includes `css.modules.classNameStrategy: 'non-scoped'`.
- `useAuth.ts` is a thin re-export of `useAuthContext` from `AuthContext.tsx` — kept to satisfy the public API described in the task spec.
- `AuthContext.tsx` exposes `AuthProvider` + `useAuthContext`; `useAuth.ts` re-exports as `useAuth` per the task spec.

## Learnings

- MSW v2 with `msw/node` intercepts fetch but requires absolute URLs in handler definitions. In vitest/jsdom, `fetch('/relative')` is executed in Node.js context which does NOT resolve relative URLs against a base. Fix: define `import.meta.env.VITE_API_BASE_URL` in vitest config to force absolute API base.
- Removing explicit `act()` wrapper around `userEvent.click()` calls in integration tests eliminates the "not configured to support act(...)" warning — `userEvent` v14 handles async state transitions internally.

## Files / Surfaces

- `apps/backoffice/src/shared/api/client.ts` — API client with XSRF, credentials, 401 interceptor, `noRedirectOn401` option
- `apps/backoffice/src/shared/api/types.ts` — LoginRequest, LoginResponse, ApiError DTOs
- `apps/backoffice/src/features/auth/AuthContext.tsx` — AuthProvider + useAuthContext
- `apps/backoffice/src/features/auth/hooks/useAuth.ts` — re-exports useAuth
- `apps/backoffice/src/features/auth/pages/LoginPage.tsx` + `.module.css`
- `apps/backoffice/src/shared/auth/RequireOperator.tsx`
- `apps/backoffice/src/shared/components/Topbar.tsx` + `.module.css`
- `apps/backoffice/src/shared/components/Sidebar.tsx` + `.module.css`
- `apps/backoffice/src/shared/layouts/AppLayout.tsx` + `.module.css`
- `apps/backoffice/src/app/routes.tsx`
- `apps/backoffice/src/main.tsx` — updated with BrowserRouter + AuthProvider
- `apps/backoffice/src/App.tsx` — updated to render AppRoutes
- `apps/backoffice/vitest.config.ts` — added `define` for VITE_API_BASE_URL
- Tests: `client.test.ts`, `AuthContext.test.tsx`, `LoginPage.test.tsx`, `integration.test.tsx`, `App.test.tsx`
- Dependencies added: `react-router-dom`, `msw` (devDep)

## Errors / Corrections

- Initial integration tests failed because MSW handlers used `http://localhost/api` base but API client called `/api/...` (relative). Fixed by adding `define: { 'import.meta.env.VITE_API_BASE_URL': '"http://localhost/api"' }` to vitest.config.ts.
- Had to add `noRedirectOn401: true` to the `/me` session check call to prevent auth-init loop (401 → redirect → re-init).

## Ready for Next Run

Task 22 (wizard) and task 23 (list/details) can now import `useAuth`, `apiClient`, `AppLayout`, and `RequireOperator` from the surfaces above. The `/condominios` route renders a placeholder `<h2>Condomínios</h2>` — tasks 22/23 should replace that.
