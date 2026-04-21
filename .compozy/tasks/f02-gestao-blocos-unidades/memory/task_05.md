# Task Memory: task_05.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Gerar e validar a migration `AddBlocoAndUnidade`, incluindo partial unique indexes, FKs com `ON DELETE RESTRICT`, `CHECK andar >= 0`, snapshot atualizado e smoke tests de `Up`/`Down` em PostgreSQL real.

## Important Decisions

- A migration gerada pelo EF Core ja atendeu o SQL esperado do TechSpec (`filter: "ativo = true"`), entao nao foi necessario complementar com `migrationBuilder.Sql(...)`.
- Os smoke tests ficaram em `tests/PortaBox.Api.IntegrationTests/Persistence/AddBlocoAndUnidadeMigrationSmokeTests.cs`, reusando a fixture real de Postgres/Testcontainers para cobrir `Up`, indices e `Down` sem antecipar os cenarios funcionais da task 10.

## Learnings

- O script SQL gerado por `dotnet ef migrations script` confirmou `CREATE UNIQUE INDEX ... WHERE ativo = true` para `bloco` e `unidade` sem ajustes manuais.
- A validacao local contra o Postgres do compose exige sobrescrever `ConnectionStrings__Postgres`, porque o design-time factory usa outro par de credenciais por default.

## Files / Surfaces

- `src/PortaBox.Infrastructure/Persistence/Migrations/20260420154647_AddBlocoAndUnidade.cs`
- `src/PortaBox.Infrastructure/Persistence/Migrations/20260420154647_AddBlocoAndUnidade.Designer.cs`
- `src/PortaBox.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `tests/PortaBox.Api.IntegrationTests/Persistence/AddBlocoAndUnidadeMigrationSmokeTests.cs`

## Errors / Corrections

- Os smoke tests falharam inicialmente por uso de `GetMigrationsAsync`; a correcao foi trocar para `Database.GetMigrations()` compativel com o baseline atual do projeto de testes.
- A primeira tentativa de `dotnet ef database update` local falhou por autenticacao (`postgres/postgres` vs `portabox/portabox`); a rerun com `ConnectionStrings__Postgres` explicita validou aplicacao e rollback corretamente.

## Ready for Next Run

- A migration esta aplicada novamente no Postgres local do compose ao final da validacao e os smoke tests especificos da task passaram.
