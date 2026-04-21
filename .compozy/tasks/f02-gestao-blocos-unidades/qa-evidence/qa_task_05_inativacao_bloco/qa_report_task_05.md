# QA Report — CF-05 Inativacao de Bloco

**Task ID:** qa_task_05
**Data/Hora:** 2026-04-21T00:30:00Z
**Status Geral:** PASS

---

## Contexto

- **User Story:** CF-05 — Inativacao de Bloco (sindico inativa bloco; soft-delete; sem cascata automatica)
- **Ambiente:** http://localhost:5272/api/v1 (backend) | http://localhost:5174 (apps/sindico)
- **Tipos de teste:** API, Banco, UI
- **Autenticacao:** Sim (cookie session ASP.NET Identity)
- **Bloco principal testado:** Bloco QA-03 (ff2ed42b-e9d9-426e-81fa-6bd51b767174)

---

## Casos de Teste

| ID    | Descricao                                                    | Tipo    | Status |
|-------|--------------------------------------------------------------|---------|--------|
| CT-01 | Happy path — inativar Bloco QA-03 → 200, ativo=false, inativadoEm preenchido | API | PASS |
| CT-02 | Bloco ja inativo → 422 invalid-transition                    | API     | PASS |
| CT-03 | Bloco inexistente → 404                                      | API     | PASS |
| CT-04 | Sem autenticacao → 401                                       | API     | PASS |
| CT-05 | Cross-tenant — sindico A inativar bloco de tenant B → 403    | API     | PASS |
| CT-06 | Persistencia DB — ativo=false, inativado_em, inativado_por   | Banco   | PASS |
| CT-07 | Audit entry event_kind=7 (BlocoInativado)                    | Banco   | PASS |
| CT-08 | GET estrutura sem includeInactive oculta bloco inativo       | API     | PASS |
| CT-09 | GET estrutura com includeInactive=true mostra bloco inativo com ativo=false | API | PASS |
| CT-10 | Sem cascata — unidade permanece ativa apos inativacao do bloco | API+Banco | PASS |
| CT-11 | Criar unidade em bloco inativo → 422 invalid-transition      | API     | PASS |
| UT-01 | Inativar bloco via UI — modal confirma, bloco some da arvore padrao | UI | PASS |
| UT-02 | Toggle "Mostrar inativos" faz bloco inativo reaparecer       | UI      | PASS |
| UT-03 | Modal de confirmacao tem copy pt-BR explicando nao-cascata   | UI      | PASS |

---

## Detalhes por Caso

### CT-01 — Happy Path PASS

**Expected:** 200 + { id, condominioId, nome, ativo:false, inativadoEm: timestamp ISO8601 }
**Actual:** 200 + body correto

Response capturado:
- id: ff2ed42b-e9d9-426e-81fa-6bd51b767174
- condominioId: 4cce551d-4f18-474b-a42a-2deb6c2a0451
- nome: Bloco QA-03
- ativo: false
- inativadoEm: 2026-04-21T00:00:13.9757943Z

**Evidencias:** `requests.log` bloco CT-01

---

### CT-02 — Bloco ja inativo → 422 PASS

**Expected:** 422, type=https://portabox.app/problems/invalid-transition
**Actual:** 422, type=https://portabox.app/problems/invalid-transition

**Evidencias:** `requests.log` bloco CT-02

---

### CT-03 — Bloco inexistente → 404 PASS

**Expected:** 404
**Actual:** 404

**Evidencias:** `requests.log` bloco CT-03

---

### CT-04 — Sem autenticacao → 401 PASS

**Expected:** 401
**Actual:** 401

**Evidencias:** `requests.log` bloco CT-04

---

### CT-05 — Cross-tenant → 403 PASS

**Expected:** 403 ou 404 (contrato aceita ambos)
**Actual:** 403 — sindico A tentou inativar Bloco B de tenant B (id=849b6750-0cca-4cef-a798-5d90d04246ff)
**Verificacao adicional:** Bloco B permanece ativo=True apos a tentativa cross-tenant (nenhuma escrita cross-tenant ocorreu)

**Evidencias:** `requests.log` bloco CT-05 (UPDATED)

---

### CT-06 — Persistencia DB PASS

**Query:** SELECT id, ativo, inativado_em, inativado_por FROM bloco WHERE id = 'ff2ed42b-...'

**Expected:** ativo=f, inativado_em=non-null, inativado_por=sindico A user ID
**Actual:**
- ativo: f
- inativado_em: 2026-04-21 00:00:13.975794+00
- inativado_por: 9ae7217c-7c68-43ba-b663-63bb9f235d97 (sindico A — correto)

**Evidencias:** `db_check.log` bloco CT-06

---

### CT-07 — Audit entry BlocoInativado (event_kind=7) PASS

**Query:** SELECT event_kind, performed_by_user_id, metadata_json FROM tenant_audit_log WHERE event_kind=7 ORDER BY occurred_at DESC LIMIT 3

**Expected:** ao menos 1 registro com event_kind=7, performed_by_user_id=sindico A, metadata_json com nome/blocoId
**Actual:** 2 registros encontrados

Registro do Bloco QA-03:
- event_kind: 7
- performed_by_user_id: 9ae7217c-7c68-43ba-b663-63bb9f235d97
- metadata_json: {"nome": "Bloco QA-03", "blocoId": "ff2ed42b-e9d9-426e-81fa-6bd51b767174"}
- occurred_at: 2026-04-21 00:00:13.976635+00

**Shape do metadata_json:** { "nome": string, "blocoId": uuid }

**Nota schema:** Tabela = tenant_audit_log (nao tenant_audit_entry). event_kind=7 = BlocoInativado (consistente com event_kind=5 = BlocoCriado descoberto em task_01).

**Evidencias:** `db_check.log` bloco CT-07

---

### CT-08 — GET estrutura sem includeInactive oculta bloco PASS

**Expected:** Bloco QA-03 NAO presente em GET /estrutura (default, includeInactive=false)
**Actual:** Bloco QA-03 nao encontrado na lista de blocos — status 200

**Evidencias:** `requests.log` bloco CT-08

---

### CT-09 — GET estrutura com includeInactive=true mostra bloco PASS

**Expected:** Bloco QA-03 presente com ativo=false em GET /estrutura?includeInactive=true
**Actual:** Bloco QA-03 encontrado na resposta com ativo=false

**Evidencias:** `requests.log` bloco CT-09

---

### CT-10 — Sem cascata automatica PASS

**Passos executados:**
1. Criar bloco "Bloco Temp Cascata QA" → 201, id=ac7b8af5-4bfe-47f0-84b1-c03bc3160895
2. Criar unidade andar=1, numero=101 no novo bloco → 201, id=1e990dbb-aed4-40c0-a48b-7a721692f872
3. Inativar o bloco → 200, ativo=false
4. SELECT ativo FROM unidade WHERE id = '1e990dbb-...' → resultado: t (TRUE)

**Expected:** Unidade permanece ativa (sem cascata automatica — PRD Non-Goal)
**Actual:** Unidade ativo=t (TRUE) — sem cascata confirmada

**Nota PRD:** "Non-Goals: Inativacao em cascata automatica" — comportamento correto implementado.

**Evidencias:** `requests.log` bloco CT-10, `db_check.log` bloco CT-10

---

### CT-11 — Criar unidade em bloco inativo → 422 PASS

**Expected:** 422, type=https://portabox.app/problems/invalid-transition
**Actual:** 422, type=https://portabox.app/problems/invalid-transition

Tentativa de POST /blocos/{blocoQA03}/unidades com Bloco QA-03 inativo — rejeitada corretamente.

**Evidencias:** `requests.log` bloco CT-11

---

### UT-01 — Inativar bloco via UI PASS

**Passos executados:**
1. Navegar para /estrutura com cookie autenticado (CORS fix via page.route)
2. Arvore carregou com "Bloco UI Inativar QA" visivel
3. Clicar "Acoes" do bloco — menu aberto com opcoes "Renomear" e "Inativar"
4. Clicar "Inativar" — modal de confirmacao aberto
5. Clicar "Inativar bloco" — confirmacao enviada
6. Aguardar refresh da arvore

**Expected:** Bloco desaparece da arvore padrao (includeInactive=false)
**Actual:** Bloco "Bloco UI Inativar QA" nao mais visivel na arvore pos-inativacao

**Screenshot pos-inativacao:** ut01_pos_inativacao.png — arvore mostra 3 blocos (QA-01, QA-02 Renomeado, UI-QA-01) sem o inativado

**Nota ambiente:** A API ao responder a requests cross-origin (5174 -> 5272) com Access-Control-Allow-Origin: * e credentials:include e bloqueada pelo browser (CORS). O test usa page.route para interceptar respostas de 5272 e substituir o header por allow-origin especifico para 5174. Esta e uma falha de configuracao CORS do servidor (mesma causa raiz do FAIL UT-01 em qa_task_01).

**Evidencias:**
- `screenshots/ut01_inicio.png`
- `screenshots/ut01_estrutura_com_arvore.png`
- `screenshots/ut01_menu_acoes_aberto.png`
- `screenshots/ut01_modal_confirmacao.png`
- `screenshots/ut01_pos_inativacao.png`

---

### UT-02 — Toggle Mostrar inativos PASS

**Passos executados:**
1. Navegar /estrutura — arvore sem inativos (default)
2. Marcar checkbox "Mostrar inativos"
3. Aguardar refresh

**Expected:** Bloco "Bloco UI Inativar QA" aparece na arvore com visual de inativo
**Actual:** Bloco aparece — screenshot ut02_com_inativos.png mostra 7 blocos incluindo os inativos com icone de status diferenciado (icone de desligar) e visual descontrastandado

Verificado na screenshot: blocos inativos sao exibidos com icone de power-off e fundo acinzentado, diferenciando visualmente dos blocos ativos.

**Evidencias:**
- `screenshots/ut02_default_sem_inativos.png`
- `screenshots/ut02_com_inativos.png`

---

### UT-03 — Modal copy pt-BR sobre nao-cascata PASS

**Modal capturado (bloco Bloco QA-01):**
Titulo: "Inativar bloco"
Descricao: "Inativar Bloco QA-01 vai oculta-lo de novos cadastros; unidades ativas permanecem e precisam ser inativadas separadamente."
Botoes: "Cancelar" | "Inativar bloco"

**Assertions verificadas:**
- hasPtBrInativar: true ("Inativar" presente) — PASS
- hasNoCascadeMsg: true ("permanecem" e "separadamente" presentes) — PASS
- hasCancelBtn: true ("Cancelar" presente) — PASS

**Evidencias:**
- `screenshots/ut03_modal_copy.png` — modal visivel
- `screenshots/ut03_modal_texto_completo.png` — modal com texto completo

---

## Descobertas Notaveis

### BUG-01: Configuracao CORS incompativel com credentials:include (pre-existente, ja documentada em task_01)
**Descricao:** O servidor ASP.NET Core retorna Access-Control-Allow-Origin: * nas respostas da API. Quando o frontend (porta 5174) faz fetch com credentials:include para a API (porta 5272), o browser bloqueia a resposta porque o CORS spec proibe wildcard com credentials. Isso impede que o app funcione em dev server sem configuracao adicional (VITE_API_BASE_URL ou proxy).
**Impacto:** Testes UI requerem workaround (page.route para fixar CORS). Usuarios em dev sem .env correto nao conseguem usar o app.
**Recomendacao para dev:** Configurar VITE_API_BASE_URL=http://localhost:5272/api na .env.local OU adicionar proxy no vite.config.ts.

---

## Resumo de Evidencias

```
qa_task_05_inativacao_bloco/
├── test_plan.md
├── qa_report_task_05.md
├── created_resources.txt
├── requests.log              (CT-01 a CT-11, UT-01/02/03 browser logs)
├── db_check.log              (CT-06, CT-07, CT-10)
└── screenshots/
    ├── ut01_inicio.png
    ├── ut01_estrutura_com_arvore.png
    ├── ut01_menu_acoes_aberto.png
    ├── ut01_modal_confirmacao.png
    ├── ut01_pos_inativacao.png
    ├── ut02_default_sem_inativos.png
    ├── ut02_com_inativos.png
    ├── ut03_menu_acoes.png
    ├── ut03_modal_copy.png
    └── ut03_modal_texto_completo.png
```

---

## Status para o Orquestrador

**Status:** PASS
**Todas as 14 cases passaram** (11 API/Banco + 3 UI)

**Estado final de Bloco QA-03:**
- id: ff2ed42b-e9d9-426e-81fa-6bd51b767174
- nome: Bloco QA-03
- ativo: false
- inativadoEm: 2026-04-21T00:00:13.975794Z
- inativadoPor: 9ae7217c-7c68-43ba-b663-63bb9f235d97 (sindico A)
- Pronto para qa_task_06 (reativacao)

**Nota cross-task:**
- BUG CORS pre-existente (documentado em task_01 FAIL-03) continua presente
- Workaround via page.route funciona corretamente para os testes
- qa_task_06 (reativacao) pode iniciar: Bloco QA-03 esta inativo conforme esperado
