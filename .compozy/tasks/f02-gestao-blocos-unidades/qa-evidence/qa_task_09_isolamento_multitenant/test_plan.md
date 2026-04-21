# Plano de Testes — Isolamento Multi-tenant (qa_task_09)

**Task ID:** qa_task_09_isolamento_multitenant
**Tipos:** API, Banco

## Contexto

- Tenant A: 4cce551d-4f18-474b-a42a-2deb6c2a0451 (QA Teste A) — Sindico A
- Tenant B: 23fb219d-460a-4eee-a9e7-308d7665350b (QA Teste B) — Sindico B
- Bloco de referencia Tenant A: BLOCO_QA01_ID=88037273-d560-4415-a1e2-b45a00dc5be4
- Unidade de referencia Tenant A: f2a0b7cc-13d3-4c18-a36e-b5ba9fcfce33 (andar=1, numero=101)
- Bloco de referencia Tenant B: criado no setup desta task

## Regra anti-interrupcao

Falhas nao param a bateria. Todos os 14 casos sao executados e reportados.

## Casos de Teste

### CT-00: Setup — Re-login Sindico A
- **Pre-condicao:** Sessoes expiradas desde qa_task_00
- **Passos:** POST /api/v1/auth/login com credenciais sindico A
- **Expected:** HTTP 200, role=Sindico, tenantId=Tenant A
- **Tipo:** API

### CT-00B: Setup — Re-login Sindico B
- **Pre-condicao:** Sessoes expiradas desde qa_task_00
- **Passos:** POST /api/v1/auth/login com credenciais sindico B
- **Expected:** HTTP 200, role=Sindico, tenantId=Tenant B
- **Tipo:** API

### CT-SETUP-B: Setup — Criar bloco e unidade em Tenant B
- **Pre-condicao:** Sindico B autenticado
- **Passos:** POST /condominios/{B}/blocos com nome="Bloco B-QA-01"; POST unidade andar=1 numero=101
- **Expected:** HTTP 201 para ambos
- **Tipo:** API

### CT-01: GET estrutura cross-tenant (A->B)
- **Pre-condicao:** Sindico A autenticado
- **Passos:** GET /api/v1/condominios/{TENANT_B_ID}/estrutura usando cookies de Sindico A
- **Expected:** 403 ou 404 (nao 200)
- **Tipo:** API

### CT-02: GET estrutura cross-tenant (B->A)
- **Pre-condicao:** Sindico B autenticado
- **Passos:** GET /api/v1/condominios/{TENANT_A_ID}/estrutura usando cookies de Sindico B
- **Expected:** 403 ou 404 (nao 200)
- **Tipo:** API

### CT-03: POST bloco cross-tenant (A->B)
- **Pre-condicao:** Sindico A autenticado
- **Passos:** POST /api/v1/condominios/{TENANT_B_ID}/blocos body {"nome":"Bloco Invasor"}
- **Expected:** 403 ou 404. Se 201: FALHA CRITICA de isolamento
- **Tipo:** API

### CT-04: PATCH bloco cross-tenant (A->B)
- **Pre-condicao:** Sindico A autenticado, blocoB existe
- **Passos:** PATCH /api/v1/condominios/{B}/blocos/{blocoB_ID} body {"nome":"Invasao"}
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-05: POST :inativar bloco cross-tenant (A->B)
- **Pre-condicao:** Sindico A autenticado, blocoB existe
- **Passos:** POST /api/v1/condominios/{B}/blocos/{blocoB_ID}:inativar
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-06: POST :reativar bloco cross-tenant (A->B)
- **Pre-condicao:** Sindico A autenticado, blocoB existe
- **Passos:** POST /api/v1/condominios/{B}/blocos/{blocoB_ID}:reativar
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-07: POST unidade cross-tenant (A->B)
- **Pre-condicao:** Sindico A autenticado, blocoB existe
- **Passos:** POST /api/v1/condominios/{B}/blocos/{blocoB_ID}/unidades body {"andar":9,"numero":"901"}
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-08: POST :inativar unidade cross-tenant (A->B)
- **Pre-condicao:** Sindico A autenticado, unidadeB existe
- **Passos:** POST /api/v1/condominios/{B}/blocos/{blocoB_ID}/unidades/{unidadeB_ID}:inativar
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-09: POST :reativar unidade cross-tenant (A->B)
- **Pre-condicao:** Sindico A autenticado, unidadeB existe
- **Passos:** POST /api/v1/condominios/{B}/blocos/{blocoB_ID}/unidades/{unidadeB_ID}:reativar
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-10: Path mix — condominioId proprio + blocoId de outro tenant
- **Pre-condicao:** Sindico A autenticado, blocoB existe
- **Passos:** POST /api/v1/condominios/{A}/blocos/{blocoB_ID}/unidades body {"andar":8,"numero":"801"}
- **Expected:** 404 (bloco de B nao pertence a A). Se 201: BUG GRAVE
- **Tipo:** API

### CT-11: Path mix inverso — condominioId de B + blocoId de A
- **Pre-condicao:** Sindico A autenticado, blocoA existe
- **Passos:** POST /api/v1/condominios/{B}/blocos/{blocoA_ID}/unidades body {"andar":7,"numero":"701"}
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-12: Reconfirmacao admin endpoint sem role
- **Pre-condicao:** Sindico A autenticado (role=Sindico, nao Operator)
- **Passos:** GET /api/v1/admin/condominios/{A}/estrutura
- **Expected:** 403 (sem role de admin)
- **Tipo:** API

### CT-13: Validacao DB — tenant_id preenchido em bloco e unidade
- **Passos:**
  1. SELECT tenant_id, count(*) FROM bloco GROUP BY tenant_id
  2. SELECT tenant_id, count(*) FROM unidade GROUP BY tenant_id
- **Expected:** Sem registros com tenant_id NULL; counts consistentes com recursos criados
- **Tipo:** Banco

### CT-14: Validacao DB — bloco sem cross-tenant (tenant_id = condominio_id)
- **Passos:** SELECT * FROM bloco WHERE tenant_id != condominio_id LIMIT 5
- **Expected:** Zero linhas retornadas
- **Tipo:** Banco
