---
status: completed
title: Entidade OptInRecord + tabela + repositório
type: backend
complexity: low
dependencies:
  - task_05
  - task_06
---

# Task 07: Entidade OptInRecord + tabela + repositório

## Overview
Modela o registro dos metadados do opt-in coletivo LGPD (ADR-002): relação 1..1 com `Condominio`, contendo data da assembleia, quórum, signatário e data do termo. A tabela é multi-tenant (implementa `ITenantEntity`) e é a evidência estrutural consultada pelo auditor/síndico quando necessário.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar entidade `OptInRecord` implementando `ITenantEntity` com campos definidos no TechSpec (seção Data Models — `opt_in_record`)
- MUST criar migração EF Core com índice UNIQUE em `tenant_id` (1..1 com `Condominio`)
- MUST implementar validador de CPF (formato + dígito verificador) como utility
- MUST armazenar CPF normalizado (11 dígitos, sem máscara)
- MUST expor `IOptInRecordRepository` com operações usadas pelo handler (`AddAsync`, `GetByTenantIdAsync`)
</requirements>

## Subtasks
- [x] 07.1 Implementar entidade `OptInRecord`
- [x] 07.2 Implementar validador de CPF (formato + DV)
- [x] 07.3 Configurar `IEntityTypeConfiguration<OptInRecord>`
- [x] 07.4 Gerar migração EF Core com tabela + UNIQUE(tenant_id)
- [x] 07.5 Implementar `IOptInRecordRepository` + implementação

## Implementation Details
Conforme seção **Data Models** do TechSpec e ADR-002. A tabela tem UNIQUE em `tenant_id` para impedir múltiplos registros de opt-in por tenant no MVP (se evoluir para histórico de opt-ins, tratar em outro PRD).

### Relevant Files
- `src/PortaBox.Modules.Gestao/Domain/OptInRecord.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Validators/CpfValidator.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Infrastructure/Persistence/OptInRecordConfiguration.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Repositories/IOptInRecordRepository.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Infrastructure/Repositories/OptInRecordRepository.cs` (a criar)
- `src/PortaBox.Infrastructure/Persistence/Migrations/*_OptInRecord.cs` (a criar)

### Dependent Files
- `task_12` (CreateCondominio handler) persiste `OptInRecord` junto com `Condominio`
- `task_17` (GetCondominioDetails query) inclui dados do opt-in na resposta

### Related ADRs
- [ADR-002: Registro do Opt-in Coletivo LGPD com Metadados Obrigatórios e Upload Opcional](../adrs/adr-002.md) — define os campos obrigatórios desta entidade.

## Deliverables
- Entidade `OptInRecord` + mapeamento EF Core
- Validador de CPF coberto por testes
- Migração com UNIQUE(tenant_id)
- Repositório com interface minimalista
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para persistência + constraint **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `CpfValidator.IsValid("12345678909")` retorna true para CPF válido
  - [x] `CpfValidator.IsValid("11111111111")` retorna false (DV inválido)
  - [x] `CpfValidator.Normalize("123.456.789-09")` retorna `"12345678909"`
  - [x] `OptInRecord.Create(...)` com campos obrigatórios preenchidos instancia corretamente
  - [x] `OptInRecord.Create(...)` com CPF inválido lança exceção de domínio
- Integration tests:
  - [x] Inserir `OptInRecord` para tenant `A`, tentar inserir segundo para tenant `A` viola `UNIQUE(tenant_id)`
  - [x] Dois tenants distintos podem cada um ter seu próprio `OptInRecord` sem conflito
  - [x] Query filter multi-tenant de task_05 impede tenant `B` de ver `OptInRecord` do tenant `A`
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Tabela `opt_in_record` com UNIQUE(tenant_id)
- CPF validado e normalizado antes de persistência
- Repositório pronto para uso em task_12 e task_17
