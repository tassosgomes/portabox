# Task Memory: task_09.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

- Expor F02 na API com 9 rotas minimal API aderentes ao `api-contract.yaml`, registrar handlers/validators faltantes e conectar o mapeamento `Result` -> `ProblemDetails`.
- Objetivo concluído sem commit automático; diff ficou pronto para revisão manual.

## Important Decisions

- Seguir o baseline real do repositório: repositórios de F02 permanecem registrados em `src/PortaBox.Infrastructure/DependencyInjection.cs`; o módulo registra handlers e validators.
- Implementar os endpoints no caminho pedido pela task (`src/PortaBox.Api/Features/Estrutura/EstruturaEndpoints.cs`), preservando as convenções existentes da API para auth, route groups e respostas.
- Normalizar `ProblemDetails` em pt-BR no pipeline da API para alinhar 400/401/403/404/409/422/500 ao contrato sem sobrescrever detalhes específicos já definidos pelos endpoints.
- Adicionar `Microsoft.AspNetCore.OpenApi` ao projeto da API para suportar `.WithOpenApi()` nos minimal APIs.

## Learnings

- `AddPortaBoxModuleGestao` já registra `GetEstruturaQueryHandler`, mas ainda não registra os handlers e validators de blocos/unidades.
- `AddProblemDetails` já popula `instance` e `traceId`; o helper novo precisa preencher `type`, `title`, `status`, `detail` e `errors` no mesmo pipeline.
- Os `IResult` de `Results.Problem` dependem de `RequestServices` configurado no `HttpContext`; os testes unitários do helper precisam montar um container mínimo com `AddProblemDetails()`.
- O tenant do síndico precisa ser validado explicitamente nos delegates de escrita/leitura de F02 para garantir 403 quando o `condominioId` do path diverge do `tenant_id` autenticado.

## Files / Surfaces

- `src/PortaBox.Modules.Gestao/DependencyInjection.cs`
- `src/PortaBox.Api/Program.cs`
- `src/PortaBox.Api/Extensions/ServiceCollectionExtensions.cs`
- `src/PortaBox.Api/Infrastructure/ProblemDetailsExceptionHandler.cs`
- `src/PortaBox.Api/Extensions/ResultExtensions.cs`
- `src/PortaBox.Api/Features/Estrutura/EstruturaEndpoints.cs`
- `src/PortaBox.Api/PortaBox.Api.csproj`
- `tests/PortaBox.Api.UnitTests/ResultExtensionsTests.cs`
- `tests/PortaBox.Api.UnitTests/ProblemDetailsExceptionHandlerTests.cs`
- `tests/PortaBox.Api.IntegrationTests/ApiBootstrapTests.cs`

## Errors / Corrections

- A task pede registro de repositórios no módulo, mas o shared memory e o código atual indicam que o local correto e durável desses registros é a DI da infrastructure.
- O primeiro build falhou porque o projeto ainda não referenciava `Microsoft.AspNetCore.OpenApi`; a dependência foi adicionada antes da revalidação.
- Os primeiros testes do helper falharam por falta de `IProblemDetailsService` no `HttpContext` de teste; o fixture foi corrigido para montar `RequestServices` mínimos.

## Ready for Next Run

- Próxima task deve cobrir os 9 endpoints de F02 em integração/contrato (`task_10`); nesta task ficaram apenas unit tests do helper e a passagem do pipeline existente da solução.
