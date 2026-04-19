---
status: completed
title: ActivateCondominioCommandHandler + tabela tenant_audit_log
type: backend
complexity: medium
dependencies:
  - task_06
  - task_11
---

# Task 14: ActivateCondominioCommandHandler + tabela tenant_audit_log

## Overview
Implementa a ação de go-live manual definida em ADR-001: transição `PreAtivo → Ativo` via `POST /api/v1/admin/condominios/{id}:activate` (endpoint em task_18). Ao mesmo tempo, cria a tabela global `tenant_audit_log` que registra todas as transições de estado do tenant (criação, ativação, futuras suspensões, reenvios de magic link).

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar entidade `TenantAuditEntry` + tabela global `tenant_audit_log` conforme TechSpec (seção Data Models)
- MUST definir `ActivateCondominioCommand { CondominioId, PerformedByUserId, Note? }`
- MUST validar que o condomínio existe e está em `PreAtivo`
- MUST transicionar `Condominio.status` para `Ativo` + setar `activated_at` e `activated_by_user_id`
- MUST gravar `TenantAuditEntry` com `event_kind=Activated` na mesma transação
- MUST emitir evento `CondominioAtivadoV1` via `AggregateRoot.AddDomainEvent`
- MUST retornar erro `AlreadyActive` quando o condomínio já está `Ativo`
- MUST retrofitar a escrita de `TenantAuditEntry` com `event_kind=Created` no handler de task_12 (`CreateCondominio`) — se task_12 já foi implementado antes deste task, ajustar via edição
- SHOULD permitir que o handler escreva um log estruturado `condominio.activated` com `condominio_id`, `activated_by`, opcional `note` (sanitizado)
</requirements>

## Subtasks
- [x] 14.1 Implementar `TenantAuditEntry` + configuração EF + migração
- [x] 14.2 Definir `ActivateCondominioCommand` + `TenantAuditEventKind` enum
- [x] 14.3 Implementar `ActivateCondominioCommandValidator`
- [x] 14.4 Implementar `ActivateCondominioCommandHandler`
- [x] 14.5 Adicionar evento `CondominioAtivadoV1`
- [x] 14.6 Retrofitar `CreateCondominio` para registrar `TenantAuditEntry(Created)`

## Implementation Details
Conforme ADR-001. Tabela `tenant_audit_log` é global (sem `tenant_id` como FK separada; inclui coluna `tenant_id` mas como referência e não como filtro de isolamento — operadores precisam consultar entre tenants). JSONB opcional `metadata_json` para extensibilidade.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Domain/TenantAuditEntry.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Domain/TenantAuditEventKind.cs` — enum (a criar)
- `src/PortaBox.Modules.Gestao/Infrastructure/Persistence/TenantAuditEntryConfiguration.cs` (a criar)
- `src/PortaBox.Infrastructure/Persistence/Migrations/*_TenantAuditLog.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/ActivateCondominio/ActivateCondominioCommand.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/ActivateCondominio/ActivateCondominioCommandHandler.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/ActivateCondominio/ActivateCondominioCommandValidator.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Domain/Events/CondominioAtivadoV1.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/CreateCondominio/CreateCondominioCommandHandler.cs` — editar para incluir audit entry

### Dependent Files
- `task_17` (GetCondominioDetails) inclui últimas entradas do audit log
- `task_18` (controller) expõe endpoint de ativação
- `task_23` (backoffice) exibe histórico de auditoria

### Related ADRs
- [ADR-001: Onboarding de Tenant no MVP — Go-live Manual Independente](../adrs/adr-001.md) — define a transição manual.

## Deliverables
- Tabela `tenant_audit_log` + migração
- Command + Handler + Validator + Evento
- Retrofit no handler `CreateCondominio`
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para fluxo de ativação **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] Handler retorna `Result.Failure(NotFound)` quando `CondominioId` não existe
  - [ ] Handler retorna `Result.Failure(AlreadyActive)` quando status já é `Ativo`
  - [ ] Handler em happy path muda `status` para `Ativo`, seta `activated_at`/`activated_by_user_id`, emite `CondominioAtivadoV1`
  - [ ] Validator reprova `Note` com > 500 caracteres
- Integration tests:
  - [ ] Ativar tenant em `PreAtivo` → `status=Ativo` em `condominio` + 1 linha em `tenant_audit_log` com `event_kind=Activated`
  - [ ] `tenant_audit_log` acumula entradas de creation (task_12) e activation (task_14) para o mesmo tenant
  - [ ] Tentar ativar tenant já `Ativo` não grava nova entrada no audit log
  - [ ] Evento `CondominioAtivadoV1` é gravado em `domain_event_outbox` após commit
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Audit log consolidando criação + ativação
- Transição idempotente (ativar duas vezes = uma entrada)
- Handler pronto para endpoint REST em task_18
