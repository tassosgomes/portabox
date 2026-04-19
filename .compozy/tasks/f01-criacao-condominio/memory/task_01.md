# Task Memory: task_01.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Scaffold inicial criado para `PortaBox.sln`, monorepo frontend `pnpm`, `docker-compose.dev.yml`, documentação raiz e smoke script.

## Important Decisions

- O workspace frontend foi estruturado com `pnpm` por já estar disponível no ambiente e atender o requisito de monorepo sem antecipar a baseline Vite da `task_20`.
- Os projetos .NET foram alinhados para `net8.0`, em linha com o TechSpec que fixa ASP.NET Core 8 como baseline.
- O compose usa credenciais MinIO `admin/adminadmin` porque versões atuais do MinIO rejeitam a senha curta `admin` pedida no task spec; a divergência foi documentada no `README.md`.

## Learnings

- O SDK instalado (`10.0.106`) gera `.slnx` por padrão; foi necessário forçar `dotnet new sln -f sln` para cumprir o requisito de `PortaBox.sln`.
- `dotnet test --no-build` apresentou comportamento inválido do VSTest neste ambiente; `dotnet test PortaBox.sln` executou corretamente e foi usado como evidência válida.

## Files / Surfaces

- `PortaBox.sln`
- `src/PortaBox.Api`
- `src/PortaBox.Application.Abstractions`
- `src/PortaBox.Domain`
- `src/PortaBox.Infrastructure`
- `src/PortaBox.Modules.Gestao`
- `tests/PortaBox.Modules.Gestao.UnitTests`
- `tests/PortaBox.Api.IntegrationTests`
- `apps/backoffice`
- `apps/sindico`
- `packages/ui`
- `package.json`
- `pnpm-workspace.yaml`
- `docker-compose.dev.yml`
- `scripts/smoke.sh`
- `.editorconfig`
- `.gitignore`
- `README.md`

## Errors / Corrections

- O primeiro `dotnet build` falhou porque `PortaBox.Infrastructure` e `PortaBox.Modules.Gestao` não referenciavam as abstrações `Microsoft.Extensions.*`; foram adicionadas dependências mínimas de DI/configuração.
- O primeiro teste unitário tentou inferir referências pelo assembly compilado e falhou; foi corrigido para validar o `.csproj`, que é o contrato arquitetural real desta task.
- O smoke test ficou bloqueado por conflito externo de porta: o container `authz-postgres` já ocupa `0.0.0.0:5432`, impedindo subir o `postgres` do compose desta task.

## Ready for Next Run

- Se a porta `5432` for liberada ou o usuário autorizar parar o container conflitante, rerodar `./scripts/smoke.sh` e, se passar, atualizar `task_01.md` e `_tasks.md` para `completed`.
