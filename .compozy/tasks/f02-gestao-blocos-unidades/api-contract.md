# API Contract — F02: Gestão de Blocos e Unidades

> **Gerado a partir de:** `.compozy/tasks/f02-gestao-blocos-unidades/_prd.md` + `_techspec.md` (ADR-009)
> **Data:** 2026-04-18
> **Status:** Aprovado (derivado de TechSpec já aprovada)
> **Versão do contrato:** 1.0.0
> **Arquivo OpenAPI:** `api-contract.yaml`

---

## Premissas e Decisões

| Decisão | Escolha | Motivo |
|---------|---------|--------|
| Autenticação | JWT Bearer (ASP.NET Core Identity) | Baseline herdado de F01 (ADR-005) |
| Papéis | `Sindico`, `Operator` | Dois frontends distintos; escrita restrita ao síndico (ADR-005 F02) |
| Versionamento | Prefixo `/api/v1` | Convenção F01; breaking changes via major version |
| Case dos campos JSON | `camelCase` | Default do System.Text.Json; alinha com TypeScript do frontend |
| Idioma de mensagens | `pt-BR` em `title`/`detail` | Alinha com UI do síndico e copy do `portabox-design` |
| Formato de datas | ISO 8601 UTC (`2026-04-18T12:34:56Z`) | Padrão de interop; evita fuso horário ambíguo |
| IDs | UUID v4 como string | Convenção F01 (Condominio já usa GUID v4) |
| Formato de erros | RFC 7807 ProblemDetails (`application/problem+json`) | Padrão ASP.NET Core + já adotado em F01 |
| Paginação | N/A (árvore completa) | ADR-009: tree ≤ 30 KB para 300 unidades; cache via TanStack Query |
| Custom verbs | `:inativar` / `:reativar` | Google AIP-style para ações que não mapeiam em verbos HTTP REST puros |
| Unicidade | Partial unique index no Postgres (`WHERE ativo = true`) | ADR-007; permite reinserção canônica após soft-delete |

---

## Resumo de Endpoints

### Síndico (role `Sindico`)

| Método | Path | Descrição | Auth | Status Possíveis |
|--------|------|-----------|------|------------------|
| `GET` | `/api/v1/condominios/{condominioId}/estrutura` | Ler árvore de estrutura do próprio tenant | ✅ | 200, 401, 403, 404, 500 |
| `POST` | `/api/v1/condominios/{condominioId}/blocos` | Criar bloco | ✅ | 201, 400, 401, 403, 404, 409, 500 |
| `PATCH` | `/api/v1/condominios/{condominioId}/blocos/{blocoId}` | Renomear bloco | ✅ | 200, 400, 401, 403, 404, 409, 422, 500 |
| `POST` | `/api/v1/condominios/{condominioId}/blocos/{blocoId}:inativar` | Inativar bloco (soft-delete) | ✅ | 200, 401, 403, 404, 422, 500 |
| `POST` | `/api/v1/condominios/{condominioId}/blocos/{blocoId}:reativar` | Reativar bloco | ✅ | 200, 401, 403, 404, 409, 422, 500 |
| `POST` | `/api/v1/condominios/{condominioId}/blocos/{blocoId}/unidades` | Criar unidade | ✅ | 201, 400, 401, 403, 404, 409, 422, 500 |
| `POST` | `/api/v1/condominios/{condominioId}/blocos/{blocoId}/unidades/{unidadeId}:inativar` | Inativar unidade | ✅ | 200, 401, 403, 404, 422, 500 |
| `POST` | `/api/v1/condominios/{condominioId}/blocos/{blocoId}/unidades/{unidadeId}:reativar` | Reativar unidade | ✅ | 200, 401, 403, 404, 409, 422, 500 |

### Operador (role `Operator`)

| Método | Path | Descrição | Auth | Status Possíveis |
|--------|------|-----------|------|------------------|
| `GET` | `/api/v1/admin/condominios/{condominioId}/estrutura` | Ler estrutura de qualquer tenant (read-only) | ✅ | 200, 401, 403, 404, 500 |

---

## Endpoints Detalhados

### `GET /api/v1/condominios/{condominioId}/estrutura` — Ler árvore (síndico)

**Propósito:** alimentar a tela `EstruturaPage` do `apps/sindico` com a árvore completa do condomínio.
**Consumido por:** `apps/sindico` → `src/features/estrutura/hooks/useEstrutura.ts` (TanStack Query, key `queryKeys.estrutura(condominioId)`).

#### Path Parameters

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `condominioId` | UUID | Tenant do síndico autenticado |

#### Query Parameters

| Parâmetro | Tipo | Obrigatório | Default | Descrição |
|-----------|------|-------------|---------|-----------|
| `includeInactive` | boolean | Não | `false` | Quando `true`, inclui blocos e unidades inativos |

#### Response 200

```json
{
  "condominioId": "550e8400-e29b-41d4-a716-446655440000",
  "nomeFantasia": "Condomínio Residencial Alfa",
  "blocos": [
    {
      "id": "aa111111-2222-3333-4444-555555555555",
      "nome": "Bloco A",
      "ativo": true,
      "andares": [
        {
          "andar": 1,
          "unidades": [
            { "id": "bb111111-...", "numero": "101", "ativo": true },
            { "id": "bb222222-...", "numero": "101A", "ativo": true },
            { "id": "bb333333-...", "numero": "102", "ativo": true }
          ]
        },
        {
          "andar": 2,
          "unidades": [
            { "id": "bb444444-...", "numero": "201", "ativo": true },
            { "id": "bb555555-...", "numero": "201A", "ativo": false }
          ]
        }
      ]
    }
  ],
  "geradoEm": "2026-04-18T12:34:56Z"
}
```

#### Notas

- Blocos ordenados **alfabeticamente** pelo `nome`.
- Andares ordenados **numericamente crescente**.
- Unidades ordenadas por **parte numérica do `numero`** seguida do sufixo alfabético (`99 < 101 < 101A < 102`).
- `includeInactive=false` omite completamente blocos e unidades inativos da árvore.

---

### `POST /api/v1/condominios/{condominioId}/blocos` — Criar bloco

**Propósito:** cadastrar um novo bloco no condomínio. Primeiro passo para qualquer cadastro estrutural.
**Consumido por:** `<BlocoForm>` no empty state e no CTA "Novo bloco" da `EstruturaPage`.

#### Request Body

```json
{
  "nome": "Bloco A"
}
```

#### Response 201

```json
{
  "id": "aa111111-2222-3333-4444-555555555555",
  "condominioId": "550e8400-e29b-41d4-a716-446655440000",
  "nome": "Bloco A",
  "ativo": true,
  "inativadoEm": null
}
```

Header: `Location: /api/v1/condominios/550e8400.../blocos/aa111111...`

#### Erros Possíveis

| HTTP | `type` do ProblemDetails | Quando ocorre |
|------|--------------------------|---------------|
| 400 | `validation-error` | `nome` vazio, > 50 caracteres ou whitespace após trim |
| 409 | `canonical-conflict` | Já existe bloco **ativo** com o mesmo nome no tenant |
| 404 | `not-found` | `condominioId` não existe ou pertence a outro tenant |

---

### `PATCH /api/v1/condominios/{condominioId}/blocos/{blocoId}` — Renomear bloco

**Propósito:** alterar o nome de exibição do bloco (único campo editável — ADR-003).
**Consumido por:** `<BlocoForm>` em modo rename acionado pelo menu de ações de um `<TreeNode>`.

#### Request Body

```json
{
  "nome": "Torre Alfa"
}
```

#### Response 200

Mesmo shape do 201 de Create, com `nome` atualizado.

#### Erros Possíveis

| HTTP | `type` | Quando ocorre |
|------|--------|---------------|
| 400 | `validation-error` | Nome inválido |
| 422 | `invalid-transition` | Tentativa de renomear bloco inativo |
| 422 | `invalid-transition` | Novo nome igual ao atual |
| 409 | `canonical-conflict` | Nome novo já existe em outro bloco ativo do tenant |

---

### `POST /api/v1/condominios/{condominioId}/blocos/{blocoId}:inativar` — Inativar bloco

**Propósito:** soft-delete do bloco. Sem cascata no MVP — unidades ativas permanecem.
**Consumido por:** `<ConfirmModal>` após confirmação do síndico.

#### Request Body
Sem body.

#### Response 200
Shape de `Bloco` com `ativo: false`, `inativadoEm` preenchido.

#### Erros Possíveis

| HTTP | `type` | Quando ocorre |
|------|--------|---------------|
| 422 | `invalid-transition` | Bloco já está inativo |

---

### `POST /api/v1/condominios/{condominioId}/blocos/{blocoId}:reativar` — Reativar bloco

**Propósito:** reverter inativação anterior.
**Consumido por:** `<ConfirmModal>` acionado pelo menu de ações de bloco inativo.

#### Erros Possíveis

| HTTP | `type` | Quando ocorre |
|------|--------|---------------|
| 422 | `invalid-transition` | Bloco já está ativo |
| 409 | `canonical-conflict` | Já existe outro bloco ativo com o mesmo nome (inative-o antes) |

---

### `POST /api/v1/condominios/{condominioId}/blocos/{blocoId}/unidades` — Criar unidade

**Propósito:** cadastrar unidade dentro de um bloco ativo.
**Consumido por:** `<UnidadeForm>` na árvore (`apps/sindico`); UX de rajada mantém o modal aberto após sucesso.

#### Request Body

```json
{
  "andar": 2,
  "numero": "201A"
}
```

- `andar` deve ser inteiro ≥ 0.
- `numero` aceita caixa mista no input (regex `^[0-9]{1,4}[A-Za-z]?$`); **servidor normaliza para caixa alta**.

#### Response 201

```json
{
  "id": "bb222222-3333-4444-5555-666666666666",
  "blocoId": "aa111111-2222-3333-4444-555555555555",
  "andar": 2,
  "numero": "201A",
  "ativo": true,
  "inativadoEm": null
}
```

Header: `Location: /api/v1/condominios/.../blocos/.../unidades/bb222222...`

#### Erros Possíveis

| HTTP | `type` | Quando ocorre |
|------|--------|---------------|
| 400 | `validation-error` | `andar` < 0, `numero` fora do regex, vazio ou > 5 caracteres |
| 422 | `invalid-transition` | Bloco alvo está inativo |
| 409 | `canonical-conflict` | Já existe unidade ativa com a mesma tripla `(blocoId, andar, numero)` |

---

### `POST /api/v1/.../unidades/{unidadeId}:inativar` — Inativar unidade

**Propósito:** soft-delete de unidade individual.
**Consumido por:** `<ConfirmModal>` acionado pelo menu de ações da unidade.

#### Response 200
`Unidade` com `ativo: false`.

#### Erros Possíveis

| HTTP | `type` | Quando ocorre |
|------|--------|---------------|
| 422 | `invalid-transition` | Unidade já está inativa |

---

### `POST /api/v1/.../unidades/{unidadeId}:reativar` — Reativar unidade

**Propósito:** reverter inativação de unidade.

#### Erros Possíveis

| HTTP | `type` | Quando ocorre |
|------|--------|---------------|
| 422 | `invalid-transition` | Unidade já está ativa ou bloco pai está inativo |
| 409 | `canonical-conflict` | Outra unidade ativa tem a mesma tripla canônica |

---

### `GET /api/v1/admin/condominios/{condominioId}/estrutura` — Ler estrutura (operador)

**Propósito:** permitir que operadores da plataforma visualizem estrutura de qualquer tenant em modo read-only para atendimento de suporte (ADR-005).
**Consumido por:** `apps/backoffice` → `src/features/tenants/estrutura/hooks/useEstruturaAdmin.ts` (query key `queryKeys.estruturaAdmin(condominioId)`, isolada da key de síndico).

#### Path Parameters

| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `condominioId` | UUID | Tenant alvo (explícito no path pois operador não tem tenant fixo) |

#### Query Parameters
Mesmos do endpoint de síndico (`includeInactive`).

#### Response 200
**Shape idêntico** ao endpoint de síndico (`Estrutura`). Frontend renderiza sem controles de edição.

#### Erros Possíveis

| HTTP | `type` | Quando ocorre |
|------|--------|---------------|
| 403 | `forbidden` | Usuário autenticado não possui role `Operator` |
| 404 | `not-found` | `condominioId` não existe |

---

## Schemas de Entidades

### Estrutura (leitura)

| Campo | Tipo | Obrigatório | Nullable | Descrição |
|-------|------|-------------|----------|-----------|
| `condominioId` | UUID | ✅ | ❌ | Identificador do condomínio |
| `nomeFantasia` | string | ✅ | ❌ | Nome de exibição |
| `blocos` | BlocoNode[] | ✅ | ❌ | Blocos ordenados alfabeticamente |
| `geradoEm` | datetime (ISO 8601 UTC) | ✅ | ❌ | Timestamp da geração |

### BlocoNode (nó da árvore)

| Campo | Tipo | Obrigatório | Nullable | Descrição |
|-------|------|-------------|----------|-----------|
| `id` | UUID | ✅ | ❌ | Identificador do bloco |
| `nome` | string | ✅ | ❌ | Nome de exibição |
| `ativo` | boolean | ✅ | ❌ | `false` só aparece com `includeInactive=true` |
| `andares` | AndarNode[] | ✅ | ❌ | Andares ordenados numericamente |

### AndarNode (agrupamento interno)

| Campo | Tipo | Obrigatório | Nullable | Descrição |
|-------|------|-------------|----------|-----------|
| `andar` | integer (≥ 0) | ✅ | ❌ | Número do andar (0 = térreo) |
| `unidades` | UnidadeLeaf[] | ✅ | ❌ | Unidades do andar, ordenadas semanticamente |

### UnidadeLeaf (folha da árvore)

| Campo | Tipo | Obrigatório | Nullable | Descrição |
|-------|------|-------------|----------|-----------|
| `id` | UUID | ✅ | ❌ | Identificador |
| `numero` | string (regex `^[0-9]{1,4}[A-Z]?$`) | ✅ | ❌ | Número canônico em caixa alta |
| `ativo` | boolean | ✅ | ❌ | Status |

### Bloco (response de mutações)

| Campo | Tipo | Obrigatório | Nullable | Descrição |
|-------|------|-------------|----------|-----------|
| `id` | UUID | ✅ | ❌ | Identificador |
| `condominioId` | UUID | ✅ | ❌ | Condomínio pai |
| `nome` | string (1–50) | ✅ | ❌ | Nome (mutável via PATCH) |
| `ativo` | boolean | ✅ | ❌ | Status |
| `inativadoEm` | datetime | ❌ | ✅ | `null` quando ativo |

### Unidade (response de mutações)

| Campo | Tipo | Obrigatório | Nullable | Descrição |
|-------|------|-------------|----------|-----------|
| `id` | UUID | ✅ | ❌ | Identificador |
| `blocoId` | UUID | ✅ | ❌ | Bloco pai (imutável) |
| `andar` | integer (≥ 0) | ✅ | ❌ | Andar (imutável) |
| `numero` | string (regex) | ✅ | ❌ | Número canônico (imutável) |
| `ativo` | boolean | ✅ | ❌ | Status |
| `inativadoEm` | datetime | ❌ | ✅ | `null` quando ativa |

### CreateBlocoRequest / RenameBlocoRequest

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `nome` | string (1–50) | ✅ | Trim aplicado no servidor |

### CreateUnidadeRequest

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `andar` | integer (≥ 0) | ✅ | 0 = térreo |
| `numero` | string (regex case-insensitive) | ✅ | Normalizado para caixa alta no servidor |

---

## Códigos de Erro (RFC 7807 ProblemDetails)

Todos os erros seguem o formato [RFC 7807](https://datatracker.ietf.org/doc/html/rfc7807) com `Content-Type: application/problem+json`.

| HTTP | `type` (URI) | Título (pt-BR) | Quando ocorre |
|------|--------------|----------------|---------------|
| 400 | `https://portabox.app/problems/validation-error` | Falha de validação | Campo obrigatório ausente, formato inválido |
| 401 | `https://portabox.app/problems/unauthorized` | Não autorizado | Token ausente, expirado ou inválido |
| 403 | `https://portabox.app/problems/forbidden` | Acesso negado | Role errada ou tenant alheio |
| 404 | `https://portabox.app/problems/not-found` | Recurso não encontrado | Recurso inexistente (ou existente em outro tenant — mesma resposta para não vazar existência) |
| 409 | `https://portabox.app/problems/canonical-conflict` | Conflito canônico | Violação de unicidade (nome de bloco ativo duplicado; tripla canônica de unidade duplicada) |
| 422 | `https://portabox.app/problems/invalid-transition` | Transição inválida | Transição de estado impossível (ex.: criar unidade em bloco inativo; inativar entidade já inativa) |
| 500 | `https://portabox.app/problems/internal-error` | Erro interno | Erro inesperado — `traceId` preenchido para correlação |

### Formato de Erro Padrão (ProblemDetails)

```json
{
  "type": "https://portabox.app/problems/canonical-conflict",
  "title": "Conflito canônico",
  "status": 409,
  "detail": "Já existe unidade ativa para Bloco A / Andar 2 / Apto 201. Inative-a antes de reativar esta.",
  "instance": "/api/v1/condominios/550e8400.../blocos/.../unidades/.../reativar",
  "traceId": null
}
```

### Formato de Erro de Validação (400 — ValidationProblemDetails)

Estende `ProblemDetails` com campo `errors` agrupando mensagens por nome de campo:

```json
{
  "type": "https://portabox.app/problems/validation-error",
  "title": "Falha de validação",
  "status": 400,
  "detail": "Um ou mais campos estão inválidos",
  "instance": "/api/v1/condominios/550e8400.../blocos",
  "errors": {
    "nome": [
      "O nome é obrigatório",
      "O nome deve ter no máximo 50 caracteres"
    ],
    "andar": [
      "O andar deve ser maior ou igual a 0"
    ]
  }
}
```

---

## Como usar este contrato

### Backend (.NET 8)

- Implementar os 9 endpoints exatamente como especificado em `EstruturaEndpoints.cs` (ver TechSpec task 09).
- Usar `ProblemDetails` da `Microsoft.AspNetCore.Mvc` ou handler customizado para erros.
- Seguir `x-backend-notes` em cada operação para hints específicos (índices, race conditions, métricas).

### Frontend (React + Vite + TypeScript)

1. Gerar tipos TypeScript a partir do schema:
   ```bash
   pnpm add -D openapi-typescript
   pnpm exec openapi-typescript .compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml \
     -o packages/api-client/src/generated.ts
   ```

2. Mockar API durante desenvolvimento:
   ```bash
   pnpm add -D @stoplight/prism-cli
   pnpm exec prism mock .compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml
   # API mock disponível em http://localhost:4010
   ```

3. Configurar TanStack Query com as query keys canônicas (ver ADR-010):
   - `queryKeys.estrutura(condominioId)` — síndico
   - `queryKeys.estruturaAdmin(condominioId)` — operador

### Testes de Contrato

Validar implementação contra o YAML:

```bash
# Dredd
pnpm exec dredd .compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml http://localhost:5000

# Ou Schemathesis (mais moderno)
schemathesis run .compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml --base-url http://localhost:5000
```

---

## Questões em Aberto

Herdadas do PRD e TechSpec que podem afetar o contrato em Phase 2:

- [ ] **Ordenação default da árvore**: alfabética de bloco já está explícita; se surgir demanda de ordenação custom (ex.: por data de criação), adicionar `?orderBy=` ao endpoint de estrutura.
- [ ] **Log de acesso do operador** (ADR-005): não há endpoint dedicado para registrar acesso read-only do operador no MVP. Phase 2 pode introduzir `POST /admin/audit-access`; contrato precisará ser estendido.
- [ ] **Endpoint de histórico de auditoria visível ao síndico** (Open Question do PRD): Phase 2 pode adicionar `GET /condominios/{id}/audit?kind=BlocoRenomeado&since=...` — fora do escopo de F02.
- [ ] **Andares negativos / subsolo**: schema atual usa `minimum: 0`. Se Phase 2 demandar subsolos, mudar para `minimum: -5` (versão major do contrato).
- [ ] **Limite de volume por tenant** (blocos/unidades): não há hard limit no MVP; adicionar validação e `429 Too Many Requests` se se tornar necessário.
- [ ] **Geração automática de tipos TS via pipeline CI**: atualmente geração manual com `openapi-typescript`; deferido para Phase 2.

---

*Contrato gerado pela skill `flow-contract-creator`. Fonte de verdade para a implementação backend (F02 task_09) e frontend (F02 tasks 11–17).*
