# Task Memory: task_07.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Implementar commands, handlers, DTOs e validators de `Unidade` para criar, inativar e reativar, seguindo o padrao de `Bloco` e usando auditoria estrutural + `TimeProvider`.

## Important Decisions

- Os commands seguem o baseline real do modulo e carregam `PerformedByUserId` explicitamente, sem introduzir abstractions novas fora de escopo.
- `CreateUnidadeCommandHandler` retorna falha de dominio curta para bloco nao encontrado, bloco inativo, conflito canonico e race de unique index; a normalizacao do numero ocorre antes do check canonico e antes da criacao.
- `InativarUnidadeCommandHandler` e `ReativarUnidadeCommandHandler` validam pelo par `unidadeId` + `blocoId`, mantendo o padrao simples ja usado pelos handlers de `Bloco`.

## Learnings

- O workspace real nao possui `AGENTS.md` nem `CLAUDE.md`; a execucao foi guiada pelos docs da task, techspec, ADRs e pelo codigo existente.
- A cobertura agregada do projeto `PortaBox.Modules.Gestao.UnitTests` segue baixa por codigo legado fora do escopo, mas os handlers/validators novos de `Application/Unidades` ficaram exercitados pela nova suite desta task.

## Files / Surfaces

- `src/PortaBox.Modules.Gestao/Application/Unidades/CreateUnidadeCommand.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/InativarUnidadeCommand.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/ReativarUnidadeCommand.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/CreateUnidadeRequest.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/UnidadeDto.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/CreateUnidadeCommandValidator.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/InativarUnidadeCommandValidator.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/ReativarUnidadeCommandValidator.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/CreateUnidadeCommandHandler.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/InativarUnidadeCommandHandler.cs`
- `src/PortaBox.Modules.Gestao/Application/Unidades/ReativarUnidadeCommandHandler.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/CreateUnidadeCommandHandlerTests.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/InativarReativarUnidadeCommandHandlerTests.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/CreateUnidadeCommandValidatorTests.cs`

## Errors / Corrections

- Nenhum erro de compilacao apos a implementacao; `dotnet test` passou na primeira execucao.
- O requisito de cobertura `>=80%` nao e atingido pelo agregado inteiro do projeto de unit tests (`coverage.cobertura.xml` reportou `line-rate="0.18"`), entao isso deve ser tratado como gap preexistente fora do escopo desta task se o criterio for global e nao por superficie alterada.

## Ready for Next Run

- Task 09 ainda precisa registrar esses handlers/validators em DI e mapear os endpoints HTTP correspondentes.
