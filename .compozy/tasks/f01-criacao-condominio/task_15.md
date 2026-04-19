---
status: completed
title: ResendMagicLinkCommandHandler
type: backend
complexity: low
dependencies:
  - task_06
  - task_09
  - task_10
---

# Task 15: ResendMagicLinkCommandHandler

## Overview
Permite ao operador reemitir o magic link para um síndico quando o link original expirou ou não foi recebido (CF4 do PRD, caso de uso "Reenviar magic link"). Invalida tokens pendentes, emite novo, dispara e-mail e registra entrada no audit log.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST definir `ResendMagicLinkCommand { CondominioId, SindicoUserId, PerformedByUserId }`
- MUST validar que o `Sindico` pertence ao `Condominio` informado
- MUST chamar `IMagicLinkService.InvalidatePendingAsync(userId, PasswordSetup)` antes de emitir novo
- MUST chamar `IMagicLinkService.IssueAsync(userId, PasswordSetup)`
- MUST chamar `IEmailSender.SendAsync(...)` com o template `MagicLinkPasswordSetup`
- MUST registrar `TenantAuditEntry(event_kind = MagicLinkResent)` com `performed_by_user_id`
- MUST retornar erro `RateLimited` quando o `IMagicLinkService` bloquear por rate-limit
- MUST retornar erro específico quando o síndico já tem senha definida (`AlreadyHasPassword`)
</requirements>

## Subtasks
- [x] 15.1 Definir `ResendMagicLinkCommand`
- [x] 15.2 Implementar validator (síndico pertence ao tenant, síndico ainda sem senha)
- [x] 15.3 Implementar handler coordenando invalidate → issue → send → audit
- [x] 15.4 Registrar handler via DI

## Implementation Details
Conforme ADR-006 (rate-limit e invalidação). O handler é pequeno, mas crítico para UX do operador quando o primeiro link falha por qualquer motivo. O endpoint REST `POST /api/v1/admin/condominios/{id}/sindicos/{userId}:resend-magic-link` é criado em task_18.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Application/Commands/ResendMagicLink/ResendMagicLinkCommand.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/ResendMagicLink/ResendMagicLinkCommandHandler.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/ResendMagicLink/ResendMagicLinkCommandValidator.cs` (a criar)

### Dependent Files
- `task_18` (controller) expõe endpoint
- `task_23` (backoffice — detalhes do tenant) dispara a ação

### Related ADRs
- [ADR-006: Magic Link com Token Opaco](../adrs/adr-006.md) — fluxo de invalidação e reemissão.

## Deliverables
- Command + Handler + Validator
- Entrada audit `MagicLinkResent`
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para caminho feliz e rate-limit **(REQUIRED)**

## Tests
- Unit tests:
  - [x] Handler retorna `Result.Failure(NotFound)` quando síndico não pertence ao tenant
  - [x] Handler retorna `Result.Failure(AlreadyHasPassword)` quando `AppUser.PasswordHash` já existe
  - [x] Handler invoca `InvalidatePendingAsync` antes de `IssueAsync`
  - [x] Handler invoca `IEmailSender.SendAsync` exatamente uma vez em sucesso
- Integration tests:
  - [x] Fluxo completo: 2 requests consecutivos → primeiro token é invalidado, segundo token é o único consumível
  - [x] Rate-limit: 6ª tentativa na mesma janela retorna `RateLimited` sem tocar o `IEmailSender`
  - [x] Entrada `TenantAuditEntry(MagicLinkResent)` gravada em cada reemissão bem-sucedida
  - [x] Síndico com senha já definida é rejeitado antes de emitir novo token
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Operador recupera situações de link expirado sem refazer o wizard
- Audit log reflete todos os reenvios
