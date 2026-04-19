---
status: completed
title: Scaffold solution .NET + monorepo frontend + docker-compose dev
type: infra
complexity: medium
dependencies: []
---

# Task 01: Scaffold solution .NET + monorepo frontend + docker-compose dev

## Overview
Estabelece a estrutura física do projeto greenfield: solution .NET com projetos seguindo Clean Architecture do skill `csharp-dotnet-architecture`, monorepo frontend com `apps/` e `packages/` para as duas SPAs React + biblioteca compartilhada, e um `docker-compose.dev.yml` com Postgres 16, MinIO e MailHog para desenvolvimento local. Sem isso, nenhuma outra task pode começar.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar `PortaBox.sln` com projetos `PortaBox.Api`, `PortaBox.Application.Abstractions`, `PortaBox.Domain`, `PortaBox.Infrastructure`, `PortaBox.Modules.Gestao` e projetos de teste `PortaBox.Modules.Gestao.UnitTests`, `PortaBox.Api.IntegrationTests`
- MUST configurar referencias entre projetos respeitando Clean Architecture (Api → Modules/Infra/Application; Modules → Application/Domain; Infra → Application/Domain; Domain sem dependências externas)
- MUST criar monorepo frontend com `apps/backoffice`, `apps/sindico`, `packages/ui` e workspace manager (pnpm ou npm workspaces)
- MUST criar `docker-compose.dev.yml` com serviços: `postgres:16` (porta 5432), `minio` (portas 9000/9001 + admin/admin), `mailhog` (portas 1025/8025), com volumes persistentes nomeados
- MUST incluir `.editorconfig`, `.gitignore`, `README.md` raiz descrevendo como subir o ambiente dev
- SHOULD incluir um smoke test que verifique que `dotnet build` passa e `docker compose -f docker-compose.dev.yml config` valida
</requirements>

## Subtasks
- [ ] 01.1 Criar solution .NET e projetos com Clean Architecture em camadas
- [ ] 01.2 Configurar references entre projetos `.csproj` respeitando a direção permitida
- [ ] 01.3 Criar estrutura do monorepo frontend (`apps/`, `packages/`, workspace config)
- [ ] 01.4 Criar `docker-compose.dev.yml` com Postgres + MinIO + MailHog
- [ ] 01.5 Criar documentação inicial (`README.md` raiz) com passos de setup
- [ ] 01.6 Adicionar smoke script (`scripts/smoke.sh` ou equivalente) que valida build .NET e docker-compose

## Implementation Details
Seguir estrutura da seção **Component Overview** do TechSpec. Os nomes dos projetos respeitam o rename `PortaBox.*` aplicado no TechSpec. Configurar snake_case convention no EF Core será feito em task_03 (este task só prepara a árvore de projetos).

### Relevant Files
- `PortaBox.sln` — solution .NET raiz (a criar)
- `src/PortaBox.Api/PortaBox.Api.csproj` — host ASP.NET Core (a criar)
- `src/PortaBox.Application.Abstractions/PortaBox.Application.Abstractions.csproj` — contratos (a criar)
- `src/PortaBox.Domain/PortaBox.Domain.csproj` — entidades e eventos de domínio (a criar)
- `src/PortaBox.Infrastructure/PortaBox.Infrastructure.csproj` — persistência, adapters, workers (a criar)
- `src/PortaBox.Modules.Gestao/PortaBox.Modules.Gestao.csproj` — módulo D01 (a criar)
- `tests/PortaBox.Modules.Gestao.UnitTests/*.csproj` — testes unitários (a criar)
- `tests/PortaBox.Api.IntegrationTests/*.csproj` — testes de integração (a criar)
- `apps/backoffice/package.json` — SPA do operador (a criar)
- `apps/sindico/package.json` — SPA do síndico (a criar)
- `packages/ui/package.json` — componentes compartilhados (a criar)
- `pnpm-workspace.yaml` ou `package.json` — workspace config (a criar)
- `docker-compose.dev.yml` — ambiente local (a criar)
- `README.md` — instruções de setup (a criar)

### Dependent Files
- Todas as tasks subsequentes dependem desta estrutura para colocar seus artefatos no lugar correto.

### Related ADRs
- [ADR-005: Backoffice como SPA React Separado](../adrs/adr-005.md) — motiva a separação `apps/backoffice` vs `apps/sindico`.

## Deliverables
- Solution .NET buildável (`dotnet build` retorna 0)
- Monorepo frontend instalável (`pnpm install` ou `npm install` retorna 0)
- `docker-compose.dev.yml` validável (`docker compose config` retorna 0)
- README raiz com passos reproduzíveis de setup local
- Smoke test que executa em CI e valida que build + compose passam
- Unit tests com 80%+ coverage **(REQUIRED)** — neste task, apenas o smoke test conta como cobertura (não há código de produção ainda)
- Integration tests para scaffold **(REQUIRED)** — smoke test que roda `dotnet build` e `docker compose config`

## Tests
- Unit tests:
  - [ ] `dotnet build PortaBox.sln` retorna exit code 0 no CI
  - [ ] Instalação de dependências do monorepo frontend retorna exit code 0
- Integration tests:
  - [ ] `docker compose -f docker-compose.dev.yml config` valida sem erros
  - [ ] Smoke script levanta containers e consegue conectar em `localhost:5432` (Postgres) e `localhost:9000` (MinIO) e `localhost:1025` (MailHog)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Solution buildável com todos os projetos em Clean Architecture
- Monorepo com 3 pacotes (`backoffice`, `sindico`, `ui`)
- Docker compose dev operacional (Postgres, MinIO, MailHog acessíveis em localhost)
- README raiz permite a qualquer novo dev subir o ambiente local em < 10 minutos
