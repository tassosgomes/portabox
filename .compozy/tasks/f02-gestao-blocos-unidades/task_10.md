---
status: completed
title: Integration tests F02 (endpoints + soft-delete filter + cross-tenant + audit)
type: test
complexity: high
dependencies:
  - task_09
---

# Task 10: Integration tests F02 (endpoints + soft-delete filter + cross-tenant + audit)

## Overview
Cobre o comportamento real de F02 via `WebApplicationFactory` + Testcontainers Postgres + Respawner, exercitando todos os 9 endpoints, o global query filter de soft-delete, o isolamento cross-tenant e a materialização correta da auditoria em `TenantAuditEntry`. É a rede de segurança que garante que a stack inteira (API → handler → EF → Postgres) funciona em conjunto.

> **⚠️ Validação de contrato:** além dos testes funcionais, esta task MUST incluir validação automática de que a implementação aderir ao [`api-contract.yaml`](../api-contract.yaml) — via inspeção do `swagger.json` gerado pelo backend vs contrato de referência, OU via `schemathesis` rodando o contrato contra o host de teste. A meta é que qualquer drift entre contrato e implementação falhe no CI, não em produção.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar classe `EstruturaEndpointsTests` em `tests/PortaBox.Api.IntegrationTests/Features/Estrutura/` usando `PostgresDatabaseCollection` existente de F01
- MUST cobrir **cada um dos 9 endpoints** com pelo menos 1 caso feliz + 1 caso de erro
- MUST incluir classe `SoftDeleteFilterTests` validando que queries default omitem inativos e que `.IgnoreQueryFilters()` reverte o comportamento
- MUST incluir classe `CrossTenantIsolationTests` validando que síndico do tenant A não consegue (via 403 ou 404) acessar blocos/unidades do tenant B
- MUST incluir classe `AuditIntegrationTests` validando que cada operação cria exatamente 1 `TenantAuditEntry` com `EventKind` e `MetadataJson` esperados
- MUST seedar, em cada teste ou via helper, um tenant em `ativo` (não `pre-ativo`) + síndico + 1 bloco + 3 unidades para casos realistas
- MUST usar helper `TestAuthContext.SindicoOf(tenantId)` (ou equivalente) para simular autenticação; criar helper se não existir em F01
- MUST limpar DB entre testes via `Respawner` (padrão já estabelecido pela `PostgresDatabaseFixture` de F01)
- MUST adicionar classe `ContractConformanceTests` validando que a implementação não diverge do `api-contract.yaml`:
  - Opção A (preferida): chama `GET /swagger/v1/swagger.json` do backend em teste e compara paths, métodos, status codes e shape de response com o contrato de referência (deserializar ambos e fazer diff de estrutura — checar que cada path/método do contrato existe no swagger gerado e vice-versa)
  - Opção B: rodar `schemathesis` como processo filho contra o host de teste usando o contrato como spec (gera casos de teste fuzzy automaticamente)
  - Deve falhar o build em caso de qualquer diferença estrutural
- SHOULD incluir 1 teste de performance informal: seed de 300 unidades, chama `GET .../estrutura`, afirma duração < 500ms (informacional; não faz o build falhar)
</requirements>

## Subtasks
- [x] 10.1 Criar `TestAuthContext` helper para emitir JWT fake aceito pelo `WebApplicationFactory` (se não existir de F01)
- [x] 10.2 Criar `EstruturaEndpointsTests` cobrindo os 9 endpoints (feliz + erro)
- [x] 10.3 Criar `SoftDeleteFilterTests` validando query filter e bypass controlado
- [x] 10.4 Criar `CrossTenantIsolationTests` com 2 tenants e confirmar que tenant A não vê tenant B
- [x] 10.5 Criar `AuditIntegrationTests` validando audit entries por operação
- [x] 10.6 Criar `ContractConformanceTests` validando aderência de implementação ao `api-contract.yaml`
- [x] 10.7 Adicionar teste informal de performance (300 unidades, 500ms)

## Implementation Details
Ver TechSpec seção **Testing Approach → Integration Tests** para detalhamento dos casos. Usar `WebApplicationFactory<Program>` configurando um scheme de auth customizado que aceita JWTs forjados pelo `TestAuthContext`.

Exemplo de setup por teste:
```csharp
public class EstruturaEndpointsTests : IClassFixture<IntegrationTestWebAppFactory>
{
    [Fact]
    public async Task POST_bloco_como_sindico_retorna_201_e_atualiza_arvore()
    {
        var (factory, tenantId, sindicoId) = await SetupTenantWithSindico();
        var client = factory.CreateClient(TestAuthContext.SindicoOf(tenantId, sindicoId));
        var response = await client.PostAsJsonAsync(
            $"/api/v1/condominios/{condominioId}/blocos",
            new { nome = "Bloco A" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        // ...
    }
}
```

Para `CrossTenantIsolationTests`, seedear 2 tenants distintos; autenticar como síndico do tenant A e tentar acessar rota com `condominioId` do tenant B. Assertion: status 404 (para não vazar existência).

### Relevant Files
- `.compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml` — contrato de referência para `ContractConformanceTests`
- `tests/PortaBox.Api.IntegrationTests/Features/Estrutura/EstruturaEndpointsTests.cs` — novo
- `tests/PortaBox.Api.IntegrationTests/Features/Estrutura/SoftDeleteFilterTests.cs` — novo
- `tests/PortaBox.Api.IntegrationTests/Features/Estrutura/CrossTenantIsolationTests.cs` — novo
- `tests/PortaBox.Api.IntegrationTests/Features/Estrutura/AuditIntegrationTests.cs` — novo
- `tests/PortaBox.Api.IntegrationTests/Features/Estrutura/ContractConformanceTests.cs` — novo
- `tests/PortaBox.Api.IntegrationTests/Helpers/TestAuthContext.cs` — novo ou estendido
- `tests/PortaBox.Api.IntegrationTests/Fixtures/PostgresDatabaseFixture.cs` — reuso

### Dependent Files
- Nenhum; esta task é a cobertura final do backend
- Task 18 (smoke E2E) consome estes testes como gate antes de passar para frontend testing

### Related ADRs
- [ADR-007: Soft-Delete Padronizado](adrs/adr-007.md) — filter testado
- [ADR-008: Auditoria via EventKind + MetadataJson](adrs/adr-008.md) — verificação de metadados
- [ADR-009: Endpoint Único Retornando Árvore Completa](adrs/adr-009.md) — shape do payload

## Deliverables
- 4 classes de teste (EstruturaEndpoints, SoftDeleteFilter, CrossTenantIsolation, AuditIntegration)
- `TestAuthContext` helper funcional
- Cobertura de pelo menos 1 happy + 1 erro para cada endpoint
- Unit tests with 80%+ coverage **(REQUIRED)** — esta task é integração, mas o termo "unit tests" no checklist cobre o sentido de "testes automatizados" da feature
- Integration tests para F02 end-to-end **(REQUIRED)** — objeto central desta task

## Tests
- Unit tests:
  - [ ] N/A — esta task é de integration tests; unit cobertos em 01–09
- Integration tests:
  - [ ] `POST /blocos` como síndico retorna 201 + Location; `GET /estrutura` reflete
  - [ ] `POST /blocos` com nome duplicado entre ativos retorna 409 + `ProblemDetails.Title` claro
  - [ ] `PATCH /blocos/{id}` atualiza nome e cria audit entry `BlocoRenomeado` com `nomeAntes`/`nomeDepois`
  - [ ] `POST /blocos/{id}:inativar` oculta bloco em `GET /estrutura?includeInactive=false`; `?includeInactive=true` exibe
  - [ ] `POST /blocos/{id}:reativar` em bloco com conflito canônico retorna 409
  - [ ] `POST /unidades` com bloco inativo retorna 422
  - [ ] `POST /unidades` com tripla duplicada ativa retorna 409
  - [ ] `POST /unidades/{id}:inativar` + `POST /unidades/{id}:reativar` ciclo completo gera 2 audit entries
  - [ ] `GET /admin/condominios/{id}/estrutura` como operador retorna 200; como síndico retorna 403
  - [ ] `GET /condominios/{id}/estrutura` retornado para síndico do tenant A com `id` do tenant B retorna 404
  - [ ] Soft-delete filter: entidade inativa no DB não aparece em query default; aparece com `.IgnoreQueryFilters()`
  - [ ] Cross-tenant: 2 tenants seedados; síndico do tenant A não vê blocos/unidades do tenant B em nenhuma query
  - [ ] Audit: 1 operação de bloco gera exatamente 1 `TenantAuditEntry` com `EventKind` e `MetadataJson` esperados (parseáveis via `System.Text.Json`)
  - [ ] Contract conformance: cada path do `api-contract.yaml` existe no swagger gerado com o mesmo método e status codes; JSON response de um caso happy valida contra o schema declarado
  - [ ] Contract conformance: response de erro 409 tem shape `ProblemDetails` com `type`, `title`, `status`, `detail` e status code numérico bate com `status`
  - [ ] Performance informal: 300 unidades seedadas → `GET /estrutura` responde em < 500ms (informacional)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80% (contando coverage de F02 backend via CI)
- Nenhum falso positivo por estado residual entre testes (Respawner funcionando)
- Performance informal: 300 unidades < 500ms no endpoint de árvore
- Isolamento cross-tenant comprovado: nunca é possível vazar dados entre tenants

## Verification Notes
- `dotnet test tests/PortaBox.Api.IntegrationTests/PortaBox.Api.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~Features.Estrutura` -> Passed (`29` tests)
- `dotnet test tests/PortaBox.Api.IntegrationTests/PortaBox.Api.IntegrationTests.csproj --no-restore` -> Passed (`124` tests)
- `dotnet test PortaBox.sln --no-restore` -> Passed (`72` API unit + `133` Gestao unit + `124` integration)
