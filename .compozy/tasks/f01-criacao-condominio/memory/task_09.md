# Task Memory: task_09.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Entregar a stack de e-mail transacional do F01: `IEmailSender`, adapter SMTP com MailKit, fallback persistente em `email_outbox`, worker de retry, templates do magic link e cobertura unit/integration.

## Important Decisions
- O retry SMTP foi implementado como 3 tentativas totais por ciclo de envio (`SmtpEmailDispatcher`), não 3 retries adicionais; falha persistente grava `email_outbox.attempts = 3`.
- `IEmailTemplateRenderer` foi introduzido junto com `IEmailSender` para deixar os templates de e-mail consumíveis pela aplicação sem acoplamento a paths de infraestrutura.
- O worker real (`EmailOutboxRetryWorker`) delega o processamento para `EmailOutboxProcessor`, o que simplifica teste de integração do comportamento de retry sem depender do loop/timer do `BackgroundService`.

## Learnings
- `MailKit` publicado em 2026-04-15 (`4.16.0`) é necessário para evitar o advisory `GHSA-9j88-vvj5-vhgr`; versões `< 4.16.0` mantêm warning de vulnerabilidade no restore/build.
- Para tornar o cenário de falha de rede determinístico nos testes de integração, reservar e liberar uma porta local gera `connection refused` rápido e evita timeout longo no SMTP client.
- O log estruturado de envio/retry precisa expor a propriedade exatamente como `to_hash`; usar apenas `ToHash` no template de mensagem não atende o contrato observável da task.

## Files / Surfaces
- `src/PortaBox.Application.Abstractions/Email/*`
- `src/PortaBox.Infrastructure/Email/*`
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs`
- `src/PortaBox.Infrastructure/Persistence/Configurations/EmailOutboxEntryConfiguration.cs`
- `src/PortaBox.Infrastructure/Persistence/Migrations/20260418155301_AddEmailOutbox*.cs`
- `src/PortaBox.Infrastructure/DependencyInjection.cs`
- `src/PortaBox.Api/appsettings*.json`
- `tests/PortaBox.Api.UnitTests/EmailInfrastructureTests.cs`
- `tests/PortaBox.Api.IntegrationTests/Fixtures/MailHogFixture.cs`
- `tests/PortaBox.Api.IntegrationTests/EmailInfrastructureIntegrationTests.cs`

## Errors / Corrections
- O primeiro corte do Polly usava `WaitAndRetryAsync(3)`, gerando 4 tentativas totais; corrigido para 3 tentativas totais (`RetryAttempts - 1` retries).
- O cenário de falha SMTP inicialmente podia travar a suite; corrigido com timeout explícito no `MailKitEmailTransport` e porta indisponível determinística no teste.

## Ready for Next Run
- Task pronta para consumo por `task_12`/`task_15`: usar `IEmailSender` + `IEmailTemplateRenderer` para montar e disparar o magic link sem acessar SMTP diretamente.
