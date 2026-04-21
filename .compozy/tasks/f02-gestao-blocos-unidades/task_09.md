---
status: completed
title: Registro DI + EstruturaEndpoints + mapeamento em Program.cs (9 rotas)
type: backend
complexity: high
dependencies:
  - task_05
  - task_06
  - task_07
  - task_08
---

# Task 09: Registro DI + EstruturaEndpoints + mapeamento em Program.cs (9 rotas)

## Overview
Integra todos os handlers de F02 na API, expondo 9 endpoints REST (8 para síndico + 1 admin para operador) em um único arquivo de extensão `EstruturaEndpoints.cs` via minimal APIs, e registrando validators, handlers e repositórios no `DependencyInjection.cs` do módulo. É a task que transforma F02 de biblioteca em feature consumível.

> **⚠️ Contrato autoritativo:** a implementação destes 9 endpoints DEVE seguir exatamente o definido em [`api-contract.yaml`](../api-contract.yaml) (path, método, params, request/response schemas, status codes, ProblemDetails). Qualquer desvio quebra o contrato com o frontend e é falha de aceitação. Use o contrato como checklist durante o code review.

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
- MUST criar `src/PortaBox.Api/Features/Estrutura/EstruturaEndpoints.cs` como static class com método de extensão `MapEstruturaEndpoints(this IEndpointRouteBuilder endpoints)` que adiciona **9 rotas** ao route group `/api/v1` conforme [`api-contract.yaml`](../api-contract.yaml):
  - `GET /condominios/{condominioId}/estrutura?includeInactive={bool}` → `[Authorize(Roles = "Sindico")]` — `operationId: getEstrutura`
  - `POST /condominios/{condominioId}/blocos` → `[Authorize(Roles = "Sindico")]` — `operationId: criarBloco`
  - `PATCH /condominios/{condominioId}/blocos/{blocoId}` → idem — `renomearBloco`
  - `POST /condominios/{condominioId}/blocos/{blocoId}:inativar` → idem — `inativarBloco`
  - `POST /condominios/{condominioId}/blocos/{blocoId}:reativar` → idem — `reativarBloco`
  - `POST /condominios/{condominioId}/blocos/{blocoId}/unidades` → idem — `criarUnidade`
  - `POST /condominios/{condominioId}/blocos/{blocoId}/unidades/{unidadeId}:inativar` → idem — `inativarUnidade`
  - `POST /condominios/{condominioId}/blocos/{blocoId}/unidades/{unidadeId}:reativar` → idem — `reativarUnidade`
  - `GET /admin/condominios/{condominioId}/estrutura?includeInactive={bool}` → `[Authorize(Roles = "Operator")]` e aplicação explícita de `ITenantContext.BeginScope(condominioId)` no handler — `getEstruturaAdmin`
- MUST mapear `Result<T>` → HTTP status codes conforme tabela do contrato: 200 (update), 201 (create com `Location`), 400 (validation), 401 (unauth), 403 (role errada), 404 (not found), 409 (conflito canônico/unicidade), 422 (transição inválida), 500 (interno)
- MUST usar `ProblemDetails` (RFC 7807) para todos os erros; campos `type`, `title`, `status`, `detail`, `instance` e (para 400) `errors` por campo — shape exato em `components/schemas/ProblemDetails` e `ValidationProblemDetails` do contrato
- MUST serializar JSON em `camelCase` (default System.Text.Json do ASP.NET Core 8) — **não** alterar policy
- MUST mensagens de erro (`title`, `detail`) em `pt-BR` conforme contrato
- MUST adicionar uma linha em `Program.cs` para chamar `app.MapEstruturaEndpoints()` dentro do route group versionado
- MUST documentar cada endpoint via `.WithOpenApi()` + `.WithSummary()` + `.WithTags("Estrutura"|"Blocos"|"Unidades"|"Admin")` + `.WithName(operationId)` com os mesmos tags e `operationId` do contrato (permite que Swagger do backend espelhe o contract)
- SHOULD configurar Swashbuckle para exportar o `swagger.json` gerado a partir dos endpoints em modo dev, permitindo comparação manual com `api-contract.yaml` durante code review
</requirements>

## Subtasks
- [x] 09.1 Estender `DependencyInjection.cs` com repositórios, validators e 8 handlers
- [x] 09.2 Criar `EstruturaEndpoints.cs` com os 9 endpoints (8 do síndico + 1 admin) conforme `api-contract.yaml`
- [x] 09.3 Implementar mapeamento `Result<T>` → HTTP status codes com `ProblemDetails`
- [x] 09.4 Para o endpoint admin, aplicar `ITenantContext.BeginScope` explicitamente no delegate antes de invocar o handler
- [x] 09.5 Adicionar chamada `app.MapEstruturaEndpoints()` em `Program.cs`
- [x] 09.6 Adicionar metadados OpenAPI (`.WithOpenApi()`, `.WithTags("Estrutura")`, `.WithSummary(...)`) para cada endpoint

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
- `.compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml` — **contrato autoritativo** que estes endpoints materializam
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
  - [x] Mapping helper: `Result.Failure("Bloco não encontrado")` → 404 + `ProblemDetails.Title` preenchido
  - [x] Mapping helper: `Result.Failure("Já existe bloco ativo com este nome")` → 409
  - [x] Mapping helper: `Result.Failure("não é possível renomear bloco inativo")` → 422
  - [ ] Endpoint admin: sem role `Operator` → 403 (testado via `WebApplicationFactory` em task_10)
- Integration tests:
  - [ ] Cobertos em task_10 para todos os 9 endpoints
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- **Zero divergência** entre paths/métodos/status-codes/schema de resposta implementados e os definidos em `api-contract.yaml` (validado manualmente no code review + automaticamente em task_10)
- `curl` contra cada endpoint (com JWT apropriado) retorna status code e payload esperados
- Swagger UI lista os 9 endpoints de F02 agrupados nos tags do contrato (`Estrutura`, `Blocos`, `Unidades`, `Admin`)
- Endpoint admin retorna 403 para síndico que tenta acessar; endpoint síndico retorna 403 para operador
- Mensagens de erro (`ProblemDetails.title` e `detail`) em pt-BR, shape alinhado ao contrato
