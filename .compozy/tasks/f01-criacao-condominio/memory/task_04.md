# Task Memory: task_04.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Integrar ASP.NET Core Identity ao `AppDbContext` com `Guid` PK, seed idempotente das roles `Operator`/`Sindico`, operador dev apenas em Development e baseline de cookie auth/policies para as próximas tasks.

## Important Decisions
- `AppDbContext` passou a herdar de `IdentityDbContext<AppUser, AppRole, Guid, ...>` e remapeia explicitamente as tabelas do Identity para `asp_net_*` para preservar o baseline `snake_case`.
- O seed roda no startup via `ApplyIdentityMigrationsAndSeedAsync()` quando `Persistence:ApplyMigrationsOnStartup` não está desligado.
- A senha mínima vem de configuração em `Identity:Password`; o operador dev usa `Identity:DevelopmentOperator`.
- As policies nomeadas expostas para uso futuro são `RequireOperator` e `RequireSindico`.

## Learnings
- Os testes de bootstrap existentes precisaram desligar a aplicação automática de migrations no startup, porque não sobem PostgreSQL real.
- A cobertura global da solução continua puxada para baixo por módulos fora do escopo, mas a cobertura agregada das superfícies tocadas nesta task ficou em 90%.

## Files / Surfaces
- `src/PortaBox.Infrastructure/DependencyInjection.cs`
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs`
- `src/PortaBox.Infrastructure/Persistence/Migrations/*AddIdentityBaseline*.cs`
- `src/PortaBox.Infrastructure/Identity/*`
- `src/PortaBox.Api/Extensions/AuthenticationExtensions.cs`
- `src/PortaBox.Api/Extensions/ServiceCollectionExtensions.cs`
- `src/PortaBox.Api/Program.cs`
- `src/PortaBox.Api/appsettings.json`
- `src/PortaBox.Api/appsettings.Development.json`
- `tests/PortaBox.Api.UnitTests/IdentitySeederTests.cs`
- `tests/PortaBox.Api.IntegrationTests/IdentityIntegrationTests.cs`
- `tests/PortaBox.Api.IntegrationTests/ApiBootstrapTests.cs`

## Errors / Corrections
- A primeira migration saiu com nomes físicos `AspNet*`; foi removida e regenerada após mapear explicitamente `asp_net_*` no `AppDbContext`.
- `IdentitySeeder` exigiu `IHostEnvironment` em vez de tipos web para manter `PortaBox.Infrastructure` desacoplado do ASP.NET web layer.

## Ready for Next Run
- Task pronta para task_10/task_16/task_18 consumirem `AppUser`, policies e o baseline de autenticação/roles.
