---
status: completed
title: PasswordSetupCommandHandler
type: backend
complexity: medium
dependencies:
    - task_04
    - task_10
---

# Task 16: PasswordSetupCommandHandler

## Overview
Consome o magic link e define a senha do síndico na mesma transação (CF3 do PRD). Valida o token via `IMagicLinkService.ValidateAndConsumeAsync`, aplica `UserManager.AddPasswordAsync` e marca `magic_link.consumed_at`. O endpoint público `POST /api/v1/auth/password-setup` é criado em task_18.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST definir `PasswordSetupCommand { RawToken, Password, IpAddress? }`
- MUST aplicar política de senha mínima (configurável via `Identity:Password:*` — detalhamento é Open Question; aplicar valores defensivos: min 10 caracteres, 1 letra + 1 dígito, sem limite superior)
- MUST invocar `IMagicLinkService.ValidateAndConsumeAsync(token, PasswordSetup)` e falhar em caso de resultado inválido com resposta genérica
- MUST definir senha via `UserManager.AddPasswordAsync(user, password)` em sucesso
- MUST tudo na mesma transação: consumo do token + definição da senha
- MUST retornar 200 em sucesso (sem body sensível); 400 genérico em qualquer falha
- MUST registrar log estruturado `password-setup.succeeded` / `password-setup.failed` com `user_id` e `reason_code` (falha); nunca logar o token em claro
</requirements>

## Subtasks
- [ ] 16.1 Definir `PasswordSetupCommand`
- [ ] 16.2 Implementar validador (senha mínima, token presente)
- [ ] 16.3 Implementar handler com transação única
- [ ] 16.4 Garantir resposta genérica e log estruturado

## Implementation Details
Conforme ADR-006 (respostas genéricas). A política de senha fica configurável e lê defaults; a especificação definitiva fica dependente de F05 (Open Question do PRD).

### Relevant Files
- `src/PortaBox.Modules.Gestao/Application/Commands/PasswordSetup/PasswordSetupCommand.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/PasswordSetup/PasswordSetupCommandHandler.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/PasswordSetup/PasswordSetupCommandValidator.cs` (a criar)

### Dependent Files
- `task_18` (auth controller) expõe `POST /api/v1/auth/password-setup`
- `task_24` (síndico SPA) consome o endpoint
- `task_26` (hardening) adiciona rate-limit ao endpoint

### Related ADRs
- [ADR-006: Magic Link com Token Opaco](../adrs/adr-006.md) — especifica consumo atômico.

## Deliverables
- Command + Handler + Validator
- Política de senha configurável
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para sucesso, token inválido, expirado, já usado **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] Validator reprova senha com menos de 10 caracteres
  - [ ] Validator reprova senha só com letras (sem dígito)
  - [ ] Handler retorna `Result.Failure(Generic)` para token inexistente
  - [ ] Handler retorna `Result.Success` quando `ValidateAndConsumeAsync` sucede e `AddPasswordAsync` sucede
- Integration tests:
  - [ ] Fluxo feliz: emitir magic link → consumir com senha válida → `AppUser.PasswordHash` populado + `magic_link.consumed_at` marcado + síndico consegue login subsequente
  - [ ] Token expirado retorna 400 genérico; log estruturado contém `reason_code=expired`
  - [ ] Segunda tentativa com mesmo token retorna 400 genérico; `reason_code=already_consumed`
  - [ ] Falha de política de senha retorna 400 (sem tocar o token)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Síndico consegue fazer o primeiro login via fluxo completo
- Respostas não vazam razão da falha
- Log estruturado fornece dados suficientes para ops sem comprometer segurança
