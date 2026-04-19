---
status: completed
title: Hardening (rate-limit, CSRF/SameSite, STARTTLS, secrets, CSP, mascaramento)
type: backend
complexity: high
dependencies:
  - task_18
  - task_19
  - task_25
---

# Task 26: Hardening (rate-limit, CSRF/SameSite, STARTTLS, secrets, CSP, mascaramento)

## Overview
Aplica os ajustes de segurança necessários antes do primeiro go-live do piloto, endereçando os riscos listados no TechSpec (seção Known Risks) e as ações adiadas nas tasks anteriores. Inclui rate-limit no `/auth/password-setup`, CSRF/SameSite cookies, STARTTLS SMTP em prod, externalização de secrets, headers CSP nas SPAs, mascaramento de query strings sensíveis em access log e revisão do checklist de `dotnet-production-readiness`.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST aplicar rate-limit em `POST /api/v1/auth/password-setup` via `Microsoft.AspNetCore.RateLimiting`: 10 tentativas por IP em 10 min, 429 em excesso com `Retry-After`
- MUST aplicar rate-limit em `POST /api/v1/auth/login` (mesma política, politicamente mais restritiva se necessário)
- MUST configurar cookies com `SameSite=Strict` e `Secure=true` em produção; em dev permitir `Lax` + HTTP
- MUST habilitar antiforgery por padrão (configurado em task_18) e documentar requisito do `X-XSRF-TOKEN` header
- MUST configurar SMTP com STARTTLS obrigatório em produção (flag `Email:UseStartTls=true`); dev mantém MailHog sem TLS
- MUST externalizar secrets (SMTP, S3, Identity key ring, antiforgery key ring) via variáveis de ambiente — nada em `appsettings.*.json` com valor real
- MUST adicionar headers CSP nas respostas HTML das SPAs (`default-src 'self'; img-src 'self' data: https://fonts.gstatic.com; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; connect-src 'self' {VITE_API_BASE_URL}`)
- MUST adicionar middleware que mascara `?token=` na access log (remove valor) — teste explícito em log capturado
- MUST executar checklist consolidado da skill `dotnet-production-readiness` (OpenTelemetry OTLP, JSON logs, probes, sanitização, secrets) e documentar evidências
- MUST revisar política de Data Protection (chaves persistentes entre deploys — volume ou Key Vault em prod)
- SHOULD adicionar alertas mínimos descritos no TechSpec (falhas de e-mail, idade do outbox) como runbook em `docs/runbooks/` (texto; integração com alerting tool fica para deploy real)
</requirements>

## Subtasks
- [x] 26.1 Configurar rate-limit nos endpoints públicos de auth
- [x] 26.2 Ajustar cookies (SameSite/Secure) por ambiente
- [x] 26.3 Habilitar STARTTLS em prod no SMTP
- [x] 26.4 Mover secrets para env vars + documentar template `.env.sample`
- [x] 26.5 Adicionar headers CSP nas respostas das SPAs
- [x] 26.6 Implementar middleware de mascaramento de query string sensível em access log
- [x] 26.7 Configurar Data Protection com chaves persistentes (volume ou provider)
- [x] 26.8 Executar checklist `dotnet-production-readiness` e anexar evidências em `docs/production-readiness-f01.md`
- [x] 26.9 Escrever runbooks mínimos em `docs/runbooks/`

## Implementation Details
Seguir skill `dotnet-production-readiness` (checklist consolidado). Secrets em prod vão em Azure Key Vault / AWS Secrets Manager / ou env vars injetadas pelo orquestrador — este TechSpec não fixa provedor, apenas proíbe `appsettings` com valor. Data Protection keys devem persistir entre restarts para evitar invalidar cookies/tokens.

### Relevant Files
- `src/PortaBox.Api/Extensions/RateLimitingExtensions.cs` (a criar)
- `src/PortaBox.Api/Extensions/SecurityHeadersExtensions.cs` (a criar)
- `src/PortaBox.Api/Middleware/AccessLogSanitizerMiddleware.cs` (a criar)
- `src/PortaBox.Api/Program.cs` — amarrar rate-limit + CSP + sanitizer + Data Protection (editar)
- `src/PortaBox.Api/appsettings.Production.json` — remover qualquer valor real e apontar para env vars (editar)
- `docs/production-readiness-f01.md` (a criar)
- `docs/runbooks/magic-link-failures.md` (a criar)
- `docs/runbooks/outbox-backlog.md` (a criar)
- `.env.sample` (a criar)

### Dependent Files
- Configurações existentes das tasks 02, 04, 09, 18, 19 recebem ajustes finais aqui
- `task_25` deve continuar passando após as mudanças

### Related ADRs
- [ADR-005: Identity com roles](../adrs/adr-005.md) — cookie policies.
- [ADR-006: Magic Link](../adrs/adr-006.md) — rate-limit.
- [ADR-008: Email SMTP](../adrs/adr-008.md) — STARTTLS.

## Deliverables
- Rate-limit operante nos endpoints públicos
- Cookies seguros em produção
- STARTTLS em SMTP prod
- Secrets externalizados + `.env.sample`
- CSP headers nas SPAs
- Mascaramento de token em access logs
- Runbooks + relatório de production readiness
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para cada controle de segurança **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] `AccessLogSanitizerMiddleware` remove `?token=XXX` da URL logada
  - [ ] `SecurityHeadersExtensions` adiciona `Content-Security-Policy` em rotas HTML
  - [ ] Validação: em `Environment.Production`, builder falha se `Email:UseStartTls=false`
  - [ ] Validação: em `Environment.Production`, builder falha se secret vier de `appsettings.Production.json` (guard contra regressão)
- Integration tests:
  - [ ] 11 tentativas de `POST /auth/password-setup` em 10 min devolvem 429 na 11ª com header `Retry-After`
  - [ ] Cookie emitido em prod tem `Secure` e `SameSite=Strict`
  - [ ] Resposta de `/login` contém header `Content-Security-Policy`
  - [ ] Access log capturado não contém `token=` em claro
  - [ ] Data Protection keys persistem entre restart da fixture (cookies emitidos antes continuam válidos após reload)
  - [ ] Todos os testes de `task_25` continuam passando com as novas configurações
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Checklist `dotnet-production-readiness` 100% executado e documentado
- `.env.sample` cobre todas as variáveis obrigatórias
- Runbooks prontos para serem consultados durante oncall
- F01 pronto para go-live do piloto
