---
status: completed
title: IEmailSender (MailKit) + email_outbox + retry worker
type: backend
complexity: high
dependencies:
  - task_02
---

# Task 09: IEmailSender (MailKit) + email_outbox + retry worker

## Overview
Introduz a abstração `IEmailSender` com implementação sobre SMTP genérico via MailKit, a tabela `email_outbox` para retries persistidos e um `BackgroundService` que reprocessa mensagens em falha. Todo envio transacional do projeto (magic link no F01, notificações futuras) passa por essa abstração — ADR-008 adia a escolha de provedor comercial.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST definir `IEmailSender` em `PortaBox.Application.Abstractions` com `SendAsync(EmailMessage, CancellationToken)`
- MUST implementar `SmtpEmailSender` baseado em MailKit
- MUST implementar `FakeEmailSender` em memória para testes e ambiente de dev (quando MailHog não está disponível)
- MUST criar tabela `email_outbox` conforme TechSpec (seção Data Models)
- MUST envolver envio em Polly retry exponencial (3 tentativas); em falha persistente, persistir em `email_outbox`
- MUST implementar `EmailOutboxRetryWorker` como `BackgroundService` que varre `email_outbox` com `next_attempt_at <= now()` e `sent_at IS NULL`
- MUST marcar `sent_at` após sucesso; incrementar `attempts` e ajustar `next_attempt_at` em backoff exponencial em falhas
- MUST ler templates HTML de `Resources/EmailTemplates/*` (ou strings embutidas) com variáveis substituíveis
- MUST usar `EmailOptions` bindado de configuração (`Host`, `Port`, `Username`, `Password`, `FromAddress`, `UseStartTls`)
- SHOULD sanitizar endereços antes de logar (hash SHA-256 ou apenas domínio)
</requirements>

## Subtasks
- [x] 09.1 Definir `IEmailSender`, `EmailMessage`, `EmailOptions`
- [x] 09.2 Implementar `SmtpEmailSender` com MailKit + Polly retry
- [x] 09.3 Implementar `FakeEmailSender` (memória) para testes
- [x] 09.4 Criar tabela `email_outbox` + configuração EF + migração
- [x] 09.5 Implementar `EmailOutboxRetryWorker` BackgroundService
- [x] 09.6 Criar template do magic link (HTML + texto) em `Resources/EmailTemplates/MagicLinkPasswordSetup.*`
- [x] 09.7 Registrar adapters via DI conforme ambiente (Smtp em prod/dev; Fake em testes)

## Implementation Details
Seguir ADR-008 e skills `dotnet-dependency-config` (Polly baseline) e `dotnet-observability` (logs com `to_hash`). Em Development, SMTP aponta para MailHog (`localhost:1025`, sem TLS). Em prod, usa SMTP relay comercial a ser escolhido.

### Relevant Files
- `src/PortaBox.Application.Abstractions/Email/IEmailSender.cs` (a criar)
- `src/PortaBox.Application.Abstractions/Email/EmailMessage.cs` (a criar)
- `src/PortaBox.Application.Abstractions/Email/EmailOptions.cs` (a criar)
- `src/PortaBox.Infrastructure/Email/SmtpEmailSender.cs` (a criar)
- `src/PortaBox.Infrastructure/Email/FakeEmailSender.cs` (a criar)
- `src/PortaBox.Infrastructure/Email/EmailOutboxRetryWorker.cs` (a criar)
- `src/PortaBox.Infrastructure/Email/EmailOutboxEntry.cs` (a criar, entidade da tabela)
- `src/PortaBox.Infrastructure/Email/Resources/EmailTemplates/MagicLinkPasswordSetup.html` (a criar)
- `src/PortaBox.Infrastructure/Email/Resources/EmailTemplates/MagicLinkPasswordSetup.txt` (a criar)
- `src/PortaBox.Infrastructure/Persistence/Configurations/EmailOutboxEntryConfiguration.cs` (a criar)
- `src/PortaBox.Infrastructure/Persistence/Migrations/*_EmailOutbox.cs` (a criar)

### Dependent Files
- `task_12` (CreateCondominio) dispara e-mail de magic link através do handler in-process de task_11
- `task_15` (Resend magic link) consome `IEmailSender`
- `task_26` (Hardening) adiciona STARTTLS e credenciais reais

### Related ADRs
- [ADR-008: IEmailSender sobre SMTP Genérico no MVP](../adrs/adr-008.md) — fixa a abstração, MailKit, MailHog em dev.

## Deliverables
- `IEmailSender` + dois adapters (Smtp, Fake)
- Tabela `email_outbox` + migração
- `EmailOutboxRetryWorker` operante
- Template do magic link (HTML + texto)
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para envio + outbox + retry **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `SmtpEmailSender.SendAsync` formata endereço `From`, `To`, `Subject`, corpo HTML e texto
  - [x] Polly retry tenta 3 vezes antes de persistir em outbox
  - [x] Template `MagicLinkPasswordSetup` substitui `{nome}`, `{nome_condominio}`, `{link}` corretamente
  - [x] `FakeEmailSender.SendAsync` apenas acumula `EmailMessage` em memória
- Integration tests:
  - [x] Envio para MailHog (Testcontainers) resulta em mensagem recuperável na API de inspeção do MailHog
  - [x] Falha de rede persistente grava entrada em `email_outbox` com `attempts >= 3`
  - [x] `EmailOutboxRetryWorker` processa entrada pendente e marca `sent_at` em sucesso
  - [x] Log de envio contém `to_hash` (SHA-256) ao invés de endereço em claro
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Envio operacional em dev via MailHog
- Outbox + worker lidam com falhas transitórias sem perda de mensagem
- Templates HTML e texto prontos para uso em task_12
