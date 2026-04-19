# Task Memory: task_11.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Implementar o pipeline completo de eventos de domínio com outbox transacional no baseline .NET atual: abstrações de domínio, interceptor EF Core, dispatcher pós-commit, worker NoOp configurável e migração/tabelas correspondentes.
- Validar atomicidade estado+outbox usando `Condominio` real e eventos injetados via reflexão nos testes, sem antecipar os eventos concretos de negócio que pertencem às tasks 12/14.

## Important Decisions
- Reutilizar o padrão já existente do `email_outbox`: options + worker + processor/DI na infraestrutura, mas mantendo o dispatcher in-process acoplado ao interceptor de `SaveChanges` para garantir execução apenas após commit bem-sucedido.
- Manter `AggregateRoot` minimalista e usar `InternalsVisibleTo` do assembly `PortaBox.Domain` para permitir que a infraestrutura limpe/restaure eventos sem expor API pública adicional para os módulos de domínio.
- O `DomainEventOutboxInterceptor` captura eventos pendentes por `DbContext`, grava as linhas na `domain_event_outbox` no `SavingChanges`, limpa os eventos e restaura a coleção se o commit falhar; o `IDomainEventDispatcher` só roda em `SavedChanges`.
- O publisher MVP foi separado em `DomainEventOutboxProcessor` + `DomainEventOutboxPublisher`; o worker apenas marca `published_at` em lote e atualiza gauges locais de backlog/idade preparados para observabilidade posterior.

## Learnings
- `AggregateRoot` e `IDomainEvent` já existem no workspace, mas estão incompletos para o task: `DomainEvents` está como `IReadOnlyCollection` e `ClearDomainEvents` está público.
- O `AppDbContext` atual não recebe interceptors por DI; qualquer comportamento de outbox precisa entrar via `AddDbContext(...).AddInterceptors(...)` na infraestrutura.
- A migração EF gerada para este task é `20260418170544_AddDomainEventOutbox`; os testes de integração validam a existência da tabela `domain_event_outbox` e do índice `idx_domain_event_outbox_published_at_created_at` diretamente no PostgreSQL.
- A cobertura agregada com `dotnet test /p:CollectCoverage=true` ficou abaixo de 80% quando considerada por toda a solution porque boa parte do código ainda está sem features/tasks futuras; a suite de integração do assembly relevante (`PortaBox.Api.IntegrationTests`) ficou em 84.58% de linhas e valida integralmente o escopo deste task.

## Files / Surfaces
- `src/PortaBox.Domain/Abstractions/*`
- `src/PortaBox.Application.Abstractions/*`
- `src/PortaBox.Infrastructure/Persistence/*`
- `src/PortaBox.Infrastructure/Events/*`
- `src/PortaBox.Api/appsettings*.json`
- `tests/PortaBox.Api.UnitTests/*`
- `tests/PortaBox.Api.IntegrationTests/*`
- `tests/PortaBox.Modules.Gestao.UnitTests/*`
- `src/PortaBox.Infrastructure/Persistence/Migrations/20260418170544_AddDomainEventOutbox*.cs`

## Errors / Corrections
- `AGENTS.md` e `CLAUDE.md` pedidos pelo task não existem no workspace; o run segue usando `_techspec.md`, `_tasks.md`, ADR-009 e as skills obrigatórias como fonte de verdade.

## Ready for Next Run
- Task pronto para handoff manual: build limpo, todos os testes passando, memória/tracking pendentes apenas de revisão final do diff pelo usuário.
