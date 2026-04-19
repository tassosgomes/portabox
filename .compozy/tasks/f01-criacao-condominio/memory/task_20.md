# Task Memory: task_20.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
COMPLETED. Monorepo frontend baseline fully established:
- 3 packages buildable: `apps/backoffice`, `apps/sindico`, `packages/ui`
- 6 shared components in `packages/ui` consuming design tokens via CSS vars
- 69 tests passing, coverage ≥ 80% across all packages
- ESLint v9 flat config + Prettier at repo root
- `packages/ui/preview.html` for visual validation

## Important Decisions
- Token import: `apps/*/src/styles/tokens.css` → `@import url('../../../../.claude/skills/portabox-design/colors_and_type.css')`. Relative path keeps skill as upstream (ADR-010).
- `packages/ui` uses `vitest.config.ts` with `esbuild.jsx: 'automatic'` (no `@vitejs/plugin-react`); apps use `vitest.config.ts` with the plugin. Both separate from `vite.config.ts`.
- CSS Modules `classNameStrategy: 'non-scoped'` in all `vitest.config.ts` files.
- `color-mix(in srgb, var(--pb-danger) 80%, black)` used for danger-hover instead of hardcoded hex (passes no-hardcode token test).
- `color-mix(in srgb, var(--pb-orange-500) 8%, white)` for step-indicator current circle background.

## Learnings
(Promoted to MEMORY.md for cross-task use)

## Files / Surfaces
- apps/backoffice/: vite.config.ts, vitest.config.ts, tsconfig.json/app/node, index.html, src/main.tsx, App.tsx, App.test.tsx, styles/tokens.css, vite-env.d.ts, test-setup.ts
- apps/sindico/: same structure as backoffice
- packages/ui/src/: Button, Input, Card, Badge, Modal, StepIndicator (each with .tsx + .module.css + .test.tsx), icons/index.ts, index.ts, test-setup.ts, vite-env.d.ts, exports.test.ts, tokens.test.ts
- packages/ui/: vitest.config.ts, tsconfig.json, preview.html
- Root: eslint.config.js, .prettierrc, .prettierignore, .npmrc (onlyBuiltDependencies[]=esbuild)

## Errors / Corrections
- `test` property in `vite.config.ts` breaks `tsc -b` — moved to separate `vitest.config.ts`
- `@vitejs/plugin-react` not hoisted by pnpm to `packages/ui` — use esbuild JSX transform in vitest
- CSS Module class names hashed in tests — fixed with `classNameStrategy: 'non-scoped'`
- `import.meta.url` pathname resolution in tokens.test.ts — use `import.meta.dirname` (Node ≥21)

## Ready for Next Run
Task complete. tasks 21–25 can consume `@portabox/ui` via `workspace:*`.
