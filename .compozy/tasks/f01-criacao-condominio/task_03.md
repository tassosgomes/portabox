---
status: completed
title: EF Core DbContext + snake_case + fixture Testcontainers
type: backend
complexity: high
dependencies:
  - task_02
---

# Task 03: EF Core DbContext + snake_case + fixture Testcontainers

## Overview
Inicializa a camada de persistência com `AppDbContext`, convenção snake_case, connection pooling e uma fixture base de Testcontainers PostgreSQL reutilizável por toda a suite de integração. Toda tabela que vier nas próximas tasks será configurada a partir deste DbContext; toda suite de testes de integração herdará desta fixture.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar `AppDbContext : DbContext` em `PortaBox.Infrastructure`
- MUST aplicar convenção snake_case para nomes de tabelas, colunas e índices (via convenção customizada ou `EFCore.NamingConventions`)
- MUST registrar o DbContext em DI com `AddDbContextPool` apontando para PostgreSQL via string de conexão em `appsettings`
- MUST configurar `Npgsql.EnableDynamicJson()` quando necessário para colunas JSONB
- MUST criar migração inicial vazia (`InitialCreate`) apenas para ancorar o history; tabelas reais virão nas tasks subsequentes
- MUST criar fixture de Testcontainers (`PostgresDatabaseFixture`) reutilizável que inicia container Postgres 16, aplica migrations e disponibiliza uma `ConnectionString` ao teste
- SHOULD incluir helper para resetar dados entre testes (Respawn ou truncate controlado)
</requirements>

## Subtasks
- [x] 03.1 Implementar `AppDbContext` com convenção snake_case
- [x] 03.2 Registrar DbContext com pool em `Program.cs` + string de conexão configurável
- [x] 03.3 Gerar migração `InitialCreate` (vazia) via EF Core tools
- [x] 03.4 Implementar `PostgresDatabaseFixture` baseada em Testcontainers
- [x] 03.5 Configurar helper de reset de dados entre testes (Respawn)
- [x] 03.6 Documentar padrão de uso da fixture em testes de integração (comentários no código ou README da pasta de testes)

## Implementation Details
Conforme skills `csharp-dotnet-architecture` (Repository Pattern + EF Core) e `dotnet-testing` (Testcontainers PostgreSQL padrão oficial). Connection string por ambiente usa `Host=localhost;Port=5432;...` em dev (aponta para container do `docker-compose.dev.yml` do task_01) e variável de ambiente em prod.

### Relevant Files
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs` — DbContext principal (a criar)
- `src/PortaBox.Infrastructure/Persistence/Configurations/` — diretório para `IEntityTypeConfiguration<T>` das próximas tasks (a criar vazio)
- `src/PortaBox.Infrastructure/Persistence/Migrations/` — migrations do EF Core (a criar)
- `src/PortaBox.Infrastructure/DependencyInjection.cs` — extensão `AddInfrastructure(IServiceCollection, IConfiguration)` (a criar)
- `tests/PortaBox.Api.IntegrationTests/Fixtures/PostgresDatabaseFixture.cs` — fixture Testcontainers (a criar)
- `tests/PortaBox.Api.IntegrationTests/Fixtures/DatabaseResetExtensions.cs` — helper de reset (a criar)
- `src/PortaBox.Api/appsettings.Development.json` — adicionar `ConnectionStrings:Postgres`

### Dependent Files
- Todas as tasks que criam tabelas (task_04, task_06, task_07, task_08, task_09, task_10, task_11, task_14) configuram entities via `AppDbContext`
- Todas as tasks de testes de integração (task_25 e internas de cada handler) usam `PostgresDatabaseFixture`

### Related ADRs
- [ADR-004: Isolamento Multi-tenant via Shared Schema](../adrs/adr-004.md) — motiva snake_case + índices compostos começando por `tenant_id` (índices virão com cada entidade).

## Deliverables
- `AppDbContext` registrado em DI com pooling
- Migração `InitialCreate` aplicada sem erro no container de testes
- `PostgresDatabaseFixture` estável reutilizável
- Documento curto em `tests/PortaBox.Api.IntegrationTests/README.md` explicando como usar a fixture
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para a fixture **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `AppDbContext` aplica convenção snake_case em entidade de teste (ex.: `SampleEntity.SomeColumn` vira `some_column`)
  - [x] `AddInfrastructure` registra `AppDbContext` com `ServiceLifetime.Scoped` e resolve corretamente via provider
- Integration tests:
  - [x] `PostgresDatabaseFixture` inicia container Postgres 16, aplica `InitialCreate` e responde `SELECT 1`
  - [x] Reset entre testes trunca todas as tabelas de usuário sem recriar schema
  - [x] Dois testes que usam a mesma fixture conseguem rodar em sequência sem interferência de estado
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- DbContext funcional com pool e snake_case
- Fixture Testcontainers reutilizável em toda a suite de integração
- Migração inicial aplicada sem erro
