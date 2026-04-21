# QA Report — Isolamento Multi-tenant (Cross-tenant Isolation)

**Task ID:** qa_task_09_isolamento_multitenant
**Data/Hora:** 2026-04-20T23:xx:xxZ (UTC)
**Status Geral:** PASS

---

## Contexto

- **User Story:** Sindico de um tenant nao pode acessar nem modificar nenhum recurso de outro tenant
- **Ambiente:** http://localhost:5272
- **Tipos de teste:** API, Banco
- **Autenticacao:** Sim (cookie session — re-login executado pois sessoes da task_00 expiraram)
- **Tenant A:** 4cce551d-4f18-474b-a42a-2deb6c2a0451 (Sindico A: qa-sindico-a-1776724904@portabox.test)
- **Tenant B:** 23fb219d-460a-4eee-a9e7-308d7665350b (Sindico B: qa-sindico-b-1776724968@portabox.test)

---

## Setup da Task

Antes dos casos de teste, foram necessarios:
1. Re-login de Sindico A e Sindico B (sessoes da task_00 expiraram)
2. Criacao de bloco "Bloco B-QA-01" em Tenant B via Sindico B (BLOCO_B_ID: 7a600791-d5ac-4f70-9741-c7c86ea49ca5)
3. Criacao de unidade andar=1 numero=101 em Tenant B (UNIDADE_B_ID: f24e0bdf-22fd-4300-9919-94da8618672e)

---

## Casos de Teste

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-00 | Re-login Sindico A | API | PASS |
| CT-00B | Re-login Sindico B | API | PASS |
| CT-01 | GET estrutura cross-tenant (A->B) | API | PASS |
| CT-02 | GET estrutura cross-tenant (B->A) | API | PASS |
| CT-03 | POST bloco cross-tenant (A->B) — CRITICO | API | PASS |
| CT-04 | PATCH bloco cross-tenant (A->B) | API | PASS |
| CT-05 | POST :inativar bloco cross-tenant (A->B) | API | PASS |
| CT-06 | POST :reativar bloco cross-tenant (A->B) | API | PASS |
| CT-07 | POST unidade cross-tenant (A->B) | API | PASS |
| CT-08 | POST :inativar unidade cross-tenant (A->B) | API | PASS |
| CT-09 | POST :reativar unidade cross-tenant (A->B) | API | PASS |
| CT-10 | Path mix — condominioId=A + blocoId=B | API | PASS |
| CT-11 | Path mix inverso — condominioId=B + blocoId=A | API | PASS |
| CT-12 | Admin endpoint — Sindico sem role de admin | API | PASS |
| CT-13 | DB: tenant_id preenchido em bloco e unidade | Banco | PASS |
| CT-14 | DB: bloco sem cross-tenant (tenant_id = condominio_id) | Banco | PASS |

---

## Detalhes por Caso

### CT-00 — Re-login Sindico A PASS

**Expected:** HTTP 200, role=Sindico, tenantId=Tenant A
**Actual:** HTTP 200, userId=9ae7217c-7c68-43ba-b663-63bb9f235d97, role=Sindico, tenantId=4cce551d-4f18-474b-a42a-2deb6c2a0451
**Evidencias:** `requests.log` bloco "CT-00", `cookies_sindico_a.txt`

---

### CT-00B — Re-login Sindico B PASS

**Expected:** HTTP 200, role=Sindico, tenantId=Tenant B
**Actual:** HTTP 200, userId=de785ac5-f834-47f2-8bbe-cf7ee3be24d9, role=Sindico, tenantId=23fb219d-460a-4eee-a9e7-308d7665350b
**Evidencias:** `requests.log` bloco "CT-00B", `cookies_sindico_b.txt`

---

### CT-01 — GET estrutura cross-tenant (A->B) PASS

**Cenario:** Sindico A tenta ler a estrutura do Tenant B
**Expected:** 403 ou 404
**Actual:** HTTP 403
**Evidencias:** `requests.log` bloco "CT-01"

---

### CT-02 — GET estrutura cross-tenant (B->A) PASS

**Cenario:** Sindico B tenta ler a estrutura do Tenant A
**Expected:** 403 ou 404
**Actual:** HTTP 403
**Evidencias:** `requests.log` bloco "CT-02"

---

### CT-03 — POST bloco cross-tenant (A->B) PASS (CENARIO CRITICO)

**Cenario:** Sindico A tenta criar bloco diretamente no path de Tenant B. Se retornasse 201 seria falha critica de isolamento.
**Expected:** 403 ou 404
**Actual:** HTTP 403 — bloco invasor nao criado
**Evidencias:** `requests.log` bloco "CT-03"

---

### CT-04 — PATCH bloco cross-tenant (A->B) PASS

**Cenario:** Sindico A tenta editar nome do bloco de Tenant B
**Expected:** 403 ou 404
**Actual:** HTTP 403
**Evidencias:** `requests.log` bloco "CT-04"

---

### CT-05 — POST :inativar bloco cross-tenant (A->B) PASS

**Cenario:** Sindico A tenta inativar bloco de Tenant B
**Expected:** 403 ou 404
**Actual:** HTTP 403
**Evidencias:** `requests.log` bloco "CT-05"

---

### CT-06 — POST :reativar bloco cross-tenant (A->B) PASS

**Cenario:** Sindico A tenta reativar bloco de Tenant B
**Expected:** 403 ou 404
**Actual:** HTTP 403
**Evidencias:** `requests.log` bloco "CT-06"

---

### CT-07 — POST unidade cross-tenant (A->B) PASS

**Cenario:** Sindico A tenta criar unidade em bloco de Tenant B
**Expected:** 403 ou 404
**Actual:** HTTP 403
**Evidencias:** `requests.log` bloco "CT-07"

---

### CT-08 — POST :inativar unidade cross-tenant (A->B) PASS

**Cenario:** Sindico A tenta inativar unidade de Tenant B
**Expected:** 403 ou 404
**Actual:** HTTP 403
**Evidencias:** `requests.log` bloco "CT-08"

---

### CT-09 — POST :reativar unidade cross-tenant (A->B) PASS

**Cenario:** Sindico A tenta reativar unidade de Tenant B
**Expected:** 403 ou 404
**Actual:** HTTP 403
**Evidencias:** `requests.log` bloco "CT-09"

---

### CT-10 — Path mix: condominioId=A + blocoId=B PASS

**Cenario cirurgico:** Sindico A autentica em seu proprio tenant (A) mas usa na URL o blocoId pertencente ao Tenant B. O path /condominios/{A}/blocos/{blocoDeB}/unidades e tecnicamente autorizado pelo middleware de tenant (condominio e do proprio sindico), mas o blocoId nao pertence ao condominio A.
**Expected:** 404 (bloco de B nao encontrado no escopo de A)
**Actual:** HTTP 404
**Interpretacao:** O sistema valida corretamente o pertencimento do bloco ao condominio antes de processar a requisicao. Nao houve vazamento de dados nem criacao de unidade.
**Evidencias:** `requests.log` bloco "CT-10"

---

### CT-11 — Path mix inverso: condominioId=B + blocoId=A PASS

**Cenario:** Sindico A usa condominioId de Tenant B + blocoId do proprio Tenant A na URL
**Expected:** 403 ou 404
**Actual:** HTTP 403 — acesso negado pelo middleware de tenant (condominioId=B nao e o tenant do Sindico A)
**Evidencias:** `requests.log` bloco "CT-11"

---

### CT-12 — Admin endpoint sem role de admin PASS

**Cenario:** Sindico A (role=Sindico) tenta acessar GET /api/v1/admin/condominios/{A}/estrutura, que requer role=Operator
**Expected:** 403
**Actual:** HTTP 403
**Observacao:** Reconfirmacao da barreira de role — sem regressao detectada
**Evidencias:** `requests.log` bloco "CT-12"

---

### CT-13 — DB: tenant_id preenchido em bloco e unidade PASS

**Query 1:** SELECT tenant_id, count(*) FROM bloco GROUP BY tenant_id
**Resultado:**
- 23fb219d-460a-4eee-a9e7-308d7665350b (Tenant B) = 3 blocos
- 4cce551d-4f18-474b-a42a-2deb6c2a0451 (Tenant A) = 13 blocos

**Query 2:** SELECT count(*) FROM bloco WHERE tenant_id IS NULL
**Resultado:** 0 — nenhum bloco com tenant_id NULL

**Query 3:** SELECT tenant_id, count(*) FROM unidade GROUP BY tenant_id
**Resultado:**
- 23fb219d-460a-4eee-a9e7-308d7665350b (Tenant B) = 2 unidades
- 4cce551d-4f18-474b-a42a-2deb6c2a0451 (Tenant A) = 24 unidades

**Query 4:** SELECT count(*) FROM unidade WHERE tenant_id IS NULL
**Resultado:** 0 — nenhuma unidade com tenant_id NULL

**Expected:** Sem registros com tenant_id NULL; dois grupos distintos (Tenant A e Tenant B)
**Actual:** Conforme esperado. Dois grupos com tenant_ids distintos, zero NULLs.
**Evidencias:** `db_check.log` bloco "CT-13"

---

### CT-14 — DB: tenant_id = condominio_id em todos os blocos PASS

**Query:** SELECT id, tenant_id, condominio_id FROM bloco WHERE tenant_id != condominio_id LIMIT 5
**Resultado:** Zero linhas retornadas

**Expected:** Zero linhas (nenhum bloco com tenant_id diferente do condominio_id)
**Actual:** Zero linhas — nenhum inconsistencia de tenant por design
**Observacao:** A tabela bloco possui coluna condominio_id separada de tenant_id, e ambas estao sempre iguais para todos os registros. Confirmado que o invariante tenant_id = condominio_id e mantido.
**Evidencias:** `db_check.log` bloco "CT-14"

---

## Resumo de Evidencias

```
qa_task_09_isolamento_multitenant/
├── test_plan.md
├── cross_tenant_matrix.md
├── created_resources.txt
├── cookies_sindico_a.txt
├── cookies_sindico_b.txt
├── requests.log
├── db_check.log
├── screenshots/               (sem testes UI nesta task)
└── videos/                    (sem testes UI nesta task)
```

---

## Observacoes Tecnicas

1. **Mecanismo de protecao:** O backend implementa isolamento via middleware de tenant que verifica se o `condominioId` da URL corresponde ao `tenantId` do usuario autenticado. Qualquer divergencia resulta em HTTP 403 antes mesmo de consultar o banco.

2. **Cenario CT-10 (path mix):** O unico caso onde retornou 404 em vez de 403 foi o cenario cirurgico com condominioId do proprio tenant mas blocoId de outro tenant. Isso indica que o sistema primeiro valida o tenant (passa — condominio e do sindico A) e depois verifica se o bloco pertence ao condominio (falha — bloco e de B), retornando 404. Este comportamento e correto pois impede criacao de unidades em blocos de outros tenants.

3. **Contagem de blocos no DB:** Tenant A tem 13 blocos e Tenant B tem 3 blocos (1 criado nesta task + 2 de tasks anteriores). Todos com tenant_id preenchido e correto.

4. **Coluna condominio_id em bloco:** A tabela possui tanto `tenant_id` quanto `condominio_id` como colunas distintas, e ambas sao sempre identicas (tenant_id = condominio_id), confirmando integridade referencial.

---

## Status para o Orquestrador

**Status:** PASS
**Motivo da falha:** N/A — todos os 14 casos (+ 2 de setup) passaram
**Leaks detectados:** ZERO (0 de 12 cenarios cross-tenant resultaram em acesso indevido)
**Tasks possivelmente impactadas:** Nenhuma
