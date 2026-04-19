---
status: pending
title: "Infra soft-delete: ISoftDeletable + SoftDeleteableAggregateRoot + global filter por reflection"
type: backend
complexity: high
dependencies: []
---

# Task 01: Infra soft-delete: ISoftDeletable + SoftDeleteableAggregateRoot + global filter por reflection

## Overview
Estabelece o padrão de soft-delete para todo o projeto PortaBox: marker interface `ISoftDeletable`, classe base abstrata `SoftDeleteableAggregateRoot` (herda `AggregateRoot`) e extensão do `AppDbContext.OnModelCreating` aplicando global query filter por reflection. Esta infra inaugura o padrão que será reutilizado por `Bloco`, `Unidade` (F02), `Morador` (F03) e `DispositivoPortaria` (F06).

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar `ISoftDeletable` em `PortaBox.Domain.Abstractions` com propriedades read-only `Ativo`, `InativadoEm`, `InativadoPor`
- MUST criar `SoftDeleteableAggregateRoot` em `PortaBox.Domain` herdando `AggregateRoot`, implementando `ISoftDeletable` e expondo métodos `protected Inativar(Guid porUserId, DateTime agoraUtc)` e `protected Reativar(Guid porUserId, DateTime agoraUtc)` que retornam `Result` e protegem contra transições inválidas
- MUST estender `AppDbContext.OnModelCreating` com bloco de reflection que, para todo `IEntityType` cujo `ClrType` implemente `ISoftDeletable`, aplique `HasQueryFilter(e => e.Ativo == true)` via `Expression`
- MUST compor o filter com o filter de tenant existente (`ITenantEntity`) em `AND` — EF Core combina automaticamente quando `HasQueryFilter` é chamado múltiplas vezes via a mesma entidade; **a implementação deve gerar um único lambda combinado por entidade para evitar override** (último `HasQueryFilter` vence)
- MUST garantir que métodos `Inativar`/`Reativar` falhem (`Result.Failure`) quando a transição não faz sentido (já inativo ao inativar, já ativo ao reativar)
- SHOULD expor entry point de "bypass" explícito via `.IgnoreQueryFilters()` apenas em repositórios dedicados (`GetByIdIncludingInactiveAsync`); não introduzir flag global
</requirements>

## Subtasks
- [ ] 01.1 Criar `ISoftDeletable` com propriedades read-only em `PortaBox.Domain.Abstractions`
- [ ] 01.2 Criar `SoftDeleteableAggregateRoot` com `Inativar`/`Reativar` protegidos retornando `Result`
- [ ] 01.3 Estender `AppDbContext.OnModelCreating` com reflection que aplica filter combinado (tenant + soft-delete) a cada entidade aplicável
- [ ] 01.4 Criar entidade fake `TestSoftDeletable` no projeto de testes (não-persistida em produção) para validar o filter isoladamente
- [ ] 01.5 Escrever unit tests do comportamento da base class (transições inválidas, eventos)
- [ ] 01.6 Escrever integration test que persiste entidade fake em Postgres (Testcontainers) e valida que queries default não trazem inativos

## Implementation Details
Ver TechSpec seção **Core Interfaces** para o shape exato de `ISoftDeletable` e `SoftDeleteableAggregateRoot`. O mecanismo de reflection em `OnModelCreating` deve seguir o mesmo padrão já usado para `ITenantEntity` (confirmar arquivo `AppDbContext.cs`), preservando a composição dos filtros.

Ordem das extensões no `OnModelCreating`: aplicar o filter de tenant primeiro (já existente), então compor `AND Ativo == true` na mesma chamada de `HasQueryFilter` quando a entidade também for `ISoftDeletable`. Evitar duas chamadas sequenciais a `HasQueryFilter` pois a segunda sobrescreve a primeira em EF Core.

### Relevant Files
- `src/PortaBox.Domain.Abstractions/ISoftDeletable.cs` — novo marker interface
- `src/PortaBox.Domain/SoftDeleteableAggregateRoot.cs` — nova base class
- `src/PortaBox.Domain/AggregateRoot.cs` — classe base existente (herdada)
- `src/PortaBox.Domain/Result.cs` ou `src/PortaBox.Application.Abstractions/Result.cs` — tipo de retorno das guardas
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs` — extensão de `OnModelCreating`
- `tests/PortaBox.Modules.Gestao.UnitTests/Domain/SoftDeleteableAggregateRootTests.cs` — unit tests
- `tests/PortaBox.Api.IntegrationTests/Persistence/SoftDeleteFilterTests.cs` — integration test

### Dependent Files
- `src/PortaBox.Modules.Gestao/Domain/Blocos/Bloco.cs` (task_03) — herdará de `SoftDeleteableAggregateRoot`
- `src/PortaBox.Modules.Gestao/Domain/Unidades/Unidade.cs` (task_04) — idem
- Futuras entidades de F03 (Morador) e F06 (DispositivoPortaria) — mesmo padrão

### Related ADRs
- [ADR-007: Soft-Delete Padronizado via ISoftDeletable + SoftDeleteableAggregateRoot](adrs/adr-007.md) — define todo o padrão implementado nesta task
- [ADR-003: Remoção Exclusivamente por Inativação; Edição Restrita](adrs/adr-003.md) — política de produto que motiva a infra

## Deliverables
- `ISoftDeletable.cs` + `SoftDeleteableAggregateRoot.cs` novos e compiláveis
- Extensão de `AppDbContext.OnModelCreating` aplicando filter combinado tenant+soft-delete
- Unit tests cobrindo transições válidas e inválidas de `Inativar`/`Reativar`
- Integration test que persiste entidade fake `ISoftDeletable` + `ITenantEntity` e verifica que queries default omitem inativos
- Unit tests with 80%+ coverage **(REQUIRED)**
- Integration tests para soft-delete filter **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] `Inativar` em entidade ativa → `Result.Success`; `Ativo=false`, `InativadoEm` e `InativadoPor` preenchidos
  - [ ] `Inativar` em entidade já inativa → `Result.Failure` com mensagem descritiva; estado não muda
  - [ ] `Reativar` em entidade inativa → `Result.Success`; `Ativo=true`, `InativadoEm=null`
  - [ ] `Reativar` em entidade já ativa → `Result.Failure`; estado não muda
  - [ ] Entidade recém-criada nasce com `Ativo=true` e `InativadoEm=null`
- Integration tests:
  - [ ] Seed de 1 entidade ativa + 1 inativa; `context.Set<T>().ToList()` retorna apenas a ativa
  - [ ] `context.Set<T>().IgnoreQueryFilters().ToList()` retorna ambas
  - [ ] Filter combinado: entidade de tenant A inativa não aparece em query de tenant A nem de tenant B
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- `ISoftDeletable` e `SoftDeleteableAggregateRoot` disponíveis para consumo por task_03 e task_04 sem retrabalho
- Queries default do `AppDbContext` omitem entidades inativas automaticamente
- Nenhum ponto no código existente de F01 precisa ser alterado exceto `AppDbContext.OnModelCreating`
