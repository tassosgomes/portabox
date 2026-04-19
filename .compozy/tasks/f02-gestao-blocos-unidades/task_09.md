---
status: pending
title: Registro DI + EstruturaEndpoints + mapeamento em Program.cs (8 rotas)
type: backend
complexity: high
dependencies:
  - task_05
  - task_06
  - task_07
  - task_08
---

# Task 09: Registro DI + EstruturaEndpoints + mapeamento em Program.cs (8 rotas)

## Overview
Integra todos os handlers de F02 na API, expondo 8 endpoints REST (7 para síndico + 1 admin para operador) em um único arquivo de extensão `EstruturaEndpoints.cs` via minimal APIs, e registrando validators, handlers e repositórios no `DependencyInjection.cs` do módulo. É a task que transforma F02 de biblioteca em feature consumível.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST estender `PortaBox.Modules.Gestao.DependencyInjection.AddPortaBoxModuleGestao` registrando:
  - `IBlocoRepository`, `IUnidadeRepository` (scoped, implementações de task_03/04)
  - Os 7 validators (4 Bloco + 3 Unidade) como `AddValidatorsFromAssemblyContaining<T>()` ou registros individuais seguindo convenção de F01
  - Os 7 handlers (4 Bloco + 3 Unidade) + `GetEstruturaQueryHandler`
- MUST criar `src/PortaBox.Api/Features/Estrutura/EstruturaEndpoints.cs` como static class com método de extensão `MapEstruturaEndpoints(this IEndpointRouteBuilder endpoints)` que adiciona 8 rotas ao route group `/api/v1`:
  - `GET /condominios/{condominioId}/estrutura?includeInactive={bool}` → `[Authorize(Roles = "Sindico")]`
  - `POST /condominios/{condominioId}/blocos` → `[Authorize(Roles = "Sindico")]`
  - `PATCH /condominios/{condominioId}/blocos/{blocoId}` → idem
  - `POST /condominios/{condominioId}/blocos/{blocoId}:inativar` → idem
  - `POST /condominios/{condominioId}/blocos/{blocoId}:reativar` → idem
  - `POST /condominios/{condominioId}/blocos/{blocoId}/unidades` → idem
  - `POST /condominios/{condominioId}/blocos/{blocoId}/unidades/{unidadeId}:inativar` → idem
  - `POST /condominios/{condominioId}/blocos/{blocoId}/unidades/{unidadeId}:reativar` → idem
  - `GET /admin/condominios/{condominioId}/estrutura?includeInactive={bool}` → `[Authorize(Roles = "Operator")]` e aplicação explícita de `ITenantContext.BeginScope(condominioId)` no handler
- MUST mapear `Result<T>` → HTTP status codes: 200 (update), 201 (create com `Location`), 400 (validation), 403 (role errada), 404 (not found), 409 (conflito canônico/unicidade), 422 (transição inválida)
- MUST usar `ProblemDetails` (RFC 7807) para todos os erros, seguindo padrão estabelecido em F01
- MUST adicionar uma linha em `Program.cs` para chamar `app.MapEstruturaEndpoints()` dentro do route group versionado
- SHOULD documentar cada endpoint via `.WithOpenApi()` + `.WithSummary()` + `.WithTags("Estrutura")` para surgir corretamente em Swagger/OpenAPI
</requirements>

## Subtasks
- [ ] 09.1 Estender `DependencyInjection.cs` com repositórios, validators e 8 handlers
- [ ] 09.2 Criar `EstruturaEndpoints.cs` com os 8 endpoints do síndico + 1 admin
- [ ] 09.3 Implementar mapeamento `Result<T>` → HTTP status codes com `ProblemDetails`
- [ ] 09.4 Para o endpoint admin, aplicar `ITenantContext.BeginScope` explicitamente no delegate antes de invocar o handler
- [ ] 09.5 Adicionar chamada `app.MapEstruturaEndpoints()` em `Program.cs`
- [ ] 09.6 Adicionar metadados OpenAPI (`.WithOpenApi()`, `.WithTags("Estrutura")`, `.WithSummary(...)`) para cada endpoint

## Implementation Details
Ver TechSpec seções **API Endpoints** (tabela completa de rotas) e **Data Flow C1–C6** (comportamento esperado por endpoint).

Convenção de mapeamento de erro (extrair para helper `ResultExtensions.ToHttpResult(result)` se não existir):
- `Result.Failure` com mensagem contendo "não encontrado" → 404
- Com "conflito" ou "já existe" → 409
- Com "inválid" ou "inativo" → 422
- Padrão → 400

Esta convenção é frágil; se F01 já tem um padrão mais robusto (ex.: tipo `AppError` com `Kind` enum), seguir esse padrão. Em inspeção, alinhar.

Endpoint admin de estrutura: o middleware `TenantResolutionMiddleware` não aplica scope para role `Operator` (por design). O endpoint aceita `condominioId` no path; o delegate deve chamar:

```csharp
await using (tenantContext.BeginScope(tenantIdFromPath))
{
    var result = await queryHandler.HandleAsync(...);
    return result.ToHttpResult();
}
```

### Relevant Files
- `src/PortaBox.Modules.Gestao/DependencyInjection.cs` — estender (existente de F01)
- `src/PortaBox.Api/Features/Estrutura/EstruturaEndpoints.cs` — novo
- `src/PortaBox.Api/Program.cs` — adicionar `app.MapEstruturaEndpoints()` no route group `/api/v1`
- `src/PortaBox.Api/Extensions/ResultExtensions.cs` (se existir de F01) ou novo helper
- Todos os handlers, validators, repositórios das tasks 06–08
- `tests/PortaBox.Api.IntegrationTests/...` — consumirá os endpoints em task_10

### Dependent Files
- Integration tests (task_10) exercitarão cada endpoint
- `packages/api-client` (task_11) consome os contratos HTTP daqui
- Frontend (tasks 14–17) consome via api-client

### Related ADRs
- [ADR-005: Escrita Exclusiva do Síndico; Backoffice Read-Only Cross-Tenant](adrs/adr-005.md) — motivação do endpoint admin separado
- [ADR-009: Endpoint Único Retornando Árvore Completa](adrs/adr-009.md) — shape da rota de leitura

## Deliverables
- `DependencyInjection.cs` atualizado com registros
- `EstruturaEndpoints.cs` com 9 endpoints (8 síndico + 1 admin)
- `Program.cs` chamando `MapEstruturaEndpoints()`
- Mapeamento `Result<T>` → HTTP consistente em todos os endpoints
- Metadados OpenAPI (aparece em Swagger UI)
- Unit tests with 80%+ coverage **(REQUIRED)** — via tests de endpoints mínimos (ex.: validar que role check funciona) ou via integration tests em task_10
- Integration tests — cobertos em task_10

## Tests
- Unit tests:
  - [ ] Mapping helper: `Result.Failure("Bloco não encontrado")` → 404 + `ProblemDetails.Title` preenchido
  - [ ] Mapping helper: `Result.Failure("Já existe bloco ativo com este nome")` → 409
  - [ ] Mapping helper: `Result.Failure("não é possível renomear bloco inativo")` → 422
  - [ ] Endpoint admin: sem role `Operator` → 403 (testado via `WebApplicationFactory` em task_10)
- Integration tests:
  - [ ] Cobertos em task_10 para todos os 9 endpoints
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- `curl` contra cada endpoint (com JWT apropriado) retorna status code e payload esperados
- Swagger UI lista todos os endpoints de F02 agrupados em "Estrutura"
- Endpoint admin retorna 403 para síndico que tenta acessar; endpoint síndico retorna 403 para operador
