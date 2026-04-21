# QA Report — CF-02 Cadastro de Unidade

**Task ID:** qa_task_02
**Data/Hora:** 2026-04-21T00:40:00Z
**Status Geral:** PASS

---

## Contexto

- **User Story:** Como sindico, quero cadastrar unidades em um bloco para que eu possa identificar cada apartamento/sala pelo seu andar e numero
- **Ambiente:** http://localhost:5272/api/v1 (backend) | http://localhost:5174 (sindico app)
- **Tenant A:** 4cce551d-4f18-474b-a42a-2deb6c2a0451
- **Bloco QA-01:** 88037273-d560-4415-a1e2-b45a00dc5be4
- **Tipos de teste:** API, Banco, UI
- **Autenticacao:** Sim (cookie portabox.auth, sindico A)

---

## Casos de Teste

| ID    | Descricao                                           | Tipo  | Status |
|-------|-----------------------------------------------------|-------|--------|
| CT-01 | Happy path andar=1 numero=101                       | API   | PASS   |
| CT-02 | Andar 0 (terreo)                                    | API   | PASS   |
| CT-03 | Numero com sufixo minusculo normalizado p/ maiuscula| API   | PASS   |
| CT-04 | Numero com sufixo maiusculo                         | API   | PASS   |
| CT-05 | Numero com 4 digitos                                | API   | PASS   |
| CT-06 | Numero invalido (6 chars) -> 400                    | API   | PASS   |
| CT-07 | Numero so letra -> 400                              | API   | PASS   |
| CT-08 | Numero com simbolos -> 400                          | API   | PASS   |
| CT-09 | Andar negativo -> 400                               | API   | PASS   |
| CT-10 | Conflito canonico (duplicata de tripla) -> 409      | API   | PASS   |
| CT-11 | Bloco inexistente -> 404                            | API   | PASS   |
| CT-12 | Bloco inativo -> 422                                | API   | PASS   |
| CT-13 | Sindico A em tenant B -> 403                        | API   | PASS   |
| CT-14 | Sem autenticacao -> 401                             | API   | PASS   |
| CT-15 | Persistencia DB — todos os campos corretos          | Banco | PASS   |
| CT-16 | Audit entry event_kind=9 (UnidadeCriada)            | Banco | PASS   |
| UT-01 | Adicionar unidade via UI, visivel na arvore         | UI    | PASS   |
| UT-02 | Validacao andar negativo — erro inline              | UI    | PASS   |
| UT-03 | Validacao numero invalido — erro inline             | UI    | PASS   |
| UT-04 | Normalizacao numero para maiuscula na arvore        | UI    | PASS   |

**Total:** 20/20 PASS

---

## Detalhes por Caso

### CT-01 — Happy Path PASS

**Expected:** 201 + body {id, blocoId, andar:1, numero:"101", ativo:true, inativadoEm:null} + Location header  
**Actual:** 201, body correto, id=f2a0b7cc-13d3-4c18-a36e-b5ba9fcfce33  
**Evidencias:** `requests.log` linhas 5-23

---

### CT-02 — Andar 0 (terreo) PASS

**Expected:** 201 (andar=0 permitido)  
**Actual:** 201, id=0608df78-27c8-439f-a0a5-b6511107fcad  
**Evidencias:** `requests.log` linhas 26-44

---

### CT-03 — Normalizacao para maiuscula na API PASS

**Expected:** 201, numero retornado como "101A" (uppercase)  
**Actual:** 201, numero="101A" — normalizacao confirmada no servidor  
**Evidencias:** `requests.log` linhas 47-65

---

### CT-04 — Sufixo maiusculo PASS

**Expected:** 201  
**Actual:** 201, numero="102B"  
**Evidencias:** `requests.log` linhas 68-86

---

### CT-05 — Numero 4 digitos PASS

**Expected:** 201  
**Actual:** 201, numero="2001", andar=20  
**Evidencias:** `requests.log` linhas 89-107

---

### CT-06 — Numero invalido (6 chars) PASS

**Expected:** 400 + ValidationProblemDetails  
**Actual:** 400, errors.numero = "O numero da unidade deve seguir o formato de 1 a 4 digitos com sufixo alfabetico opcional."  
**Evidencias:** `requests.log` linhas 110-133

---

### CT-07 — Numero so letra PASS

**Expected:** 400  
**Actual:** 400, mesmo erro de validacao  
**Evidencias:** `requests.log` linhas 136-159

---

### CT-08 — Numero com simbolos PASS

**Expected:** 400  
**Actual:** 400, mesmo erro de validacao  
**Evidencias:** `requests.log` linhas 162-185

---

### CT-09 — Andar negativo PASS

**Expected:** 400  
**Actual:** 400, errors.andar = "O andar da unidade deve ser maior ou igual a zero."  
**Evidencias:** `requests.log` linhas 188-211

---

### CT-10 — Conflito canonico PASS

**Expected:** 1a: 201, 2a: 409 type=canonical-conflict  
**Actual:** 1a: 201 (id=368496a7-8296-41de-a908-9e03b83d4184), 2a: 409 type=https://portabox.app/problems/canonical-conflict, detail="Unidade ja existe"  
**Evidencias:** `requests.log` linhas 214-248

---

### CT-11 — Bloco inexistente PASS

**Expected:** 404  
**Actual:** 404, detail="Bloco nao encontrado"  
**Evidencias:** `requests.log` linhas 251-269

---

### CT-12 — Bloco inativo PASS

**Expected:** 422 (bloco inativo nao aceita novas unidades)  
**Actual:** 422, type=invalid-transition, detail="Bloco inativo". Bloco temp (id=54d4fbe9-e749-4ee4-8dc4-442355762b6c) criado e inativado como prerequisito  
**Evidencias:** `requests.log` linhas 272-303

---

### CT-13 — Cross-tenant PASS

**Expected:** 403 ou 404  
**Actual:** 403, type=forbidden, detail="Voce nao tem permissao para executar esta operacao"  
**Evidencias:** `requests.log` linhas 306-325

---

### CT-14 — Sem autenticacao PASS

**Expected:** 401  
**Actual:** 401, detail="Token de autenticacao invalido ou expirado"  
**Evidencias:** `requests.log` linhas 328-347

---

### CT-15 — Persistencia DB PASS

**Expected:** Registro com tenant_id=tenantA, bloco_id=qa01, andar=1, numero="101", ativo=true  
**Actual:** todos os campos corretos — tenant_id=4cce551d-4f18-474b-a42a-2deb6c2a0451, bloco_id=88037273-d560-4415-a1e2-b45a00dc5be4, andar=1, numero=101, ativo=t, criado_por=9ae7217c-7c68-43ba-b663-63bb9f235d97  
**Evidencias:** `db_check.log` linhas 5-19

---

### CT-16 — Audit Entry PASS

**Expected:** event_kind=9 (UnidadeCriada) com metadata_json contendo unidadeId e numero da unidade criada  
**Actual:** entrada encontrada: `{"andar": 1, "numero": "101", "blocoId": "88037273...", "unidadeId": "f2a0b7cc..."}` com event_kind=9  
**Evidencias:** `db_check.log` linhas 40-42

---

### UT-01 — Adicionar unidade via UI PASS

**Expected:** Modal abre, formulario preenchido com andar=99 numero=9901, submit, modal fecha, unidade "9901" visivel na arvore do Bloco QA-01 (andar 99)  
**Actual:** Modal abriu, form preenchido, submit executado, modal fechou, arvore expandida (bloco QA-01 -> andar 99), "9901" visivel  
**Evidencias:** `screenshots/ut01_tree_expandido.png` | `requests.log` — "Tree shows 9901: true"

---

### UT-02 — Validacao andar negativo PASS

**Expected:** Modal permanece aberto, mensagem "Use andar 0 ou maior." visivel inline  
**Actual:** Modal ficou aberto, "Use andar 0 ou maior" visivel  
**Evidencias:** `screenshots/ut02_apos_submit.png` | `requests.log` — "Validation error visible: true"

---

### UT-03 — Validacao numero invalido PASS

**Expected:** Modal permanece aberto, mensagem "Use ate 4 digitos..." visivel  
**Actual:** Modal ficou aberto, "Use ate 4 digitos" visivel  
**Evidencias:** `screenshots/ut03_apos_submit.png` | `requests.log` — "Validation error visible: true"

---

### UT-04 — Normalizacao para maiuscula na UI PASS

**Expected:** Entrada "9902a" normalizada; unidade aparece na arvore como "9902A" (maiuscula)  
**Actual:** Input value apos fill = "9902A" (campo ja normaliza no onChange); submit, modal fechou, "9902A" visivel na arvore do andar 99  
**Evidencias:** `screenshots/ut04_tree_expandido.png` | `requests.log` — "Input value after fill('9902a'): '9902A'" e "Tree shows 9902A: true"

---

## Notas tecnicas

- **Infra UI:** sindico app (Vite dev) usa cookies HttpOnly para auth. Playwright injeta cookie de sessao pre-obtido via `context.addCookies()`. Proxy intercepta `**/api/**` e corrige CORS headers (backend retorna `Access-Control-Allow-Origin: *` incompativel com `credentials:include`; proxy substitui pelo origin especifico da app).
- **Arvore 4-niveis:** A arvore de estrutura tem 4 niveis (condominio > bloco > andar > unidade). Apos mutacao bem-sucedida o TanStack Query invalida o cache e refetch colapsa os no's. O teste expande manualmente o bloco e depois o andar para verificar a unidade.
- **Numeros usados:** UT-01 usou andar=99/numero=9901 e UT-04 usou andar=99/numero=9902a (9902A normalizado). Numeros dos API tests (101, 01, 101A, etc.) foram usados conforme plano original.

---

## Resumo de Evidencias

```
qa-evidence/qa_task_02_cadastro_unidade/
├── test_plan.md
├── requests.log           — CT-01..CT-14 (API) + UT-01..UT-04 (UI)
├── db_check.log           — CT-15, CT-16 (Banco)
├── created_resources.txt  — IDs de todos os recursos criados
├── cookies_sindico_a.txt  — cookie de sessao (renovado para UI tests)
└── screenshots/
    ├── ut01_estrutura_inicial.png
    ├── ut01_bloco_selecionado.png
    ├── ut01_modal_aberto.png
    ├── ut01_form_preenchido.png
    ├── ut01_modal_fechado.png
    ├── ut01_tree_expandido.png
    ├── ut02_modal_aberto.png
    ├── ut02_form_negativo_preenchido.png
    ├── ut02_apos_submit.png
    ├── ut03_modal_aberto.png
    ├── ut03_form_preenchido.png
    ├── ut03_apos_submit.png
    ├── ut04_modal_aberto.png
    ├── ut04_form_preenchido_lowercase.png
    ├── ut04_modal_fechado.png
    └── ut04_tree_expandido.png
```

---

## Status para o Orquestrador

**Status:** PASS  
**Motivo da falha:** N/A — todos os 20 casos passaram  
**Tasks possivelmente impactadas:** nenhuma — todos os casos PASS
