# Task Memory: task_15.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Implementar `ResendMagicLinkCommandHandler` com validação de pertencimento ao condomínio, bloqueio para síndico com senha, reemissão auditada e cobertura unit/integration.

## Important Decisions
- O handler usa `IIdentityUserLookupService` para consultar `AppUser.PasswordHash` e e-mail sem acoplar `PortaBox.Modules.Gestao` à infraestrutura/Identity diretamente.
- O lookup do `Sindico` no backoffice usa `ISindicoRepository.GetByUserIdIgnoreQueryFiltersAsync(...)` porque o operador não executa sob `TenantContext` do condomínio alvo.
- O fluxo do handler faz `CanIssueAsync` -> `InvalidatePendingAsync` -> `IssueAsync` para cumprir a ordem pedida pela task sem invalidar o link atual quando o serviço responde `RateLimited`.

## Learnings
- `IssueAsync` já invalida pendentes internamente; a chamada explícita exigida pela task precisa de um preflight anterior para evitar regressão de UX em rate-limit.
- `CondominioRepository.GetByIdAsync` continua suficiente para o reenvio porque `Condominio` é entidade global e não sofre query filter multi-tenant.

## Files / Surfaces
- `src/PortaBox.Modules.Gestao/Application/Commands/ResendMagicLink/*`
- `src/PortaBox.Application.Abstractions/Identity/IIdentityUserLookupService.cs`
- `src/PortaBox.Application.Abstractions/Identity/IdentityUserLookup.cs`
- `src/PortaBox.Application.Abstractions/MagicLinks/IMagicLinkService.cs`
- `src/PortaBox.Infrastructure/Identity/IdentityUserLookupService.cs`
- `src/PortaBox.Infrastructure/MagicLinks/MagicLinkService.cs`
- `src/PortaBox.Infrastructure/Repositories/SindicoRepository.cs`
- `src/PortaBox.Modules.Gestao/DependencyInjection.cs`
- `src/PortaBox.Infrastructure/DependencyInjection.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/ResendMagicLinkCommandHandlerTests.cs`
- `tests/PortaBox.Api.IntegrationTests/ResendMagicLinkCommandIntegrationTests.cs`

## Errors / Corrections
- A primeira versão do handler invalidava pendentes antes de descobrir que a nova emissão seria barrada por rate-limit; isso foi corrigido introduzindo `CanIssueAsync` no serviço.
- `dotnet test PortaBox.sln` em execução paralela abortou uma vez com falha transitória em `CreateCondominioCommandHandlerTests`; a verificação ampla passou com `dotnet build PortaBox.sln && dotnet test PortaBox.sln -m:1`.

## Ready for Next Run
- Task pronta para o endpoint REST da task 18 consumir `ICommandHandler<ResendMagicLinkCommand, ResendMagicLinkResult>` e mapear `NotFound`, `AlreadyHasPassword` e `RateLimited` para HTTP.
