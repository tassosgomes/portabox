# QA Report — CF-03 Edicao do Nome de Bloco

**Task ID:** qa_task_03
**Data/Hora:** 2026-04-21T00:50:00Z
**Status Geral:** FAIL

---

## Contexto

- **User Story:** CF-03 — Edicao do Nome de Bloco
- **Ambiente:** http://localhost:5272/api/v1 (backend) | http://localhost:5174 (apps/sindico)
- **Tipos de teste:** API, Banco, UI
- **Autenticacao:** Sim (cookie session ASP.NET Identity)

---

## Casos de Teste

| ID    | Descricao                                                        | Tipo  | Status |
|-------|------------------------------------------------------------------|-------|--------|
| CT-01 | Happy path — renomear Bloco QA-02 para "Bloco QA-02 Renomeado"  | API   | PASS   |
| CT-02 | Mesmo nome deve retornar 422 invalid-transition                  | API   | FAIL   |
| CT-03 | Nome vazio deve retornar 400                                     | API   | PASS   |
| CT-04 | Nome > 50 chars deve retornar 400                                | API   | PASS   |
| CT-05 | Conflito canonico deve retornar 409                              | API   | PASS   |
| CT-06 | Bloco inexistente deve retornar 404                              | API   | PASS   |
| CT-07 | Sem autenticacao deve retornar 401                               | API   | PASS   |
| CT-08 | Cross-tenant deve retornar 403                                   | API   | PASS   |
| CT-09 | Renomear bloco inativo deve retornar 422 invalid-transition      | API   | FAIL   |
| CT-10 | Persistencia DB — verificar nome no banco                        | Banco | PASS   |
| CT-11 | Audit entry event_kind=6 com nomeAntes/nomeDepois                | Banco | PASS   |
| CT-12 | Rename sequencial — renomear para "Bloco QA-02 V3"              | API   | PASS   |
| UT-01 | Renomear Bloco UI-QA-01 via interface para "Bloco UI-QA-01 Editado" | UI | PASS   |
| UT-02 | Renomear com nome vazio — erro visivel                           | UI    | PASS*  |
| UT-03 | Renomear para nome conflitante — toast/erro visivel              | UI    | PASS   |

*UT-02: A assertion Playwright falhou por regex inadequado na spec, mas o comportamento da UI e CORRETO — a mensagem "Informe o nome do bloco." e exibida em vermelho abaixo do campo. Classificado como PASS com nota de spec defect.

---

## Detalhes por Caso

### CT-01 — Happy path: renomear Bloco QA-02 PASS

**Expected:** HTTP 200, body com `nome` = "Bloco QA-02 Renomeado", `ativo` = true
**Actual:** HTTP 200, body com `nome` = "Bloco QA-02 Renomeado", `ativo` = true

**Evidencias:** `requests.log` — CT-01

---

### CT-02 — Mesmo nome deve retornar 422 invalid-transition FAIL

**Passos executados:**
1. Autenticacao como Sindico A — OK
2. PATCH Bloco QA-02 com nome "Bloco QA-02 Renomeado" (nome atual apos CT-01)
3. Servidor retornou 400 em vez de 422

**Expected:**
- HTTP Status: 422
- type: `https://portabox.app/problems/invalid-transition`

**Actual:**
- HTTP Status: 400
- type: `https://portabox.app/problems/validation-error`
- detail: "O novo nome do bloco deve ser diferente do nome atual."

**Erro capturado:**
```json
{
    "type": "https://portabox.app/problems/validation-error",
    "title": "Falha de validacao",
    "status": 400,
    "detail": "O novo nome do bloco deve ser diferente do nome atual.",
    "instance": "/api/v1/condominios/4cce551d-4f18-474b-a42a-2deb6c2a0451/blocos/4c936a72-fc12-4f32-809f-3f290c4bc8ae"
}
```

**Contrato violado:** api-contract.yaml define 422 com schema `UnprocessableEntity` para "Tentativa de operacao impossivel por estado atual". A implementacao trata como 400 validation-error em vez de 422 invalid-transition.

**Evidencias:** `requests.log` — CT-02

---

### CT-03 — Nome vazio deve retornar 400 PASS

**Expected:** HTTP 400
**Actual:** HTTP 400 (validation error)

**Evidencias:** `requests.log` — CT-03

---

### CT-04 — Nome > 50 chars deve retornar 400 PASS

**Expected:** HTTP 400
**Actual:** HTTP 400 (nome de 76 chars — validation error)

**Evidencias:** `requests.log` — CT-04

---

### CT-05 — Conflito canonico deve retornar 409 PASS

**Expected:** HTTP 409 (tentar renomear QA-02 para "Bloco QA-01" que ja existe)
**Actual:** HTTP 409

**Evidencias:** `requests.log` — CT-05

---

### CT-06 — Bloco inexistente deve retornar 404 PASS

**Expected:** HTTP 404
**Actual:** HTTP 404

**Evidencias:** `requests.log` — CT-06

---

### CT-07 — Sem autenticacao deve retornar 401 PASS

**Expected:** HTTP 401
**Actual:** HTTP 401

**Evidencias:** `requests.log` — CT-07

---

### CT-08 — Cross-tenant deve retornar 403 PASS

**Expected:** HTTP 403 ou 404 (Sindico B tenta renomear bloco de Tenant A)
**Actual:** HTTP 403

**Evidencias:** `requests.log` — CT-08

---

### CT-09 — Renomear bloco inativo deve retornar 422 FAIL

**Passos executados:**
1. Criar "Bloco Temp Rename QA" — HTTP 201, id=e6451b5b-bc1d-47e1-9300-2c5a50244273
2. Inativar via POST .../blocos/{id}:inativar — HTTP 200, ativo=false
3. PATCH para renomear bloco inativo
4. Servidor retornou 404 em vez de 422

**Expected:**
- HTTP Status: 422
- type: `https://portabox.app/problems/invalid-transition`
- title: Similar a "Transicao invalida"

**Actual:**
- HTTP Status: 404
- type: `https://portabox.app/problems/not-found`
- detail: "Bloco nao encontrado"

**Erro capturado:**
```json
{
    "type": "https://portabox.app/problems/not-found",
    "title": "Recurso nao encontrado",
    "status": 404,
    "detail": "Bloco nao encontrado",
    "instance": "/api/v1/condominios/4cce551d-4f18-474b-a42a-2deb6c2a0451/blocos/e6451b5b-bc1d-47e1-9300-2c5a50244273"
}
```

**Analise:** O endpoint PATCH para renomear bloco exclui blocos inativos da busca, retornando 404 ao inves de encontrar o bloco e rejeitar a operacao com 422. O contrato especifica que blocos inativos devem retornar 422 invalid-transition ao se tentar renomea-los. A implementacao trata a ausencia de blocos ativos como "nao encontrado".

**Estado pos-teste:** Bloco Temp Rename QA (id=e6451b5b-...) permanece inativo conforme instrucao.

**Evidencias:** `requests.log` — CT-09

---

### CT-10 — Persistencia DB PASS

**Expected:** `SELECT nome FROM bloco WHERE id = '4c936a72-...'` retorna nome valido
**Actual:** "Bloco QA-02 Renomeado" (nome correto apos CT-01; CT-12 ainda nao executado no momento desta query)

**Evidencias:** `requests.log` — CT-10, `db_check.log`

---

### CT-11 — Audit entry event_kind=6 PASS

**Expected:** Registros com event_kind=6 e `nomeAntes`/`nomeDepois` no metadata_json
**Actual:** 1 registro encontrado:
```
event_kind=6 | metadata_json={"blocoId": "4c936a72-...", "nomeAntes": "Bloco QA-02", "nomeDepois": "Bloco QA-02 Renomeado"} | occurred_at=2026-04-21 00:00:24
```

**Evidencias:** `requests.log` — CT-11, `db_check.log`

---

### CT-12 — Rename sequencial PASS

**Expected:** HTTP 200, nome="Bloco QA-02 V3", 2a entrada no audit log para QA-02
**Actual:** HTTP 200, nome="Bloco QA-02 V3", 2 entradas de audit para QA-02:
- "Bloco QA-02" → "Bloco QA-02 Renomeado" (CT-01)
- "Bloco QA-02 Renomeado" → "Bloco QA-02 V3" (CT-12)

**Evidencias:** `requests.log` — CT-12, `db_check.log`

---

### UT-01 — Renomear Bloco UI-QA-01 para "Bloco UI-QA-01 Editado" PASS

**Passos executados:**
1. Navegar para /estrutura com cookie autenticado (CORS fix via page.route)
2. Arvore carregou com "Bloco UI-QA-01" visivel
3. Clicar "Acoes" do bloco — menu aberto com opcoes "Renomear" e "Inativar"
4. Clicar "Renomear" — modal BlocoForm aberto com titulo "Renomear bloco"
5. Limpar campo e preencher com "Bloco UI-QA-01 Editado"
6. Clicar "Salvar nome" — API retornou 200
7. Arvore atualizada: "Bloco UI-QA-01 Editado" visivel

**Expected:** Novo nome visivel na arvore
**Actual:** "Bloco UI-QA-01 Editado" visivel na arvore (screenshot ut01_pos_rename.png confirma)

**Screenshots:**
- `screenshots/ut01_inicio.png` — estado inicial da arvore
- `screenshots/ut01_menu_acoes.png` — menu aberto com "Renomear"
- `screenshots/ut01_modal_renomear.png` — modal BlocoForm aberto
- `screenshots/ut01_nome_preenchido.png` — novo nome digitado
- `screenshots/ut01_pos_rename.png` — arvore com novo nome confirmado

---

### UT-02 — Renomear com nome vazio — erro visivel PASS*

**Nota:** Playwright assertion FALHOU por regex inadequado na spec (`/obrigat|requerido|min.*car|deve ter|invalid|erro/i` nao cobre "Informe o nome do bloco."). O COMPORTAMENTO DA UI E CORRETO.

**Passos executados:**
1. Abrir modal de renomear bloco
2. Limpar campo nome e submeter vazio
3. UI exibe mensagem de erro em vermelho

**Expected:** Mensagem de erro visivel para nome vazio
**Actual:** Mensagem "Informe o nome do bloco." exibida em vermelho abaixo do input (campo com borda laranja de erro)

**Modal text capturado:** "Renomear bloco✕Nome do blocoInforme o nome do bloco.CancelarSalvar nome"

**Defect na spec:** A regex de assertion nao incluia o padrao "Informe" — o elemento de erro nao tinha `role="alert"` e sim uma class CSS especifica nao coberta pelo seletor. A UI funciona corretamente.

**Screenshots:**
- `screenshots/ut02_inicio.png` — estado inicial
- `screenshots/ut02_nome_vazio.png` — campo vazio antes de submit
- `screenshots/ut02_erro_visivel.png` — erro "Informe o nome do bloco." visivel em vermelho

---

### UT-03 — Renomear para nome conflitante — erro de conflito visivel PASS

**Passos executados:**
1. Abrir modal de renomear para Bloco QA-02 V3
2. Preencher com "Bloco QA-01" (nome de outro bloco ativo)
3. Clicar "Salvar nome" — API retornou 409
4. Toast "Ja existe bloco ativo com este nome" apareceu no topo
5. Mensagem inline "Ja existe bloco ativo com este nome" tambem exibida no modal

**Expected:** Toast ou erro indicando conflito de nome
**Actual:** Toast + mensagem inline "Ja existe bloco ativo com este nome" visiveis

**Screenshots:**
- `screenshots/ut03_inicio.png` — estado inicial
- `screenshots/ut03_menu_aberto.png` — menu acoes
- `screenshots/ut03_nome_conflitante.png` — nome conflitante digitado
- `screenshots/ut03_pos_submit.png` — estado intermediario pos-submit
- `screenshots/ut03_erro_conflito.png` — toast + erro inline visiveis

---

## Estado Final dos Blocos

| ID | Nome atual | Ativo |
|----|-----------|-------|
| 88037273-d560-4415-a1e2-b45a00dc5be4 | Bloco QA-01 | true |
| 4c936a72-fc12-4f32-809f-3f290c4bc8ae | Bloco QA-02 V3 | true |
| ff2ed42b-e9d9-426e-81fa-6bd51b767174 | Bloco QA-03 | false |
| 9aab2b47-1672-4686-ac12-6ce39b4c0f50 | Bloco UI-QA-01 Editado | true |
| e6451b5b-bc1d-47e1-9300-2c5a50244273 | Bloco Temp Rename QA | false |

---

## Resumo de Evidencias

```
qa_task_03_edicao_nome_bloco/
├── test_plan.md
├── qa_report_task_03.md
├── requests.log           — CT-01..CT-12 + UI logs
├── db_check.log           — CT-10, CT-11, CT-12 audit, estado final
├── created_resources.txt  — estado final dos blocos
├── screenshots/
│   ├── ut01_inicio.png
│   ├── ut01_menu_acoes.png
│   ├── ut01_modal_renomear.png
│   ├── ut01_nome_preenchido.png
│   ├── ut01_pos_rename.png
│   ├── ut02_inicio.png
│   ├── ut02_nome_vazio.png
│   ├── ut02_erro_visivel.png
│   ├── ut03_inicio.png
│   ├── ut03_menu_aberto.png
│   ├── ut03_nome_conflitante.png
│   ├── ut03_pos_submit.png
│   └── ut03_erro_conflito.png
└── videos/                — gravacoes Playwright dos 3 testes UI
```

---

## Status para o Orquestrador

**Status:** FAIL
**Total de casos:** 15 (CT-01..CT-12 + UT-01..UT-03)
**PASS:** 13 | **FAIL:** 2

### Falhas

**CT-02 — Mesmo nome retorna 400 em vez de 422:**
O endpoint PATCH retorna HTTP 400 (`validation-error`) ao receber o nome atual do bloco, enquanto o contrato define HTTP 422 (`invalid-transition`). Semanticamente, "mesmo nome" e tratado como falha de validacao de entrada em vez de transicao de estado invalida.

**CT-09 — Renomear bloco inativo retorna 404 em vez de 422:**
O endpoint PATCH exclui blocos inativos da busca e retorna HTTP 404 (`not-found`) ao se tentar renomear um bloco inativo. O contrato especifica HTTP 422 (`invalid-transition`). A implementacao nao distingue "bloco nao existe" de "bloco inativo nao pode ser renomeado".

**Tasks possivelmente impactadas:**
- qa_task_06 (reativacao) — pode ter discrepancias similares em codigos de status de transicao invalida
- qa_task_10 (audit_trail) — validacao de audit para renomear bloco ja foi feita aqui (OK)
