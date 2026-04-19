---
status: pending
title: Agregado Bloco (entity + eventos + EF Configuration + IBlocoRepository + BlocoRepository)
type: backend
complexity: medium
dependencies:
  - task_01
---

# Task 03: Agregado Bloco (entity + eventos + EF Configuration + IBlocoRepository + BlocoRepository)

## Overview
Introduz o agregado `Bloco` como subdomínio de D01: entidade com regras (criar, renomear, inativar, reativar), 4 eventos de domínio, mapeamento EF com partial unique index, e repositório abstrato + implementação. É pré-requisito para os handlers de F02 (task_06), para a migration (task_05) e para o agregado `Unidade` indiretamente (via FK por ID, mas acoplamento de leitura em task_07).

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar `Bloco` em `PortaBox.Modules.Gestao/Domain/Blocos/Bloco.cs` herdando `SoftDeleteableAggregateRoot` e implementando `ITenantEntity`
- MUST expor apenas getters públicos; nenhum setter público; construção via `static Result<Bloco> Create(Guid id, Guid tenantId, Guid condominioId, string nome, Guid porUserId, IClock clock)`
- MUST implementar `Result Rename(string novoNome, Guid porUserId, DateTime agoraUtc)` com guardas (nome inválido; novo nome igual ao atual; bloco inativo) e emissão de `BlocoRenomeadoV1`
- MUST sobrescrever `Inativar`/`Reativar` da base class emitindo `BlocoInativadoV1` e `BlocoReativadoV1` com payload contendo `blocoId` e `nome`
- MUST criar eventos `BlocoCriadoV1`, `BlocoRenomeadoV1`, `BlocoInativadoV1`, `BlocoReativadoV1` em `Domain/Blocos/Events/` como records `: IDomainEvent` com campos mínimos (id, tenantId, blocoId, datas, antes/depois quando aplicável)
- MUST criar `BlocoConfiguration : IEntityTypeConfiguration<Bloco>` com tabela `bloco`, colunas snake_case conforme convenção do projeto, índice `idx_bloco_condominio`, índice **partial unique** `idx_bloco_nome_ativo_unique ON bloco (tenant_id, condominio_id, nome) WHERE ativo = true` declarado via `HasFilter("ativo = true")`
- MUST criar `IBlocoRepository` (`Application/Blocos/`) com `GetByIdAsync`, `GetByIdIncludingInactiveAsync`, `ExistsActiveWithNameAsync(condominioId, nome, ct)`, `ListByCondominioAsync(condominioId, includeInactive, ct)`, `AddAsync`, `SaveAsync`
- MUST implementar `BlocoRepository` em `Infrastructure/Repositories/BlocoRepository.cs` com `.IgnoreQueryFilters()` exclusivamente no método `GetByIdIncludingInactiveAsync`
- SHOULD colocar validações de entrada (nome 1–50 chars, não vazio, trim) no `Bloco.Create` e no `Bloco.Rename` (o FluentValidation nos handlers de task_06 é complementar, não substituto)
</requirements>

## Subtasks
- [ ] 03.1 Criar `Bloco.cs` com factory `Create`, método `Rename` e overrides `Inativar`/`Reativar`
- [ ] 03.2 Criar os 4 eventos de domínio em `Domain/Blocos/Events/`
- [ ] 03.3 Criar `BlocoConfiguration` com índices e partial unique index
- [ ] 03.4 Criar `IBlocoRepository` no Application e `BlocoRepository` no Infrastructure
- [ ] 03.5 Registrar `DbSet<Bloco>` em `AppDbContext`
- [ ] 03.6 Escrever unit tests para `Bloco` (factory, rename, inativar/reativar, guardas)

## Implementation Details
Ver TechSpec seções **Core Interfaces** (signature de `Bloco`) e **Data Models** (schema de `bloco` e índices). O partial unique index deve ser declarado em EF Core via `.HasIndex(...).HasFilter("ativo = true").IsUnique()`; EF gera `CREATE UNIQUE INDEX ... WHERE ativo = true` automaticamente no Postgres.

Eventos emitidos via `AddDomainEvent()` herdado de `AggregateRoot`. O `DomainEventOutboxInterceptor` (existente de F01) persiste automaticamente no outbox durante `SaveChangesAsync`.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Domain/Blocos/Bloco.cs` — entity (novo)
- `src/PortaBox.Modules.Gestao/Domain/Blocos/Events/BlocoCriadoV1.cs` — event record (novo)
- `src/PortaBox.Modules.Gestao/Domain/Blocos/Events/BlocoRenomeadoV1.cs` — event record (novo)
- `src/PortaBox.Modules.Gestao/Domain/Blocos/Events/BlocoInativadoV1.cs` — event record (novo)
- `src/PortaBox.Modules.Gestao/Domain/Blocos/Events/BlocoReativadoV1.cs` — event record (novo)
- `src/PortaBox.Modules.Gestao/Infrastructure/EfConfigurations/BlocoConfiguration.cs` — EF mapping (novo)
- `src/PortaBox.Modules.Gestao/Application/Blocos/IBlocoRepository.cs` — contract (novo)
- `src/PortaBox.Modules.Gestao/Infrastructure/Repositories/BlocoRepository.cs` — impl (novo)
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs` — adicionar `DbSet<Bloco>`
- `tests/PortaBox.Modules.Gestao.UnitTests/Domain/Blocos/BlocoTests.cs` — unit tests (novo)

### Dependent Files
- Handlers de Bloco (task_06) consumirão factory e métodos
- `CreateUnidadeCommandHandler` (task_07) consultará `IBlocoRepository` para validar bloco ativo
- Migration (task_05) gera DDL a partir desta configuração

### Related ADRs
- [ADR-002: Forma Canônica Estrita — Bloco e Andar Obrigatórios](adrs/adr-002.md) — nome 1–50 chars; bloco obrigatório
- [ADR-003: Remoção Exclusivamente por Inativação; Edição Restrita](adrs/adr-003.md) — apenas nome é editável em Bloco
- [ADR-007: Soft-Delete Padronizado](adrs/adr-007.md) — base class + partial unique index

## Deliverables
- `Bloco` + 4 eventos + `BlocoConfiguration` + `IBlocoRepository` + `BlocoRepository` compiláveis
- `DbSet<Bloco>` registrado em `AppDbContext`
- Partial unique index declarado em EF Configuration
- Unit tests cobrindo factory, rename, inativar/reativar, guardas de estado
- Unit tests with 80%+ coverage **(REQUIRED)**
- Integration tests para constraint de unicidade e soft-delete — cobertos em task_10

## Tests
- Unit tests:
  - [ ] `Create` com nome válido → `Result.Success`; entidade com `Ativo=true`, eventos contém `BlocoCriadoV1`
  - [ ] `Create` com nome `""`, `"   "`, ou > 50 chars → `Result.Failure`
  - [ ] `Create` com `nome.Trim()` distinto — valor armazenado é o trimado
  - [ ] `Rename` com novo nome válido → `Result.Success`; `Nome` atualizado; evento `BlocoRenomeadoV1` com `nomeAntes` e `nomeDepois`
  - [ ] `Rename` em bloco inativo → `Result.Failure("não é possível renomear bloco inativo")`
  - [ ] `Rename` com mesmo nome atual → `Result.Failure`
  - [ ] `Inativar` emite `BlocoInativadoV1` com `blocoId` e `nome` atuais
  - [ ] `Reativar` emite `BlocoReativadoV1`
  - [ ] `IBlocoRepository.ExistsActiveWithNameAsync` retorna `false` para bloco inativo com mesmo nome
- Integration tests:
  - [ ] Cobertos em task_10 (unicidade canônica, soft-delete filter, cross-tenant)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- `Bloco` e seu repositório prontos para consumo por task_05 (migration) e task_06 (handlers)
- Partial unique index garante que dois blocos ativos não podem ter o mesmo `(tenant_id, condominio_id, nome)` mas um ativo + um inativo com mesmo nome coexistem
- Nenhuma alteração em código de F01 exceto o registro de `DbSet<Bloco>`
