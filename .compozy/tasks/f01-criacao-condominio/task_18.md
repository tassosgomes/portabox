---
status: completed
title: REST controllers + policies + auth cookie + DTOs Mapster
type: backend
complexity: high
dependencies:
    - task_04
    - task_12
    - task_13
    - task_14
    - task_15
    - task_16
    - task_17
---

# Task 18: REST controllers + policies + auth cookie + DTOs Mapster

## Overview
Exporta toda a superfície HTTP do F01 seguindo as convenções do skill `common-restful-api`: `AdminCondominiosController` com os endpoints de criação/listagem/detalhes/ativação/reenvio/upload, `AuthController` com login/logout/password-setup, policies `RequireOperator` e `RequireSindicoOfTenant`, DTOs Mapster para request/response e Problem Details RFC 7807 para erros. É a "cola" entre os handlers das tasks 12–17 e o mundo externo.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST implementar `AdminCondominiosController` com todos os endpoints da seção **API Endpoints** do TechSpec para o prefixo `/api/v1/admin/condominios`
- MUST implementar `AuthController` com `POST /api/v1/auth/login`, `POST /api/v1/auth/logout`, `POST /api/v1/auth/password-setup`
- MUST aplicar `[Authorize(Policy = "RequireOperator")]` em toda ação admin; endpoints de auth ficam abertos (`[AllowAnonymous]`)
- MUST mapear comandos e queries para DTOs via Mapster (configurar `TypeAdapterConfig` global)
- MUST responder Problem Details RFC 7807 em todos os erros (via handler global de task_02)
- MUST retornar 201 com `Location` em criação, 200 em sucesso simples, 202 em operações assíncronas (N/A aqui), 204 em ações sem body
- MUST validar tamanho máximo de upload a nível de endpoint (10 MB) via configuração de `FormOptions`
- MUST usar antiforgery token em endpoints POST/PUT/DELETE servidos a SPAs cookie-based
- MUST incluir versionamento `/api/v1` no prefixo (herda de task_02)
- MUST registrar OpenAPI (Swashbuckle) com esquemas dos DTOs para documentação
</requirements>

## Subtasks
- [ ] 18.1 Configurar Mapster global (`TypeAdapterConfig.GlobalSettings`) + mapeamentos por feature
- [ ] 18.2 Implementar `AdminCondominiosController` com os 7 endpoints admin
- [ ] 18.3 Implementar `AuthController` (login/logout/password-setup)
- [ ] 18.4 Configurar `FormOptions.MultipartBodyLengthLimit=10MB` e request size limits
- [ ] 18.5 Configurar antiforgery para SPAs (cookie + header `X-XSRF-TOKEN`)
- [ ] 18.6 Configurar OpenAPI Swashbuckle com tags por controller
- [ ] 18.7 Criar contratos DTO (request/response) em `PortaBox.Api/Dtos/`

## Implementation Details
Seguir TechSpec seção **API Endpoints** (tabela completa) e skill `common-restful-api`. Nome dos endpoints admin segue convenção com ação sentada em subresource (`:activate`, `:resend-magic-link`, `:download`) — consistente com Google AIP 136. Upload usa `IFormFile` e delega ao handler de task_13 passando `file.OpenReadStream()`.

### Relevant Files
- `src/PortaBox.Api/Controllers/AdminCondominiosController.cs` (a criar)
- `src/PortaBox.Api/Controllers/AuthController.cs` (a criar)
- `src/PortaBox.Api/Dtos/Condominios/*` (a criar, um arquivo por DTO)
- `src/PortaBox.Api/Dtos/Auth/*` (a criar)
- `src/PortaBox.Api/Mapping/ModulosGestaoMapsterProfile.cs` (a criar)
- `src/PortaBox.Api/Extensions/AuthorizationExtensions.cs` — registrar policies `RequireOperator` e `RequireSindicoOfTenant` (a criar)
- `src/PortaBox.Api/Program.cs` — registrar Mapster, antiforgery, form options, Swashbuckle (editar)

### Dependent Files
- `task_19` (observability) adiciona métricas HTTP por endpoint
- `task_21`–`task_24` (frontend) consomem os contratos
- `task_25` (integration tests) exerce os endpoints ponta a ponta
- `task_26` (hardening) aplica rate-limit e CSRF/SameSite

### Related ADRs
- [ADR-001: Onboarding de Tenant no MVP](../adrs/adr-001.md)
- [ADR-005: Backoffice como SPA React Separado; Identity com roles](../adrs/adr-005.md)

## Deliverables
- Dois controllers completos com políticas e DTOs
- Configuração Mapster centralizada
- OpenAPI/Swagger refletindo todos os endpoints
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para todos os endpoints **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] Mapster mapeia `CreateCondominioRequest` → `CreateCondominioCommand` sem campos a mais ou a menos
  - [ ] Mapster mapeia `CondominioDetailsDto` → `CondominioDetailsResponse` com mascaramentos preservados
  - [ ] Controller `Activate` delega ao handler e devolve 200/204 com body vazio em sucesso
  - [ ] Controller `CreateCondominio` devolve 201 com header `Location` em sucesso
- Integration tests:
  - [ ] `POST /api/v1/admin/condominios` como `Operator` → 201 + tenant criado
  - [ ] `POST /api/v1/admin/condominios` sem auth → 401
  - [ ] `POST /api/v1/admin/condominios` como `Sindico` → 403
  - [ ] `POST /api/v1/admin/condominios/{id}:activate` em tenant `Ativo` → 409 Problem Details
  - [ ] `GET /api/v1/admin/condominios?status=Ativo&page=1&pageSize=10` → 200 com paginação correta
  - [ ] `GET /api/v1/admin/condominios/{id}` retorna DTO com todas as colunas mascaradas corretamente
  - [ ] `POST /api/v1/admin/condominios/{id}/opt-in-documents` multipart 12 MB → 413
  - [ ] `POST /api/v1/admin/condominios/{id}/opt-in-documents` tipo inválido → 400 Problem Details
  - [ ] `POST /api/v1/auth/login` com credenciais válidas do operador → 200 + cookie de auth
  - [ ] `POST /api/v1/auth/password-setup` com token válido → 200 e `AppUser.PasswordHash` populado
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Todos os endpoints da tabela do TechSpec implementados e testados
- OpenAPI navegável em Development
- Policies aplicadas corretamente (401/403 onde esperado)
- Contratos DTO prontos para consumo dos frontends
