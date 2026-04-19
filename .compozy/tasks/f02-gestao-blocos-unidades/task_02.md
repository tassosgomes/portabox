---
status: pending
title: Extensão TenantAuditEntry.EventKind + StructuralAuditMetadata + IAuditService.RecordStructuralAsync
type: backend
complexity: low
dependencies: []
---

# Task 02: Extensão TenantAuditEntry.EventKind + StructuralAuditMetadata + IAuditService.RecordStructuralAsync

## Overview
Estende a infraestrutura de auditoria de F01 para cobrir as 7 operações estruturais de F02 (bloco criado/renomeado/inativado/reativado; unidade criada/inativada/reativada). Adiciona valores ao enum `TenantAuditEntry.EventKind`, cria helper `StructuralAuditMetadata` para padronizar o payload JSONB e expõe um método no `IAuditService` para consumidores (handlers de F02) não precisarem conhecer o shape da `TenantAuditEntry`.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST adicionar valores ao enum `TenantAuditEntry.EventKind`: `BlocoCriado=5`, `BlocoRenomeado=6`, `BlocoInativado=7`, `BlocoReativado=8`, `UnidadeCriada=9`, `UnidadeInativada=10`, `UnidadeReativada=11`
- MUST preservar os valores existentes (`Created=1`, `Activated=2`, `MagicLinkResent=3`, `Other=4`) sem renumerar
- MUST criar `StructuralAuditMetadata` (static class) em `PortaBox.Modules.Gestao/Application/Audit/` com métodos `For{Kind}(...)` que produzem `IDictionary<string, object>` com o schema documentado por kind (ver ADR-008)
- MUST estender `IAuditService` com método `RecordStructuralAsync(TenantAuditKind kind, Guid tenantId, Guid performedByUserId, IDictionary<string, object> metadata, string? note, CancellationToken ct)` que cria `TenantAuditEntry` no `DbContext` (não faz commit; o handler commita junto com a mutação)
- MUST garantir que o schema de cada kind é testado via unit test (construção do dictionary retorna chaves esperadas)
- SHOULD auditar switches existentes em F01 que consomem `EventKind` sem `default` clause e adicionar `default: throw new InvalidOperationException($"EventKind {kind} não tratado");` para prevenir regressões silenciosas
</requirements>

## Subtasks
- [ ] 02.1 Estender enum `EventKind` com os 7 novos valores
- [ ] 02.2 Auditar uso existente de `EventKind` em F01 e adicionar `default` clauses onde faltarem
- [ ] 02.3 Criar `StructuralAuditMetadata` com um método estático por kind, seguindo schema do ADR-008
- [ ] 02.4 Estender `IAuditService` e a implementação concreta com `RecordStructuralAsync`
- [ ] 02.5 Escrever unit tests para cada método de `StructuralAuditMetadata` validando chaves presentes
- [ ] 02.6 Escrever unit test para `RecordStructuralAsync` verificando que adiciona `TenantAuditEntry` ao contexto sem executar commit

## Implementation Details
Schema de `metadata` por kind conforme ADR-008 seção Implementation Notes:
- `BlocoCriado`: `{ "blocoId", "nome" }`.
- `BlocoRenomeado`: `{ "blocoId", "nomeAntes", "nomeDepois" }`.
- `BlocoInativado` / `BlocoReativado`: `{ "blocoId", "nome" }`.
- `UnidadeCriada`: `{ "unidadeId", "blocoId", "andar", "numero" }`.
- `UnidadeInativada` / `UnidadeReativada`: idem UnidadeCriada.

A coluna Postgres `event_kind` é `smallint` (não muda); a conversão EF via `HasConversion<short>()` aceita novos valores automaticamente.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Domain/TenantAuditEntry.cs` — enum a estender
- `src/PortaBox.Modules.Gestao/Application/Audit/IAuditService.cs` — contrato a estender (se não existir, criar)
- `src/PortaBox.Modules.Gestao/Application/Audit/StructuralAuditMetadata.cs` — novo helper
- `src/PortaBox.Modules.Gestao/Infrastructure/Audit/AuditService.cs` (ou equivalente) — implementação concreta
- `tests/PortaBox.Modules.Gestao.UnitTests/Audit/StructuralAuditMetadataTests.cs` — novos tests
- `tests/PortaBox.Modules.Gestao.UnitTests/Audit/AuditServiceTests.cs` — novos tests

### Dependent Files
- Handlers de task_06 (Bloco) e task_07 (Unidade) consumirão `RecordStructuralAsync`
- Migration de task_05 não altera schema de `tenant_audit_entry`; enum é só C#

### Related ADRs
- [ADR-008: Auditoria via Extensão de TenantAuditEntry.EventKind + MetadataJson](adrs/adr-008.md) — define padrão e schemas
- [ADR-005: Escrita Exclusiva do Síndico; Backoffice Read-Only Cross-Tenant](adrs/adr-005.md) — auditoria registra autor (síndico) de cada operação

## Deliverables
- Enum `EventKind` com 11 valores
- Helper `StructuralAuditMetadata` com 7 métodos estáticos documentados em xmldoc
- `IAuditService.RecordStructuralAsync` disponível para consumo
- Unit tests verificando schema do metadata por kind
- Unit tests with 80%+ coverage **(REQUIRED)**
- Integration tests não necessários aqui (cobertos em task_10 via `AuditIntegrationTests`)

## Tests
- Unit tests:
  - [ ] `StructuralAuditMetadata.ForBlocoCriado(blocoId, nome)` retorna dict com chaves `["blocoId", "nome"]` e valores corretos
  - [ ] `StructuralAuditMetadata.ForBlocoRenomeado(...)` inclui `nomeAntes` e `nomeDepois`
  - [ ] `StructuralAuditMetadata.ForUnidadeCriada(...)` inclui `andar` como `int` e `numero` como `string`
  - [ ] `AuditService.RecordStructuralAsync` adiciona `TenantAuditEntry` ao `DbContext` com `EventKind` correto, `PerformedByUserId` e `MetadataJson` serializado
  - [ ] `RecordStructuralAsync` não chama `SaveChangesAsync` (caller-controlled commit)
  - [ ] Switch existentes em F01 com novo `default` clause continuam compilando e cobrem os 11 valores
- Integration tests:
  - [ ] N/A — cobertura integrada em task_10 (`AuditIntegrationTests`)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Handlers de task_06/task_07 podem consumir `IAuditService.RecordStructuralAsync` sem conhecer o shape de `TenantAuditEntry`
- Código existente de F01 compila sem warnings sobre `default` clause faltante
- `MetadataJson` serializa para JSONB válido no Postgres (validado indiretamente em task_10)
