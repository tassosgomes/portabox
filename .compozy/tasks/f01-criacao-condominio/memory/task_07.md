# Task Memory: task_07.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Implementar `OptInRecord` completo no backend com entidade, validador de CPF, mapeamento EF Core, repositório, migração e testes unitários/integrados.

## Important Decisions
- Mantido o padrão já adotado nas tasks 05/06: entidades e contratos ficam em `PortaBox.Modules.Gestao`, enquanto configuração EF, `DbSet`, migrações e repositórios concretos ficam em `PortaBox.Infrastructure`.
- `OptInRecord.Create(...)` normaliza o CPF para 11 dígitos e rejeita CPF inválido antes da persistência; as datas obrigatórias também não aceitam valor default.

## Learnings
- A cobertura do assembly `PortaBox.Modules.Gestao` inicialmente ficou abaixo da meta por falta de testes para `Sindico` e `DependencyInjection`; adicionar esses testes elevou a cobertura do pacote para 91.95%.

## Files / Surfaces
- `src/PortaBox.Modules.Gestao/Application/Repositories/IOptInRecordRepository.cs`
- `src/PortaBox.Modules.Gestao/Application/Validators/CpfValidator.cs`
- `src/PortaBox.Modules.Gestao/Domain/OptInRecord.cs`
- `src/PortaBox.Infrastructure/DependencyInjection.cs`
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs`
- `src/PortaBox.Infrastructure/Persistence/OptInRecordConfiguration.cs`
- `src/PortaBox.Infrastructure/Persistence/Migrations/20260418152518_AddOptInRecord.cs`
- `src/PortaBox.Infrastructure/Persistence/Migrations/20260418152518_AddOptInRecord.Designer.cs`
- `src/PortaBox.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/PortaBox.Infrastructure/Repositories/OptInRecordRepository.cs`
- `tests/PortaBox.Api.IntegrationTests/OptInRecordPersistenceTests.cs`
- `tests/PortaBox.Api.UnitTests/AppDbContextAndInfrastructureRegistrationTests.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/CpfValidatorAndOptInRecordTests.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/SindicoAndDependencyInjectionTests.cs`

## Errors / Corrections
- O teste de integração para tenants distintos assumia ordem estável dos GUIDs ao listar registros; a asserção foi corrigida para validar presença por `tenantId` sem depender de ordenação.

## Ready for Next Run
- `task_12` já pode persistir `OptInRecord` via `IOptInRecordRepository`.
- `task_17` já pode consultar os metadados de opt-in por `tenantId`.
