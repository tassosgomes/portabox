# TechSpec — F02: Gestão de Blocos e Unidades

> **Nível 3 da hierarquia de documentação.** Traduz `_prd.md` em decisões técnicas e plano de implementação. Este TechSpec estende o baseline arquitetural fixado em F01 (Clean Architecture + CQRS + EF Core + multi-tenant shared schema) com padrões novos que passam a valer para todas as features seguintes: **soft-delete padronizado** (ADR-007) e **baseline frontend TanStack Query + Context** (ADR-010). O próximo passo do pipeline é `cy-create-tasks`.

**Domínio:** D01 — Gestão do Condomínio
**Feature:** F02 — Gestão de Blocos e Unidades
**Última revisão:** 2026-04-18

---

## Executive Summary

F02 adiciona duas entidades centrais de D01 — `Bloco` e `Unidade` — ao módulo existente `PortaBox.Modules.Gestao` (ADR-006), sem criar novos projetos .NET. O desenho segue o baseline já estabelecido em F01: Clean Architecture em camadas, CQRS nativo (Commands/Queries + handlers explicitamente registrados), `AppDbContext` único com global query filters de tenant e soft-delete, outbox de eventos de domínio com dispatcher in-process pós-commit e ProblemDetails (RFC 7807) em erros. A novidade técnica é o par `ISoftDeletable` + `SoftDeleteableAggregetRoot` (ADR-007), que inaugura o padrão de inativação obrigatória reutilizado por F03 (Morador) e F06 (Dispositivo da Portaria).

O trade-off primário é a escolha por **uniformidade arquitetural sobre otimizações pontuais**: um único endpoint de leitura retorna a árvore completa (ADR-009), sem paginação nem lazy-loading — cabível para o volume do piloto (≤300 unidades ≈ 30 KB) e simples de invalidar via TanStack Query; soft-delete via reflection no `OnModelCreating` uniformiza o filtro em toda entidade que implementar a interface, evitando filtros esquecidos. Em contrapartida, tenants atípicos (1k+ unidades em fase futura) poderão exigir paginação, e o padrão de herança (`SoftDeleteableAggregateRoot`) restringe um pouco a flexibilidade de modelagem. O ganho de consistência vence em todos os cenários do MVP.

No frontend, F02 inaugura o baseline TanStack Query + React Context (ADR-010): toda feature subsequente fará `useQuery`/`useMutation` com query keys hierárquicas, cache automático e invalidação explícita. A árvore hierárquica é o único modo de visualização (PRD CF-07) e consome `GET /api/v1/condominios/{id}/estrutura`; mutações voltam por CRUD convencional (`POST`/`PATCH`) e disparam `invalidateQueries(['estrutura', condominioId])`. O design system `portabox-design` fornece tokens, tipografia e componentes base (Card, Button, Modal, Badge) já planejados em F01 task 18; F02 adiciona apenas os componentes específicos: `<EstruturaTree>`, `<BlocoNode>`, `<UnidadeLeaf>`, `<InativarConfirmModal>`.

---

## System Architecture

### Component Overview

| Componente | Responsabilidade | Tipo |
|---|---|---|
| `PortaBox.Modules.Gestao` — `Domain/Blocos/` | Agregado `Bloco` (entidade, value objects, eventos) | Novo no módulo |
| `PortaBox.Modules.Gestao` — `Domain/Unidades/` | Agregado `Unidade` (entidade, eventos) | Novo no módulo |
| `PortaBox.Domain.Abstractions.ISoftDeletable` | Marker interface para entidades com soft-delete | Novo |
| `PortaBox.Domain.SoftDeleteableAggregateRoot` | Classe base abstrata herdada por `Bloco`, `Unidade` (e futuras) | Novo |
| `PortaBox.Modules.Gestao` — `Application/Blocos/` | Commands, queries, handlers, validators de bloco | Novo |
| `PortaBox.Modules.Gestao` — `Application/Unidades/` | Commands, queries, handlers, validators de unidade | Novo |
| `PortaBox.Modules.Gestao` — `Application/Estrutura/` | Query agregadora `GetEstruturaQuery` (árvore completa) | Novo |
| `PortaBox.Modules.Gestao` — `Infrastructure/EfConfigurations/BlocoConfiguration` | EF mapping, índices, partial unique index | Novo |
| `PortaBox.Modules.Gestao` — `Infrastructure/EfConfigurations/UnidadeConfiguration` | EF mapping, FK, partial unique index canônico | Novo |
| `PortaBox.Infrastructure.Persistence.AppDbContext` | Extensão de `OnModelCreating` para aplicar soft-delete global filter via reflection | Modificado |
| `PortaBox.Api` — `Features/Estrutura/*` | Endpoints minimal API para CRUD e leitura de árvore | Novo |
| Migration `AddBlocoAndUnidade` | DDL das tabelas `bloco` e `unidade`; adiciona valores ao enum `EventKind` | Novo |
| `packages/ui` — `<Tree>`, `<TreeNode>`, `<ConfirmModal>` | Componentes genéricos de árvore (reuso por features futuras) | Novo |
| `packages/api-client` | Client HTTP tipado + query keys helper (baseline) | Novo |
| `apps/sindico` — `pages/estrutura/*` | Telas de gerência da estrutura do síndico | Novo |
| `apps/backoffice` — `pages/tenants/{id}/estrutura` | Visualização read-only cross-tenant para operador | Novo |

### Data Flow — Principais Cenários

**C1. Cadastrar primeiro bloco** (síndico autenticado em `apps/sindico`)
1. Síndico clica em "Cadastrar primeiro bloco" no empty state da árvore; modal abre com form `<NovoBlocoForm>`.
2. Submit chama `useMutation(criarBloco)` com payload `{ nome }`.
3. `POST /api/v1/condominios/{condominioId}/blocos` → handler `CreateBlocoCommandHandler` valida (FluentValidation: nome 1–50 caracteres, único no tenant entre ativos), cria `Bloco` via factory, persiste, adiciona evento `BlocoCriadoV1` no outbox e audit entry `EventKind.BlocoCriado`.
4. Resposta 201 com `{ id, nome, ativo, condominioId }`.
5. Frontend invalida `queryKeys.estrutura(condominioId)`; TanStack Query refaz `GET .../estrutura` e a árvore renderiza o novo bloco.
6. `DomainEventOutboxInterceptor` despacha `BlocoCriadoV1` in-process após commit (sem handler consumer no MVP; pronto para F07/F09).

**C2. Cadastrar unidade em bloco existente**
1. Síndico seleciona bloco ativo na árvore, clica em "Adicionar unidade".
2. Form pede andar (int não-negativo) + número (string `^[0-9]{1,4}[A-Z]?$`).
3. `POST /api/v1/condominios/{id}/blocos/{blocoId}/unidades` → `CreateUnidadeCommandHandler` valida (canonical form, regex, bloco existe e ativo), checa unicidade da tripla `(bloco_id, andar, número)` entre unidades ativas do tenant (via `IUnidadeRepository.ExistsActiveAsync`), persiste, adiciona audit entry `EventKind.UnidadeCriada`, emite `UnidadeCriadaV1`.
4. Resposta 201 com `{ id, blocoId, andar, numero, ativo }`.
5. Optimistic update no frontend insere a unidade na árvore local; invalida cache após sucesso; refetch silencioso sincroniza.

**C3. Renomear bloco**
1. Síndico clica em "Editar" no bloco; modal pede novo nome.
2. `PATCH /api/v1/condominios/{id}/blocos/{blocoId}` com `{ nome }`.
3. `RenameBlocoCommandHandler` valida (novo nome distinto do atual, único entre ativos), chama `Bloco.Rename(novoNome, porUserId, agoraUtc)` que atualiza o nome e adiciona evento `BlocoRenomeadoV1` com diff.
4. Audit entry `EventKind.BlocoRenomeado` com `metadata: { nomeAntes, nomeDepois }`.
5. 200 com bloco atualizado; frontend invalida árvore.

**C4. Inativar unidade**
1. Síndico clica em "Inativar" na unidade; modal `<InativarConfirmModal>` explica impactos (moradores associados continuam vinculados mas unidade some do cadastro operacional).
2. `POST /api/v1/condominios/{id}/blocos/{blocoId}/unidades/{unidadeId}:inativar` → handler chama `Unidade.Inativar(porUserId, agoraUtc)` da base class (valida `Ativo == true`).
3. Audit entry `EventKind.UnidadeInativada`; evento `UnidadeInativadaV1`.
4. 200 com unidade (agora `ativo: false`); frontend invalida árvore; toggle de incluir inativos oferece visibilidade.

**C5. Reativar unidade com conflito canônico**
1. Síndico tenta reativar unidade inativa `(bloco_id=A, andar=2, numero=201)`.
2. Handler verifica se existe unidade ativa com mesma tripla → encontra; retorna `Result.Failure("Já existe unidade ativa para Bloco A / Andar 2 / Apto 201; inative-a antes de reativar esta")`.
3. Backend responde 409 (Conflict) com `ProblemDetails` explicativo; frontend exibe toast com a mensagem.

**C6. Leitura da estrutura (árvore) no backoffice**
1. Operador seleciona tenant no dropdown; rota `/tenants/{id}/estrutura` é ativada.
2. `useQuery(['estrutura-admin', condominioId], ...)` dispara `GET /api/v1/admin/condominios/{id}/estrutura?tenantId={id}`.
3. Handler aceita a rota pois role é `Operator`; `ITenantContext.BeginScope(tenantId)` é aplicado explicitamente no handler (não pelo middleware).
4. Resposta idêntica ao endpoint do síndico; frontend renderiza árvore em modo read-only (componentes sem botões).

**C7. Consumo por F04 (via F07 API interna — quando F04 for implementado)**
1. F04 parseia planilha; para cada linha `(bloco_nome, andar, numero, morador...)`:
2. Chama `IUnidadeRepository.FindActiveByCanonicalAsync(tenantId, blocoNome, andar, numero)` (internal API, não é endpoint público).
3. Se `null`: adiciona linha ao relatório de erros com razão `"Unidade {nome} / Andar {x} / Apto {y} não existe; cadastre em F02 antes de reimportar"`.
4. Se encontrada: usa `unidadeId` para criar o morador.

### External System Interactions

Nenhuma. F02 é inteiramente interno ao monolito; consome apenas PostgreSQL (compartilhado com F01) e opera sobre a árvore in-memory no handler. Sem chamadas HTTP externas, sem mensageria externa (o outbox é local e apenas marca `PublishedAt` no MVP — pronto para RabbitMQ futuramente conforme ADR-009 de F01).

---

## Implementation Design

### Core Interfaces

Exemplos em C# (baseline .NET do projeto). Limitados a 20 linhas cada.

**Marker de soft-delete e classe base:**

```csharp
// PortaBox.Domain.Abstractions/ISoftDeletable.cs
public interface ISoftDeletable
{
    bool Ativo { get; }
    DateTime? InativadoEm { get; }
    Guid? InativadoPor { get; }
}

// PortaBox.Domain/SoftDeleteableAggregateRoot.cs
public abstract class SoftDeleteableAggregateRoot : AggregateRoot, ISoftDeletable
{
    public bool Ativo { get; protected set; } = true;
    public DateTime? InativadoEm { get; protected set; }
    public Guid? InativadoPor { get; protected set; }

    protected Result Inativar(Guid porUserId, DateTime agoraUtc) { /* guard + set + return */ }
    protected Result Reativar(Guid porUserId, DateTime agoraUtc) { /* guard + set + return */ }
}
```

**Entidade `Bloco`:**

```csharp
// PortaBox.Modules.Gestao/Domain/Blocos/Bloco.cs
public sealed class Bloco : SoftDeleteableAggregateRoot, ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CondominioId { get; private set; }
    public string Nome { get; private set; } = default!;
    public DateTime CriadoEm { get; private set; }
    public Guid CriadoPor { get; private set; }

    public static Result<Bloco> Create(Guid id, Guid tenantId, Guid condominioId,
        string nome, Guid porUserId, IClock clock) { /* valida + cria + evento */ }

    public Result Rename(string novoNome, Guid porUserId, DateTime agoraUtc) { /* ... */ }
    public new Result Inativar(Guid porUserId, DateTime agoraUtc) { /* delega + evento */ }
    public new Result Reativar(Guid porUserId, DateTime agoraUtc) { /* delega + evento */ }
}
```

**Entidade `Unidade`:**

```csharp
// PortaBox.Modules.Gestao/Domain/Unidades/Unidade.cs
public sealed class Unidade : SoftDeleteableAggregateRoot, ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BlocoId { get; private set; }
    public int Andar { get; private set; }
    public string Numero { get; private set; } = default!; // normalizado maiúscula
    public DateTime CriadoEm { get; private set; }
    public Guid CriadoPor { get; private set; }

    public static Result<Unidade> Create(Guid id, Guid tenantId, Bloco bloco,
        int andar, string numero, Guid porUserId, IClock clock) { /* valida + cria + evento */ }

    // Sem método Rename (imutável por design — ADR-003)
    public new Result Inativar(Guid porUserId, DateTime agoraUtc) { /* delega + evento */ }
    public new Result Reativar(Guid porUserId, DateTime agoraUtc) { /* delega + evento */ }
}
```

**Repository contract:**

```csharp
// PortaBox.Modules.Gestao/Application/Blocos/IBlocoRepository.cs
public interface IBlocoRepository
{
    Task<Bloco?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Bloco?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsActiveWithNameAsync(Guid condominioId, string nome, CancellationToken ct);
    Task<IReadOnlyList<Bloco>> ListByCondominioAsync(Guid condominioId, bool includeInactive, CancellationToken ct);
    Task AddAsync(Bloco bloco, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

// Análogo para IUnidadeRepository com FindActiveByCanonicalAsync(tenantId, blocoId, andar, numero)
```

**Command/Query exemplos:**

```csharp
// Application/Blocos/CreateBlocoCommand.cs
public sealed record CreateBlocoCommand(Guid CondominioId, string Nome) : ICommand<BlocoDto>;

// Application/Estrutura/GetEstruturaQuery.cs
public sealed record GetEstruturaQuery(Guid CondominioId, bool IncludeInactive) : IQuery<EstruturaDto>;

public sealed record EstruturaDto(Guid CondominioId, string NomeFantasia,
    IReadOnlyList<BlocoNodeDto> Blocos, DateTime GeradoEm);
public sealed record BlocoNodeDto(Guid Id, string Nome, bool Ativo, IReadOnlyList<AndarNodeDto> Andares);
public sealed record AndarNodeDto(int Andar, IReadOnlyList<UnidadeLeafDto> Unidades);
public sealed record UnidadeLeafDto(Guid Id, string Numero, bool Ativo);
```

### Data Models

**Tabela `bloco`:**

| Coluna | Tipo | Constraints |
|---|---|---|
| `id` | `uuid` | PK, fornecido pela aplicação (GUID v4) |
| `tenant_id` | `uuid` | NOT NULL, FK → `condominio.id` via conv.; indexado |
| `condominio_id` | `uuid` | NOT NULL, FK → `condominio.id` (ON DELETE RESTRICT) |
| `nome` | `varchar(50)` | NOT NULL |
| `ativo` | `boolean` | NOT NULL DEFAULT true |
| `inativado_em` | `timestamptz` | NULL |
| `inativado_por` | `uuid` | NULL, FK → `aspnetusers.id` |
| `criado_em` | `timestamptz` | NOT NULL |
| `criado_por` | `uuid` | NOT NULL, FK → `aspnetusers.id` |

Índices:
- `pk_bloco` — primary key `(id)`.
- `idx_bloco_condominio` — `(condominio_id)`.
- `idx_bloco_nome_ativo_unique` — `CREATE UNIQUE INDEX ... ON bloco (tenant_id, condominio_id, nome) WHERE ativo = true;`

**Tabela `unidade`:**

| Coluna | Tipo | Constraints |
|---|---|---|
| `id` | `uuid` | PK |
| `tenant_id` | `uuid` | NOT NULL, indexado |
| `bloco_id` | `uuid` | NOT NULL, FK → `bloco.id` (ON DELETE RESTRICT) |
| `andar` | `int` | NOT NULL, CHECK (andar >= 0) |
| `numero` | `varchar(5)` | NOT NULL; regex `^[0-9]{1,4}[A-Z]?$` aplicado na aplicação |
| `ativo` | `boolean` | NOT NULL DEFAULT true |
| `inativado_em` | `timestamptz` | NULL |
| `inativado_por` | `uuid` | NULL |
| `criado_em` | `timestamptz` | NOT NULL |
| `criado_por` | `uuid` | NOT NULL |

Índices:
- `pk_unidade` — primary key `(id)`.
- `idx_unidade_bloco` — `(bloco_id)`.
- `idx_unidade_canonica_ativa` — `CREATE UNIQUE INDEX ... ON unidade (tenant_id, bloco_id, andar, numero) WHERE ativo = true;`

**Alteração em `TenantAuditEntry.EventKind`** (enum via `HasConversion<short>()`): adicionar `BlocoCriado=5`, `BlocoRenomeado=6`, `BlocoInativado=7`, `BlocoReativado=8`, `UnidadeCriada=9`, `UnidadeInativada=10`, `UnidadeReativada=11`. Nenhuma alteração de schema na coluna (`smallint` continua válido).

**DTOs da API** (resposta padrão):

```csharp
public sealed record BlocoDto(Guid Id, Guid CondominioId, string Nome, bool Ativo, DateTime? InativadoEm);
public sealed record UnidadeDto(Guid Id, Guid BlocoId, int Andar, string Numero, bool Ativo, DateTime? InativadoEm);
public sealed record RenameBlocoRequest(string Nome);
public sealed record CreateBlocoRequest(string Nome);
public sealed record CreateUnidadeRequest(int Andar, string Numero);
```

### API Endpoints

Todos sob `/api/v1`; response de sucesso é `application/json`; erros seguem RFC 7807 (`application/problem+json`).

**Síndico (role `Sindico`, tenant resolvido via middleware):**

| Método | Path | Request | Response | Status |
|---|---|---|---|---|
| `GET` | `/condominios/{condominioId}/estrutura?includeInactive={bool}` | — | `EstruturaDto` | 200 |
| `POST` | `/condominios/{condominioId}/blocos` | `CreateBlocoRequest` | `BlocoDto` | 201 (+`Location`) |
| `PATCH` | `/condominios/{condominioId}/blocos/{blocoId}` | `RenameBlocoRequest` | `BlocoDto` | 200 |
| `POST` | `/condominios/{condominioId}/blocos/{blocoId}:inativar` | — | `BlocoDto` | 200 |
| `POST` | `/condominios/{condominioId}/blocos/{blocoId}:reativar` | — | `BlocoDto` | 200 |
| `POST` | `/condominios/{condominioId}/blocos/{blocoId}/unidades` | `CreateUnidadeRequest` | `UnidadeDto` | 201 |
| `POST` | `/condominios/{condominioId}/blocos/{blocoId}/unidades/{unidadeId}:inativar` | — | `UnidadeDto` | 200 |
| `POST` | `/condominios/{condominioId}/blocos/{blocoId}/unidades/{unidadeId}:reativar` | — | `UnidadeDto` | 200 |

**Operador (role `Operator`, tenant explícito na rota admin):**

| Método | Path | Request | Response | Status |
|---|---|---|---|---|
| `GET` | `/admin/condominios/{condominioId}/estrutura?includeInactive={bool}` | — | `EstruturaDto` | 200 |

**Erros comuns:**

- 400 Bad Request — validação FluentValidation (campos).
- 401 Unauthorized — sem autenticação.
- 403 Forbidden — síndico tentando acessar tenant alheio; operador sem role.
- 404 Not Found — recurso inexistente.
- 409 Conflict — violação de unicidade canônica; conflito em reativação; tentativa de criar unidade em bloco inativo.
- 422 Unprocessable Entity — já ativo / já inativo em operação de inativar/reativar.

---

## Integration Points

F02 não integra com sistemas externos no MVP, mas estabelece contratos internos críticos:

- **F07 (API Interna de Busca)**: consumida por F03 e F04 para validar unidade canônica e buscar moradores. F02 fornece a implementação subjacente via `IUnidadeRepository.FindActiveByCanonicalAsync` + `IBlocoRepository.GetByNameAsync`. F07 será formalizada em seu próprio PRD/TechSpec; F02 já expõe as abstrações internas.
- **Design system `portabox-design`**: obrigatório (ADR-010 de F01). Antes de qualquer implementação frontend, invocar skill `portabox-design` para carregar tokens, tipografia, ícones Lucide. Componentes de F02 usam `<Card>`, `<Button>`, `<Modal>`, `<Badge>` do `packages/ui` (criados em F01 task 18).
- **Outbox de eventos**: F02 adiciona 7 tipos de evento (`BlocoCriadoV1`, `BlocoRenomeadoV1`, `BlocoInativadoV1`, `BlocoReativadoV1`, `UnidadeCriadaV1`, `UnidadeInativadaV1`, `UnidadeReativadaV1`). Sem consumer in-process no MVP — persistidos para consumo futuro por F09 e eventuais workers do Mastra (contexto D04).

---

## Impact Analysis

| Componente | Tipo | Descrição e Risco | Ação Requerida |
|---|---|---|---|
| `PortaBox.Domain.Abstractions` | Novo | Interface `ISoftDeletable` adicionada; nenhum consumidor existente afetado | Adicionar arquivo |
| `PortaBox.Domain` | Novo | Classe base `SoftDeleteableAggregateRoot` adicionada | Adicionar arquivo |
| `PortaBox.Modules.Gestao` (módulo) | Modificado | Adição de agregados `Bloco` e `Unidade`, respectivos handlers, EF configs, repositórios. Nenhuma alteração em código existente de F01 | Extensão em pastas novas (`Domain/Blocos`, `Domain/Unidades`, `Application/Blocos`, `Application/Unidades`, `Application/Estrutura`) |
| `PortaBox.Modules.Gestao.DependencyInjection` | Modificado | Registra validators, handlers e repositórios novos | Estender método `AddPortaBoxModuleGestao` |
| `PortaBox.Infrastructure.AppDbContext` | Modificado | Extensão do `OnModelCreating` com reflection para aplicar global filter `ISoftDeletable`; adição de `DbSet<Bloco>` e `DbSet<Unidade>` | Diff focado; cobrir com unit test de OnModelCreating |
| `PortaBox.Modules.Gestao.Domain.TenantAuditEntry.EventKind` | Modificado (não-breaking) | Adiciona valores 5–11 ao enum. Risco: consumidores existentes que fazem `switch` sem `default` podem perder casos | Auditar `switch` em F01; adicionar `default` onde necessário |
| Migration `20260418000000_AddBlocoAndUnidade` | Novo | DDL + partial unique indexes + FK constraints | Gerar via `dotnet ef migrations add` |
| `PortaBox.Api` — routes `Features/Estrutura/*.cs` | Novo | 8 endpoints novos (7 síndico + 1 admin operador) | Criar arquivo de extensão `EstruturaEndpoints.cs` mapeado em `Program.cs` |
| `PortaBox.Api.Program.cs` | Modificado | Chamada a `app.MapEstruturaEndpoints()` no route group `/api/v1` | Uma linha |
| `packages/ui` | Modificado | Adiciona componentes `<Tree>`, `<TreeNode>`, `<ConfirmModal>` | Criar arquivos; garantir aderência ao design system |
| `packages/api-client` | Novo | Primeira versão do HTTP client tipado + query keys | Criar `packages/api-client/package.json` + fontes |
| `apps/sindico/src/features/estrutura/*` | Novo | Telas `EstruturaPage`, `BlocoForm`, `UnidadeForm`, `InativarConfirmModal`; hooks `useEstrutura`, `useCriarBloco`, etc. | Novas pastas |
| `apps/backoffice/src/features/tenants/{id}/estrutura/*` | Novo | Versão read-only da mesma tela (componentes reutilizados sem botões de ação) | Novas pastas |
| `tests/PortaBox.Modules.Gestao.UnitTests` | Modificado | Adiciona testes de `Bloco`, `Unidade`, `SoftDeleteableAggregateRoot`; não altera arch tests | Novos arquivos |
| `tests/PortaBox.Api.IntegrationTests` | Modificado | Adiciona cobertura de endpoints de F02 e do filtro de soft-delete em queries | Novos arquivos de test class |

---

## Testing Approach

### Unit Tests (xUnit + FluentAssertions, no módulo `PortaBox.Modules.Gestao.UnitTests`)

**Alvo**: regras de negócio e comportamento de agregados.

- `SoftDeleteableAggregateRootTests`: `Inativar` em estado ativo → sucesso; em estado inativo → `Result.Failure`. `Reativar` análogo. Campos `Ativo`, `InativadoEm`, `InativadoPor` atualizados consistentemente.
- `BlocoTests`:
  - `Create` com nome válido → sucesso + evento `BlocoCriadoV1`.
  - `Create` com nome vazio / > 50 chars → falha.
  - `Rename` para o mesmo nome → falha ("novo nome igual ao atual").
  - `Rename` em bloco inativo → falha ("não é possível renomear bloco inativo").
  - `Inativar` → evento `BlocoInativadoV1`; `Reativar` análogo.
- `UnidadeTests`:
  - `Create` com número inválido (`1AB`, `20000`, minúscula, com espaços) → falha.
  - `Create` com bloco inativo → falha.
  - `Create` em andar negativo → falha (CHECK lógico na aplicação).
  - Eventos emitidos em cada operação.
- `CreateBlocoCommandHandlerTests` (com mock de `IBlocoRepository`):
  - Caminho feliz: cria, chama audit, chama outbox, retorna DTO.
  - Nome duplicado entre ativos: retorna `Result.Failure`; nenhum efeito colateral.
- `CreateUnidadeCommandHandlerTests`:
  - Caminho feliz.
  - Tripla canônica já ativa: falha 409.
  - Bloco inexistente ou inativo: falha 404/422.
- `GetEstruturaQueryHandlerTests`: agrupamento correto por bloco e por andar; ordenação alfabética de blocos; ordenação numérica de unidades (com sufixo alfabético — `101`, `101A`, `102`).

**Mocking**:
- Repositórios mockados com NSubstitute (mantendo convenção de F01 se aplicável; caso F01 use Moq, seguir padrão existente).
- `IClock` real (fake com horário fixo) para determinismo.
- `IAuditService` real contra `InMemoryAuditWriter` para verificar chamadas.

### Integration Tests (xUnit + Testcontainers Postgres + Respawner, em `PortaBox.Api.IntegrationTests`)

**Alvo**: comportamento real do stack via `WebApplicationFactory`.

- `EstruturaEndpointsTests`:
  - `POST /blocos` como síndico → 201; GET da árvore reflete.
  - `POST /blocos` com nome duplicado → 409.
  - `PATCH /blocos/{id}` com novo nome → 200; audit registrada com `EventKind.BlocoRenomeado`.
  - `POST /blocos/{id}:inativar` → 200; GET da árvore com `includeInactive=false` oculta; com `=true` mostra.
  - `POST /blocos/{id}:reativar` em bloco com conflito canônico → 409.
  - Cross-tenant isolation: síndico do tenant A não acessa blocos do tenant B (403 ou 404 — decidir no handler; preferir 404 para não vazar existência).
- `UnidadeEndpointsTests`: análogo.
- `SoftDeleteFilterTests`:
  - Com entidade inativa no banco, `IBlocoRepository.GetByIdAsync` retorna `null` (filter aplicado); `GetByIdIncludingInactiveAsync` retorna a entidade.
  - Query de listagem padrão omite inativos; flag `IgnoreQueryFilters` em admin retorna todos.
- `AuditIntegrationTests`:
  - Cada operação cria exatamente uma `TenantAuditEntry` com payload esperado.
  - `MetadataJson` é JSON válido com campos documentados.

**Setup**:
- Base fixture `PostgresDatabaseFixture` (já existente em F01) reusada sem alterações.
- Seed de um tenant `pre-ativo` + síndico `Sindico` com role claim; seed de um operador `Operator`.
- Autenticação via helper (a ser confirmado pelo state de F01 — se não existir, F02 cria helper `TestAuthContext.SindicoOf(tenantId)` que emite JWT fake aceito pelo `TestAuthenticationSchemeProvider` configurado em `WebApplicationFactory`).

### Frontend Tests (Vitest + React Testing Library — opcional no MVP)

- Render do `<EstruturaTree>` com mock de response da API; expand/collapse funcionam.
- Submit do `<BlocoForm>` chama mutation e invalida query; verificar via `queryClient.getQueryCache()`.
- Modo read-only no backoffice esconde botões de ação.

No MVP, priorizar unit + integration backend. Frontend tests podem entrar na Phase 2 se bandwidth permitir.

---

## Development Sequencing

### Build Order

Cada passo declara explicitamente suas dependências. Passos sem dependência no mesmo número podem ser paralelizados.

1. **Infra de soft-delete (Domain)** — criar `ISoftDeletable` e `SoftDeleteableAggregateRoot` em `PortaBox.Domain.Abstractions` / `PortaBox.Domain`. *Depende de:* nada.
2. **Infra de soft-delete (Persistence)** — estender `AppDbContext.OnModelCreating` com reflection para aplicar global filter em entidades `ISoftDeletable`. Unit test que verifica que uma entidade fake `ISoftDeletable` fica filtrada. *Depende de:* passo 1.
3. **Entidade `Bloco`** — criar `Bloco.cs`, eventos `BlocoCriadoV1`, `BlocoRenomeadoV1`, `BlocoInativadoV1`, `BlocoReativadoV1`. *Depende de:* passo 1.
4. **EF Configuration `BlocoConfiguration`** — mapping, índices (incluindo partial unique index). *Depende de:* passos 2 e 3.
5. **Repositório `IBlocoRepository` + `BlocoRepository`** — implementação concreta em Infrastructure. *Depende de:* passo 4.
6. **Entidade `Unidade`** — `Unidade.cs`, eventos `Unidade*V1`. *Depende de:* passo 1 (não depende de `Bloco` em compile-time porque a FK é por ID).
7. **EF Configuration `UnidadeConfiguration`** — mapping + partial unique index canônico. *Depende de:* passos 2 e 6.
8. **Repositório `IUnidadeRepository` + `UnidadeRepository`** — incluir `FindActiveByCanonicalAsync` (consumido por F07 futuro). *Depende de:* passo 7.
9. **Extensão do enum `TenantAuditEntry.EventKind`** — adicionar valores 5–11. *Depende de:* nada explícito, mas aplicar junto com passo 10 para agrupar migration.
10. **Migration EF** — `AddBlocoAndUnidade`: DDL + FKs + partial unique indexes + atualização do enum. Rodar `dotnet ef migrations add` e revisar SQL gerado para garantir partial indexes (EF gera como migration custom). *Depende de:* passos 4, 7, 9.
11. **`IAuditService` (extensão)** — método `RecordStructuralAsync(kind, metadata, ct)` que adiciona `TenantAuditEntry` no contexto. *Depende de:* passo 9.
12. **Commands + Handlers — Blocos**: `CreateBlocoCommand`, `RenameBlocoCommand`, `InativarBlocoCommand`, `ReativarBlocoCommand` e respectivos handlers. FluentValidation validators. *Depende de:* passos 5, 11.
13. **Commands + Handlers — Unidades**: `CreateUnidadeCommand`, `InativarUnidadeCommand`, `ReativarUnidadeCommand` e handlers. *Depende de:* passos 8, 11.
14. **Query `GetEstruturaQuery` + handler** — agrupamento em memória. *Depende de:* passos 5, 8.
15. **Registro em DI** — estender `PortaBox.Modules.Gestao.DependencyInjection` com todos os handlers, validators, repositórios. *Depende de:* passos 12, 13, 14.
16. **Endpoints `EstruturaEndpoints.cs`** — mapeamento das 8 rotas em minimal API. *Depende de:* passo 15.
17. **Testes unitários de domínio e handlers** — cobertura conforme seção Testing Approach. *Depende de:* passos 1–14.
18. **Testes de integração dos endpoints** — base fixture, seed, casos de isolamento cross-tenant. *Depende de:* passos 16, 17.
19. **Package `packages/api-client`** — `http.ts`, `queryKeys.ts`, módulos tipados (`estrutura.ts`, `blocos.ts`, `unidades.ts`). *Depende de:* passo 16 (contratos consolidados).
20. **Componentes de árvore em `packages/ui`** — `<Tree>`, `<TreeNode>`, `<ConfirmModal>`. *Depende de:* tokens e componentes base do `portabox-design` (F01 task 18) já estarem prontos.
21. **Setup TanStack Query em `apps/sindico`** — `QueryClient`, `QueryClientProvider`, devtools em dev. *Depende de:* passo 19.
22. **Feature `estrutura` em `apps/sindico`** — página, hooks, formulários. *Depende de:* passos 19, 20, 21.
23. **Setup TanStack Query em `apps/backoffice`** — espelho do setup do síndico. *Depende de:* passo 19.
24. **Feature `estrutura` read-only em `apps/backoffice`** — reusa componentes, oculta ações. *Depende de:* passos 20, 23.
25. **Smoke test end-to-end do piloto** — operador cria tenant (F01), síndico recebe magic link, define senha, cadastra 1 bloco e 3 unidades, renomeia bloco, inativa unidade. Verifica audit e árvore. *Depende de:* passo 24.

### Technical Dependencies

- **F01 em estado avançado**: específico — tasks 14, 15 (endpoints minimal API), 18 (packages/ui com Button/Card/Modal/Badge), 20 (Vite baseline em `apps/sindico` e `apps/backoffice`) precisam estar concluídas antes dos passos 20–24 de F02.
- **Postgres ≥ 16**: partial unique indexes são padrão desde versões antigas; sem bloqueio.
- **Design system `portabox-design`** v1 publicado na skill — invocar antes de qualquer frontend.
- **Node ≥ 20 LTS** para workspaces do pnpm.
- **.NET 8 SDK** conforme F01.

---

## Monitoring and Observability

Operações de F02 ficam sob a infraestrutura de logging e métricas já implementada em F01 (OpenTelemetry OTLP; Serilog estruturado; `TraceId` propagado). Adições específicas de F02:

**Logs estruturados** — cada handler loga com campos:

- `event` — ex.: `"bloco.criado"`, `"unidade.inativada"`.
- `tenant_id`, `condominio_id`, `bloco_id`, `unidade_id` (quando aplicável).
- `performed_by_user_id`.
- `outcome` — `"success"`, `"validation_failure"`, `"conflict"`.
- `duration_ms` — tempo total do handler.

**Métricas Prometheus/OTel** (sugeridas; detalhar na task de observabilidade):

- `portabox_estrutura_operations_total{operation, outcome}` — contador por operação e resultado.
- `portabox_estrutura_tree_fetch_duration_seconds` — histograma para o endpoint de árvore.
- `portabox_estrutura_unidades_ativas_gauge{tenant_id}` — gauge atualizado a cada mutação (útil para dashboards por tenant).

**Alertas** — nenhum específico no MVP. Em Phase 2, alertar se `portabox_estrutura_tree_fetch_duration_seconds` p95 > 500ms em janela de 5 min (sinal de volume crescendo).

**Auditoria humana** — `TenantAuditEntry` é a fonte canônica para "o que aconteceu neste tenant". Consulta via admin backoffice em endpoint específico (fora do escopo de F02 — pertence a feature futura de audit viewer).

---

## Technical Considerations

### Key Decisions

- **Decision**: Reutilizar `PortaBox.Modules.Gestao`. **Rationale**: D01 é um único bounded context; fragmentar prematuramente é YAGNI. **Trade-off**: módulo cresce com o tempo. **Rejected**: novo módulo `Modules.Estrutura`. *(ADR-006)*
- **Decision**: Soft-delete padronizado via `ISoftDeletable` + `SoftDeleteableAggregateRoot` + global filter por reflection. **Rationale**: primeira feature a codificar o padrão; reuso garantido em F03 e F06. **Trade-off**: herança introduz restrição em casos de modelagem atípica (sem impacto no MVP). **Rejected**: campos inline por entidade; tabela externa de deleções. *(ADR-007)*
- **Decision**: Auditoria via extensão de `TenantAuditEntry.EventKind` + `MetadataJson`. **Rationale**: mantém auditoria centralizada em tabela única; backoffice já tem pattern para consumir. **Trade-off**: consultas de diff parseiam JSONB. **Rejected**: tabela separada; confiar apenas no outbox. *(ADR-008)*
- **Decision**: `GET /api/v1/condominios/{id}/estrutura` retornando árvore completa. **Rationale**: ≤30KB para 300 unidades; cache trivial. **Trade-off**: tenants atípicos futuros podem precisar de lazy loading. **Rejected**: endpoints por bloco/andar; GraphQL. *(ADR-009)*
- **Decision**: TanStack Query + React Context como baseline frontend. **Rationale**: maturidade e recursos (cache, optimistic, devtools). **Trade-off**: +10 KB de bundle e curva de aprendizado. **Rejected**: SWR, Zustand, raw fetch, RTK Query. *(ADR-010)*
- **Decision**: Unique constraint canônica via **partial unique index no Postgres** (`WHERE ativo = true`). **Rationale**: permite reinserção da mesma tripla após inativação; DB garante integridade mesmo em race condition. **Trade-off**: comportamento específico do Postgres (não portável para MySQL sem adaptação — não é problema dado a stack fixa).
- **Decision**: Número do apartamento como `varchar(5)` (não `int`). **Rationale**: ADR-002 (produto) exige sufixo alfabético. **Trade-off**: ordenação default é lexicográfica; handler faz ordenação semântica em memória para a árvore.

### Known Risks

| Risco | Probabilidade | Mitigação |
|---|---|---|
| Reflection de `OnModelCreating` não aplica filter em entidade nova por erro de tipo (ex.: entidade usa `ISoftDeletable` da Infrastructure em vez da Domain.Abstractions) | Média | Unit test de `AppDbContext.OnModelCreating` que varre todas as entidades e verifica que entidades `ISoftDeletable` têm filter; falha explícita se faltar |
| Partial unique index rejeita operações legítimas por conflito com registros inativos (race condition em reativação + criação simultânea de duplicata) | Baixa | Testes de integração com cenário paralelo; handler de reativação faz check explícito antes e trata `DbUpdateException` convertendo em 409 |
| Payload da árvore cresce além do aceitável em tenant futuro (>1k unidades) | Baixa | Métrica `tree_fetch_duration`; revisitar decisão em Phase 2 antes que se torne problema |
| Invalidação de cache TanStack Query causa cascata de refetches durante cadastro em rajada | Média | Optimistic updates em `onMutate` dos hooks de mutação; `useMutation` com `onSuccess` minimalista |
| Drift entre schema de `MetadataJson` e consumidores futuros | Média | Centralizar construção do metadata em `StructuralAuditMetadata.For(kind, ...)` com testes; documentar schema por kind em xmldoc |
| Cross-tenant leak em query mal escrita (dev ignora filter sem querer) | Baixa-Média | Filtros globais + code review obrigatório em qualquer uso de `.IgnoreQueryFilters()`; integration test de isolamento cross-tenant como smoke |
| Handler de criar unidade falha silenciosamente se `IClock` usar `DateTime.Now` em vez de `UtcNow` | Baixa | Convenção do projeto já é `UtcNow` em `IClock`; unit test valida |

---

## Architecture Decision Records

**ADRs de produto (PRD phase):**

- [ADR-001: F02 — Abordagem MVP Pura (CRUD Manual + Árvore Hierárquica)](adrs/adr-001.md) — escopo mínimo; sem gerador em lote; sem indicadores de prontidão.
- [ADR-002: Forma Canônica Estrita — Bloco e Andar Obrigatórios; Número com Sufixo Alfabético](adrs/adr-002.md) — uniformidade vence flexibilidade.
- [ADR-003: Remoção Exclusivamente por Inativação; Edição Restrita](adrs/adr-003.md) — soft-delete; bloco renomeável, unidade imutável.
- [ADR-004: F02 como Única Fonte de Estrutura; F04 Valida e Rejeita](adrs/adr-004.md) — separação estrita de responsabilidades.
- [ADR-005: Escrita Exclusiva do Síndico; Backoffice Read-Only Cross-Tenant](adrs/adr-005.md) — modelo de permissões alinhado ao de F01.

**ADRs técnicos (TechSpec phase):**

- [ADR-006: F02 Reutiliza o Módulo `PortaBox.Modules.Gestao`](adrs/adr-006.md) — sem novo módulo; entidades dentro do mesmo bounded context.
- [ADR-007: Soft-Delete Padronizado via `ISoftDeletable` + `SoftDeleteableAggregateRoot`](adrs/adr-007.md) — base class + global filter por reflection; partial unique index.
- [ADR-008: Auditoria via Extensão de `TenantAuditEntry.EventKind` + `MetadataJson`](adrs/adr-008.md) — enum ganha valores 5–11; diff no JSONB.
- [ADR-009: Endpoint Único `GET /condominios/{id}/estrutura` Retornando Árvore Completa](adrs/adr-009.md) — cache único no frontend; sem paginação no MVP.
- [ADR-010: Baseline Frontend — TanStack Query + React Context](adrs/adr-010.md) — padrão para todas as features frontend subsequentes.

---

*TechSpec gerado com a skill `cy-create-techspec`. Próximo passo: `cy-create-tasks` para fragmentar esta spec em tarefas atômicas de implementação.*
