# QA Report — CF-06 Reativacao de Bloco ou Unidade (RETESTE)

**Task ID:** qa_task_06_reativacao (RETESTE v2)
**Data/Hora:** 2026-04-21T12:42:00Z
**Status Geral:** PASS

---

## Comparacao com execucao anterior

| Caso | STATUS_ANTERIOR | STATUS_ATUAL | Observacao |
|------|----------------|--------------|------------|
| CT-02 | FAIL (409 canonical-conflict em vez de 422) | **PASS (422 invalid-transition)** | Bug corrigido |
| CT-07 | FAIL (400 validation-error em vez de 404) | **PASS (404 not-found)** | Bug corrigido |
| CT-11 | FAIL CRITICO (200 OK — estado inconsistente criado) | **PASS (422 invalid-transition)** | Bug critico corrigido |

**Todos os 3 bugs anteriores foram corrigidos.**

---

## Contexto

- **User Story:** Como sindico, quero reativar uma unidade ou bloco inativado por engano, para volta-lo ao uso sem precisar recriar a entidade.
- **Ambiente:** http://localhost:5272/api/v1 (backend) | http://localhost:5174 (sindico app)
- **Tenant A:** 4cce551d-4f18-474b-a42a-2deb6c2a0451
- **Tipos de teste:** API, Banco, UI
- **Autenticacao:** Sim (cookie session ASP.NET Identity)

---

## Casos de Teste

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Happy path — criar bloco, inativar, reativar → 200, ativo=true, inativadoEm=null | API | PASS |
| CT-02 | Bloco ja ativo → esperar 422 invalid-transition [REVERIFICACAO] | API | PASS |
| CT-03 | Bloco inexistente → 404 | API | PASS |
| CT-04 | Conflito canonico reativacao bloco → 409 canonical-conflict | API | PASS |
| CT-05 | Happy path — criar unidade, inativar, reativar → 200, ativo=true | API | PASS |
| CT-06 | Unidade ja ativa → 422 invalid-transition | API | PASS |
| CT-07 | Unidade inexistente → 404 [REVERIFICACAO] | API | PASS |
| CT-08 | Conflito canonico reativacao unidade → 409 canonical-conflict | API | PASS |
| CT-09 | Sem autenticacao → 401 | API | PASS |
| CT-10 | Cross-tenant sindico A → 403 | API | PASS |
| CT-11 | Unidade reativada com bloco pai inativo → esperar 422 [REVERIFICACAO CRITICA] | API | PASS |
| CT-12 | Persistencia DB apos reativacao (ativo=t, inativado_em=NULL) | Banco | PASS |
| CT-13 | Audit entries event_kind=8 e event_kind=11 | Banco | PASS |
| CT-14 | Estado inconsistente legado — apenas observacao | Banco | DOCUMENTADO |
| UT-01 | Toggle inativos, reativar bloco inativo → reativacao bem-sucedida (200) | UI | PASS |
| UT-02 | Reativar unidade inativa → modal de reativacao exibido, API chamada | UI | PASS |
| UT-03 | Conflito canonico → toast/erro visivel | UI | PASS |

**Totais:** 16 PASS | 0 FAIL | 1 DOCUMENTADO

---

## Detalhes por Caso

### CT-01 — Happy path bloco PASS

**Expected:** POST :reativar bloco novo → 200, ativo=true, inativadoEm=null
**Actual:** 200 — { id: 14c086fa-..., nome: "Bloco Retest Reativar QA", ativo: true, inativadoEm: null }
**Evidencias:** requests.log CT-01

---

### CT-02 — Bloco ja ativo → 422 PASS [REVERIFICACAO — ANTERIOR: FAIL 409]

**STATUS_ANTERIOR:** FAIL — API retornava 409 canonical-conflict ao tentar reativar bloco ja ativo
**STATUS_ATUAL:** PASS — API retornou 422 invalid-transition corretamente

**Expected:** 422, type="invalid-transition"
**Actual:** 422, type="invalid-transition", detail="A entidade ja esta ativa"

**Analise de mudanca:** O bug foi corrigido. A API agora verifica primeiro se a entidade ja esta ativa (retornando 422) antes de verificar conflito canonico. Comportamento alinhado ao contrato.

**Evidencias:** requests.log CT-02

---

### CT-03 — Bloco inexistente → 404 PASS

**Expected:** 404 not-found
**Actual:** 404, type="not-found", detail="Bloco nao encontrado"
**Evidencias:** requests.log CT-03

---

### CT-04 — Conflito canonico bloco PASS

**Passos:**
1. Criar "Retest Conflito X QA" (A) → 201 (8cb7528b)
2. Criar "Retest Conflito Y QA" (B) → 201 (4585ea6d)
3. Inativar A → 200
4. PATCH B para nome "Retest Conflito X QA" → 200
5. :reativar A

**Expected:** 409 canonical-conflict
**Actual:** 409, type="canonical-conflict", detail="Ja existe bloco ativo com este nome; conflito canonico, inative o outro antes"
**Evidencias:** requests.log CT-04

---

### CT-05 — Happy path unidade PASS

**Expected:** 200, ativo=true, inativadoEm=null para unidade {andar:60, numero:601}
**Actual:** 200 — { id: c6a97000-..., andar: 60, numero: "601", ativo: true, inativadoEm: null }
**Evidencias:** requests.log CT-05

---

### CT-06 — Unidade ja ativa → 422 PASS

**Expected:** 422 invalid-transition
**Actual:** 422, type="invalid-transition", detail="A entidade ja esta ativa."
**Evidencias:** requests.log CT-06

---

### CT-07 — Unidade inexistente → 404 PASS [REVERIFICACAO — ANTERIOR: FAIL 400]

**STATUS_ANTERIOR:** FAIL — API retornava 400 validation-error para GUID inexistente
**STATUS_ATUAL:** PASS — API retornou 404 not-found corretamente

**Expected:** 404, type="not-found"
**Actual:** 404, type="not-found"

**Analise de mudanca:** Bug corrigido. O endpoint :reativar de unidade agora retorna 404 (consistente com :reativar de bloco).

**Evidencias:** requests.log CT-07

---

### CT-08 — Conflito canonico unidade PASS

**Passos:** Criar X {andar:61, numero:611}, inativar, criar Y igual, reativar X

**Expected:** 409 canonical-conflict
**Actual:** 409, type="canonical-conflict", detail="Conflito canonico; inative a duplicada antes de reativar esta unidade"
**Evidencias:** requests.log CT-08

---

### CT-09 — Sem autenticacao → 401 PASS

**Expected:** 401
**Actual:** 401
**Evidencias:** requests.log CT-09

---

### CT-10 — Cross-tenant → 403 PASS

**Passos:** Sindico A tenta reativar bloco do tenant B (849b6750) que foi inativado para o teste.

**Expected:** 403 ou 404
**Actual:** 403, type="forbidden", detail="Voce nao tem permissao para executar esta operacao"
**Bloco B restaurado apos o teste.**
**Evidencias:** requests.log CT-10

---

### CT-11 — Unidade reativada com bloco pai inativo PASS [REVERIFICACAO CRITICA — ANTERIOR: FAIL 200]

**STATUS_ANTERIOR:** FAIL CRITICO — API retornava 200, criando estado inconsistente (unidade ativa em bloco inativo)
**STATUS_ATUAL:** PASS — API retornou 422 invalid-transition com mensagem clara

**Passos:**
1. Criar bloco "Retest Pai Inativo QA" → 201 (69738a69)
2. Criar unidade {andar:62, numero:62} → 201 (e8b464ad)
3. Inativar unidade → 200
4. Inativar bloco → 200
5. Tentar :reativar unidade (bloco pai inativo)

**Expected:** 422 invalid-transition
**Actual:** 422, type="invalid-transition", detail="Nao e possivel reativar unidade em bloco inativo."

**Analise de mudanca:** Bug critico corrigido. A API agora valida se o bloco pai esta ativo antes de reativar uma unidade. Mensagem explicativa presente.

**Impacto:** Nenhum novo estado inconsistente foi criado neste reteste.

**Evidencias:** requests.log CT-11

---

### CT-12 — Persistencia DB PASS

**Bloco CT-01 (14c086fa):**
- ativo: t
- inativado_em: NULL
- inativado_por: NULL

**Unidade CT-05 (c6a97000):**
- ativo: t
- inativado_em: NULL

**Evidencias:** db_check.log CT-12

---

### CT-13 — Audit entries PASS

**Resultado (ocorrencias apos 2026-04-21T12:42:00Z):**
- event_kind=8 (BlocoReativado): "Bloco Retest Reativar QA" (14c086fa) — 2026-04-21T12:43:55Z
- event_kind=8 (BlocoReativado): "Bloco B-01" (849b6750 — cross-tenant test restauracao) — 2026-04-21T12:46:20Z
- event_kind=11 (UnidadeReativada): andar=60, numero="601" (c6a97000) — 2026-04-21T12:44:56Z

Entries presentes para CT-01 (BlocoReativado) e CT-05 (UnidadeReativada). PASS.

**Evidencias:** db_check.log CT-13

---

### CT-14 — Estado inconsistente legado DOCUMENTADO

**Query:** SELECT unidade_ativo, bloco_ativo FROM unidade JOIN bloco WHERE unidade.id = 'a2c82b48-...'

**Resultado:**
| unidade_id | unidade_ativo | bloco_id | nome | bloco_ativo |
|-----------|--------------|----------|------|------------|
| a2c82b48-... | t | bb643a2a-... | Bloco Temp Pai Inativo QA | f |

**Conclusao:** Estado inconsistente do run anterior PERSISTE. Unidade a2c82b48 continua ATIVA sob bloco bb643a2a que esta INATIVO. Este estado foi criado pelo bug do CT-11 anterior (bug ja corrigido). Nenhuma acao corretiva foi tomada conforme instrucoes.

**Evidencias:** db_check.log CT-14

---

### UT-01 — Toggle inativos, reativar bloco inativo PASS

**Bloco alvo:** Retest Pai Inativo QA (69738a69) — sem conflito canonico
**Passos:**
1. Navegar /estrutura
2. Clicar "Mostrar inativos" (botao naranja no header)
3. Bloco "Retest Pai Inativo QA" visivel na arvore (icone lock)
4. Clicar Acoes → menu exibe "Reativar"
5. Clicar Reativar via mouse.click (parent treeitem aria-disabled)
6. Modal: "Reativar Retest Pai Inativo QA vai devolve-lo aos novos cadastros. As unidades deste bloco permanecem no status atual."
7. Clicar "Reativar bloco" no modal
8. API retorna 200 → bloco reativado

**Expected:** Bloco reativado com sucesso (200)
**Actual:** PASS — reativarCalls: ["200 .../blocos/69738a69:reativar"], modalVisible=true

**Evidencias:**
- screenshots/rt_ut01_inicio.png
- screenshots/rt_ut01_com_inativos.png
- screenshots/rt_ut01_menu_aberto.png
- screenshots/rt_ut01_modal.png
- screenshots/rt_ut01_pos_reativacao.png

---

### UT-02 — Reativar unidade inativa PASS

**Unidade alvo:** 501 andar=50 (79e92757) — inativa em Bloco QA-01
**Passos:**
1. Toggle inativos
2. Expandir Bloco QA-01 → Andar 50
3. Andar 50 exibe 2 unidades 501: "Inativo" (primeira) e "Ativo" (segunda)
4. Clicar Acoes da primeira unidade 501 (inativa)
5. Menu exibe "Reativar" (confirmado no log)
6. Clicar Reativar via mouse.click
7. Modal: "Reativar a unidade 501 em Bloco QA-01 volta a exibi-la no cadastro ativo. Se ja existir outra unidade ativa com a mesma tripla, a reativacao falhara."
8. Confirmar → API retorna 409 (conflito canonico — existe outra unidade 501 ativa no andar 50)

**Nota:** A API corretamente rejeitou a reativacao com 409 porque ja existe unidade 501 ativa no mesmo andar=50 (estado criado por reativacao anterior nos testes). O fluxo UI funcionou corretamente: modal de confirmacao exibido, usuario confirmou, API respondeu com erro explicativo.

**Expected:** UI exibe modal de reativacao + trata resposta da API
**Actual:** PASS — modal exibido (modalVisible=true), API chamada (reativarCalls length=1), comportamento UI correto

**Evidencias:**
- screenshots/rt_ut02_andar50_expandido.png
- screenshots/rt_ut02_menu_aberto.png
- screenshots/rt_ut02_modal.png
- screenshots/rt_ut02_pos_reativacao.png

---

### UT-03 — Conflito canonico → toast/erro visivel PASS

**Bloco alvo:** Bloco Conflito X QA (f32f5862) — inativo, conflito com f7fcaf43 (ativo, mesmo nome)
**Passos:**
1. Toggle inativos
2. Localizar "Bloco Conflito X QA" inativo na arvore
3. Clicar Acoes → menu exibe "Reativar"
4. Clicar Reativar via mouse.click (parent aria-disabled)
5. Modal de confirmacao exibido
6. Confirmar → API retorna 409 canonical-conflict
7. UI exibe toast/alerta de conflito

**Expected:** Toast/alerta visivel com mensagem de conflito canonico
**Actual:** PASS — alertVisible=true, hasConflictError=true, apiResponses: ["409 .../blocos/f32f5862:reativar"]

**Evidencias:**
- screenshots/rt_ut03_com_inativos.png
- screenshots/rt_ut03_menu_inativo_bloco.png
- screenshots/rt_ut03_modal.png
- screenshots/rt_ut03_pos_confirmacao.png

---

## Observacao UI — aria-disabled em treeitem (mantida do run anterior)

Os nos da arvore para blocos/unidades inativos recebem `aria-disabled="true"` no `[role="treeitem"]`. Os botoes de acao dentro sao habilitados mas o Playwright os considera nao-acionaveis por herdar estado do ancestral. Solucao: `page.mouse.click()` com coordenadas reais — real interacao do usuario. Comportamento identico ao run anterior.

---

## Resumo de Evidencias

```
qa_task_06_reativacao/
├── test_plan.md            (v2)
├── qa_report_task_06.md    (este arquivo — reteste)
├── created_resources.txt   (estado final reteste)
├── requests.log            (CT-01 a CT-11, UT-01/02/03 browser logs)
├── db_check.log            (BEFORE/AFTER state, CT-12/13/14)
└── screenshots/
    ├── rt_ut01_inicio.png
    ├── rt_ut01_com_inativos.png
    ├── rt_ut01_menu_aberto.png
    ├── rt_ut01_modal.png
    ├── rt_ut01_pos_reativacao.png
    ├── rt_ut02_andar50_expandido.png
    ├── rt_ut02_bloco_expandido.png
    ├── rt_ut02_menu_aberto.png
    ├── rt_ut02_modal.png
    ├── rt_ut02_pos_reativacao.png
    ├── rt_ut03_com_inativos.png
    ├── rt_ut03_menu_inativo_bloco.png
    ├── rt_ut03_modal.png
    ├── rt_ut03_pos_confirmacao.png
    └── old_*.png           (screenshots do run anterior preservados com prefixo old_)
```

---

## Status para o Orquestrador

**Status:** PASS
**Comparacao das 3 falhas anteriores:**
- CT-02: STATUS_ANTERIOR=FAIL(409) → STATUS_ATUAL=PASS(422) — bug corrigido
- CT-07: STATUS_ANTERIOR=FAIL(400) → STATUS_ATUAL=PASS(404) — bug corrigido
- CT-11: STATUS_ANTERIOR=FAIL CRITICO(200) → STATUS_ATUAL=PASS(422) — bug critico corrigido

**Estado inconsistente legado:** PERSISTE — unidade a2c82b48 ativa sob bloco bb643a2a inativo (criado pelo bug anterior). Nenhum novo estado inconsistente foi criado neste reteste.

**Tasks possivelmente impactadas:** qa_task_09 (isolamento multitenant) e qa_task_10 (audit trail) devem ser re-executados considerando que o estado inconsistente legado ainda existe no banco.
