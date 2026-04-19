---
status: completed
title: CreateCondominioCommandHandler (orquestra tudo)
type: backend
complexity: high
dependencies:
  - task_06
  - task_07
  - task_10
  - task_11
---

# Task 12: CreateCondominioCommandHandler (orquestra tudo)

## Overview
Orquestra o passo central do wizard do F01: em uma única transação cria `Condominio` em `PreAtivo`, `Sindico` + `AppUser` (sem senha, `PendingPasswordSetup`), `OptInRecord`, emite evento `condominio.cadastrado.v1` via outbox e dispara magic link para o síndico. É o command handler mais crítico do MVP porque conecta todos os blocos criados nas tasks anteriores.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST definir `CreateCondominioCommand` com payload completo: dados do condomínio, metadados opt-in, dados do síndico (conforme PRD CF1)
- MUST validar o command via FluentValidation (CNPJ, CPF, e-mail, celular E.164, obrigatoriedades)
- MUST bloquear duplicata de CNPJ via `ICondominioRepository.ExistsByCnpjAsync` antes de persistir
- MUST criar `AppUser` para o síndico com role `Sindico`, e-mail único, sem `PasswordHash`
- MUST criar `Condominio` com `status=PreAtivo` e ligar o `Sindico` a ele
- MUST persistir `OptInRecord` com metadados obrigatórios
- MUST emitir evento `condominio.cadastrado.v1` (via `AggregateRoot.AddDomainEvent`) que será serializado na outbox pelo interceptor
- MUST registrar handler in-process `SendSindicoMagicLinkOnCondominioCreated` que, após commit, chama `IMagicLinkService.IssueAsync` e `IEmailSender.SendAsync`
- MUST rollback completo se qualquer passo falhar (transação única do DbContext)
- MUST retornar `CreateCondominioResult` com `CondominioId` e `SindicoUserId`
- MUST registrar entrada `TenantAuditEntry` (`event_kind = Created`) — tabela criada em task_14, ou usa referência forward; se ainda não existir, pode ser criada stub aqui e formalizada em task_14
</requirements>

## Subtasks
- [x] 12.1 Definir `CreateCondominioCommand` + `CreateCondominioResult`
- [x] 12.2 Implementar `CreateCondominioCommandValidator` (FluentValidation)
- [x] 12.3 Implementar `CreateCondominioCommandHandler` com transação única
- [x] 12.4 Implementar handler in-process `SendSindicoMagicLinkOnCondominioCreated`
- [x] 12.5 Adicionar evento `CondominioCadastradoV1` em `PortaBox.Modules.Gestao.Domain.Events`
- [x] 12.6 Registrar todos os componentes via DI

## Implementation Details
Seguir a seção **Data Flow C1** do TechSpec. A transação única é crítica: qualquer falha entre criação do usuário, persistência do opt-in e registro de auditoria aborta tudo. O magic link é disparado **após** o commit via `SavedChangesAsync` do dispatcher in-process de task_11 — isso evita enviar e-mail para um tenant que não foi persistido.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Application/Commands/CreateCondominio/CreateCondominioCommand.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/CreateCondominio/CreateCondominioCommandHandler.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/CreateCondominio/CreateCondominioCommandValidator.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/CreateCondominio/CreateCondominioResult.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Domain/Events/CondominioCadastradoV1.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/EventHandlers/SendSindicoMagicLinkOnCondominioCreated.cs` (a criar)

### Dependent Files
- `task_18` (controllers) expõe endpoint `POST /api/v1/admin/condominios`
- `task_22` (wizard frontend) consome esse endpoint

### Related ADRs
- [ADR-001: Onboarding de Tenant no MVP](../adrs/adr-001.md) — define estado inicial `PreAtivo`.
- [ADR-003: CNPJ Obrigatório como Identificador Canônico](../adrs/adr-003.md) — dedup bloqueante.
- [ADR-009: Eventos de Domínio In-process + Outbox](../adrs/adr-009.md) — uso do AggregateRoot + outbox.

## Deliverables
- Command + Handler + Validator + Result
- Evento `CondominioCadastradoV1`
- Handler in-process de disparo de magic link
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para fluxo ponta a ponta **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] Validator reprova CNPJ inválido, CPF inválido, e-mail inválido e celular fora de E.164
  - [ ] Handler com CNPJ duplicado retorna `Result.Failure` com código `CnpjAlreadyExists`
  - [ ] Handler com e-mail de síndico já cadastrado retorna `Result.Failure` com código `SindicoEmailAlreadyExists`
  - [ ] Handler em happy path cria `Condominio`, `Sindico`, `AppUser`, `OptInRecord`, acumula evento `CondominioCadastradoV1`
- Integration tests:
  - [ ] **Fluxo completo:** `CreateCondominioCommand` → DB contém todas as 4 entidades + 1 entrada em `domain_event_outbox` + magic link emitido + e-mail no `FakeEmailSender`
  - [ ] Falha simulada no passo de `OptInRecord` (violação) **não** cria o `Condominio` nem o `AppUser` (rollback)
  - [ ] `AppUser` criado não tem `PasswordHash` (PendingPasswordSetup)
  - [ ] Magic link emitido tem `purpose = PasswordSetup` e TTL 72h
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Atomicidade garantida por teste de falha simulada
- Handler pronto para ser exposto no endpoint REST (task_18)

## Verification Notes
- `dotnet build`
- `dotnet test tests/PortaBox.Modules.Gestao.UnitTests/PortaBox.Modules.Gestao.UnitTests.csproj`
- `dotnet test tests/PortaBox.Api.IntegrationTests/PortaBox.Api.IntegrationTests.csproj --filter FullyQualifiedName~CreateCondominioCommandIntegrationTests`
