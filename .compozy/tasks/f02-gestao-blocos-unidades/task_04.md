---
status: completed
title: Agregado Unidade (entity + eventos + EF Configuration + IUnidadeRepository + UnidadeRepository)
type: backend
complexity: medium
dependencies:
  - task_01
---

# Task 04: Agregado Unidade (entity + eventos + EF Configuration + IUnidadeRepository + UnidadeRepository)

## Overview
Introduz o agregado `Unidade` como subdomínio de D01: entidade imutável em seus atributos canônicos (bloco + andar + número), 3 eventos de domínio, mapeamento EF com partial unique index canônico e repositório com método dedicado `FindActiveByCanonicalAsync` que será consumido por F07 (API interna) e F04 (importação de moradores).

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar `Unidade` em `PortaBox.Modules.Gestao/Domain/Unidades/Unidade.cs` herdando `SoftDeleteableAggregateRoot` e implementando `ITenantEntity`
- MUST expor apenas getters; construção via `static Result<Unidade> Create(Guid id, Guid tenantId, Bloco bloco, int andar, string numero, Guid porUserId, IClock clock)` — a factory recebe `Bloco` (não apenas `blocoId`) para permitir validação inline de `bloco.Ativo == true` e de `tenantId` coerente
- MUST validar: `andar >= 0`; `numero` casando com regex `^[0-9]{1,4}[A-Z]?$` após normalização para caixa alta; bloco ativo
- MUST **não expor** método de Rename ou de edição de atributos canônicos — unidade é imutável por design (ADR-003)
- MUST sobrescrever `Inativar`/`Reativar` da base class emitindo `UnidadeInativadaV1` e `UnidadeReativadaV1`
- MUST criar eventos `UnidadeCriadaV1`, `UnidadeInativadaV1`, `UnidadeReativadaV1` em `Domain/Unidades/Events/`
- MUST criar `UnidadeConfiguration` com tabela `unidade`, coluna `numero varchar(5)`, `CHECK (andar >= 0)` via `ToTable(tb => tb.HasCheckConstraint(...))`, índice `idx_unidade_bloco` e partial unique index `idx_unidade_canonica_ativa ON unidade (tenant_id, bloco_id, andar, numero) WHERE ativo = true`
- MUST criar `IUnidadeRepository` com `GetByIdAsync`, `GetByIdIncludingInactiveAsync`, `FindActiveByCanonicalAsync(tenantId, blocoId, andar, numero, ct)`, `ExistsActiveWithCanonicalAsync(tenantId, blocoId, andar, numero, ct)`, `ListByBlocoAsync(blocoId, includeInactive, ct)`, `AddAsync`, `SaveAsync`
- MUST implementar `UnidadeRepository` em `Infrastructure/Repositories/UnidadeRepository.cs`
- SHOULD garantir que `Create` rejeita caixa baixa (`101a`) — normaliza para `101A` antes da validação regex, mas o teste documenta que input original pode ser minúsculo
</requirements>

## Subtasks
- [x] 04.1 Criar `Unidade.cs` com factory `Create` recebendo `Bloco` e validações estruturais
- [x] 04.2 Criar os 3 eventos de domínio em `Domain/Unidades/Events/`
- [x] 04.3 Criar `UnidadeConfiguration` com CHECK constraint e partial unique index canônico
- [x] 04.4 Criar `IUnidadeRepository` com `FindActiveByCanonicalAsync` e implementar `UnidadeRepository`
- [x] 04.5 Registrar `DbSet<Unidade>` em `AppDbContext`
- [x] 04.6 Escrever unit tests cobrindo regex do número, andar inválido, bloco inativo, bloco de outro tenant, eventos

## Implementation Details
Ver TechSpec seções **Core Interfaces** (signature de `Unidade`) e **Data Models** (schema de `unidade`).

A factory `Create` recebe `Bloco` (não apenas `blocoId`) intencionalmente — a intenção é forçar o consumidor (handler) a carregar o bloco pelo repositório antes, o que já valida filter de tenant + soft-delete. Assim a entidade nunca é construída com um `blocoId` órfão.

Normalização do número: `numero = numero?.Trim()?.ToUpperInvariant()` antes da validação regex. Rejeitar null/empty/whitespace como validação explícita no método.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Domain/Unidades/Unidade.cs` — entity (novo)
- `src/PortaBox.Modules.Gestao/Domain/Unidades/Events/UnidadeCriadaV1.cs` — event (novo)
- `src/PortaBox.Modules.Gestao/Domain/Unidades/Events/UnidadeInativadaV1.cs` — event (novo)
- `src/PortaBox.Modules.Gestao/Domain/Unidades/Events/UnidadeReativadaV1.cs` — event (novo)
- `src/PortaBox.Modules.Gestao/Infrastructure/EfConfigurations/UnidadeConfiguration.cs` — EF mapping (novo)
- `src/PortaBox.Modules.Gestao/Application/Unidades/IUnidadeRepository.cs` — contract (novo)
- `src/PortaBox.Modules.Gestao/Infrastructure/Repositories/UnidadeRepository.cs` — impl (novo)
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs` — adicionar `DbSet<Unidade>`

### Dependent Files
- Handlers de Unidade (task_07) consumirão factory e repositório
- `GetEstruturaQueryHandler` (task_08) consumirá `IUnidadeRepository.ListByBlocoAsync`
- F04 (futura feature) consumirá `FindActiveByCanonicalAsync`
- Migration (task_05) materializa schema

### Related ADRs
- [ADR-002: Forma Canônica Estrita — Bloco e Andar Obrigatórios; Número com Sufixo Alfabético](adrs/adr-002.md) — regex do número, andar ≥ 0
- [ADR-003: Remoção Exclusivamente por Inativação; Edição Restrita](adrs/adr-003.md) — Unidade imutável
- [ADR-007: Soft-Delete Padronizado](adrs/adr-007.md) — partial unique canônico

## Deliverables
- `Unidade` + 3 eventos + `UnidadeConfiguration` + `IUnidadeRepository` + `UnidadeRepository` compiláveis
- `DbSet<Unidade>` registrado em `AppDbContext`
- Partial unique index canônico declarado
- Unit tests cobrindo regex do número, andar inválido, bloco inativo/tenant diferente, eventos emitidos
- Unit tests with 80%+ coverage **(REQUIRED)**
- Integration tests para unicidade canônica e soft-delete — cobertos em task_10

## Tests
- Unit tests:
  - [x] `Create` com bloco ativo, andar=2, numero="201" → sucesso; `UnidadeCriadaV1` emitido
  - [x] `Create` normaliza `numero="101a"` para `"101A"` antes de persistir
  - [x] `Create` com `numero="1AB"`, `"12345"`, `"20000"`, `""`, `" "` → `Result.Failure` com mensagem clara
  - [x] `Create` com `andar=-1` → `Result.Failure`
  - [x] `Create` com bloco inativo → `Result.Failure("bloco inativo")`
  - [x] `Create` com `bloco.TenantId != tenantId` do parâmetro → `Result.Failure("inconsistência de tenant")`
  - [x] `Inativar`/`Reativar` emitem eventos corretos com `blocoId`, `andar`, `numero` no payload
  - [x] `IUnidadeRepository.FindActiveByCanonicalAsync` retorna `null` para unidade inativa mesmo com tripla exata
  - [x] Método `GetByIdIncludingInactiveAsync` retorna unidade inativa
- Integration tests:
  - [ ] Cobertos em task_10 (unicidade canônica com reinserção após inativação, cross-tenant)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- `Unidade` e seu repositório prontos para task_05 (migration) e task_07 (handlers)
- Impossível criar duas unidades ativas com mesma tripla canônica no mesmo tenant
- Impossível renomear atributos canônicos (não há método exposto)
