# Task Memory: task_02.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Entregar a baseline do `PortaBox.Api` com Serilog JSON, Problem Details global, prefixo `/api/v1`, CORS por ambiente, Swagger só em `Development` e middleware de `X-Request-Id`.
- Fechar a task com testes unitários e de integração cobrindo erro 400/404/500, correlação de logs e cobertura >=80%.

## Important Decisions
- O versionamento foi estabelecido por prefixo de rota via `ApiRoutes.V1` e `MapGroup("/api/v1")`, deixando a convenção pronta para os próximos endpoints.
- O log estruturado usa Serilog Console com formatter JSON customizado e enrichment por `Activity.Current`; o middleware de `request-id` também emite um log explícito de correlação para validação de escopo.
- Swagger/OpenAPI foi registrado e exposto apenas em `Development`; `CORS` usa `AllowAnyOrigin()` em dev e `Cors:AllowedOrigins` em produção via `IOptions<CorsSettings>`.

## Learnings
- `AddProblemDetails()` não registra sozinho `ProblemDetailsFactory`; foi necessário adicionar `AddControllers()` para sustentar o `ProblemDetailsExceptionHandler` baseado em factory no runtime.
- Para consolidar cobertura da API com coverlet, o merge entre unit e integration precisou usar `coverlet.msbuild` e `CoverletOutputFormat=json%2ccobertura`.

## Files / Surfaces
- `src/PortaBox.Api/Program.cs`
- `src/PortaBox.Api/PortaBox.Api.csproj`
- `src/PortaBox.Api/appsettings.json`
- `src/PortaBox.Api/appsettings.Development.json`
- `src/PortaBox.Api/appsettings.Production.json`
- `src/PortaBox.Api/Extensions/ServiceCollectionExtensions.cs`
- `src/PortaBox.Api/Infrastructure/ActivityEnricher.cs`
- `src/PortaBox.Api/Infrastructure/ApiJsonFormatter.cs`
- `src/PortaBox.Api/Infrastructure/ProblemDetailsExceptionHandler.cs`
- `src/PortaBox.Api/Infrastructure/RequestIdMiddleware.cs`
- `src/PortaBox.Api/Options/ApiRoutes.cs`
- `src/PortaBox.Api/Options/CorsSettings.cs`
- `tests/PortaBox.Api.UnitTests/*`
- `tests/PortaBox.Api.IntegrationTests/*`

## Errors / Corrections
- Build inicial falhou por namespaces ausentes e pelo uso direto de `Dictionary<string,string[]>` com `CreateValidationProblemDetails`; corrigido com imports explícitos e `ModelStateDictionary`.
- Integração falhou até o host registrar `ProblemDetailsFactory`; corrigido via `AddControllers()` no bootstrap da API.
- O teste de logs inicialmente inspecionava o evento errado do Serilog request logging; corrigido emitindo um log de correlação no middleware e validando esse evento.

## Ready for Next Run
- Próximas tasks podem assumir que exceções não tratadas e status codes 4xx/5xx já saem em RFC 7807 com `traceId`, e que `/swagger` só existe em `Development`.
- Se novos endpoints de teste forem necessários em integração, usar a chave de config `Testing:EnableExceptionEndpoint` como padrão para não expor rotas artificiais em produção.
