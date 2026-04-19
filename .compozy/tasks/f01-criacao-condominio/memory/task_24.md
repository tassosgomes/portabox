# Task Memory: task_24.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
Síndico SPA skeleton: `/setup-password`, `/login`, `/` with auth guard — all implemented and all 28 tests passing, build clean, coverage 97%.

## Important Decisions
- `LoginPage` uses `e.currentTarget.checkValidity()` (DOM API) instead of `if (!email || !password)` for the required-field guard. The state-based guard caused flaky tests because `fireEvent.change` doesn't flush React 18 state synchronously and the guard would fire early.
- Integration test for the 2-second redirect uses real timers with `waitFor({ timeout: 3000 })` instead of `vi.useFakeTimers()`. Fake timers called after the real `setTimeout` was registered have no effect on it.
- `vi.resetAllMocks()` is used in `beforeEach` instead of `vi.clearAllMocks()`. `clearAllMocks` only clears call history, NOT the one-time implementation queue. Leftover `mockResolvedValueOnce` from "validates required" test (which never called the mock) poisoned subsequent tests.

## Learnings
- **Mock queue leakage**: `vi.clearAllMocks()` does NOT clear `mockResolvedValueOnce`/`mockRejectedValueOnce` queues. A "once" impl set in test A that is never consumed stays in the queue and is consumed in test B. Always use `vi.resetAllMocks()` in `beforeEach` unless you deliberately want queue persistence.
- **fireEvent.change + React 18**: `fireEvent.change` from `@testing-library/react` does NOT wrap in `act`. In React 18, state updates may not be flushed synchronously. If a guard reads state immediately after `fireEvent.change`, it may see stale state. Use `userEvent.type` or `e.currentTarget.checkValidity()` to avoid reading React state in the event handler.
- **Fake timers timing**: `vi.useFakeTimers()` only intercepts `setTimeout` calls made AFTER it's invoked. If a `useEffect` already called real `setTimeout`, `advanceTimersByTime` won't fire it. Enable fake timers BEFORE rendering if the component uses `setTimeout`.
- **`await act(async () => { await userEvent.click() })`**: Useful when userEvent dispatches an event that triggers an async handler that rejects — the outer act flushes the rejection microtask and applies state updates. But the root cause (mock queue poisoning) should be fixed first.

## Files / Surfaces
- `apps/sindico/src/shared/api/client.ts` — copied from backoffice
- `apps/sindico/src/shared/api/types.ts` — LoginRequest/Response, PasswordSetupRequest
- `apps/sindico/src/features/auth/AuthContext.tsx` — same pattern as backoffice
- `apps/sindico/src/features/auth/hooks/useAuth.ts` — re-exports useAuthContext
- `apps/sindico/src/features/auth/pages/LoginPage.tsx` — form with `noValidate`, checkValidity guard
- `apps/sindico/src/features/auth/pages/SetupPasswordPage.tsx` — password policy, success state, 2s redirect via useEffect
- `apps/sindico/src/features/home/HomePage.tsx` — placeholder
- `apps/sindico/src/shared/auth/RequireSindico.tsx` — auth guard
- `apps/sindico/src/shared/layouts/PublicLayout.tsx` / `PrivateLayout.tsx`
- `apps/sindico/src/app/routes.tsx` — React Router config
- `apps/sindico/src/App.tsx` / `main.tsx` — BrowserRouter + AuthProvider
- Test files: LoginPage.test.tsx, SetupPasswordPage.test.tsx, integration.test.tsx, RequireSindico.test.tsx, HomePage.test.tsx

## Errors / Corrections
- Stale mock queue: fixed with `vi.resetAllMocks()` instead of `vi.clearAllMocks()`
- Integration redirect: fixed by removing fake timers, using real `waitFor({ timeout: 3000 })`
- Required field guard: changed from state check to `checkValidity()` for DOM-based validation

## Ready for Next Run
Task 24 is COMPLETE. All tests pass, build is green. Next task is task_25 (E2E Playwright tests for F01 flow).
