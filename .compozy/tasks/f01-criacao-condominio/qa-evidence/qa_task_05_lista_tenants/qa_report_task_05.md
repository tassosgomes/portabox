# QA Report — Lista de Tenants no Backoffice PortaBox (CF5)

**Task ID:** qa_task_05
**Data/Hora (rerun2):** 2026-04-20T17:01:20Z
**Status Geral:** PASS — 8/8 casos de teste aprovados

---

## Contexto

- **User Story:** CF5 — Listagem de tenants com filtros por status e busca por nome/CNPJ
- **Ambiente:** http://localhost:5173 (Frontend) / http://localhost:5272 (API)
- **Tipos de teste:** UI (Playwright — `qa_task_05_rerun2.spec.ts`)
- **Autenticacao:** Cookie de sessao via login no frontend (operator@portabox.dev)

---

## Estado do banco confirmado via API (execucao rerun2)

| Nome | ID | Status | CNPJ mascarado | Ativado em |
|------|----|--------|----------------|------------|
| Residencial Rerun QA | 4a3d87ea-f62f-4d9c-80de-a34237d0dae3 | 2 (Ativo) | ****7000161 | 2026-04-20T16:55:45 |
| Residencial Teste QA API Check | 1792efd1-3e2b-4156-8e55-7b7c43576fe0 | 1 (PreAtivo) | ****8000195 | — |
| Residencial Teste QA | f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5 | 2 (Ativo) | ****3000181 | 2026-04-19T01:22:51 |

**totalCount: 3** (PAGE_SIZE=20 — totalPages=1, paginacao nao deve aparecer)

NOTA IMPORTANTE: O briefing da rerun2 informava "Residencial Rerun QA" como status=1
(PreAtivo) e "Residencial Teste QA" como unico status=2 (Ativo). Porem, ao iniciar
a execucao, a API revelou que "Residencial Rerun QA" foi ativado em
2026-04-20T16:55:45 — durante a sessao de testes imediatamente anterior a esta.
Os casos de teste foram adaptados ao estado real do banco confirmado via API
antes da execucao dos specs Playwright.

---

## Casos de Teste

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| TC-01 | Lista renderiza sem crash e exibe tenants | UI | PASS (rerun1) |
| TC-02 | Colunas obrigatorias presentes no header | UI | PASS (rerun1) |
| TC-03 | Filtro Pre-ativo: PreAtivos visiveis; Ativos ausentes | UI | PASS (rerun2) |
| TC-04 | Filtro Ativo: ambos Ativos visiveis; PreAtivo ausente | UI | PASS (rerun2) |
| TC-05 | Busca por nome "Rerun" retorna apenas Rerun QA | UI | PASS (rerun2) |
| TC-06 | Busca por CNPJ "444" retorna apenas Rerun QA | UI | PASS (rerun2) |
| TC-07 | Clique em Rerun QA redireciona para URL correta | UI | PASS (rerun2) |
| TC-08 | Paginacao ausente com totalCount=3 < PAGE_SIZE=20 | UI | PASS (rerun2) |

---

## Detalhes por Caso

### TC-01 — Lista renderiza sem crash e exibe tenants PASS (rerun1)

**Expected:** Pagina /condominios renderiza sem crash React (zero page errors) e exibe
ao menos um dos tenants documentados.

**Actual:**
- Zero page errors detectados — bug cnpjMasked confirmado corrigido
- "Residencial Teste QA" visivel: true
- "Residencial Rerun QA" visivel: true
- Body text length: 439 (pagina nao esta em branco)
- Tabela renderizada com thead e tbody presentes

**Evidencias:** `screenshots/rerun_tc01_*.png`, `requests.log` — bloco RERUN TC-01

---

### TC-02 — Colunas da tabela estao presentes PASS (rerun1)

**Expected:** Colunas "Nome", "CNPJ", "Status", "Criado em", "Ativado em" presentes.

**Actual:** Headers encontrados: ["Nome","CNPJ","Status","Criado em","Ativado em"] — todos presentes.

**Evidencias:** `screenshots/rerun_tc02_*.png`, `requests.log` — bloco RERUN TC-02

---

### TC-03 — Filtro Pre-ativo: PreAtivos visiveis; Ativos ausentes PASS (rerun2)

**Contexto:** Na rerun1, assertion falhava por colisao de substring: o locator
`filter({ hasText: "Residencial Teste QA" })` encontrava tambem "Residencial Teste QA API Check".
Na rerun2, foi usada assertion de texto exato (`getByText(name, { exact: true })`).
Adicionalmente, o briefing da rerun2 estava desatualizado — "Residencial Rerun QA" ja
e status=2 (Ativo). O teste foi adaptado ao estado real confirmado via API.

**Expected:**
- "Residencial Teste QA API Check" (status=1=PreAtivo): visivel (count >= 1)
- "Residencial Teste QA" (status=2=Ativo, exact match): ausente (count=0)
- "Residencial Rerun QA" (agora status=2=Ativo): ausente (count=0)

**Actual:**
- "Residencial Teste QA API Check": count=1 — CORRETO
- "Residencial Teste QA" (exact): count=0 — CORRETO
- "Residencial Rerun QA": count=0 — CORRETO

Tabela com filtro Pre-ativo (log):
```
["Residencial Teste QA API Check****8000195Pré-ativo18/04/2026—"]
```

**Evidencias:**
- `screenshots/rerun2_tc03_antes_filtro.png`
- `screenshots/rerun2_tc03_resultado_filtro.png`
- `screenshots/rerun2_tc03_pass.png`
- `requests.log` — bloco RERUN2 TC-03

---

### TC-04 — Filtro Ativo: ambos Ativos visiveis; PreAtivo ausente PASS (rerun2)

**Expected:**
- "Residencial Rerun QA" (status=2=Ativo): visivel (count >= 1)
- "Residencial Teste QA" (status=2=Ativo, exact match): visivel (count >= 1)
- "Residencial Teste QA API Check" (status=1=PreAtivo): ausente (count=0)

**Actual:**
- "Residencial Rerun QA": count=1 — CORRETO
- "Residencial Teste QA" (exact): count=1 — CORRETO
- "Residencial Teste QA API Check": count=0 — CORRETO

Tabela com filtro Ativo (log):
```
["Residencial Rerun QA****7000161Ativo20/04/202620/04/2026",
 "Residencial Teste QA****3000181Ativo18/04/202618/04/2026"]
```

**Evidencias:**
- `screenshots/rerun2_tc04_antes_filtro.png`
- `screenshots/rerun2_tc04_resultado_filtro.png`
- `screenshots/rerun2_tc04_pass.png`
- `requests.log` — bloco RERUN2 TC-04

---

### TC-05 — Busca por nome "Rerun" retorna apenas Residencial Rerun QA PASS (rerun2)

**Expected:**
- "Residencial Rerun QA": visivel (count >= 1)
- "Residencial Teste QA" (exact): ausente (count=0)

**Actual:**
- "Residencial Rerun QA": count=1 — CORRETO
- "Residencial Teste QA" (exact): count=0 — CORRETO

Tabela apos busca "Rerun" (log):
```
["Residencial Rerun QA****7000161Ativo20/04/202620/04/2026"]
```

**Evidencias:**
- `screenshots/rerun2_tc05_resultado_busca.png`
- `screenshots/rerun2_tc05_pass.png`
- `requests.log` — bloco RERUN2 TC-05

---

### TC-06 — Busca por CNPJ "444" retorna apenas Residencial Rerun QA PASS (rerun2)

**Premissa:** CNPJ de "Residencial Rerun QA" e 11.444.777/0001-61; parcial "444"
deve retornar apenas ele (confirmado via API: q=444 retorna totalCount=1).

**Expected:**
- "Residencial Rerun QA": visivel (count >= 1)
- "Residencial Teste QA" (exact): ausente (count=0)
- "Residencial Teste QA API Check": ausente (count=0)

**Actual:**
- "Residencial Rerun QA": count=1 — CORRETO
- "Residencial Teste QA" (exact): count=0 — CORRETO
- "Residencial Teste QA API Check": count=0 — CORRETO

Tabela apos busca "444" (log):
```
["Residencial Rerun QA****7000161Ativo20/04/202620/04/2026"]
```

**Evidencias:**
- `screenshots/rerun2_tc06_resultado_busca.png`
- `screenshots/rerun2_tc06_pass.png`
- `requests.log` — bloco RERUN2 TC-06

---

### TC-07 — Clique em Residencial Rerun QA redireciona para URL correta PASS (rerun2)

**Expected:**
- href do link: `/condominios/4a3d87ea-f62f-4d9c-80de-a34237d0dae3`
- URL apos clique: `http://localhost:5173/condominios/4a3d87ea-f62f-4d9c-80de-a34237d0dae3`
- URL nao deve conter "/undefined"

**Actual:**
- href encontrado: `/condominios/4a3d87ea-f62f-4d9c-80de-a34237d0dae3` — CORRETO
- URL apos clique: `http://localhost:5173/condominios/4a3d87ea-f62f-4d9c-80de-a34237d0dae3` — CORRETO
- "/undefined": nao presente — CORRETO

**Evidencias:**
- `screenshots/rerun2_tc07_lista_completa.png`
- `screenshots/rerun2_tc07_apos_clique.png`
- `screenshots/rerun2_tc07_pass.png`
- `requests.log` — bloco RERUN2 TC-07

---

### TC-08 — Paginacao ausente com totalCount=3 < PAGE_SIZE=20 PASS (rerun2)

**Expected:**
- Controles de paginacao (nav role, botoes Proxima/Anterior) ausentes
- Total de linhas na tabela: 3 (= totalCount do banco)
- Total de linhas <= 20 (PAGE_SIZE)

**Actual:**
- Total de linhas na tabela: 3 — CORRETO
- Botao "Proxima" encontrado: 0 — CORRETO
- Botao "Anterior" encontrado: 0 — CORRETO
- paginationFound: false — CORRETO

**Evidencias:**
- `screenshots/rerun2_tc08_lista.png`
- `screenshots/rerun2_tc08_pass.png`
- `requests.log` — bloco RERUN2 TC-08

---

## Resumo de Evidencias

```
qa_task_05_lista_tenants/
├── qa_report_task_05.md         (este arquivo — sobrescrito)
├── test_plan.md
├── requests.log
├── auth_state.json
├── screenshots/
│   ├── rerun_setup_login*.png          (rerun1)
│   ├── rerun_tc01_*.png                (rerun1)
│   ├── rerun_tc02_*.png                (rerun1)
│   ├── rerun_tc03_*.png                (rerun1 — FAIL por substring collision)
│   ├── rerun2_setup_*.png              (rerun2)
│   ├── rerun2_tc03_*.png               (rerun2 — PASS)
│   ├── rerun2_tc04_*.png               (rerun2 — PASS)
│   ├── rerun2_tc05_*.png               (rerun2 — PASS)
│   ├── rerun2_tc06_*.png               (rerun2 — PASS)
│   ├── rerun2_tc07_*.png               (rerun2 — PASS)
│   └── rerun2_tc08_*.png               (rerun2 — PASS)
└── videos/
```

---

## Status para o Orquestrador

**Status:** PASS

**TC-01:** PASS (rerun1)
**TC-02:** PASS (rerun1)
**TC-03:** PASS (rerun2) — assertion corrigida para texto exato; estado do banco
  atualizado para refletir que "Residencial Rerun QA" foi ativado (status=2) em
  sessao anterior. Filtro Pre-ativo retorna corretamente apenas tenants status=1.
**TC-04:** PASS (rerun2) — filtro Ativo retorna ambos tenants Ativos e exclui PreAtivo.
**TC-05:** PASS (rerun2) — busca por nome "Rerun" retorna apenas "Residencial Rerun QA".
**TC-06:** PASS (rerun2) — busca por CNPJ "444" retorna apenas "Residencial Rerun QA".
**TC-07:** PASS (rerun2) — clique navega para /condominios/4a3d87ea-f62f-4d9c-80de-a34237d0dae3.
**TC-08:** PASS (rerun2) — paginacao ausente com 3 tenants < PAGE_SIZE=20.

**Mudanca de estado documentada:**
O tenant "Residencial Rerun QA" foi ativado (status PreAtivo -> Ativo) em
2026-04-20T16:55:45 durante a sessao de testes anterior a esta. O briefing da
rerun2 estava desatualizado. Os testes foram adaptados ao estado real do banco
confirmado via API antes da execucao Playwright.
