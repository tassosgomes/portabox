# Task Memory: task_10.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Implementar a suíte de integração de F02 sobre `PostgresDatabaseCollection`, cobrindo os 9 endpoints, soft-delete, isolamento cross-tenant, auditoria e conformidade com `api-contract.yaml`.

## Important Decisions

- Usar um `WebApplicationFactory<Program>` específico para F02 com autenticação de teste por header/claims, em vez de depender do fluxo real de login por cookie. Isso mantém o escopo da task em F02, simplifica a simulação de síndico e operador e atende ao requisito de `TestAuthContext`.
- Seedar tenants, usuários e estrutura diretamente no `AppDbContext` com helpers de teste, em vez de reutilizar o fluxo de criação de condomínio de F01. O objetivo aqui é preparar estados controlados para cenários de integração de F02.

## Learnings

- O endpoint de síndico `GET /condominios/{id}/estrutura` hoje bloqueia tenant diferente com `403` no próprio endpoint (`IsSindicoTenantAuthorized`), enquanto o handler retorna `404` apenas quando o tenant scope chega até ele. Os testes de isolamento precisam refletir esse comportamento real sem expandir escopo para alterar produção.
- O repositório já tem um `tests/PortaBox.Api.IntegrationTests/Persistence/SoftDeleteFilterTests.cs`; a task 10 ainda pede uma suíte focada em F02 em `Features/Estrutura`, então a nova cobertura deve complementar, não substituir, a suíte existente.

## Files / Surfaces

- `tests/PortaBox.Api.IntegrationTests/PortaBox.Api.IntegrationTests.csproj`
- `tests/PortaBox.Api.IntegrationTests/Features/Estrutura/*`
- `tests/PortaBox.Api.IntegrationTests/Helpers/*`
- `tests/PortaBox.Api.IntegrationTests/Fixtures/*` (somente se precisar de factory/helper específico de F02)

## Errors / Corrections

- A task fala em helper `TestAuthContext` com JWT fake, mas o baseline real do repositório usa cookie auth. A correção de rota mínima é introduzir autenticação de teste no `WebApplicationFactory`, sem alterar o pipeline de produção.

## Ready for Next Run

- A validação de contrato ficou em comparação `api-contract.yaml` vs `swagger.json` gerado, filtrando os paths de F02. Como `Microsoft.OpenApi.Readers` não aceita OpenAPI 3.1, os testes rebaixam `openapi: 3.1.0` para `3.0.3` apenas em memória antes de deserializar.
- O bloqueio final de verificação veio de testes legados/manuais construindo `DbContextOptions` sem suprimir `ManyServiceProvidersCreatedWarning`; o último caso remanescente foi `tests/PortaBox.Api.IntegrationTests/Persistence/SoftDeleteFilterTests.cs`.
- Verificação final concluída com sucesso em `dotnet test PortaBox.sln --no-restore`.
