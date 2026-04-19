---
status: completed
title: Backoffice SPA — wizard de criação (3 etapas + revisão + submit)
type: frontend
complexity: high
dependencies:
  - task_18
  - task_20
  - task_21
---

# Task 22: Backoffice SPA — wizard de criação (3 etapas + revisão + submit)

## Overview
Implementa o fluxo central de F01: wizard em 3 etapas + revisão final para o operador criar um novo condomínio. Integra com `POST /api/v1/admin/condominios` e, em sucesso, redireciona para a tela de detalhes do tenant criado. Usa `StepIndicator` e componentes do `packages/ui`, seguindo a copy pt-BR aprovada em ADR-010.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST invocar skill `portabox-design` antes de implementar (ADR-010)
- MUST implementar rota `/condominios/novo` com `StepIndicator` exibindo 3 etapas em pt-BR:
  - `1. Dados do condomínio`
  - `2. Consentimento LGPD`
  - `3. Síndico responsável`
- MUST implementar formulário com gerenciamento de estado (react-hook-form ou similar) validando:
  - Etapa 1: nome fantasia obrigatório, CNPJ com máscara + validação de DV, endereço, administradora opcional
  - Etapa 2: data assembleia (≤ hoje), quórum obrigatório, CPF do signatário válido, data termo (≤ hoje)
  - Etapa 3: nome, e-mail, celular E.164
- MUST permitir navegar entre etapas (voltar/avançar) preservando dados
- MUST mostrar tela de revisão consolidada antes do submit final
- MUST submeter para `POST /api/v1/admin/condominios`
- MUST mostrar erro específico em caso de CNPJ duplicado ("Este CNPJ já está cadastrado como `{nome}`, criado em `{data}`") usando os dados do Problem Details RFC 7807
- MUST em sucesso redirecionar para `/condominios/{id}` e mostrar toast "Condomínio criado em estado pré-ativo. Enviamos o link de definição de senha para o síndico."
- MUST desabilitar botão primário durante submit
- MUST aplicar CTA primário pill laranja + CTA secundário navy ghost (packages/ui Button)
- SHOULD implementar upload opcional de documentos de opt-in após criação (chamada separada — pode ficar em task_23 se for mais coeso com tela de detalhes)
</requirements>

## Subtasks
- [x] 22.1 Implementar formulário multi-etapa com estado persistido em memória
- [x] 22.2 Implementar validações client-side com mensagens pt-BR
- [x] 22.3 Implementar tela de revisão consolidando os dados
- [x] 22.4 Integrar com endpoint `POST /api/v1/admin/condominios`
- [x] 22.5 Tratar erros RFC 7807 (CNPJ duplicado, validação, 500)
- [x] 22.6 Implementar toast de sucesso + redirecionamento

## Implementation Details
Usar `StepIndicator` e `Button`/`Input`/`Card` de `packages/ui`. Máscara de CNPJ e validação reaproveita utilitários (pode usar `cpf-cnpj-validator` ou função local). Estado do formulário não precisa persistir em localStorage no MVP — perda de contexto ao navegar é aceitável para piloto.

### Relevant Files
- `apps/backoffice/src/features/condominios/pages/NovoCondominioPage.tsx` (a criar)
- `apps/backoffice/src/features/condominios/components/StepDadosCondominio.tsx` (a criar)
- `apps/backoffice/src/features/condominios/components/StepOptIn.tsx` (a criar)
- `apps/backoffice/src/features/condominios/components/StepSindico.tsx` (a criar)
- `apps/backoffice/src/features/condominios/components/Revisao.tsx` (a criar)
- `apps/backoffice/src/features/condominios/api.ts` — funções do client (a criar)
- `apps/backoffice/src/features/condominios/validation.ts` — validadores (a criar)
- `apps/backoffice/src/features/condominios/types.ts` — tipos (a criar)

### Dependent Files
- `task_23` usa as mesmas tipagens/API
- `task_25` (Playwright) exerce o fluxo end-to-end

### Related ADRs
- [ADR-001: Onboarding de Tenant no MVP](../adrs/adr-001.md) — operador executa o wizard.
- [ADR-010: Design system PortaBox](../adrs/adr-010.md) — copy pt-BR, componentes.

## Deliverables
- Wizard operante com 3 etapas + revisão + submit
- Tratamento de erros específico para CNPJ duplicado
- Redirecionamento e toast pt-BR em sucesso
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests com mock da API **(REQUIRED)**

## Tests
- Unit tests (Vitest + Testing Library):
  - [ ] Renderização inicial mostra etapa 1 ativa
  - [ ] Avançar sem preencher CNPJ impede navegação e mostra mensagem "CNPJ inválido"
  - [ ] CNPJ válido avança para etapa 2
  - [ ] CPF inválido em etapa 2 impede avanço
  - [ ] Celular fora de E.164 em etapa 3 impede submit
  - [ ] Tela de revisão mostra todos os dados formatados
- Integration tests (MSW):
  - [ ] Submissão bem-sucedida redireciona para `/condominios/{id}` e exibe toast
  - [ ] Submissão com 409 (CNPJ duplicado) exibe mensagem com nome + data do tenant existente
  - [ ] Botão primário fica disabled durante submit em andamento
  - [ ] Submissão com 500 exibe toast de erro genérico
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Operador consegue completar o wizard end-to-end em < 3 minutos (medido por teste)
- Erros são claros e acionáveis
- Copy pt-BR + design system corretos
