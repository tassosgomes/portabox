---
status: completed
title: ASP.NET Identity + roles seed (Operator, Sindico) + operador dev
type: backend
complexity: medium
dependencies:
  - task_03
---

# Task 04: ASP.NET Identity + roles seed (Operator, Sindico) + operador dev

## Overview
Adiciona ASP.NET Core Identity ao `AppDbContext` com as tabelas padrão (`AspNetUsers`, `AspNetRoles`, etc.), configura autenticação por cookie e cria migration + seed para as roles `Operator` e `Sindico`. Também cria um usuário `Operator` seed para o ambiente de desenvolvimento, viabilizando login no backoffice desde o primeiro dia.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST integrar Identity ao `AppDbContext` (via `IdentityDbContext<TUser, TRole, TKey>` ou composição equivalente com `AddIdentityCore` + `AddEntityFrameworkStores`)
- MUST usar `Guid` como chave primária do usuário
- MUST configurar autenticação por cookie com `SameSite=Strict`, `Secure=true` em produção (em Development pode ser `Lax`/`false`), `HttpOnly=true`
- MUST criar migração Identity e aplicá-la
- MUST seed das roles `Operator` e `Sindico` (idempotente)
- MUST seed de um usuário `operator@portabox.dev` com role `Operator` apenas em `Development`, com senha configurável via `appsettings.Development.json` ou variável de ambiente
- MUST definir política de senha mínima (configurável, não hardcoded) — será validada em task_16 e task_18 contra PRD/Open Questions
- SHOULD expor `RequireOperator` e `RequireSindico` como policies nomeadas registradas em DI (uso real em task_18)
</requirements>

## Subtasks
- [x] 04.1 Adicionar Identity ao `AppDbContext` com Guid PK
- [x] 04.2 Configurar cookie authentication com política segura por ambiente
- [x] 04.3 Gerar migração Identity + aplicar
- [x] 04.4 Implementar `IdentitySeeder` idempotente que cria roles `Operator` e `Sindico`
- [x] 04.5 Implementar seed do usuário operador dev ativado somente em Development
- [x] 04.6 Registrar policies `RequireOperator` e `RequireSindico` (contrato usado adiante)

## Implementation Details
Seguir ADR-005 para a topologia de roles e ADR-010 para referência ao produto. Tabelas `AspNet*` usam snake_case conforme convenção de task_03.

### Relevant Files
- `src/PortaBox.Infrastructure/Identity/AppUser.cs` — entidade de usuário (Guid PK) (a criar)
- `src/PortaBox.Infrastructure/Identity/AppRole.cs` — entidade de role (a criar)
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs` — editar para herdar de `IdentityDbContext<AppUser, AppRole, Guid>` (existente de task_03)
- `src/PortaBox.Infrastructure/Persistence/Migrations/*_Identity.cs` — migração gerada (a criar)
- `src/PortaBox.Infrastructure/Identity/IdentitySeeder.cs` — seed idempotente (a criar)
- `src/PortaBox.Api/Extensions/AuthenticationExtensions.cs` — registro de cookie auth + policies (a criar)
- `src/PortaBox.Api/Program.cs` — chamar `AddIdentityBaseline` e `UseAuthentication/UseAuthorization` (editar)

### Dependent Files
- `task_10` (IMagicLinkService) referencia `AppUser` para `user_id`
- `task_16` (PasswordSetupCommandHandler) usa `UserManager<AppUser>.AddPasswordAsync`
- `task_18` (controllers) aplica `[Authorize(Roles = "Operator")]` / `[Authorize(Roles = "Sindico")]`
- `task_21` (Backoffice auth) consome endpoint de login

### Related ADRs
- [ADR-005: Backoffice como SPA React Separado; Autenticação via ASP.NET Core Identity](../adrs/adr-005.md) — define modelo de roles e cookie auth.

## Deliverables
- `AppDbContext` integrado a Identity com Guid PK
- Cookie authentication configurado por ambiente
- Migração Identity aplicada
- `IdentitySeeder` idempotente criando `Operator` e `Sindico`
- Usuário operador dev ativo apenas em Development
- Policies `RequireOperator` e `RequireSindico` registradas
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para Identity + seed **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `IdentitySeeder.RunAsync` cria role `Operator` quando não existe
  - [x] `IdentitySeeder.RunAsync` é idempotente (chamar duas vezes não duplica registros)
  - [x] Em `Environment.Development`, `IdentitySeeder` cria usuário operador com senha lida da configuração
  - [x] Em `Environment.Production`, `IdentitySeeder` não cria usuário operador dev
- Integration tests:
  - [x] Migração Identity aplica sem erro em container fresco (via `PostgresDatabaseFixture`)
  - [x] Após seed, `RoleManager` encontra `Operator` e `Sindico`
  - [x] Após seed em Development, `UserManager.FindByEmailAsync("operator@portabox.dev")` retorna o usuário com role `Operator` atribuída
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Identity integrado ao `AppDbContext` com tabelas `AspNet*` em snake_case
- Seeds idempotentes para roles e usuário dev
- Cookie auth com políticas adequadas por ambiente
