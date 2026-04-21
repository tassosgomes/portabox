---
status: completed
title: apps/sindico mutações de Bloco (hooks + BlocoForm + modais)
type: frontend
complexity: medium
dependencies:
  - task_14
---

# Task 15: apps/sindico mutações de Bloco (hooks + BlocoForm + modais)

## Overview
Adiciona à `<EstruturaPage>` as 4 mutações de Bloco: criar (form), renomear (form), inativar (confirm modal) e reativar (confirm modal). Cada mutação usa `useMutation` do TanStack Query com optimistic update + invalidação da query `['estrutura', condominioId]` após sucesso, e mapeia erros de API (`ApiError` 409/422) em mensagens acionáveis.

> **Contrato:** requests (`CreateBlocoRequest`, `RenameBlocoRequest`) e response (`Bloco`) vêm tipados de `@portabox/api-client` — derivados do [`api-contract.yaml`](../api-contract.yaml). Valida o contrato mental da mutação: a regex de validação do `nome` (1–50 chars) já está no schema do contrato e no validator backend (task_06); o zod schema no form deve refletir as mesmas restrições.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar hooks em `apps/sindico/src/features/estrutura/hooks/`: `useCriarBloco`, `useRenomearBloco`, `useInativarBloco`, `useReativarBloco` — cada um um `useMutation` com `onSuccess` invalidando `queryKeys.estrutura(condominioId)`
- MUST criar `<BlocoForm>` reutilizável (modo create + modo rename) com `react-hook-form` + validação `zod` refletindo as regras do validator backend (nome 1–50 chars, trim não-vazio)
- MUST criar modais de confirmação para inativar e reativar (reutilizam `<ConfirmModal>` de `packages/ui`) com copy explicando implicações (ex.: "Inativar este bloco vai ocultá-lo de novos cadastros; unidades ativas permanecem e precisam ser inativadas separadamente")
- MUST integrar os botões na `<EstruturaPage>`: CTA "Novo bloco" no header da árvore; menu de ações em cada `<TreeNode>` de bloco com "Renomear", "Inativar"/"Reativar" conforme status
- MUST mapear `ApiError.status === 409` em mensagem de toast + abrir automaticamente opção de "reativar ao invés de criar" se o erro indicar bloco inativo com mesmo nome
- MUST implementar optimistic update em `useCriarBloco`: inserir nó temporário na árvore local antes do round-trip; reverter em caso de erro
- SHOULD adicionar a11y aos modais (focus trap, Escape fecha, aria-labelledby)
</requirements>

## Subtasks
- [x] 15.1 Criar 4 hooks (`useCriarBloco`, `useRenomearBloco`, `useInativarBloco`, `useReativarBloco`) com optimistic update onde aplicável
- [x] 15.2 Criar `<BlocoForm>` reutilizável (create/rename) com react-hook-form + zod
- [x] 15.3 Integrar CTA "Novo bloco" na `<EstruturaPage>` + menu de ações por bloco
- [x] 15.4 Implementar modais de inativar/reativar consumindo `<ConfirmModal>`
- [x] 15.5 Mapear erros `ApiError` (409/422) em copy acionável
- [x] 15.6 Escrever testes Vitest + Testing Library dos fluxos

## Implementation Details
Ver TechSpec seção **Data Flow C1, C3, C4, C5** para comportamento esperado; ADR-010 para padrões de `useMutation`.

Exemplo de hook:
```typescript
export function useCriarBloco(condominioId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { nome: string }) => criarBloco({ condominioId, nome: input.nome }),
    onMutate: async (input) => { /* optimistic insert */ },
    onError: (err, input, context) => { /* revert */ },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.estrutura(condominioId) }),
  });
}
```

Zod schema:
```typescript
const blocoSchema = z.object({
  nome: z.string().trim().min(1, 'Obrigatório').max(50, 'Máximo 50 caracteres'),
});
```

### Relevant Files
- `apps/sindico/src/features/estrutura/hooks/useCriarBloco.ts` — novo
- `apps/sindico/src/features/estrutura/hooks/useRenomearBloco.ts` — novo
- `apps/sindico/src/features/estrutura/hooks/useInativarBloco.ts` — novo
- `apps/sindico/src/features/estrutura/hooks/useReativarBloco.ts` — novo
- `apps/sindico/src/features/estrutura/components/BlocoForm.tsx` — novo
- `apps/sindico/src/features/estrutura/components/BlocoActionsMenu.tsx` — novo (menu contextual por nó)
- `apps/sindico/src/features/estrutura/EstruturaPage.tsx` — estender (integrar botões e modais)
- `apps/sindico/src/features/estrutura/__tests__/` — testes
- `packages/ui/src/ConfirmModal/ConfirmModal.tsx` — consumido
- `@portabox/api-client/src/modules/blocos.ts` — consumido

### Dependent Files
- Task 16 adiciona mutações de Unidade seguindo o mesmo padrão — referência cruzada
- Task 18 valida fluxo completo de criação → renomeação → inativação → reativação no smoke E2E

### Related ADRs
- [ADR-003: Remoção Exclusivamente por Inativação; Edição Restrita](adrs/adr-003.md) — bloco renomeável
- [ADR-009: Endpoint Único Retornando Árvore Completa](adrs/adr-009.md) — invalidação de cache por query key
- [ADR-010: TanStack Query + React Context](adrs/adr-010.md) — padrão de mutations

## Deliverables
- 4 hooks de mutação de Bloco com invalidação correta
- `<BlocoForm>` (create/rename) com validação zod
- Menu de ações por bloco + modais de confirmação
- Tratamento amigável de erros (409, 422)
- Unit tests with 80%+ coverage **(REQUIRED)**
- Integration tests com MSW cobrindo fluxo completo **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `useCriarBloco` chama `criarBloco` com payload correto e invalida `queryKeys.estrutura(id)` após sucesso
  - [x] `useCriarBloco` aplica optimistic update (nó temporário aparece na árvore) e reverte em erro
  - [x] `useRenomearBloco` chama `renomearBloco` e invalida query
  - [x] `useInativarBloco` chama `inativarBloco` e invalida query
  - [x] `useReativarBloco` trata erro 409 "conflito canônico" com mensagem clara
  - [x] `<BlocoForm>` valida nome vazio (erro inline), > 50 chars (erro inline), submit com sucesso (chama `onSubmit`)
  - [x] Modais de inativar/reativar: cancel fecha sem chamar mutation; confirm chama mutation
- Integration tests:
  - [x] MSW simula fluxo: user clica "Novo bloco" → preenche form → submit → bloco aparece na árvore
  - [x] MSW simula 409 em create (nome duplicado) → toast explica erro; form fica aberto para edição
  - [x] MSW simula fluxo de rename → nome atualiza na árvore
  - [x] MSW simula fluxo de inativar → bloco some (com toggle inativos off) e reaparece (com toggle on, descontrast)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Síndico consegue fazer o ciclo completo de CRUD soft-delete de Bloco pela UI
- Mensagens de erro 409/422 explicativas suficientes para o síndico agir sozinho
- Nenhum flicker visível em optimistic updates
