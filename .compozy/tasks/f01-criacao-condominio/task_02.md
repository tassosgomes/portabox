---
status: completed
title: Configuração base da API (Serilog JSON, ProblemDetails, versionamento, CORS)
type: backend
complexity: medium
dependencies:
  - task_01
---

# Task 02: Configuração base da API (Serilog JSON, ProblemDetails, versionamento, CORS)

## Overview
Configura o host ASP.NET Core com os transversais obrigatórios para produção: Serilog em JSON estruturado, `IExceptionHandler` global devolvendo Problem Details (RFC 7807), versionamento de rotas em `/api/v1`, CORS permissivo para dev e restrito em prod, e `IOptions<T>` bindado para as seções de configuração. Essa baseline é pré-requisito para qualquer endpoint real do projeto.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST configurar Serilog com sink Console em JSON estruturado (campos `timestamp`, `level`, `message`, `request_id`, `trace_id`, `span_id`)
- MUST registrar `IExceptionHandler` global que devolve Problem Details RFC 7807 com `type`, `title`, `status`, `detail`, `instance` e `traceId`
- MUST configurar versionamento de rotas via prefixo `/api/v1` (não query string)
- MUST configurar CORS: `AllowAnyOrigin()` em `Development`; origens específicas em `Production` (lido de `Cors:AllowedOrigins`)
- MUST expor Swagger/OpenAPI em `/swagger` apenas em `Development`
- MUST ler `appsettings.json`, `appsettings.{Environment}.json` e variáveis de ambiente, nessa ordem de precedência
- SHOULD registrar middleware de `request-id` que propaga `X-Request-Id` e adiciona ao log scope
</requirements>

## Subtasks
- [x] 02.1 Configurar Serilog JSON estruturado em `Program.cs`
- [x] 02.2 Implementar `ProblemDetailsExceptionHandler` e registrá-lo como `IExceptionHandler` global
- [x] 02.3 Configurar versionamento `/api/v1` (convenção de rota + prefixo global ou `ApiVersioning`)
- [x] 02.4 Configurar CORS por ambiente via `IOptions<CorsOptions>`
- [x] 02.5 Configurar Swagger/OpenAPI apenas em Development
- [x] 02.6 Implementar middleware de `X-Request-Id` + log scope

## Implementation Details
Seguir as orientações das skills `dotnet-dependency-config` (pacotes baseline, Options pattern) e `dotnet-observability` (Serilog JSON, scopes). Problem Details deve usar `ProblemDetailsFactory` do ASP.NET Core e adicionar `traceId` do `Activity.Current`. Arquivos `appsettings.*` ficam em `src/PortaBox.Api/`.

### Relevant Files
- `src/PortaBox.Api/Program.cs` — bootstrap da aplicação (a criar/editar)
- `src/PortaBox.Api/appsettings.json` — config base (a criar)
- `src/PortaBox.Api/appsettings.Development.json` — overrides dev (a criar)
- `src/PortaBox.Api/appsettings.Production.json` — overrides prod (a criar)
- `src/PortaBox.Api/Infrastructure/ProblemDetailsExceptionHandler.cs` — handler global (a criar)
- `src/PortaBox.Api/Infrastructure/RequestIdMiddleware.cs` — middleware X-Request-Id (a criar)
- `src/PortaBox.Api/Extensions/ServiceCollectionExtensions.cs` — registros DI (a criar)

### Dependent Files
- `src/PortaBox.Api/Controllers/*` — consumirão Problem Details e versionamento (tasks 18)
- Todos os handlers CQRS (tasks 12–17) dependem de exceção sendo capturada corretamente

### Related ADRs
- [ADR-005: Backoffice como SPA React Separado](../adrs/adr-005.md) — motiva CORS distinto entre origens do backoffice e painel do síndico.

## Deliverables
- `Program.cs` com Serilog, Problem Details, versionamento, CORS e Swagger configurados
- `appsettings.*` por ambiente
- `ProblemDetailsExceptionHandler` registrado globalmente
- Middleware de `X-Request-Id`
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para o bootstrap **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `ProblemDetailsExceptionHandler` captura `ValidationException` e devolve status 400 com corpo RFC 7807 contendo `errors`
  - [x] `ProblemDetailsExceptionHandler` captura `Exception` genérica e devolve status 500 com `traceId`
  - [x] `RequestIdMiddleware` propaga `X-Request-Id` do header de entrada; gera GUID quando ausente
- Integration tests:
  - [x] `GET /api/v1/healthz-nonexistent` retorna Problem Details 404 com `type`, `title`, `status`, `traceId`
  - [x] Endpoint que lança `InvalidOperationException` retorna Problem Details 500 sem expor stack trace em produção
  - [x] Logs gerados em uma request contêm `request_id` e `trace_id` no mesmo escopo
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Exceptions não tratadas sempre respondem Problem Details RFC 7807
- Logs em formato JSON estruturado com correlação por `request_id`
- Swagger UI operacional apenas em Development
- CORS configurável por ambiente via `IOptions`
