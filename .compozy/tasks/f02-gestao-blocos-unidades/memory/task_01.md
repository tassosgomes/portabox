# Task Memory: task_01.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Implementar a infra reutilizavel de soft-delete para F02/F03/F06: marker interface, base class com guard clauses e filtro global combinado tenant+ativo no `AppDbContext`.
- Cobrir a base class com unit tests e provar o filtro com testes de modelo e integracao em Postgres/Testcontainers.

## Important Decisions

- Foi criado `PortaBox.Domain.Result` nao generico porque o dominio nao referencia `PortaBox.Application.Abstractions`; isso evita acoplamento invertido so para as guard clauses de `Inativar`/`Reativar`.
- O `AppDbContext` agora gera um unico `HasQueryFilter` por entidade usando `Expression.AndAlso` para compor `ITenantEntity` e `ISoftDeletable` sem override do ultimo filtro.
- A entidade fake de teste ficou em contextos derivados dentro dos projetos de teste; assim a validacao do filtro nao adiciona tabela nem mapping ao codigo de producao.

## Learnings

- Para o reflection do `AppDbContext` enxergar uma entidade fake em testes, a entidade precisa ser registrada no `ModelBuilder` antes do `base.OnModelCreating(modelBuilder)` no contexto derivado.
- O fixture de integracao existente falha imediatamente sem Docker acessivel, antes mesmo da execucao dos asserts dos testes de soft-delete.

## Files / Surfaces

- `src/PortaBox.Domain/Abstractions/ISoftDeletable.cs`
- `src/PortaBox.Domain/Result.cs`
- `src/PortaBox.Domain/SoftDeleteableAggregateRoot.cs`
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/Domain/SoftDeleteableAggregateRootTests.cs`
- `tests/PortaBox.Api.UnitTests/AppDbContextSoftDeleteQueryFilterTests.cs`
- `tests/PortaBox.Api.IntegrationTests/Persistence/SoftDeleteFilterTests.cs`

## Errors / Corrections

- O primeiro desenho dos testes registrava a entidade fake depois do `base.OnModelCreating`; isso impediria a aplicacao automatica do filtro global. Corrigido registrando a entidade antes da chamada base.
- A verificacao de integracao ficou bloqueada por ambiente: `Testcontainers` nao conseguiu acessar `unix:///var/run/docker.sock`, entao a task nao deve ser marcada como concluida ainda.

## Ready for Next Run

- Proximo passo quando Docker estiver disponivel: rerodar `dotnet test tests/PortaBox.Api.IntegrationTests/PortaBox.Api.IntegrationTests.csproj --filter SoftDeleteFilterTests` e, se verde, revisar tracking files (`task_01.md` e `_tasks.md`) antes de marcar a task como completa.
