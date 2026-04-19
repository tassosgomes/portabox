---
status: completed
title: AggregateRoot + IDomainEvent + outbox + interceptor + publisher NoOp
type: backend
complexity: critical
dependencies:
  - task_03
---

# Task 11: AggregateRoot + IDomainEvent + outbox + interceptor + publisher NoOp

## Overview
Implementa o padrão de eventos de domínio com Transactional Outbox descrito em ADR-009: classe base `AggregateRoot` com `DomainEvents`, interface `IDomainEvent` versionada, `SaveChangesInterceptor` que move eventos pendentes para a tabela `domain_event_outbox` **na mesma transação** do commit, e um `BackgroundService` publisher NoOp que apenas marca `published_at` no MVP. Quando o Mastra entrar, o publisher passa a publicar em RabbitMQ sem refactor de modelo.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST definir `IDomainEvent` com `string EventType { get; }` (versionado, ex.: `condominio.cadastrado.v1`) e `DateTimeOffset OccurredAt { get; }`
- MUST definir classe base `AggregateRoot` com `DomainEvents` (IReadOnlyList), `AddDomainEvent` protegido e `ClearDomainEvents` internal
- MUST definir `IDomainEventDispatcher` para despacho in-process após commit
- MUST implementar `DomainEventOutboxInterceptor : SaveChangesInterceptor` que, antes do commit, serializa `DomainEvents` em JSONB e insere em `domain_event_outbox`, depois limpa a coleção
- MUST criar tabela `domain_event_outbox` conforme TechSpec com índice `(published_at, created_at)`
- MUST implementar `DomainEventOutboxPublisher : BackgroundService` que no MVP apenas seta `published_at = now()` em lote
- MUST expor flag `DomainEvents:Publisher:Enabled` para desabilitar o worker em testes/ambientes específicos
- MUST garantir que eventos in-process (handlers locais) executem **após** commit bem-sucedido da transação
- SHOULD oferecer log estruturado com idade da fila (`domain_event_outbox_age_seconds`)
</requirements>

## Subtasks
- [x] 11.1 Definir `IDomainEvent`, `AggregateRoot`, `IDomainEventDispatcher`
- [x] 11.2 Implementar `DomainEventOutboxInterceptor`
- [x] 11.3 Implementar entidade `DomainEventOutboxEntry` + configuração + migração
- [x] 11.4 Implementar `InProcessDomainEventDispatcher` (resolve handlers via DI e invoca após commit)
- [x] 11.5 Implementar `DomainEventOutboxPublisher : BackgroundService` (NoOp no MVP, com hooks para evolução)
- [x] 11.6 Registrar interceptor em `AppDbContext`
- [x] 11.7 Adicionar métrica `domain_event_outbox_pending_count` preparada para task_19

## Implementation Details
Seguir ADR-009. Evento versionado desde o dia 1 para evitar churn quando Mastra entrar. Payload em JSONB permite evolução retrocompatível. O interceptor executa no `SavingChangesAsync` do EF Core; o dispatcher in-process executa em `SavedChangesAsync`.

### Relevant Files
- `src/PortaBox.Domain/Abstractions/IDomainEvent.cs` (a criar)
- `src/PortaBox.Domain/Abstractions/AggregateRoot.cs` (a criar)
- `src/PortaBox.Application.Abstractions/Events/IDomainEventDispatcher.cs` (a criar)
- `src/PortaBox.Infrastructure/Events/InProcessDomainEventDispatcher.cs` (a criar)
- `src/PortaBox.Infrastructure/Events/DomainEventOutboxInterceptor.cs` (a criar)
- `src/PortaBox.Infrastructure/Events/DomainEventOutboxEntry.cs` (a criar)
- `src/PortaBox.Infrastructure/Events/DomainEventOutboxPublisher.cs` (a criar)
- `src/PortaBox.Infrastructure/Persistence/Configurations/DomainEventOutboxEntryConfiguration.cs` (a criar)
- `src/PortaBox.Infrastructure/Persistence/Migrations/*_DomainEventOutbox.cs` (a criar)
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs` — registrar interceptor (editar)

### Dependent Files
- `task_06` (Condominio/Sindico): `Condominio` herda `AggregateRoot` depois deste task
- `task_12` (CreateCondominio handler) emite `CondominioCadastradoV1`
- `task_14` (Activate) emite `CondominioAtivadoV1`
- `task_19` (observability) instrumenta métricas da outbox
- `task_25` (integration tests) verifica atomicidade

### Related ADRs
- [ADR-009: Eventos de Domínio In-process no MVP com Outbox Pattern](../adrs/adr-009.md) — especificação completa.

## Deliverables
- `AggregateRoot` + `IDomainEvent` + `IDomainEventDispatcher`
- Interceptor e publisher funcionando
- Tabela `domain_event_outbox` + migração
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests de atomicidade e publicação NoOp **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `AggregateRoot.AddDomainEvent` acumula evento em `DomainEvents`
  - [x] `ClearDomainEvents` só é acessível internamente (internal)
  - [x] `InProcessDomainEventDispatcher` resolve handlers via DI e invoca todos em ordem
  - [x] `DomainEventOutboxInterceptor.SavingChangesAsync` serializa eventos em JSON e insere em outbox
- Integration tests:
  - [x] **Atomicidade:** alterar entidade + falhar commit propositadamente (ex.: violar constraint) → nenhuma linha em `domain_event_outbox` permanece
  - [x] Commit bem-sucedido grava 1 linha em `domain_event_outbox` por evento
  - [x] `DomainEventOutboxPublisher` NoOp marca `published_at` em lote após intervalo configurado
  - [x] Flag `DomainEvents:Publisher:Enabled=false` mantém `published_at=NULL` (fila cresce)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Estado + evento são atômicos (verificado por teste)
- Padrão pronto para ser consumido pelos handlers de criação/ativação
- Migração para RabbitMQ (futuro) exige apenas trocar a implementação do publisher
