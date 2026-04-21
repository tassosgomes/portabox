# Task Memory: task_03.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Implementar o agregado `Bloco` com eventos, configuracao EF, contrato/implementacao de repositorio, registro no `AppDbContext` e testes unitarios exigidos pela task.

## Important Decisions

- `Bloco.Create` foi implementado com `TimeProvider` em vez de `IClock`, porque o repositorio nao possui essa abstracao e todo o backend atual usa `TimeProvider`.
- `BlocoConfiguration` foi criada em `src/PortaBox.Infrastructure/Persistence/BlocoConfiguration.cs`, seguindo o padrao real do projeto para EF configurations, mesmo com o task spec citando `PortaBox.Modules.Gestao/Infrastructure/EfConfigurations/`.
- `ListByCondominioAsync(includeInactive: true)` foi implementado sem `IgnoreQueryFilters()` via SQL direto na conexao do `DbContext` + `Bloco.Rehydrate(...)`, para respeitar a exigencia de manter `.IgnoreQueryFilters()` exclusivo em `GetByIdIncludingInactiveAsync`.

## Learnings

- O filtro global de soft-delete aplicado no `AppDbContext` tambem afeta consultas `FromSql*`; para listar inativos sem `.IgnoreQueryFilters()`, foi necessario sair do pipeline LINQ/EF materializado.
- Os testes de repositorio com FK real precisaram semear `AppUser` e `Condominio` antes de persistir `Bloco` quando usando SQLite em memoria.

## Files / Surfaces

- `src/PortaBox.Modules.Gestao/Domain/Blocos/Bloco.cs`
- `src/PortaBox.Modules.Gestao/Domain/Blocos/Events/BlocoCriadoV1.cs`
- `src/PortaBox.Modules.Gestao/Domain/Blocos/Events/BlocoRenomeadoV1.cs`
- `src/PortaBox.Modules.Gestao/Domain/Blocos/Events/BlocoInativadoV1.cs`
- `src/PortaBox.Modules.Gestao/Domain/Blocos/Events/BlocoReativadoV1.cs`
- `src/PortaBox.Modules.Gestao/Application/Blocos/IBlocoRepository.cs`
- `src/PortaBox.Modules.Gestao/AssemblyInfo.cs`
- `src/PortaBox.Infrastructure/Persistence/BlocoConfiguration.cs`
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs`
- `src/PortaBox.Infrastructure/Repositories/BlocoRepository.cs`
- `src/PortaBox.Infrastructure/DependencyInjection.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/Domain/Blocos/BlocoTests.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/PortaBox.Modules.Gestao.UnitTests.csproj`

## Errors / Corrections

- Ajustado `ListByCondominioAsync` apos descobrir que a primeira versao ainda dependia de `IgnoreQueryFilters()` fora do metodo permitido pela task.
- Ajustado o parse de `criado_em` na materializacao manual do repositiorio para tolerar formatos retornados pelo SQLite nos testes.
- A verificacao completa com `dotnet test` continua bloqueada por indisponibilidade de Docker/Testcontainers neste host; por isso a task nao foi marcada como concluida nos arquivos de tracking.

## Ready for Next Run

- Implementacao, build e suite focada da task estao verdes.
- Se Docker estiver disponivel, rerodar `dotnet test` na raiz e entao atualizar `task_03.md` e `_tasks.md` para concluir oficialmente a task.
