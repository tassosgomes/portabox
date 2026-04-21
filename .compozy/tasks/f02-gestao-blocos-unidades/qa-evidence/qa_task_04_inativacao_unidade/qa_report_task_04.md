# QA Report — CF-04 Inativacao de Unidade

**Task ID:** qa_task_04
**Data/Hora:** 2026-04-21T01:00:00Z
**Status Geral:** FAIL

---

## Contexto

- **User Story:** Como sindico, quero inativar uma unidade para que ela deixe de aparecer nos cadastros ativos, sem perder o historico
- **Ambiente:** http://localhost:5272/api/v1 (backend) | http://localhost:5174 (sindico app)
- **Tenant A:** 4cce551d-4f18-474b-a42a-2deb6c2a0451
- **Bloco QA-01:** 88037273-d560-4415-a1e2-b45a00dc5be4
- **Tipos de teste:** API, Banco, UI
- **Autenticacao:** Sim (cookie portabox.auth, sindico A e B)

---

## Casos de Teste

| ID    | Descricao                                                        | Tipo  | Status    |
|-------|------------------------------------------------------------------|-------|-----------|
| CT-01 | Happy path — inativar unidade nova (andar=7, numero=701)        | API   | PASS      |
| CT-02 | Unidade ja inativa — 422 invalid-transition                     | API   | PASS      |
| CT-03 | Unidade inexistente (GUID valido) — esperado 404, recebido 400  | API   | FAIL      |
| CT-04 | BlocoId errado no path — 400 (aceito 400 ou 404)                | API   | PASS      |
| CT-05 | Sem autenticacao — 401                                           | API   | PASS      |
| CT-06 | Cross-tenant sindico A tentando inativar unidade de tenant B    | API   | PASS      |
| CT-07 | Persistencia DB — ativo=f, inativado_em, inativado_por corretos  | Banco | PASS      |
| CT-08 | Audit entry event_kind=10 (UnidadeInativada)                    | Banco | PASS      |
| CT-09 | GET estrutura sem includeInactive oculta unidade inativa        | API   | PASS      |
| CT-10 | GET estrutura?includeInactive=true mostra unidade com ativo=false| API   | PASS      |
| CT-11 | Moradores associados continuam ligados                           | API   | BLOCKED   |
| CT-12 | Inativacao nao afeta outras unidades do bloco                    | API   | PASS      |
| UT-01 | Inativar unidade via modal de confirmacao — some da arvore       | UI    | PASS      |
| UT-02 | Toggle "Mostrar inativos" mostra unidade inativada              | UI    | PASS      |
| UT-03 | Cancelar modal — unidade continua ativa                          | UI    | PASS      |

**Totais:** 12 PASS | 1 FAIL | 1 BLOCKED | 1 PASS-com-ressalva (CT-04)

---

## Detalhes por Caso

### CT-01 — Happy Path PASS

**Pre-condicao:** Unidade criada andar=7 numero="701" (id=d05aca01-c7b2-41b0-8b1b-c2d3b7e45eee)
**Expected:** POST /:inativar retorna 200, ativo=false, inativadoEm preenchido
**Actual:** 200, ativo=false, inativadoEm="2026-04-21T00:43:37.7849393Z"
**Evidencias:** `requests.log` — CT-01 PASS

---

### CT-02 — Unidade ja inativa (422 invalid-transition) PASS

**Expected:** 422, type contendo "invalid-transition"
**Actual:** 422, type=https://portabox.app/problems/invalid-transition, detail="A entidade ja esta inativa."
**Evidencias:** `requests.log` — CT-02 PASS

---

### CT-03 — Unidade inexistente FAIL

**Passos executados:**
1. Tentativa com GUID nao-RFC4122 (a1b2c3d4-e5f6-7890-abcd-ef1234567890) — retornou 400
2. Tentativa com GUID RFC-4122 valido (76e4f30e-49c9-485a-a80e-05d9bb928a89) — retornou 400
3. Segunda tentativa confirmacao (2456ce1b-b6f7-419a-94af-6c2873946429) — retornou 400

**Expected:** 404 (recurso nao encontrado)
**Actual:** 400, type=validation-error, detail="Unidade nao encontrada"

**Erro capturado:**
```
{"type":"https://portabox.app/problems/validation-error","title":"Falha de validação","status":400,"detail":"Unidade nao encontrada","instance":"/api/v1/.../unidades/2456ce1b-...:inativar","traceId":"..."}
```

**Observacao:** O servidor retorna 400 (validation-error) em vez de 404 para recursos inexistentes. Divergencia do contrato esperado que especifica 404 para unidade inexistente.
**Evidencias:** `requests.log` — CT-03 FAIL

---

### CT-04 — BlocoId errado no path PASS (com ressalva)

**Expected:** 404 (pois a unidade nao pertence ao bloco informado) ou 400
**Actual:** 400, type=validation-error, detail="Unidade nao encontrada"
**Observacao:** Retornou 400 em vez de 404. Aceito conforme instrucao de tarefa ("documente o status"). Status 400 indica que o servidor valida a combinacao bloco+unidade no path.
**Evidencias:** `requests.log` — CT-04 PASS

---

### CT-05 — Sem autenticacao PASS

**Expected:** 401
**Actual:** 401
**Evidencias:** `requests.log` — CT-05 PASS

---

### CT-06 — Cross-tenant PASS

**Pre-condicao:** Unidade temporaria criada em tenant B (ea741ce2-4d63-4580-a11f-89176e5341d3)
**Expected:** 403 ou 404
**Actual:** 403, type=forbidden, detail="Você não tem permissão para executar esta operação"
**Evidencias:** `requests.log` — CT-06 PASS

---

### CT-07 — Persistencia DB PASS

**Query:** SELECT id, ativo, inativado_em, inativado_por FROM unidade WHERE id = 'd05aca01-...'
**Expected:** ativo=f, inativado_em preenchido, inativado_por = userId do sindico A
**Actual:** id=d05aca01-c7b2-41b0-8b1b-c2d3b7e45eee | ativo=f | inativado_em=2026-04-21 00:43:37.784939+00 | inativado_por=9ae7217c-7c68-43ba-b663-63bb9f235d97
**Evidencias:** `db_check.log` — CT-07 PASS

---

### CT-08 — Audit Entry PASS

**Query:** SELECT event_kind, metadata_json FROM tenant_audit_log WHERE event_kind=10 ORDER BY occurred_at DESC LIMIT 3
**Expected:** Entrada com event_kind=10 referenciando unidadeId=d05aca01-...
**Actual:** 10|{"andar": 7, "numero": "701", "blocoId": "88037273-...", "unidadeId": "d05aca01-..."}
**Evidencias:** `db_check.log` — CT-08 PASS

---

### CT-09 — GET estrutura sem includeInactive oculta inativa PASS

**Expected:** Unidade 701 (d05aca01) nao aparece na resposta padrao
**Actual:** 200, unidade 701 ausente da resposta (confirmado por grep)
**Evidencias:** `requests.log` — CT-09 PASS

---

### CT-10 — GET estrutura?includeInactive=true mostra inativa PASS

**Expected:** Unidade 701 aparece com ativo=false
**Actual:** 200, unidade 701 encontrada com ativo=false na resposta JSON
**Evidencias:** `requests.log` — CT-10 PASS

---

### CT-11 — Moradores associados BLOCKED

**Motivo:** Feature F03 (Morador) nao implementada nesta sessao QA. Impossivel criar moradores e verificar vinculo apos inativacao. Aguarda implementacao de F03.
**Evidencias:** `requests.log` — CT-11 BLOCKED

---

### CT-12 — Inativacao nao afeta outras unidades do bloco PASS

**Expected:** GET estrutura sem includeInactive mostra unidades 101, 01, 101A, 102B, 501 etc ativas
**Actual:** 200, total de 12 unidades ativas visiveis. Unidade 101 (f2a0b7cc) confirmada presente e ativa. Andar 1 com 3 unidades, Andar 0 com 1, etc.
**Evidencias:** `requests.log` — CT-12 PASS

---

### UT-01 — Inativar via modal de confirmacao PASS

**Passos:**
1. Criou unidade andar=8 numero=802 via API (id=c4c0af70-e2be-4d21-a8fa-d1521791cc56) como pre-step
2. Navegou para /estrutura, expandiu Bloco QA-01 > Andar 8
3. Clicou "Acoes da unidade 802"
4. Clicou "Inativar" no menu
5. Modal apareceu com texto "Moradores associados permanecem vinculados; inative-os separadamente em F03 se necessario."
6. Clicou "Inativar unidade"
7. Modal fechou, unidade 802 sumiu da arvore (Andar 8 passou de 2 para 1 unidade; contador bloco de 14 para 13)

**Expected:** Modal com copy sobre moradores, unidade some da arvore
**Actual:** PASS — modal apareceu com copy sobre moradores, unidade 802 ausente apos confirmacao
**Evidencias:** `screenshots/ut01_modal_confirmacao.png` | `screenshots/ut01_pos_inativacao.png`

---

### UT-02 — Toggle "Mostrar inativos" PASS

**Passos:**
1. Navegou para /estrutura
2. Expandiu Bloco QA-01 — andar 7 ausente (sem unidades ativas)
3. Clicou no checkbox "Mostrar inativos"
4. Re-expandiu Bloco QA-01 > Andar 7
5. Unidade 701 apareceu com badge "Inativo" e icone de estado

**Expected:** Unidade 701 (inativa) aparece com visual de inativa
**Actual:** PASS — unidade 701 visivel com badge "Inativo" (laranja) e icone Power
**Evidencias:** `screenshots/ut02_arvore_com_inativos.png`

---

### UT-03 — Cancelar modal — unidade continua ativa PASS

**Passos:**
1. Navegou para /estrutura, expandiu Bloco QA-01 > Andar 1
2. Unidade 101 visivel
3. Clicou "Acoes da unidade 101", depois "Inativar"
4. Modal de confirmacao apareceu
5. Clicou "Cancelar"
6. Modal fechou, Unidade 101 continua visivel na arvore

**Expected:** Modal fecha, unidade 101 permanece ativa e visivel
**Actual:** PASS — modal fechou sem executar inativacao, unidade 101 permanece presente
**Evidencias:** `screenshots/ut03_modal_confirmacao.png` | `screenshots/ut03_pos_cancelar.png`

---

## Resumo de Falhas

### CT-03 — Divergencia de status code (404 esperado, 400 recebido)

O endpoint `POST /:inativar` com unidade inexistente retorna `400 validation-error` com detail "Unidade nao encontrada" em vez de `404 Not Found`. Contrato especifica 404 para recurso inexistente. O comportamento atual pode dificultar o tratamento de erros pelo cliente, que pode nao distinguir entre "body invalido" e "recurso nao encontrado".

---

## Resumo de Evidencias

```
qa-evidence/qa_task_04_inativacao_unidade/
├── test_plan.md
├── created_resources.txt      — IDs das unidades inativadas
├── requests.log               — CT-01 a CT-12 (API) + UT-01 a UT-03 (UI)
├── db_check.log               — CT-07, CT-08 (Banco)
├── screenshots/
│   ├── ut01_inicio.png
│   ├── ut01_bloco_expandido.png
│   ├── ut01_andar8_expandido.png
│   ├── ut01_menu_acoes_aberto.png
│   ├── ut01_modal_confirmacao.png   *** modal com copy sobre moradores
│   ├── ut01_pos_inativacao.png      *** unidade 802 ausente, contador 13
│   ├── ut02_inicio.png
│   ├── ut02_antes_toggle.png
│   ├── ut02_apos_toggle.png
│   ├── ut02_arvore_com_inativos.png *** unidade 701 com badge Inativo
│   ├── ut03_inicio.png
│   ├── ut03_bloco_expandido.png
│   ├── ut03_menu_acoes.png
│   ├── ut03_modal_confirmacao.png
│   └── ut03_pos_cancelar.png        *** unidade 101 continua visivel
└── videos/                    — gravacoes dos 3 testes UI (Playwright)
```

---

## IDs de Unidades Inativadas (para qa_task_06)

| ID | Andar | Numero | Bloco | Metodo de Inativacao |
|----|-------|--------|-------|---------------------|
| d05aca01-c7b2-41b0-8b1b-c2d3b7e45eee | 7 | 701 | QA-01 | API (CT-01) |
| c4c0af70-e2be-4d21-a8fa-d1521791cc56 | 8 | 802 | QA-01 | UI (UT-01) |

---

## Status para o Orquestrador

**Status:** FAIL
**Motivo da falha:** CT-03 — endpoint retorna 400 (validation-error) em vez de 404 para unidade inexistente. Divergencia de contrato.
**Impacto:** Baixo — comportamento funcional de inativacao esta correto. Apenas o status code de "nao encontrado" diverge do contrato.
**Tasks possivelmente impactadas:** qa_task_06 (reativacao) — nao impactada, unidades inativadas estao disponiveis
