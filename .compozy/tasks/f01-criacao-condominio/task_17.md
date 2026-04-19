---
status: completed
title: Queries ListCondominios + GetCondominioDetails
type: backend
complexity: medium
dependencies:
    - task_05
    - task_06
---

# Task 17: Queries ListCondominios + GetCondominioDetails

## Overview
Implementa os dois queries CQRS consumidos pelo backoffice (CF4 e CF5 do PRD): `ListCondominiosQuery` paginada com filtros, e `GetCondominioDetailsQuery` que retorna dados completos do tenant (condomínio, opt-in, síndico, documentos e audit log recente). Queries usam `AsNoTracking()` e projeções para performance conforme skill `dotnet-performance`.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST definir `ListCondominiosQuery { Page, PageSize, Status?, SearchTerm? }` com paginação default 20, max 100
- MUST retornar `PagedResult<CondominioListItemDto>` com campos: `id`, `nome_fantasia`, `cnpj_masked`, `status`, `created_at`, `activated_at`
- MUST mascarar CNPJ nas respostas (apenas 4 últimos dígitos visíveis) para reduzir exposição em logs de acesso
- MUST aceitar filtro `Status` (`PreAtivo`/`Ativo`) e busca textual `SearchTerm` em `nome_fantasia` ou CNPJ (ILIKE)
- MUST usar `AsNoTracking()` e projeção direta para DTOs
- MUST definir `GetCondominioDetailsQuery { CondominioId }`
- MUST retornar `CondominioDetailsDto` com: dados do condomínio, dados do opt-in (sem CPF completo — mascarar), dados do síndico (sem celular completo — mascarar), lista dos documentos (metadados sem `storage_key`), últimas 20 entradas do audit log, flag `sindico_senha_definida`
- MUST usar `AsSplitQuery()` onde houver múltiplas coleções relacionadas para evitar Cartesian explosion
- MUST respeitar query filter multi-tenant (herda de task_05) — operador com role `Operator` acessa via endpoint cross-tenant (tenant é parâmetro explícito; uso de `IgnoreQueryFilters` controlado)
- SHOULD incluir ordenação default por `created_at DESC` na lista
</requirements>

## Subtasks
- [x] 17.1 Definir `ListCondominiosQuery`, `CondominioListItemDto`, `PagedResult<T>`
- [x] 17.2 Implementar `ListCondominiosQueryHandler` com filtro, paginação e mascaramento
- [x] 17.3 Definir `GetCondominioDetailsQuery`, `CondominioDetailsDto`
- [x] 17.4 Implementar `GetCondominioDetailsQueryHandler` com `AsSplitQuery` e projeções
- [x] 17.5 Implementar helpers de mascaramento de CNPJ/CPF/celular (reutilizáveis)

## Implementation Details
Conforme skill `dotnet-performance` (AsNoTracking, AsSplitQuery, projeção). Para endpoints admin, operador pode listar todos os tenants — implementação usa chamada explícita `IgnoreQueryFilters()` controlada, rastreável e logada. Query de detalhes aplica `IgnoreQueryFilters` quando chamada por `Operator`, mas mantém escopo para `Sindico`.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Application/Queries/ListCondominios/ListCondominiosQuery.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Queries/ListCondominios/ListCondominiosQueryHandler.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Queries/ListCondominios/CondominioListItemDto.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Queries/GetCondominioDetails/GetCondominioDetailsQuery.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Queries/GetCondominioDetails/GetCondominioDetailsQueryHandler.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Queries/GetCondominioDetails/CondominioDetailsDto.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Common/PagedResult.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Common/Masking.cs` (a criar)

### Dependent Files
- `task_18` (controller) expõe `GET /api/v1/admin/condominios` e `GET /api/v1/admin/condominios/{id}`
- `task_23` (backoffice lista/detalhes) consome os DTOs

### Related ADRs
- [ADR-004: Shared schema + query filter](../adrs/adr-004.md) — justifica o uso controlado de `IgnoreQueryFilters`.

## Deliverables
- Dois queries + handlers + DTOs + `PagedResult`
- Helpers de mascaramento
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para paginação, filtros e isolamento **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `ListCondominiosQuery` com `PageSize=150` é clampado a 100
  - [x] `Masking.Cnpj("12345678000195")` retorna `"****8000195"` (ou similar consistente)
  - [x] `Masking.Cpf("12345678909")` retorna `"***.456.789-**"`
  - [x] `Masking.Celular("+5511999998888")` retorna `"+55 11 9****-8888"`
- Integration tests:
  - [x] `ListCondominios` retorna todos os tenants quando chamado por handler de contexto `Operator` (ignora filter)
  - [x] `ListCondominios` filtra por `Status=Ativo`
  - [x] `ListCondominios` busca por parte do nome via ILIKE
  - [x] `GetCondominioDetails` retorna DTO completo com opt-in (CPF mascarado), síndico (celular mascarado), documentos (sem `storage_key`), audit log (últimas 20 entradas)
  - [x] `GetCondominioDetails` chamada por `Sindico` de outro tenant retorna `NotFound`
- Test coverage target: >=80%
- All tests must pass

## Validation Notes
- Verificação focal da task passou: `dotnet test tests/PortaBox.Modules.Gestao.UnitTests/PortaBox.Modules.Gestao.UnitTests.csproj`, `dotnet test tests/PortaBox.Api.IntegrationTests/PortaBox.Api.IntegrationTests.csproj --filter "CondominioQueriesIntegrationTests"` e `dotnet build PortaBox.sln`.
- A verificação global `dotnet test PortaBox.sln --no-build -m:1` continua falhando por instabilidade preexistente do `PostgresDatabaseFixture`/container compartilhado (`terminating connection due to administrator command` / `connection refused`) em outras integrações fora do escopo desta task; por isso o status da task permanece `pending` até existir evidência limpa da pipeline completa.

## Success Criteria
- All tests passing
- Test coverage >=80%
- Queries com paginação e projeções performáticas
- Mascaramento de PII em todas as respostas
- DTOs prontos para serem expostos via DTOs Mapster no controller (task_18)
