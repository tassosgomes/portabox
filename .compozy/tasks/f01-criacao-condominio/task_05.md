---
status: completed
title: Multi-tenancy baseline (ITenantContext + query filter + teste de isolamento)
type: backend
complexity: critical
dependencies:
  - task_03
---

# Task 05: Multi-tenancy baseline (ITenantContext + query filter + teste de isolamento)

## Overview
Estabelece o padrão de isolamento multi-tenant adotado em ADR-004 e aplicável a todos os serviços .NET do projeto: `ITenantContext` ambíguo por request, interface de marcação `ITenantEntity`, EF Core global query filter automático e um teste de isolamento que é a principal garantia contra vazamento de dados entre tenants. Esta é a task mais crítica do backend por efeito transversal.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST definir `ITenantContext { Guid? TenantId; IDisposable BeginScope(Guid) }` em `PortaBox.Application.Abstractions`
- MUST definir `ITenantEntity { Guid TenantId }` em `PortaBox.Domain`
- MUST implementar `TenantContext` scoped que resolve o tenant a partir do claim `tenant_id` (role `Sindico`) ou do parâmetro explícito da operação (role `Operator`)
- MUST implementar middleware `TenantResolutionMiddleware` que popula `TenantContext` a partir do principal autenticado
- MUST aplicar global query filter automático em `AppDbContext.OnModelCreating` para todo tipo que implemente `ITenantEntity`, usando `_tenantContext.TenantId`
- MUST suportar `IgnoreQueryFilters()` explícito apenas para operações auditadas (ex.: operador listando múltiplos tenants via chamada de backoffice)
- MUST escrever teste de isolamento (ver Tests) que falha se o query filter for omitido em qualquer entidade futura
</requirements>

## Subtasks
- [x] 05.1 Definir `ITenantContext` e `ITenantEntity`
- [x] 05.2 Implementar `TenantContext` scoped
- [x] 05.3 Implementar `TenantResolutionMiddleware` + registro no pipeline
- [x] 05.4 Implementar aplicação automática do query filter em `AppDbContext`
- [x] 05.5 Escrever entidade de teste `SampleTenantEntity` (interna à suite) para validar isolamento
- [x] 05.6 Documentar regra "toda entidade multi-tenant implementa `ITenantEntity`" em comentário no `AppDbContext`

## Implementation Details
Conforme ADR-004. `TenantContext` usa `AsyncLocal` para permitir `BeginScope` em cenários de worker/outbox. O middleware é registrado **após** `UseAuthentication` e **antes** de qualquer endpoint. Query filter é aplicado via reflection sobre tipos que implementam `ITenantEntity` em `OnModelCreating`.

### Relevant Files
- `src/PortaBox.Application.Abstractions/MultiTenancy/ITenantContext.cs` (a criar)
- `src/PortaBox.Domain/Abstractions/ITenantEntity.cs` (a criar)
- `src/PortaBox.Infrastructure/MultiTenancy/TenantContext.cs` (a criar)
- `src/PortaBox.Api/Middleware/TenantResolutionMiddleware.cs` (a criar)
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs` — editar `OnModelCreating` para aplicar filtros
- `src/PortaBox.Api/Program.cs` — registrar middleware e `ITenantContext` scoped
- `tests/PortaBox.Api.IntegrationTests/MultiTenancy/TenantIsolationTests.cs` (a criar)

### Dependent Files
- `task_06` em diante: toda entidade multi-tenant implementa `ITenantEntity`
- `task_12`, `task_13`, `task_14`, `task_17`: handlers assumem que filtros já aplicam

### Related ADRs
- [ADR-004: Isolamento Multi-tenant via Shared Schema](../adrs/adr-004.md) — baseline arquitetural implementada aqui.

## Deliverables
- Interfaces e implementações de tenant context e tenant entity
- Middleware de resolução por request
- Query filter global automático em `AppDbContext`
- Teste de isolamento que reprova vazamentos
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests críticos de isolamento **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] `TenantContext.TenantId` retorna `null` quando não há escopo nem claim
  - [ ] `TenantContext.BeginScope(Guid x)` seta `TenantId = x` dentro do escopo e volta ao anterior ao disposar
  - [ ] `TenantResolutionMiddleware` lê claim `tenant_id` do usuário `Sindico` e popula `TenantContext`
  - [ ] `TenantResolutionMiddleware` não popula `TenantContext` para usuário `Operator` (tenant-alvo vem do endpoint)
- Integration tests:
  - [ ] **Isolamento crítico:** duas `SampleTenantEntity` — uma com `tenant_id=A`, outra com `tenant_id=B` — gravadas; com `TenantContext.TenantId=A`, `dbContext.SampleTenantEntities.ToList()` retorna apenas a de A
  - [ ] Troca de escopo dentro do mesmo request (`BeginScope(B)`) passa a ver entidades de B e deixa de ver de A
  - [ ] `IgnoreQueryFilters()` explícito retorna entidades de todos os tenants (somente em caminhos marcados)
  - [ ] Entidade nova que **não** implementa `ITenantEntity` **não** recebe filter (para não quebrar entidades globais)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Teste de isolamento passando sem falhas flutuantes em 100 execuções consecutivas
- `ITenantContext`, `ITenantEntity` e `TenantResolutionMiddleware` documentados no próprio código
- Padrão pronto para ser adotado por todas as entidades das tasks seguintes
