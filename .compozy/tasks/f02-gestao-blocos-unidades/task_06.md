---
status: completed
title: Bloco — Commands + Handlers + Validators (Create/Rename/Inativar/Reativar)
type: backend
complexity: medium
dependencies:
  - task_02
  - task_03
---

# Task 06: Bloco — Commands + Handlers + Validators (Create/Rename/Inativar/Reativar)

## Overview
Implementa os 4 Commands da camada de aplicação que orquestram o ciclo de vida de `Bloco`: criar, renomear, inativar, reativar. Cada handler valida input via FluentValidation, coordena `IBlocoRepository`, chama a entidade, registra auditoria via `IAuditService.RecordStructuralAsync` e deixa o commit para o EF Core (que aciona o `DomainEventOutboxInterceptor`).

> **Alinhamento com contrato:** os records C# `CreateBlocoRequest`, `RenameBlocoRequest` e `BlocoDto` produzem JSON que DEVE bater exatamente com os schemas `CreateBlocoRequest`, `RenameBlocoRequest` e `Bloco` de [`api-contract.yaml`](../api-contract.yaml) (campos, tipos, nullable, regra de `camelCase` aplicada pelo System.Text.Json). Os nomes das classes C# podem diferir dos nomes dos schemas OpenAPI (ex.: `BlocoDto` vs schema `Bloco`), mas o **shape serializado em JSON** é autoritativo pelo contrato.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar em `PortaBox.Modules.Gestao/Application/Blocos/`: `CreateBlocoCommand`, `RenameBlocoCommand`, `InativarBlocoCommand`, `ReativarBlocoCommand` — todos records implementando `ICommand<BlocoDto>`
- MUST criar `CreateBlocoRequest` e `RenameBlocoRequest` DTOs de entrada (usados pela API) e `BlocoDto` DTO de saída (id, condominioId, nome, ativo, inativadoEm)
- MUST criar um `FluentValidation` validator por command: nome 1–50 chars, trim non-whitespace, `condominioId`/`blocoId` não-vazio
- MUST criar handlers `Create/Rename/Inativar/ReativarBlocoCommandHandler` implementando `ICommandHandler<TCommand, BlocoDto>` — cada handler:
  - Valida input (FluentValidation + guardas de domínio)
  - Carrega o bloco via `IBlocoRepository.GetByIdAsync` (ou `GetByIdIncludingInactiveAsync` quando reativando)
  - Chama o método da entidade (`Bloco.Create`, `Rename`, `Inativar`, `Reativar`)
  - Chama `IAuditService.RecordStructuralAsync` com `StructuralAuditMetadata.For{Kind}(...)`
  - Chama `IBlocoRepository.SaveAsync` (commit)
  - Retorna `BlocoDto`
- MUST retornar `Result<BlocoDto>.Failure` com mensagem acionável em todos os caminhos de erro (bloco não encontrado, conflito de nome entre ativos, conflito canônico em reativação, transição inválida)
- MUST propagar `IClock` e `ICurrentUserContext` (ou equivalente) como dependências dos handlers
- SHOULD capturar `DbUpdateException` decorrente de violação de partial unique index e converter em `Result.Failure("Já existe bloco ativo com este nome")` como defesa em profundidade contra race condition
</requirements>

## Subtasks
- [x] 06.1 Criar commands + request/response DTOs + validators de Bloco
- [x] 06.2 Implementar `CreateBlocoCommandHandler` (check duplicado ativo → factory → add → audit → save)
- [x] 06.3 Implementar `RenameBlocoCommandHandler` (load → rename → audit → save; validar nome distinto)
- [x] 06.4 Implementar `InativarBlocoCommandHandler` e `ReativarBlocoCommandHandler` (load inclusive inativos no reativar → transição → audit → save)
- [x] 06.5 Tratar `DbUpdateException` em Create e Reativar para converter em 409 limpo
- [x] 06.6 Escrever unit tests cobrindo caminhos feliz e todos os erros

## Implementation Details
Ver TechSpec seções **Core Interfaces** (signature de Command/Handler) e **Data Flow** cenários C1 (criar) e C3 (renomear) e C5 (reativação com conflito canônico). Validators seguem padrão já estabelecido em F01 (ex.: `CreateCondominioCommandValidator`).

Autor da operação (`performedByUserId`) é obtido de `ICurrentUserContext.UserId` — confirmar nome exato do serviço existente de F01; se não existir, usar o que F01 task 18 introduziu em substituição.

Para `CreateBlocoCommandHandler`, antes de chamar `Bloco.Create`, verificar via `IBlocoRepository.ExistsActiveWithNameAsync`. A verificação é complementar ao partial unique index (que é a fonte de verdade); o check explícito melhora UX ao retornar mensagem clara em vez de erro de DB.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Application/Blocos/CreateBlocoCommand.cs` — novo
- `src/PortaBox.Modules.Gestao/Application/Blocos/RenameBlocoCommand.cs` — novo
- `src/PortaBox.Modules.Gestao/Application/Blocos/InativarBlocoCommand.cs` — novo
- `src/PortaBox.Modules.Gestao/Application/Blocos/ReativarBlocoCommand.cs` — novo
- `src/PortaBox.Modules.Gestao/Application/Blocos/Handlers/*.cs` — 4 handlers (novos)
- `src/PortaBox.Modules.Gestao/Application/Blocos/Validators/*.cs` — 4 validators (novos)
- `src/PortaBox.Modules.Gestao/Application/Blocos/BlocoDto.cs` — response DTO (novo)
- `tests/PortaBox.Modules.Gestao.UnitTests/Application/Blocos/*Tests.cs` — unit tests (novos)

### Dependent Files
- `EstruturaEndpoints.cs` (task_09) invocará esses handlers
- `DependencyInjection.cs` (task_09) registrará validators e handlers
- Integration tests (task_10) exercitarão os handlers end-to-end

### Related ADRs
- [ADR-001: Abordagem MVP Puro](adrs/adr-001.md) — CRUD manual; sem gerador em lote
- [ADR-003: Remoção Exclusivamente por Inativação; Edição Restrita](adrs/adr-003.md) — bloco renomeável
- [ADR-007: Soft-Delete Padronizado](adrs/adr-007.md) — reativação com partial unique index
- [ADR-008: Auditoria via EventKind + MetadataJson](adrs/adr-008.md) — handlers chamam `RecordStructuralAsync`

## Deliverables
- 4 Commands + 4 Handlers + 4 Validators + `BlocoDto` + `CreateBlocoRequest` + `RenameBlocoRequest`
- Conversão de `DbUpdateException` em `Result.Failure` quando aplicável
- Unit tests cobrindo todos caminhos felizes e erros
- Unit tests with 80%+ coverage **(REQUIRED)**
- Integration tests para endpoints — cobertos em task_10

## Tests
- Unit tests:
  - [ ] `CreateBlocoCommandHandler` caminho feliz: `IBlocoRepository.AddAsync` chamado, `IAuditService.RecordStructuralAsync` chamado com `EventKind.BlocoCriado`, `BlocoDto` retornado
  - [ ] `CreateBlocoCommandHandler` com nome duplicado entre ativos → `Result.Failure("Já existe bloco ativo com este nome")`; nenhum `AddAsync` chamado
  - [ ] `CreateBlocoCommandHandler` com `DbUpdateException` (simulado) por race condition → `Result.Failure` convertido; nenhum crash
  - [ ] `RenameBlocoCommandHandler` caminho feliz: `Rename` chamado, audit registra `BlocoRenomeado` com `nomeAntes` e `nomeDepois`
  - [ ] `RenameBlocoCommandHandler` em bloco inexistente → `Result.Failure("Bloco não encontrado")`
  - [ ] `RenameBlocoCommandHandler` em bloco inativo → `Result.Failure` (entidade rejeita; handler propaga)
  - [ ] `RenameBlocoCommandHandler` com mesmo nome atual → `Result.Failure`; sem audit nem save
  - [ ] `InativarBlocoCommandHandler` caminho feliz: entidade transita para inativo, audit `BlocoInativado`, save
  - [ ] `InativarBlocoCommandHandler` em bloco já inativo → `Result.Failure`
  - [ ] `ReativarBlocoCommandHandler` caminho feliz: load via `GetByIdIncludingInactiveAsync`, entidade reativa, audit `BlocoReativado`, save
  - [ ] `ReativarBlocoCommandHandler` quando já existe outro bloco ativo com mesmo nome → `Result.Failure("conflito canônico; inative o outro antes")`
  - [ ] Validators: nomes vazios/whitespace/> 50 chars são rejeitados
- Integration tests:
  - [ ] Cobertos em task_10
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Handlers consumíveis por endpoints de task_09
- Todas as transições emitem exatamente 1 evento de domínio e exatamente 1 `TenantAuditEntry`
- Race conditions em unicidade canônica são absorvidas pelo handler como 409 limpo (não crash)
