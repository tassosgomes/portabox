# Task Memory: task_04.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Implementar o agregado `Unidade` com eventos, configuracao EF, contrato/implementacao de repositorio, registro no `AppDbContext` e testes exigidos pela task.

## Important Decisions

- `Unidade.Create` usa `TimeProvider` no lugar de `IClock`, seguindo o baseline real do backend e a decisao registrada na workflow memory.
- `UnidadeConfiguration`, `UnidadeRepository` e o registro de `IUnidadeRepository` ficaram na camada `PortaBox.Infrastructure`, porque esse e o layout ja adotado pelo repositorio para EF, DI e persistencia.

## Learnings

- A cobertura do `UnidadeRepository` so passou de 80% depois de testar explicitamente os dois ramos de `ListByBlocoAsync` e a normalizacao em `ExistsActiveWithCanonicalAsync`.
- Executar dois `dotnet test` simultaneos contra o mesmo projeto pode abortar o test host; a rerodada sequencial confirmou que o crash era de execucao concorrente, nao de codigo.

## Files / Surfaces

- `src/PortaBox.Modules.Gestao/Domain/Unidades/Unidade.cs`
- `src/PortaBox.Modules.Gestao/Domain/Unidades/Events/UnidadeCriadaV1.cs`
- `src/PortaBox.Modules.Gestao/Domain/Unidades/Events/UnidadeInativadaV1.cs`
- `src/PortaBox.Modules.Gestao/Domain/Unidades/Events/UnidadeReativadaV1.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/IUnidadeRepository.cs`
- `src/PortaBox.Infrastructure/Persistence/UnidadeConfiguration.cs`
- `src/PortaBox.Infrastructure/Repositories/UnidadeRepository.cs`
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs`
- `src/PortaBox.Infrastructure/DependencyInjection.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/Domain/Unidades/UnidadeTests.cs`

## Errors / Corrections

- O primeiro rerun focado de `UnidadeTests` abortou porque outra execucao de `dotnet test` para o mesmo projeto estava rodando em paralelo; o rerun isolado passou sem alteracoes.

## Ready for Next Run

- Task implementada e validada com `dotnet build PortaBox.sln`, `dotnet test tests/PortaBox.Modules.Gestao.UnitTests/PortaBox.Modules.Gestao.UnitTests.csproj --filter FullyQualifiedName~UnidadeTests` e a suite completa de `PortaBox.Modules.Gestao.UnitTests` com cobertura.
- Cobertura verificada no `coverage.cobertura.xml` mais recente: `Unidade.cs` 95.4%, `UnidadeRepository.cs` 80.43% e `UnidadeConfiguration.cs` 100%.
