# Task Memory: task_03.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Implementar o baseline de persistência do F01 com `AppDbContext`, convenção `snake_case`, registro em DI com `AddDbContextPool`, migração inicial vazia e fixture reutilizável de PostgreSQL via Testcontainers para testes de integração.
- Manter escopo restrito à infraestrutura base; entidades reais, Identity tables e filtros multi-tenant ficam para tasks dependentes.

## Important Decisions
- Compatibilizar a extensão existente `AddPortaBoxInfrastructure` com a nomenclatura esperada pela task expondo também `AddInfrastructure`, evitando quebrar o `Program.cs` atual.
- Aplicar `snake_case` via `UseSnakeCaseNamingConvention()` no options builder do EF para manter a convenção disponível também em contexts derivados usados nos testes.
- Montar `NpgsqlDataSource` explicitamente com `EnableDynamicJson()` e registrá-lo como singleton para reutilização por `AddDbContextPool`.
- Recriar o `Respawner` a cada reset da fixture porque tabelas criadas após a inicialização não entram no catálogo capturado por uma instância única.

## Learnings
- O workspace não contém `AGENTS.md` nem `CLAUDE.md`; a orientação operacional veio do prompt, skills e documentos da PRD.
- O repositório não está inicializado como git no diretório atual, então rastreamento local será feito pelos arquivos de task/memória e pela verificação direta dos comandos.
- A suíte atual não possui nenhum baseline de EF Core/Testcontainers; os testes de integração existentes exercitam apenas o bootstrap HTTP.
- Como a migration inicial é vazia, o Respawn falha se não houver nenhuma tabela de usuário; a fixture cria `public.integration_respawn_seed` para manter o reset funcional até as próximas tasks adicionarem tabelas reais.
- O `dotnet ef` exigiu `Microsoft.EntityFrameworkCore.Design` também no startup project (`PortaBox.Api`), não só em `PortaBox.Infrastructure`.

## Files / Surfaces
- `src/PortaBox.Infrastructure/*`
- `src/PortaBox.Api/Program.cs`
- `src/PortaBox.Api/appsettings*.json`
- `tests/PortaBox.Api.UnitTests/*`
- `tests/PortaBox.Api.IntegrationTests/*`
- `.compozy/tasks/f01-criacao-condominio/task_03.md`
- `.compozy/tasks/f01-criacao-condominio/_tasks.md`

## Errors / Corrections
- `rg` não está instalado no ambiente; buscas locais estão sendo feitas com `find`/`grep`.
- Overrides via `ConfigureAppConfiguration(...AddInMemoryCollection(...))` não sobreviveram ao bootstrap da API por causa do `builder.Configuration.Sources.Clear()`; os testes foram corrigidos para usar environment variables.
- Um `Respawner` singleton não limpava tabelas criadas depois da inicialização; o reset passou a recriar o respawner dinamicamente.
- Houve conflito de versões entre EF Core 8.0.16 e `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4; o baseline foi alinhado para EF Core 8.0.11 + provider Npgsql EF 8.0.11 + `Npgsql` 8.0.6.

## Ready for Next Run
- `AppDbContext`, DI, migration inicial, fixture PostgreSQL 16, reset helper e README de uso estão implementados e verificados.
- Cobertura focada em `PortaBox.Infrastructure` ficou em 100% de linhas pelos unit tests desta task.
