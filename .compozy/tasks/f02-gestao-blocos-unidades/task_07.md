---
status: completed
title: Unidade — Commands + Handlers + Validators (Create/Inativar/Reativar)
type: backend
complexity: medium
dependencies:
  - task_02
  - task_03
  - task_04
---

# Task 07: Unidade — Commands + Handlers + Validators (Create/Inativar/Reativar)

## Overview
Implementa os 3 Commands de ciclo de vida de `Unidade`: criar (com validação de bloco ativo e unicidade canônica), inativar, reativar. Seguem o mesmo padrão dos handlers de Bloco (task_06), mas com validações canônicas adicionais (regex do número, andar ≥ 0, bloco pertence ao mesmo tenant).

> **Alinhamento com contrato:** `CreateUnidadeRequest` e `UnidadeDto` serializam para os schemas `CreateUnidadeRequest` e `Unidade` de [`api-contract.yaml`](../api-contract.yaml). Regex do `numero` e `andar ≥ 0` estão formalizados no contrato — backend valida, mas o contrato é a fonte de verdade para consumidores (frontend, testes de contrato).

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar `CreateUnidadeCommand`, `InativarUnidadeCommand`, `ReativarUnidadeCommand` em `Application/Unidades/` — records `ICommand<UnidadeDto>`
- MUST criar `CreateUnidadeRequest` DTO de entrada e `UnidadeDto` DTO de saída (id, blocoId, andar, numero, ativo, inativadoEm)
- MUST criar validators FluentValidation: `andar >= 0`, `numero` não-vazio após trim, regex `^[0-9]{1,4}[A-Za-z]?$` (aceita caixa mista no input; handler normaliza)
- MUST implementar handlers:
  - `CreateUnidadeCommandHandler`: carrega `Bloco` via `IBlocoRepository.GetByIdAsync` (aplica filter tenant + soft-delete); se `null` → 404; checa `IUnidadeRepository.ExistsActiveWithCanonicalAsync` → 409 se conflito; chama `Unidade.Create(..., bloco, ...)`; audit `UnidadeCriada`; save
  - `InativarUnidadeCommandHandler`: load → `Unidade.Inativar` → audit → save
  - `ReativarUnidadeCommandHandler`: load inclusive inativos via `GetByIdIncludingInactiveAsync` → checa conflito canônico via `ExistsActiveWithCanonicalAsync` → `Unidade.Reativar` → audit → save
- MUST capturar `DbUpdateException` em Create e Reativar (race condition em partial unique canônico) e converter em `Result.Failure` limpo
- MUST retornar 404 quando bloco ou unidade não encontrados (caller da API mapeia para HTTP 404)
- SHOULD garantir que handler de reativação usa transação implícita do EF (uma SaveChanges) — check + mutação no mesmo contexto
</requirements>

## Subtasks
- [x] 07.1 Criar commands + request/response DTOs + validators de Unidade
- [x] 07.2 Implementar `CreateUnidadeCommandHandler` incluindo load de bloco + check de unicidade canônica
- [x] 07.3 Implementar `InativarUnidadeCommandHandler`
- [x] 07.4 Implementar `ReativarUnidadeCommandHandler` com check de conflito canônico
- [x] 07.5 Tratar `DbUpdateException` em Create e Reativar
- [x] 07.6 Escrever unit tests para caminhos felizes e erros

## Implementation Details
Ver TechSpec **Data Flow** cenário C2 (criar unidade) e C5 (reativar com conflito). O `CreateUnidadeCommandHandler` consome duas abstrações (`IBlocoRepository` e `IUnidadeRepository`) — é um exemplo de handler que orquestra dois agregados da mesma bounded context.

Normalização do `numero`: o validator aceita input em caixa mista (`101a`), mas o handler chama `Unidade.Create(..., numero.Trim().ToUpperInvariant(), ...)`. A entidade também normaliza internamente como defesa adicional.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Application/Unidades/CreateUnidadeCommand.cs` — novo
- `src/PortaBox.Modules.Gestao/Application/Unidades/InativarUnidadeCommand.cs` — novo
- `src/PortaBox.Modules.Gestao/Application/Unidades/ReativarUnidadeCommand.cs` — novo
- `src/PortaBox.Modules.Gestao/Application/Unidades/Handlers/*.cs` — 3 handlers (novos)
- `src/PortaBox.Modules.Gestao/Application/Unidades/Validators/*.cs` — 3 validators (novos)
- `src/PortaBox.Modules.Gestao/Application/Unidades/UnidadeDto.cs` — response DTO (novo)
- `tests/PortaBox.Modules.Gestao.UnitTests/Application/Unidades/*Tests.cs` — unit tests (novos)

### Dependent Files
- `EstruturaEndpoints.cs` (task_09) mapeará os 3 endpoints
- DI (task_09) registrará handlers e validators
- Integration tests (task_10) exercitarão os endpoints

### Related ADRs
- [ADR-002: Forma Canônica Estrita](adrs/adr-002.md) — validação de andar/número
- [ADR-003: Remoção Exclusivamente por Inativação; Edição Restrita](adrs/adr-003.md) — unidade imutável
- [ADR-007: Soft-Delete Padronizado](adrs/adr-007.md) — reativação com check de conflito canônico
- [ADR-008: Auditoria via EventKind + MetadataJson](adrs/adr-008.md)

## Deliverables
- 3 Commands + 3 Handlers + 3 Validators + `UnidadeDto` + `CreateUnidadeRequest`
- `DbUpdateException` convertido em `Result.Failure` em Create e Reativar
- Unit tests cobrindo todos os fluxos
- Unit tests with 80%+ coverage **(REQUIRED)**
- Integration tests para endpoints — cobertos em task_10

## Tests
- Unit tests:
  - [x] `CreateUnidadeCommandHandler` caminho feliz: carrega bloco ativo, check canônico retorna falso, cria entidade, audit `UnidadeCriada`, save
  - [x] `CreateUnidadeCommandHandler` com bloco inexistente → `Result.Failure("Bloco não encontrado")` (404)
  - [x] `CreateUnidadeCommandHandler` com bloco inativo → `Result.Failure("Bloco inativo")` (422)
  - [x] `CreateUnidadeCommandHandler` com tripla duplicada entre ativas → `Result.Failure("Unidade já existe")` (409); nenhum side effect
  - [x] `CreateUnidadeCommandHandler` absorve `DbUpdateException` (race) e retorna 409 limpo
  - [x] `InativarUnidadeCommandHandler` caminho feliz → audit `UnidadeInativada`, save
  - [x] `InativarUnidadeCommandHandler` em unidade já inativa → `Result.Failure`
  - [x] `ReativarUnidadeCommandHandler` caminho feliz com carga via `GetByIdIncludingInactive` → audit `UnidadeReativada`
  - [x] `ReativarUnidadeCommandHandler` com conflito canônico → `Result.Failure("conflito canônico; inative a duplicada")`
  - [x] Validators: `numero="1AB"`, `" "`, `"12345"`, `andar=-1` → rejeitados
- Integration tests:
  - [ ] Cobertos em task_10
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Handlers prontos para mapeamento em endpoints (task_09)
- Todas as transições emitem 1 evento de domínio + 1 audit entry por operação
- Conflitos canônicos e transições inválidas sempre retornam `Result.Failure` (nunca crash)
