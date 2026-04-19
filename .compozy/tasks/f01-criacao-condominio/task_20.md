---
status: completed
title: Monorepo frontend baseline (Vite TS + tokens PortaBox + packages/ui + Lucide)
type: frontend
complexity: high
dependencies:
  - task_01
---

# Task 20: Monorepo frontend baseline (Vite TS + tokens PortaBox + packages/ui + Lucide)

## Overview
Estabelece a base de todos os frontends do projeto: Vite + React + TypeScript strict, consumo dos tokens do skill `portabox-design` via CSS vars, fontes Google (Plus Jakarta Sans, Inter, JetBrains Mono), `lucide-react` e os componentes compartilhados em `packages/ui/` (Button, Input, Card, Badge, Modal, StepIndicator). É pré-requisito para as SPAs de backoffice e síndico.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST antes de qualquer código invocar a skill `portabox-design` e ler `README.md`, `colors_and_type.css` e `ui_kits/admin-dashboard/` como referência (ADR-010)
- MUST configurar `apps/backoffice`, `apps/sindico` e `packages/ui` com Vite + React 18 + TypeScript em modo strict (seguindo skill `react-architecture` e `react-code-quality`)
- MUST importar `colors_and_type.css` do skill em `apps/*/src/styles/tokens.css` (via alias Vite `@design-tokens` ou path relativo)
- MUST adicionar Google Fonts (Plus Jakarta Sans 400–800, Inter 400–700, JetBrains Mono 400–500) no `index.html` de cada app
- MUST implementar componentes compartilhados em `packages/ui/src/`:
  - `Button` (variantes `primary` pill laranja, `secondary` navy ghost, `danger`)
  - `Input` (radius `--r-md` 10px, sombra `--sh-sm`, focus ring `--sh-focus`)
  - `Card` (radius `--r-lg` 14px, sombra `--sh-md`, sem borda)
  - `Badge` (status pill: `PreAtivo`, `Ativo`, etc.)
  - `Modal` (radius `--r-xl` 20px, sombra `--sh-lg`, backdrop)
  - `StepIndicator` (para o wizard de 3 etapas)
- MUST exportar ícones Lucide recomendados via re-export (`building-2`, `file-text`, `user-plus`, `mail`, `check-circle`, `upload-cloud`, `clipboard-check`, `power`)
- MUST configurar alias `@` em cada app conforme skill `react-architecture`
- MUST adicionar Storybook ou um `preview` simples HTML local para validar os componentes visualmente
- MUST configurar ESLint + Prettier com regras das skills `react-code-quality`
</requirements>

## Subtasks
- [x] 20.1 Scaffold dos três pacotes (`apps/backoffice`, `apps/sindico`, `packages/ui`) com Vite + TS strict
- [x] 20.2 Importar `colors_and_type.css` da skill em cada app + `index.html` com Google Fonts
- [x] 20.3 Implementar componentes básicos em `packages/ui/` seguindo `preview/component-*.html` do skill
- [x] 20.4 Configurar `lucide-react` + re-export dos ícones canônicos
- [x] 20.5 Configurar aliases Vite/tsconfig e ESLint/Prettier
- [x] 20.6 Criar `packages/ui/.storybook/` (ou página `preview.html` em dev) para validar componentes
- [x] 20.7 Adicionar script `pnpm test` (Vitest) configurado em cada pacote

## Implementation Details
Conforme ADR-010 (uso da skill `portabox-design`) e skills `react-architecture` + `react-code-quality`. Componentes em `packages/ui` usam apenas CSS vars — nada hardcoded. Cada componente tem um `.stories.tsx` ou HTML de preview mostrando os estados (default, hover, focus, disabled, error).

### Relevant Files
- `.claude/skills/portabox-design/README.md` — leitura obrigatória antes do scaffold
- `.claude/skills/portabox-design/colors_and_type.css` — tokens (importado em build)
- `apps/backoffice/vite.config.ts` (a criar)
- `apps/backoffice/tsconfig.json` (a criar)
- `apps/backoffice/src/styles/tokens.css` — re-export do skill (a criar)
- `apps/backoffice/index.html` (a criar)
- `apps/sindico/vite.config.ts` (a criar)
- `apps/sindico/tsconfig.json` (a criar)
- `apps/sindico/src/styles/tokens.css` (a criar)
- `apps/sindico/index.html` (a criar)
- `packages/ui/package.json` (a criar)
- `packages/ui/src/button/Button.tsx` (a criar)
- `packages/ui/src/input/Input.tsx` (a criar)
- `packages/ui/src/card/Card.tsx` (a criar)
- `packages/ui/src/badge/Badge.tsx` (a criar)
- `packages/ui/src/modal/Modal.tsx` (a criar)
- `packages/ui/src/step-indicator/StepIndicator.tsx` (a criar)
- `packages/ui/src/icons/index.ts` — re-export Lucide (a criar)
- `packages/ui/src/index.ts` — public API (a criar)
- `.eslintrc.cjs` (a criar)
- `.prettierrc` (a criar)

### Dependent Files
- `task_21`, `task_22`, `task_23`, `task_24` consomem `packages/ui`
- `task_25` (Playwright) usa seletores visuais das SPAs

### Related ADRs
- [ADR-005: Backoffice como SPA React Separado](../adrs/adr-005.md)
- [ADR-010: Frontend Adere ao Design System `portabox-design`](../adrs/adr-010.md)

## Deliverables
- Monorepo frontend com três pacotes buildáveis
- `packages/ui` com 6 componentes consumindo tokens + preview
- ESLint/Prettier/TS strict operacionais
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para exports e tokens **(REQUIRED)**

## Tests
- Unit tests (Vitest + Testing Library):
  - [x] `Button` renderiza com variante `primary` e classe/style derivada de `--pb-orange-500`
  - [x] `Button` muda aparência no hover (teste com user-event + assert em estilo computado ou classe)
  - [x] `Input` aplica `aria-invalid` quando em estado de erro
  - [x] `Modal` respeita `aria-modal="true"` e foco é trapped dentro do modal
  - [x] `StepIndicator` renderiza N passos com o passo atual destacado
- Integration tests:
  - [x] `pnpm build` em `apps/backoffice`, `apps/sindico` e `packages/ui` retorna exit 0
  - [x] Snapshot do `preview.html` dos componentes bate com referência esperada
  - [x] Nenhum CSS hardcoded: grep por `#` em arquivos `.tsx`/`.css` encontra apenas uso via `var(--...)` (script de lint custom ou teste)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Três pacotes buildáveis e testáveis
- `packages/ui` documentado e consumível por ambas as SPAs
- Zero violação do design system (cores/tipografia via tokens)
