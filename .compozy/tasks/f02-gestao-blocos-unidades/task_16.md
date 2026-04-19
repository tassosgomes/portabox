---
status: pending
title: apps/sindico mutações de Unidade (hooks + UnidadeForm + modais)
type: frontend
complexity: medium
dependencies:
  - task_14
---

# Task 16: apps/sindico mutações de Unidade (hooks + UnidadeForm + modais)

## Overview
Complementa `<EstruturaPage>` com as 3 mutações de Unidade: criar (form com andar + número), inativar (confirm modal) e reativar (confirm modal). Segue o mesmo padrão das mutações de Bloco (task_15) com optimistic update e invalidação da query `['estrutura', condominioId]`. Como Unidade é imutável, **não há** mutação de rename.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar hooks em `apps/sindico/src/features/estrutura/hooks/`: `useCriarUnidade`, `useInativarUnidade`, `useReativarUnidade` — cada um um `useMutation` com invalidação `queryKeys.estrutura(condominioId)` em `onSuccess`
- MUST criar `<UnidadeForm>` com campos `andar` (number, ≥ 0) e `numero` (string), validação zod refletindo regras do validator backend (regex `^[0-9]{1,4}[A-Za-z]?$`), normalização client-side para caixa alta antes do submit
- MUST criar modal de confirmação de inativação com copy explicando impacto em moradores associados ("Moradores associados permanecem vinculados; inative-os separadamente em F03 se necessário")
- MUST criar modal de confirmação de reativação com copy explicando que conflito canônico será rejeitado pelo backend ("Se já existir outra unidade ativa com a mesma tripla, a reativação falhará")
- MUST integrar na `<EstruturaPage>`:
  - Botão "Adicionar unidade" no contexto de um bloco ativo selecionado
  - Atalho "Adicionar próxima unidade" que mantém o bloco atual e pede apenas `andar` + `numero` em loop (UX de rajada — ADR-001 PRD)
  - Menu de ações por unidade com Inativar/Reativar
- MUST mapear `ApiError.status === 409` (tripla duplicada) em toast claro
- MUST mapear `ApiError.status === 422` (bloco inativo) em toast direcionando a reativar o bloco antes
- SHOULD implementar optimistic update em `useCriarUnidade`: inserir leaf temporário no andar correto
</requirements>

## Subtasks
- [ ] 16.1 Criar 3 hooks (`useCriarUnidade`, `useInativarUnidade`, `useReativarUnidade`) com invalidação
- [ ] 16.2 Criar `<UnidadeForm>` com react-hook-form + zod + normalização caixa alta
- [ ] 16.3 Implementar atalho "Adicionar próxima unidade" mantendo contexto do bloco
- [ ] 16.4 Criar modais de confirmação para inativar/reativar Unidade
- [ ] 16.5 Integrar na `<EstruturaPage>` (menu de ações por unidade + CTA por bloco)
- [ ] 16.6 Escrever testes Vitest + Testing Library para os fluxos

## Implementation Details
Ver TechSpec seção **Data Flow C2** (criar unidade) e **C5** (reativar com conflito). Zod schema:

```typescript
const unidadeSchema = z.object({
  andar: z.coerce.number().int().min(0),
  numero: z.string().trim().regex(/^[0-9]{1,4}[A-Za-z]?$/i, 'Formato inválido'),
});
```

Normalização client-side: `numero = numero.toUpperCase()` antes do submit.

UX de rajada ("Adicionar próxima unidade"): após submit bem-sucedido, limpar campos, manter modal aberto, focar primeiro campo. Tecla `Esc` fecha; `Enter` submete.

### Relevant Files
- `apps/sindico/src/features/estrutura/hooks/useCriarUnidade.ts` — novo
- `apps/sindico/src/features/estrutura/hooks/useInativarUnidade.ts` — novo
- `apps/sindico/src/features/estrutura/hooks/useReativarUnidade.ts` — novo
- `apps/sindico/src/features/estrutura/components/UnidadeForm.tsx` — novo
- `apps/sindico/src/features/estrutura/components/UnidadeActionsMenu.tsx` — novo
- `apps/sindico/src/features/estrutura/EstruturaPage.tsx` — estender integrando mutações
- `apps/sindico/src/features/estrutura/__tests__/` — testes
- `@portabox/api-client/src/modules/unidades.ts` — consumido
- `packages/ui/src/ConfirmModal/ConfirmModal.tsx` — consumido

### Dependent Files
- Task 18 exercita o fluxo completo de Unidade no smoke E2E
- Task 15 serve como referência cruzada de padrão (hooks + modais)

### Related ADRs
- [ADR-001: Abordagem MVP Puro](adrs/adr-001.md) — cadastro manual + atalho de rajada
- [ADR-002: Forma Canônica Estrita](adrs/adr-002.md) — validação de andar/número
- [ADR-003: Remoção Exclusivamente por Inativação; Edição Restrita](adrs/adr-003.md) — Unidade imutável (sem rename)
- [ADR-010: TanStack Query + React Context](adrs/adr-010.md) — padrão de mutations

## Deliverables
- 3 hooks de mutação de Unidade com invalidação correta
- `<UnidadeForm>` com validação e normalização
- Atalho "Adicionar próxima unidade" funcional (reduz cliques em cadastro em rajada)
- Modais de confirmação com copy explicativo
- Unit tests with 80%+ coverage **(REQUIRED)**
- Integration tests com MSW cobrindo fluxo completo **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] `useCriarUnidade` chama `criarUnidade` com payload correto e invalida a query
  - [ ] `useCriarUnidade` aplica optimistic update (leaf temporário) e reverte em erro
  - [ ] `useInativarUnidade` e `useReativarUnidade` invalidam query após sucesso
  - [ ] `<UnidadeForm>` rejeita `numero="1AB"`, `"12345"`, `""` com erro inline
  - [ ] `<UnidadeForm>` rejeita `andar=-1` com erro inline
  - [ ] `<UnidadeForm>` normaliza `numero="101a"` → `"101A"` antes do submit
  - [ ] Atalho "Adicionar próxima": após sucesso, campos limpam e foco volta ao primeiro campo; modal não fecha
  - [ ] `ApiError` 409 (tripla duplicada) aparece em toast com mensagem explicativa
  - [ ] `ApiError` 422 (bloco inativo) aparece em toast direcionando a reativar o bloco
- Integration tests:
  - [ ] MSW simula criação em rajada: 3 submits consecutivos → 3 leaves aparecem na árvore
  - [ ] MSW simula 409 em create → toast + form permanece aberto
  - [ ] MSW simula inativação → unidade some com toggle off; reaparece com toggle on
  - [ ] MSW simula reativação com conflito → toast + nenhuma mudança visual
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Síndico consegue cadastrar rajada de 10 unidades em < 2 minutos (medido informalmente)
- UX de "próxima unidade" reduz cliques para ≤ 3 por unidade após a primeira
- Nenhum freeze visual em optimistic update
