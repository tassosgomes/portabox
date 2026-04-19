---
status: completed
title: Backoffice SPA — autenticação + layout + API client + guards
type: frontend
complexity: medium
dependencies:
  - task_18
  - task_20
---

# Task 21: Backoffice SPA — autenticação + layout + API client + guards

## Overview
Coloca em pé a casca do backoffice: fluxo de login cookie-based contra `POST /api/v1/auth/login`, layout base (topbar com logo PortaBox + avatar do operador, sidebar com navegação), API client tipado (fetch/axios + interceptors) e guards de rota que exigem role `Operator`. Nada de features específicas de F01 ainda — apenas o chassi.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST invocar skill `portabox-design` antes de implementar qualquer tela (ADR-010)
- MUST implementar tela de login seguindo visual do kit `admin-dashboard` (card centralizado, Input, Button primário, logo)
- MUST implementar layout principal: topbar fixo (56px) com logo + nome do operador + logout; sidebar 240px com item "Condomínios"
- MUST implementar API client (`src/shared/api/client.ts`) com:
  - Base URL lida de `VITE_API_BASE_URL`
  - `credentials: 'include'` para cookies
  - Header `X-XSRF-TOKEN` propagado a partir do cookie antiforgery
  - Interceptor que redireciona para `/login` em 401
  - Tipagens de request/response gerados a partir do OpenAPI (ou definidos manualmente)
- MUST implementar `useAuth()` hook que expõe `user`, `isAuthenticated`, `login`, `logout`
- MUST implementar `<RequireOperator />` que guarda rotas e redireciona para `/login` quando sem sessão
- MUST implementar rota `/login` pública + `/` protegida (redireciona para `/condominios`)
- MUST aplicar copy pt-BR conforme ADR-010 (`Bem-vindo ao PortaBox`, `Entrar`, `Sair`)
</requirements>

## Subtasks
- [x] 21.1 Implementar tela de login com design system
- [x] 21.2 Implementar layout principal (topbar + sidebar)
- [x] 21.3 Implementar API client + antiforgery + interceptors 401
- [x] 21.4 Implementar `useAuth` hook + contexto
- [x] 21.5 Implementar `<RequireOperator />` guard
- [x] 21.6 Configurar React Router + rotas públicas/protegidas

## Implementation Details
Conforme skill `react-architecture` (feature-based) e ADR-005/010. Login chama `POST /api/v1/auth/login` com credenciais; cookie é setado automaticamente pelo browser (same-origin via proxy em dev ou subdomínios em prod).

### Relevant Files
- `apps/backoffice/src/main.tsx` (a criar)
- `apps/backoffice/src/app/routes.tsx` (a criar)
- `apps/backoffice/src/features/auth/pages/LoginPage.tsx` (a criar)
- `apps/backoffice/src/features/auth/hooks/useAuth.ts` (a criar)
- `apps/backoffice/src/features/auth/AuthContext.tsx` (a criar)
- `apps/backoffice/src/shared/layouts/AppLayout.tsx` (a criar)
- `apps/backoffice/src/shared/components/Topbar.tsx` (a criar)
- `apps/backoffice/src/shared/components/Sidebar.tsx` (a criar)
- `apps/backoffice/src/shared/api/client.ts` (a criar)
- `apps/backoffice/src/shared/auth/RequireOperator.tsx` (a criar)
- `apps/backoffice/src/shared/api/types.ts` — tipos DTOs (a criar)

### Dependent Files
- `task_22` (wizard) e `task_23` (lista/detalhes) consomem layout e API client

### Related ADRs
- [ADR-005: Backoffice como SPA React Separado](../adrs/adr-005.md)
- [ADR-010: Design system PortaBox](../adrs/adr-010.md)

## Deliverables
- Login funcional cookie-based
- Layout principal navegável
- API client com interceptors e antiforgery
- Guards de rota para role `Operator`
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests com mock da API **(REQUIRED)**

## Tests
- Unit tests (Vitest + Testing Library):
  - [ ] `LoginPage` renderiza com campos `email` e `senha`; submit chama `login({ email, password })`
  - [ ] `useAuth` retorna `isAuthenticated=true` após login com status 200
  - [ ] `useAuth` retorna `isAuthenticated=false` após logout com status 204
  - [ ] API client adiciona `X-XSRF-TOKEN` a partir do cookie `XSRF-TOKEN`
  - [ ] API client redireciona para `/login` quando recebe 401
- Integration tests (MSW ou servidor fake):
  - [ ] Fluxo login → navegação para `/condominios` → logout → redireciona para `/login`
  - [ ] Acesso direto a rota protegida sem sessão redireciona para `/login` com `redirectTo` preservado
  - [ ] Layout renderiza logo PortaBox do skill e ícones Lucide
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Operador consegue logar e ver layout; sem sessão é redirecionado
- API client é consumível por todas as features subsequentes
- Zero violação do design system (cores/tipografia via tokens)
