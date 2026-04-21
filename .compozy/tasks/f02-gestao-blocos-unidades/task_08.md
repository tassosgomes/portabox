---
status: completed
title: GetEstruturaQuery + handler + DTOs (árvore completa)
type: backend
complexity: medium
dependencies:
  - task_03
  - task_04
---

# Task 08: GetEstruturaQuery + handler + DTOs (árvore completa)

## Overview
Implementa a query de leitura da estrutura completa do condomínio, retornando a árvore Condomínio → Blocos → Andares → Unidades em um único response JSON. Esta é a query que alimenta tanto a UI do síndico quanto o modo read-only do backoffice; seu contrato é consumido pelo `packages/api-client` (task_11) com cache TanStack Query.

> **Alinhamento com contrato:** os records C# `EstruturaDto`, `BlocoNodeDto`, `AndarNodeDto`, `UnidadeLeafDto` produzem JSON que DEVE bater exatamente com os schemas `Estrutura`, `BlocoNode`, `AndarNode`, `UnidadeLeaf` de [`api-contract.yaml`](../api-contract.yaml) (incluindo `geradoEm` no root). Ordenação semântica (alfabética bloco; numérica andar; numérica+sufixo em número) está documentada tanto no contrato quanto aqui — ambos devem evoluir juntos.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar `GetEstruturaQuery(Guid CondominioId, bool IncludeInactive) : IQuery<EstruturaDto>` em `Application/Estrutura/`
- MUST criar DTOs: `EstruturaDto(Guid CondominioId, string NomeFantasia, IReadOnlyList<BlocoNodeDto> Blocos, DateTime GeradoEm)`, `BlocoNodeDto(Guid Id, string Nome, bool Ativo, IReadOnlyList<AndarNodeDto> Andares)`, `AndarNodeDto(int Andar, IReadOnlyList<UnidadeLeafDto> Unidades)`, `UnidadeLeafDto(Guid Id, string Numero, bool Ativo)`
- MUST implementar `GetEstruturaQueryHandler : IQueryHandler<GetEstruturaQuery, EstruturaDto>` que:
  - Carrega `Condominio` (`NomeFantasia`) — se não encontrado, retorna `Result.Failure` (404)
  - Carrega blocos do condomínio via `IBlocoRepository.ListByCondominioAsync(condominioId, includeInactive, ct)`
  - Carrega unidades de cada bloco via `IUnidadeRepository.ListByBlocoAsync(blocoId, includeInactive, ct)` ou via uma consulta agregada otimizada (single SQL com JOIN)
  - Agrupa unidades por `Andar` em memória; ordena blocos alfabeticamente, andares numericamente crescente, unidades por `Andar` então `Numero` (com regra de ordenação semântica: `"101" < "101A" < "102"` — ordenação lexicográfica natural já atende este caso)
  - Retorna `EstruturaDto` com `GeradoEm = clock.UtcNow`
- MUST minimizar N+1 — usar projeção EF com `Select(...)` ou uma query ad-hoc que carrega blocos + unidades em uma round-trip, agrupando em memória
- MUST garantir que a projeção respeita filtros globais (tenant + soft-delete) — quando `IncludeInactive = true`, usar `.IgnoreQueryFilters()` deliberadamente e filtrar manualmente pelo `TenantId` para não vazar cross-tenant
- SHOULD documentar em xmldoc que a resposta é ordenada alfabeticamente por bloco, numericamente crescente por andar, e lexicograficamente por número (que naturalmente produz `101, 101A, 102`)
</requirements>

## Subtasks
- [x] 08.1 Criar `GetEstruturaQuery` e os 4 DTOs (Estrutura, BlocoNode, AndarNode, UnidadeLeaf)
- [x] 08.2 Implementar `GetEstruturaQueryHandler` com carga otimizada (single query com JOIN se possível)
- [x] 08.3 Garantir que `IncludeInactive=true` ainda respeita `tenant_id` (usar `.IgnoreQueryFilters()` + `.Where(e => e.TenantId == ctx.TenantId)` explicitamente)
- [x] 08.4 Implementar agrupamento e ordenação em memória (blocos → andares → unidades)
- [x] 08.5 Tratar `condominioId` inexistente ou de outro tenant → 404 via `Result.Failure`
- [x] 08.6 Escrever unit tests com repositórios mockados + snapshot de ordenação

## Implementation Details
Ver TechSpec seções **Implementation Design → Data Models** (shape dos DTOs) e **Data Flow C6** (fluxo de leitura do backoffice).

Ordenação semântica do `Numero`: como o formato é `^[0-9]{1,4}[A-Z]?$`, a ordenação lexicográfica naturalmente produz o resultado esperado para números de mesmo tamanho (`101 < 101A < 102`). Para números de tamanhos diferentes (`99 < 101` vs `"99" > "101"` lexicograficamente), aplicar ordenação customizada: primeiro por parte numérica (int), depois por sufixo alfabético. Implementar como Comparer em memória no handler.

Exemplo de projeção EF otimizada:
```csharp
var blocos = await dbContext.Set<Bloco>()
    .Where(b => b.CondominioId == query.CondominioId)
    .Select(b => new {
        Bloco = b,
        Unidades = dbContext.Set<Unidade>().Where(u => u.BlocoId == b.Id).ToList()
    })
    .ToListAsync(ct);
```

### Relevant Files
- `src/PortaBox.Modules.Gestao/Application/Estrutura/GetEstruturaQuery.cs` — novo
- `src/PortaBox.Modules.Gestao/Application/Estrutura/GetEstruturaQueryHandler.cs` — novo
- `src/PortaBox.Modules.Gestao/Application/Estrutura/EstruturaDto.cs` — novo (inclui todos os 4 DTOs)
- `src/PortaBox.Modules.Gestao/Application/Blocos/IBlocoRepository.cs` — consumo
- `src/PortaBox.Modules.Gestao/Application/Unidades/IUnidadeRepository.cs` — consumo
- `src/PortaBox.Modules.Gestao/Application/Tenants/ICondominioRepository.cs` — consumo
- `tests/PortaBox.Modules.Gestao.UnitTests/Application/Estrutura/GetEstruturaQueryHandlerTests.cs` — unit tests

### Dependent Files
- `EstruturaEndpoints` (task_09) chamará este handler para o endpoint `GET .../estrutura`
- `packages/api-client` (task_11) consome os DTOs via TypeScript generation manual
- Integration tests (task_10) validam payload completo

### Related ADRs
- [ADR-009: Endpoint Único Retornando Árvore Completa](adrs/adr-009.md) — motivação e contrato
- [ADR-002: Forma Canônica Estrita](adrs/adr-002.md) — garante numeração compatível com ordenação

## Deliverables
- `GetEstruturaQuery` + `GetEstruturaQueryHandler` + 4 DTOs
- Ordenação correta (alfabética bloco → numérica andar → semântica unidade)
- Suporte a `IncludeInactive` preservando isolamento de tenant
- Unit tests cobrindo agrupamento, ordenação e edge cases
- Unit tests with 80%+ coverage **(REQUIRED)**
- Integration tests para endpoint — cobertos em task_10

## Tests
- Unit tests:
  - [x] Caminho feliz: condomínio com 2 blocos, cada um com 3 andares e 2 unidades → retorna árvore completa ordenada
  - [x] Ordenação de blocos: "Torre B", "Bloco A", "Torre A" → ordem final: "Bloco A", "Torre A", "Torre B"
  - [x] Ordenação de andares: [3, 1, 10, 2] → [1, 2, 3, 10]
  - [x] Ordenação de unidades: ["102", "101A", "99", "101"] → ["99", "101", "101A", "102"] (semântica: parte numérica primeiro)
  - [x] `IncludeInactive=false` omite blocos e unidades inativos
  - [x] `IncludeInactive=true` inclui inativos **mas** não inclui itens de outro tenant (smoke via `TenantContext` scope)
  - [x] Condomínio inexistente → `Result.Failure("Condomínio não encontrado")` (404)
  - [x] Condomínio de outro tenant → `Result.Failure` (404; comportamento igual a inexistente para não vazar existência)
  - [x] `GeradoEm` preenchido com `clock.UtcNow`
- Integration tests:
  - [ ] Cobertos em task_10 (árvore real com seed de Bloco + Unidade no Postgres)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Payload ≤ 40 KB para condomínio de 300 unidades (medido em integration test)
- Tempo de execução do handler < 200ms em DB com 300 unidades (medido em integration test)
- Ordenação semântica consistente em todos os cenários de teste
