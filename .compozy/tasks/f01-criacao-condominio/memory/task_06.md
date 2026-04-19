# Task Memory: task_06.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Modelar `Condominio` e `Sindico`, criar mapeamentos/tabelas EF, expor repositórios e entregar testes unitários + integração exigidos pela task 06.

## Important Decisions
- `Condominio` herda `AggregateRoot` porque a abstração já existe no repositório, apesar da nota histórica da task mencionar `task_11`.
- A normalização/validação de CNPJ ficou centralizada em `CnpjValidator`, e `Condominio.Create(...)` já persiste o valor normalizado.
- As interfaces dos repositórios ficam em `PortaBox.Modules.Gestao`, mas as implementações concretas e o registro DI ficam em `PortaBox.Infrastructure`.

## Learnings
- `AppDbContext` precisava expor `DbSet<Condominio>` e `DbSet<Sindico>` e mapear a FK opcional `AppUser.SindicoTenantId -> condominio.id` para que a migration refletisse ADR-005.
- O teste de isolamento do `SindicoRepository` precisa semear `AppUser`, `Condominio` e `Sindico` no mesmo `SaveChanges` quando `SindicoTenantId` é preenchido.

## Files / Surfaces
- `src/PortaBox.Modules.Gestao/Domain/*`
- `src/PortaBox.Modules.Gestao/Application/Repositories/*`
- `src/PortaBox.Modules.Gestao/Application/Validators/CnpjValidator.cs`
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs`
- `src/PortaBox.Infrastructure/Persistence/*Configuration.cs`
- `src/PortaBox.Infrastructure/Repositories/*`
- `src/PortaBox.Infrastructure/Persistence/Migrations/20260418151557_AddCondominioSindico*.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/CnpjValidatorAndCondominioTests.cs`
- `tests/PortaBox.Api.IntegrationTests/CondominioSindicoPersistenceTests.cs`

## Errors / Corrections
- O primeiro seed do teste de isolamento criava `AppUser` com `SindicoTenantId` antes do `Condominio` existir; isso quebrava por FK e foi corrigido agrupando os inserts no mesmo `SaveChanges`.

## Ready for Next Run
- `task_07`, `task_12`, `task_15` e `task_17` já podem reutilizar `Condominio`, `Sindico`, `ICondominioRepository` e `ISindicoRepository`.
