---
status: completed
title: Componentes Tree + TreeNode + ConfirmModal em packages/ui
type: frontend
complexity: medium
dependencies: []
---

# Task 12: Componentes Tree + TreeNode + ConfirmModal em packages/ui

## Overview
Adiciona ao pacote compartilhado `packages/ui` (já scaffoldado em F01 task_20) os componentes genéricos de árvore (`<Tree>`, `<TreeNode>`) e de confirmação (`<ConfirmModal>`) que serão consumidos por `apps/sindico` e `apps/backoffice` em F02. Estes componentes respeitam tokens do `portabox-design` e ficam prontos para reuso em features futuras que também lidem com estruturas hierárquicas (ex.: organograma de moradores por unidade em F09).

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST adicionar ao `packages/ui` os componentes `<Tree>`, `<TreeNode>`, `<ConfirmModal>` com aderência total aos tokens e tipografia de `portabox-design` (ADR-010 de F01)
- MUST expor `<Tree>` como componente genérico com prop `items: TreeItem[]` onde cada `TreeItem` tem `id`, `label`, `children?`, `badge?`, `state?: 'default' | 'inactive'`, `onClick?`
- MUST expor controle de expand/collapse por nó com estado controlado (prop `expandedIds: string[] + onExpandChange`) e fallback uncontrolled (default: todos colapsados)
- MUST expor acessibilidade WAI-ARIA: `role="tree"` e `role="treeitem"`, `aria-expanded`, `aria-level`, navegação por teclado (setas esquerda/direita colapsa/expande; cima/baixo navega; Enter aciona `onClick`)
- MUST expor `<TreeNode>` como elemento renderizado internamente — ou reutilizado por consumidores que queiram customizar rendering via render prop
- MUST criar `<ConfirmModal>` reutilizável com props `title`, `description`, `confirmLabel`, `cancelLabel`, `danger?: boolean`, `onConfirm`, `onCancel`, `open`
- MUST adicionar ícones via Lucide conforme padrão do portabox-design (`ChevronRight`, `ChevronDown`, `Building2`, `Home`)
- SHOULD expor storybook stories para os 3 componentes (se F01 task 20 introduziu Storybook); senão, stories em Vitest snapshot
</requirements>

## Subtasks
- [x] 12.1 Criar `packages/ui/src/Tree/Tree.tsx` + `TreeNode.tsx` com API genérica
- [x] 12.2 Implementar expand/collapse controlled + uncontrolled
- [x] 12.3 Implementar acessibilidade WAI-ARIA + navegação por teclado
- [x] 12.4 Criar `packages/ui/src/ConfirmModal/ConfirmModal.tsx` com variante `danger`
- [x] 12.5 Exportar via `packages/ui/src/index.ts`
- [x] 12.6 Escrever testes Vitest + Testing Library para interações principais

## Implementation Details
Ver ADR-010 de F01 para padrões de design system e consumo de tokens. Antes de iniciar, invocar a skill `portabox-design` para carregar tokens atualizados.

O componente `<Tree>` deve ser agnóstico a F02 — não conter referências a "bloco" ou "unidade". O consumidor em `apps/sindico` (task_14) transforma `Estrutura` em `TreeItem[]` e passa ao `<Tree>`.

Exemplo de shape genérico (contrato):
```typescript
type TreeItem = {
  id: string;
  label: ReactNode;
  children?: TreeItem[];
  badge?: ReactNode;
  state?: 'default' | 'inactive';
  actions?: ReactNode;
  onClick?: () => void;
};
```

### Relevant Files
- `packages/ui/src/Tree/Tree.tsx` — novo
- `packages/ui/src/Tree/TreeNode.tsx` — novo
- `packages/ui/src/Tree/types.ts` — novo
- `packages/ui/src/ConfirmModal/ConfirmModal.tsx` — novo
- `packages/ui/src/index.ts` — atualizar exports
- `packages/ui/package.json` — se precisar adicionar dependência (`lucide-react` já deve estar instalado)
- `.claude/skills/portabox-design/` — consulta para tokens

### Dependent Files
- `apps/sindico/src/features/estrutura/EstruturaPage.tsx` (task_14) consumirá `<Tree>`
- `apps/backoffice/src/features/tenants/.../estrutura/EstruturaReadOnlyPage.tsx` (task_17) idem
- Modais de confirmação em mutações (task_15, task_16, task_17) consumirão `<ConfirmModal>`

### Related ADRs
- [ADR-010: Baseline Frontend — TanStack Query + React Context](adrs/adr-010.md) — componentes compartilhados em `packages/ui` como padrão
- F01 [ADR-010 de F01: Design System PortaBox obrigatório] — consumo de tokens

## Deliverables
- `<Tree>`, `<TreeNode>`, `<ConfirmModal>` exportados de `packages/ui`
- Acessibilidade WAI-ARIA validada em tests
- Navegação por teclado funcional (setas + Enter)
- Tokens de `portabox-design` aplicados sem hardcodes
- Unit tests with 80%+ coverage **(REQUIRED)** — Vitest + Testing Library
- Integration tests via Storybook ou snapshot **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `<Tree>` renderiza itens e respeita ordem
  - [x] Click em `<TreeNode>` com `children` alterna expand/collapse (uncontrolled)
  - [x] `expandedIds` prop controla expand externamente (controlled); `onExpandChange` é chamado
  - [x] Navegação por teclado: seta direita expande; esquerda colapsa; cima/baixo move foco; Enter aciona `onClick`
  - [x] `aria-expanded` reflete estado; `aria-level` crescente por profundidade
  - [x] Item com `state="inactive"` renderiza com estilo descontrast + ícone distinto
  - [x] `<ConfirmModal>` chama `onConfirm` ao clicar em confirm; `onCancel` ao cancelar; Escape fecha
  - [x] `<ConfirmModal>` com `danger=true` estiliza botão de confirmar conforme design system
- Integration tests:
  - [x] Storybook renderiza 3 stories (Tree básica, Tree com inativos, ConfirmModal danger)
  - [x] Snapshot dos 3 componentes não regrida entre commits
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Componentes consumíveis por `apps/sindico` e `apps/backoffice` sem wrappers intermediários
- Nenhum token CSS hardcoded — tudo vem de `portabox-design`
- Lighthouse a11y score ≥ 95 para páginas que usarem `<Tree>` (medido em task_18)
