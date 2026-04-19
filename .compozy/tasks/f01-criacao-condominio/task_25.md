---
status: completed
title: Integration tests E2E do fluxo F01 + Playwright smoke
type: test
complexity: high
dependencies:
  - task_18
  - task_22
  - task_23
  - task_24
---

# Task 25: Integration tests E2E do fluxo F01 + Playwright smoke

## Overview
Suite de integração end-to-end que valida os cenários críticos do F01 declarados no TechSpec (seção **Testing Approach — Cenários obrigatórios**) usando `WebApplicationFactory<Program>` + Testcontainers (Postgres + MinIO + MailHog) para backend, e Playwright CLI para um smoke test do backoffice contra a API rodando. Garantias-chave: atomicidade, isolamento multi-tenant e fluxo completo do magic link.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar `AppFactoryFixture : WebApplicationFactory<Program>` que inicia API real contra Postgres + MinIO + MailHog via Testcontainers
- MUST cobrir os cenários listados no TechSpec seção **Testing Approach — Integration Tests**:
  - Fluxo completo do wizard
  - Deduplicação por CNPJ
  - Isolamento multi-tenant (crítico)
  - Upload de documento
  - Reemissão de magic link
  - Magic link expirado
  - Go-live
  - Outbox atomicidade + publicação NoOp
- MUST criar suite Playwright em `tests/e2e/` que sobe backoffice + API e executa smoke test do wizard end-to-end
- MUST registrar execução de Playwright no CI (com reuso de artefatos do build do backoffice)
- MUST incluir teste explícito de sanitização de logs (grep nos logs gerados por um fluxo completo para garantir ausência de token/senha/CPF completo/e-mail em claro)
- MUST alcançar cobertura agregada ≥ 80% considerando unit + integration
- MUST permitir rodar localmente via `dotnet test` e `pnpm e2e`
- SHOULD gerar relatórios HTML (Playwright) como artefato do CI
</requirements>

## Subtasks
- [x] 25.1 Implementar `AppFactoryFixture` com Testcontainers Postgres + MinIO + MailHog
- [x] 25.2 Implementar cenários listados do TechSpec como testes xUnit
- [x] 25.3 Implementar teste de sanitização de logs (captura sink em memória)
- [x] 25.4 Configurar Playwright em `tests/e2e/`
- [x] 25.5 Implementar smoke E2E: operador loga → cria condomínio → vê detalhes → ativa → síndico consome magic link e loga
- [x] 25.6 Integrar ambos no pipeline CI

## Implementation Details
Seguir skill `dotnet-testing` + `playwright-cli`. MailHog expõe API HTTP para inspecionar mensagens recebidas — usar `HttpClient` contra `http://localhost:{mailhog_port}/api/v2/messages` para recuperar magic link e extrair token. Para testes de atomicidade, injetar `FailingInterceptor` ou equivalente.

### Relevant Files
- `tests/PortaBox.Api.IntegrationTests/Fixtures/AppFactoryFixture.cs` (a criar)
- `tests/PortaBox.Api.IntegrationTests/Features/Condominios/CreateCondominioEndToEndTests.cs` (a criar)
- `tests/PortaBox.Api.IntegrationTests/Features/Condominios/CnpjDeduplicationTests.cs` (a criar)
- `tests/PortaBox.Api.IntegrationTests/MultiTenancy/TenantIsolationEndToEndTests.cs` (a criar)
- `tests/PortaBox.Api.IntegrationTests/Features/OptInDocuments/UploadTests.cs` (a criar)
- `tests/PortaBox.Api.IntegrationTests/Features/MagicLinks/MagicLinkFlowTests.cs` (a criar)
- `tests/PortaBox.Api.IntegrationTests/Features/Outbox/OutboxAtomicityTests.cs` (a criar)
- `tests/PortaBox.Api.IntegrationTests/Observability/LogSanitizationTests.cs` (a criar)
- `tests/e2e/playwright.config.ts` (a criar)
- `tests/e2e/specs/smoke-wizard.spec.ts` (a criar)

### Dependent Files
- `task_26` (hardening) depende desta suite para validar as mudanças
- Todos os handlers anteriores são exercitados aqui

### Related ADRs
- [ADR-004: Shared schema + query filter](../adrs/adr-004.md) — teste de isolamento.
- [ADR-009: Outbox pattern](../adrs/adr-009.md) — teste de atomicidade.
- Todos os demais ADRs são exercitados indiretamente.

## Deliverables
- `AppFactoryFixture` reutilizável
- Suite de integração cobrindo todos os cenários obrigatórios
- Playwright config + smoke test E2E
- Relatórios HTML em CI
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests (esta é a suite principal) **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] Helpers internos da fixture (ex.: extrator de token de e-mail MailHog) cobertos por testes unitários isolados
  - [ ] Utilitário `ReadCapturedLogs` devolve eventos na ordem correta
- Integration tests (a serem executados pela suite):
  - [x] **Fluxo completo:** criar condomínio → e-mail no MailHog → extrair token → `POST /auth/password-setup` → `POST /auth/login` com sucesso
  - [x] **Deduplicação:** CNPJ `X` existe → 409 com body contendo nome e data do tenant existente
  - [x] **Isolamento:** 2 tenants criados; síndico do tenant A não lê detalhes do B (`GET /admin/condominios/{B}` → 403)
  - [x] **Upload:** PDF 1 MB no MinIO + linha em `opt_in_document` + download via presigned URL
  - [x] **Reemissão:** dois links consecutivos → só o segundo funciona
  - [x] **Expirado:** magic link com `expires_at` forçado no passado → 400 genérico
  - [x] **Go-live:** ativar tenant em `PreAtivo` → `Ativo` + audit entry
  - [x] **Outbox atomicidade:** insert de `Condominio` cria outbox row; CNPJ duplicado → 409 sem inserção no outbox
  - [ ] **Outbox NoOp:** após criar tenant com flag `Publisher:Enabled=true`, `published_at` fica populado dentro de 10s
  - [x] **Log sanitization:** fluxo completo não emite token, senha, CPF completo ou e-mail em claro em nenhum log capturado
  - [x] **Playwright smoke:** wizard end-to-end funcional (pode rodar com backoffice apontando para a `AppFactoryFixture`)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80% agregado
- Suite completa executa em < 10 min local
- Playwright smoke estável (≤ 1 flake a cada 100 execuções)
- Todos os cenários obrigatórios do TechSpec cobertos
