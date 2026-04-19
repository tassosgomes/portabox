---
status: completed
title: Entidades Condominio + Sindico + tabelas + repositórios
type: backend
complexity: medium
dependencies:
  - task_04
  - task_05
---

# Task 06: Entidades Condominio + Sindico + tabelas + repositórios

## Overview
Modela as duas entidades raiz do domínio D01: `Condominio` (agregado raiz, também tenant root — `id == tenant_id`) e `Sindico` (entidade multi-tenant ligada a um `AppUser`). Cria as tabelas `condominio` e `sindico`, configura os mapeamentos EF Core e expõe repositórios especializados usados pelos handlers das tasks seguintes.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar entidade `Condominio` em `PortaBox.Modules.Gestao.Domain` com campos descritos no TechSpec (seção Data Models — `condominio`)
- MUST criar entidade `Sindico` implementando `ITenantEntity` com FK para `AppUser` (único) e FK para `Condominio` (via `tenant_id`)
- MUST criar migração EF Core gerando as tabelas `condominio` e `sindico` com índices (`cnpj UNIQUE`, `(tenant_id, uploaded_at DESC)` não aplicável aqui; `(tenant_id)` em `sindico`)
- MUST persistir CNPJ normalizado (14 dígitos, sem máscara)
- MUST implementar validador de CNPJ (formato + dígito verificador) como serviço/static utility
- MUST expor `ICondominioRepository` e `ISindicoRepository` com operações usadas pelos handlers (`AddAsync`, `GetByIdAsync`, `ExistsByCnpjAsync`, `UpdateAsync`)
- SHOULD marcar `Condominio` como AggregateRoot (herdar de `AggregateRoot` — `task_11` introduz essa classe, então inicialmente pode ser classe comum e ser refatorada quando task_11 entrar; documentar o gap)
</requirements>

## Subtasks
- [x] 06.1 Implementar entidade `Condominio` com campos do TechSpec
- [x] 06.2 Implementar entidade `Sindico` com `ITenantEntity`
- [x] 06.3 Implementar validador de CNPJ (formato + DV)
- [x] 06.4 Configurar `IEntityTypeConfiguration` para ambas
- [x] 06.5 Gerar migração EF Core com as duas tabelas + índices
- [x] 06.6 Implementar `ICondominioRepository` e `ISindicoRepository` + implementações

## Implementation Details
Seguir a seção **Data Models** do TechSpec. CNPJ é `CHAR(14)` com `UNIQUE INDEX` (ADR-003). A entidade `Condominio` cujo `id` é também o `tenant_id` não implementa `ITenantEntity` (ela **é** o tenant); o query filter dela é um filtro simples baseado no contexto quando aplicável. `Sindico` implementa `ITenantEntity` e tem FK composta implícita via `tenant_id`.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Domain/Condominio.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Domain/Sindico.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Domain/CondominioStatus.cs` — enum `PreAtivo`, `Ativo` (a criar)
- `src/PortaBox.Modules.Gestao/Domain/SindicoStatus.cs` — enum `Ativo`, `Inativo` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Validators/CnpjValidator.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Infrastructure/Persistence/CondominioConfiguration.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Infrastructure/Persistence/SindicoConfiguration.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Repositories/ICondominioRepository.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Repositories/ISindicoRepository.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Infrastructure/Repositories/CondominioRepository.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Infrastructure/Repositories/SindicoRepository.cs` (a criar)
- `src/PortaBox.Infrastructure/Persistence/Migrations/*_CondominioSindico.cs` (a criar)

### Dependent Files
- `task_07` (OptInRecord) depende de `Condominio`
- `task_12` (CreateCondominio handler) usa os repositórios
- `task_14` (Activate) altera `Condominio.status`
- `task_15` (Resend magic link) usa `SindicoRepository`
- `task_17` (Queries) usa ambos

### Related ADRs
- [ADR-003: CNPJ Obrigatório como Identificador Canônico](../adrs/adr-003.md) — valida formato + unicidade.
- [ADR-004: Isolamento Multi-tenant via Shared Schema](../adrs/adr-004.md) — `Sindico` implementa `ITenantEntity`.
- [ADR-005: Backoffice como SPA React Separado; Identity com roles](../adrs/adr-005.md) — `Sindico.UserId` aponta para `AspNetUsers`.

## Deliverables
- Entidades `Condominio` e `Sindico` com mapeamentos EF Core
- Validador de CNPJ coberto por testes
- Migração gerando tabelas com índices corretos
- Repositórios com interface minimalista
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para persistência + índices **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `CnpjValidator.IsValid("12345678000195")` retorna true para CNPJ válido
  - [x] `CnpjValidator.IsValid("11111111111111")` retorna false (DV inválido)
  - [x] `CnpjValidator.IsValid("123")` retorna false (tamanho inválido)
  - [x] `CnpjValidator.Normalize("12.345.678/0001-95")` retorna `"12345678000195"`
  - [x] `Condominio.Create(...)` instancia com status `PreAtivo` e `CreatedAt` do clock injetado
- Integration tests:
  - [x] Inserir `Condominio` com CNPJ `X` e tentar inserir outro com CNPJ `X` viola `UNIQUE` (Postgres devolve erro)
  - [x] `CondominioRepository.ExistsByCnpjAsync` retorna true para existente e false para inexistente
  - [x] `SindicoRepository.GetByUserIdAsync` retorna `Sindico` do tenant atual; não retorna de outro tenant (teste de isolamento herda de task_05)
  - [x] Migração aplica e rollback funciona em `PostgresDatabaseFixture`
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Tabelas `condominio` e `sindico` criadas em snake_case com índices corretos
- CNPJ normalizado e validado antes de persistência
- Repositórios consumíveis pelos handlers seguintes
