# Task Memory: task_10.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Entregar a infraestrutura de magic link do F01: contratos de aplicação, entidade/tabela `magic_link`, service com emissão segura, consumo de uso único, invalidação de pendentes, rate-limit, logs sanitizados e cobertura unit/integration.

## Important Decisions
- `IMagicLinkService` ficou em `PortaBox.Application.Abstractions/MagicLinks` para ser consumido pelos handlers seguintes sem depender do módulo ou da infraestrutura.
- `ValidateAndConsumeAsync` mantém o contrato pedido pela task e resolve `consumed_by_ip` via `IHttpContextAccessor` opcional na infraestrutura, sem expor IP na interface pública.
- Em provider relacional, consumo e invalidação usam `ExecuteUpdateAsync` para garantir update único no banco; em testes com InMemory há fallback para entidades rastreadas + `SaveChangesAsync`.

## Learnings
- O consumo via `ExecuteUpdateAsync` não atualiza entidades já rastreadas pelo EF; testes que verificam `consumed_at` depois do consumo precisam limpar o `ChangeTracker` ou recarregar a entidade com `AsNoTracking()`.
- A cobertura global do projeto de testes ainda é baixa porque a suite cobre só parte do monorepo; para esta task, a evidência relevante é a cobertura filtrada das superfícies `MagicLinks`, que ficou acima de 80%.

## Files / Surfaces
- `src/PortaBox.Application.Abstractions/MagicLinks/*`
- `src/PortaBox.Infrastructure/MagicLinks/*`
- `src/PortaBox.Infrastructure/Persistence/AppDbContext.cs`
- `src/PortaBox.Infrastructure/Persistence/Configurations/MagicLinkConfiguration.cs`
- `src/PortaBox.Infrastructure/Persistence/Migrations/20260418165015_AddMagicLink*.cs`
- `src/PortaBox.Infrastructure/DependencyInjection.cs`
- `src/PortaBox.Infrastructure/PortaBox.Infrastructure.csproj`
- `src/PortaBox.Api/appsettings.json`
- `tests/PortaBox.Api.UnitTests/MagicLinkServiceUnitTests.cs`
- `tests/PortaBox.Api.IntegrationTests/MagicLinkIntegrationTests.cs`

## Errors / Corrections
- O vetor SHA-256 esperado no teste unitário estava incorreto; corrigido com o valor real calculado para `magic-link-known-vector`.
- O primeiro teste de integração lia a entidade ainda rastreada após `ExecuteUpdateAsync`, mascarando `consumed_at`; corrigido com `ChangeTracker.Clear()` + reload `AsNoTracking()`.

## Ready for Next Run
- Task pronta para `task_12`, `task_15` e `task_16`: reemissão já invalida pendentes, consumo retorna falha genérica com `FailureReason` interno para logs/handler, e a tabela `magic_link` já está migrada.
