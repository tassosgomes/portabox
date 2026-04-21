# Task Memory: task_06.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Implementar commands, DTOs, validators e handlers de `Bloco` para criar, renomear, inativar e reativar, com auditoria estrutural e testes unitarios cobrindo sucesso e falhas.

## Important Decisions

- O repositorio nao contem `AGENTS.md` nem `CLAUDE.md`; a execucao segue os documentos de task, techspec, ADRs e o baseline real do codigo.
- O baseline real usa `TimeProvider`, nao `IClock`; os handlers desta task seguirao esse padrao.
- Nao existe servico de usuario atual injetavel na camada de aplicacao; o `performedByUserId` sera propagado explicitamente pelos commands, alinhado ao padrao dos endpoints e handlers existentes.

## Learnings

- `IBlocoRepository` expõe apenas `ExistsActiveWithNameAsync(condominioId, nome)`, sem exclusao por `blocoId`; o handler de rename precisa tratar o caso de "mesmo nome atual" antes da checagem de duplicidade.
- A camada `PortaBox.Modules.Gestao` precisou referenciar `Microsoft.EntityFrameworkCore` e `Npgsql` para capturar `DbUpdateException` por violacao de unicidade como defesa em profundidade nos handlers de create/reativar.

## Files / Surfaces

- `src/PortaBox.Modules.Gestao/Application/Blocos/*`
- `tests/PortaBox.Modules.Gestao.UnitTests/*Bloco*`
- `src/PortaBox.Modules.Gestao/PortaBox.Modules.Gestao.csproj`

## Errors / Corrections

- A primeira compilacao falhou porque `PortaBox.Modules.Gestao` nao referenciava EF Core/Npgsql; a correcao minima foi adicionar essas dependencias ao `.csproj` do modulo.
- O filtro do `catch (DbUpdateException)` precisou ficar null-safe e mais defensivo para reconhecer a excecao simulada nos testes de race condition com `PostgresException`.

## Ready for Next Run

- Commands, DTOs, validators e handlers de `Bloco` foram implementados e cobertos por testes unitarios; proximo passo natural e task_09 registrar handlers/validators na DI e expor endpoints usando `performedByUserId` extraido do `HttpContext`.
