---
status: completed
title: Síndico SPA — setup-password + login + home placeholder
type: frontend
complexity: medium
dependencies:
  - task_18
  - task_20
---

# Task 24: Síndico SPA — setup-password + login + home placeholder

## Overview
Implementa o esqueleto mínimo da SPA do síndico, cobrindo o fluxo crítico do F01: a tela de definição de senha acessada via magic link (`/setup-password?token=...`) e a tela de login. Inclui também uma home vazia ("Bem-vindo, {nome}") para fechar o fluxo. Qualquer feature funcional do síndico (F02, F03 etc.) é escopo de outros PRDs.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST invocar skill `portabox-design` antes de implementar telas (ADR-010)
- MUST implementar rota `/setup-password` pública que lê `token` da query string, mostra form com senha + confirmação, e submete `POST /api/v1/auth/password-setup`
- MUST mostrar requisitos de senha (min 10 chars, letras + dígito) conforme política configurada em task_16
- MUST tratar resposta 200 exibindo confirmação pt-BR ("Senha definida com sucesso") e redirecionando para `/login` após 2s
- MUST tratar 400 genérico com mensagem pt-BR neutra ("Link inválido ou expirado. Entre em contato com a equipe do condomínio para receber um novo link.") — sem revelar a causa
- MUST implementar rota `/login` com form de e-mail + senha chamando `POST /api/v1/auth/login`; sucesso redireciona para `/`
- MUST implementar rota `/` protegida que mostra home placeholder ("Bem-vindo, {nome}") com layout mínimo (topbar com logo + nome + logout)
- MUST usar `packages/ui` (Button, Input, Card) e tokens do design system
- MUST exibir logo PortaBox do skill (`logo-portabox.png`)
- MUST aplicar guard `<RequireSindico />` nas rotas protegidas
</requirements>

## Subtasks
- [x] 24.1 Implementar `SetupPasswordPage` com form + submit + feedback
- [x] 24.2 Implementar `LoginPage` do síndico (análoga ao backoffice mas standalone)
- [x] 24.3 Implementar `HomePage` placeholder
- [x] 24.4 Implementar guard `<RequireSindico />`
- [x] 24.5 Integrar API client (reaproveita base de task_21 ou copia minimamente)
- [x] 24.6 Configurar rotas com React Router

## Implementation Details
Conforme ADR-006 (respostas genéricas) e ADR-010 (design system). Síndico SPA é minimalista; não compartilha componentes de layout com backoffice (que tem sidebar/navegação para operador). O `packages/ui` é compartilhado; o chassi de cada SPA é independente.

### Relevant Files
- `apps/sindico/src/main.tsx` (a criar)
- `apps/sindico/src/app/routes.tsx` (a criar)
- `apps/sindico/src/features/auth/pages/LoginPage.tsx` (a criar)
- `apps/sindico/src/features/auth/pages/SetupPasswordPage.tsx` (a criar)
- `apps/sindico/src/features/home/HomePage.tsx` (a criar)
- `apps/sindico/src/shared/api/client.ts` (a criar)
- `apps/sindico/src/shared/auth/RequireSindico.tsx` (a criar)
- `apps/sindico/src/shared/layouts/PublicLayout.tsx` (a criar)
- `apps/sindico/src/shared/layouts/PrivateLayout.tsx` (a criar)

### Dependent Files
- `task_25` (Playwright) exerce a tela de setup-password dentro do fluxo E2E

### Related ADRs
- [ADR-006: Magic Link](../adrs/adr-006.md) — define respostas genéricas.
- [ADR-010: Design system PortaBox](../adrs/adr-010.md) — copy + tokens.

## Deliverables
- SPA do síndico buildável com 3 rotas funcionais
- Fluxo de setup-password end-to-end operante contra a API
- Login operante contra `POST /api/v1/auth/login`
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests com mock da API **(REQUIRED)**

## Tests
- Unit tests (Vitest + Testing Library):
  - [x] `SetupPasswordPage` desabilita submit enquanto a senha não atende política
  - [x] `SetupPasswordPage` mostra mensagem genérica em 400 sem revelar detalhes
  - [x] `LoginPage` valida e-mail + senha obrigatórios antes do submit
  - [x] `HomePage` mostra "Bem-vindo, {nome}" com o nome vindo do contexto de auth
  - [x] `<RequireSindico />` redireciona para `/login` quando sem sessão
- Integration tests (MSW):
  - [x] Fluxo feliz: abrir `/setup-password?token=XYZ` → submeter senha válida → toast sucesso → redirect para `/login` após 2s
  - [x] Token inválido: mensagem genérica + botão "Voltar para login"
  - [x] Login feliz com credenciais do síndico recém-setup chega em `/` e exibe nome
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Síndico consegue definir senha e logar end-to-end usando apenas o e-mail recebido
- Respostas não vazam razão de falha
- Design system aplicado consistentemente
