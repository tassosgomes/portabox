# Task Memory: task_05.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Entregar a baseline multi-tenant do backend com `ITenantContext`, `ITenantEntity`, middleware de resolução, query filter global automático e testes de isolamento cobrindo troca de escopo e bypass auditado.

## Important Decisions
- `TenantContext` passou a usar `AsyncLocal` com escopos aninhados restauráveis para cumprir ADR-004 e suportar overrides temporários em fluxos assíncronos.
- O query filter automático do EF Core foi mantido por reflection, mas a expressão passou a ler `AppDbContext.CurrentTenantId` em vez de capturar o serviço injetado diretamente.
- A fixture `PostgresDatabaseFixture` passou a usar `WithReuse(true)` para estabilizar execuções repetidas do teste crítico de isolamento entre processos de teste.

## Learnings
- Capturar `ITenantContext` diretamente dentro de `HasQueryFilter` pode congelar o tenant errado no modelo cacheado do EF Core; referenciar um membro do próprio `DbContext` mantém o filtro dinâmico por instância.
- O loop de 100 execuções do teste crítico falhou com o resource reaper padrão do Testcontainers; reuso explícito do container eliminou a flutuação observada.

## Files / Surfaces
- `src/PortaBox.Infrastructure/MultiTenancy/TenantContext.cs`
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs`
- `tests/PortaBox.Api.UnitTests/TenantContextTests.cs`
- `tests/PortaBox.Api.IntegrationTests/Fixtures/PostgresDatabaseFixture.cs`

## Errors / Corrections
- Pré-sinal reproduzido: `TenantIsolationTests` falhava porque o filtro via `tenantContext.TenantId` retornava coleção vazia/tenant errado.
- Durante a validação de estabilidade, a 13ª repetição falhou na inicialização do `Testcontainers` (`Could not find resource 'DockerContainer'`); a fixture foi ajustada para reuso e o loop de 100 execuções passou.

## Ready for Next Run
- Próximas entidades multi-tenant devem apenas implementar `ITenantEntity`; o filtro global já cobre o isolamento e os testes de task_05 servem como guard-rail contra regressão.
