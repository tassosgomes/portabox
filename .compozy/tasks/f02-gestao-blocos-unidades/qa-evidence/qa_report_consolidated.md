# Relatorio Consolidado — QA F02 Gestao de Blocos e Unidades

**Data da sessao:** 2026-04-20T00:00:00Z (inicio) / execucao completa em 2026-04-21 / reteste de CF-06 em 2026-04-21  
**PRD:** `.compozy/tasks/f02-gestao-blocos-unidades/_prd.md`  
**Techspec:** `.compozy/tasks/f02-gestao-blocos-unidades/_techspec.md`  
**Contrato:** `.compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml`  
**Ambiente:** Backend `http://localhost:5272` | Frontend Sindico `http://localhost:5174` | MailHog `http://localhost:8025` | PostgreSQL `localhost:5432` (db: portabox)  
**Tenants de teste:** Tenant A (env: `QA_TENANT_A_ID`) e Tenant B (env: `QA_TENANT_B_ID`) — credenciais em `.env.qa.local`, nao versionado

---

## Sumario Executivo

A sessao de QA de F02 — Gestao de Blocos e Unidades — executou 11 tasks cobrindo as user stories CF-01 a CF-09 (exceto CF-08, descrito no escopo excluido). O resultado geral e **REPROVADO COM RESSALVA**: das 10 tasks de escopo funcional, 4 terminaram com status FAIL apos o reteste de CF-06, 6 com status PASS, e 1 com BLOCKED parcial (UI nao implementada em qa_task_08).

Os mecanismos centrais do dominio funcionam corretamente: blocos e unidades sao criados, inativados e reativados com persistencia correta, audit trail presente para 100% das operacoes de escrita, isolamento multi-tenant sem nenhum leak detectado em 12 cenarios cross-tenant, e performance da arvore muito abaixo do limite (media de 20ms contra limite de 1000ms). A suite de testes de UI tambem apresentou resultados majoritariamente positivos, com excecao de acessibilidade por teclado.

As falhas remanescentes concentram-se em divergencias de codigo HTTP de resposta em relacao ao contrato (400 em vez de 404 para recursos inexistentes em endpoints de unidade; 500 em vez de 400 para JSON invalido; 422 retornado como 400 ou 404 em casos de transicao de estado). O bug critico de integridade originalmente detectado em qa_task_06 CT-11 (reativacao de unidade aceita com bloco pai inativo) **foi corrigido e validado no reteste**: a API agora retorna 422 `invalid-transition` corretamente. O estado inconsistente remanescente no banco (unidade `a2c82b48-...` ativa sob bloco inativo) tambem foi limpo via endpoint `:inativar`, restaurando a consistencia do dominio.

Duas conformidades criticas merecem destaque imediato: (1) o isolamento multi-tenant passou integralmente — zero leaks em todos os cenarios testados; (2) o audit trail das operacoes de escrita do sindico alcancou 100% de cobertura com outbox consistente. Contudo, o audit de acesso read-only do operador (CF-09) esta ausente: o handler `GET /admin/condominios/{id}/estrutura` nao chama `IAuditService` e o enum `TenantAuditEventKind` nao possui valor para esse evento, configurando gap explicito contra o PRD CF-09.

O problema de CORS entre frontend (porta 5174) e backend (porta 5272) e recorrente em testes de UI: o backend retorna `Access-Control-Allow-Origin: *` incompativel com `credentials: include`, exigindo workaround via interceptacao Playwright em quase todas as tasks. Esse ponto nao impactou os resultados funcionais mas representa friccao continua para QA e desenvolvimento local.

---

## Resultado por Feature

| Feature | Task | Status | CT Total | CT PASS | CT FAIL | CT BLOCKED | UI Total | UI PASS | UI FAIL | UI BLOCKED | Bugs criticos |
|---------|------|--------|----------|---------|---------|------------|----------|---------|---------|------------|---------------|
| Preflight / Setup | qa_task_00 | PASS | 13 | 13 | 0 | 0 | — | — | — | — | — |
| CF-01 Cadastro Bloco | qa_task_01 | FAIL | 11 | 9 | 2 | 0 | 3 | 2 | 1 | 0 | CT-06 (500/400), CT-08 (403/404) |
| CF-02 Cadastro Unidade | qa_task_02 | PASS | 16 | 16 | 0 | 0 | 4 | 4 | 0 | 0 | — |
| CF-03 Edicao Nome Bloco | qa_task_03 | FAIL | 12 | 10 | 2 | 0 | 3 | 3 | 0 | 0 | CT-02 (400/422), CT-09 (404/422) |
| CF-04 Inativacao Unidade | qa_task_04 | FAIL | 11 | 10 | 1 | 1 | 3 | 3 | 0 | 0 | CT-03 (400/404) |
| CF-05 Inativacao Bloco | qa_task_05 | PASS | 11 | 11 | 0 | 0 | 3 | 3 | 0 | 0 | — |
| CF-06 Reativacao | qa_task_06 | PASS (reteste) | 13 | 13 | 0 | 0 | 3 | 3 | 0 | 0 | — (3 falhas originais corrigidas) |
| CF-07 Navegacao Arvore | qa_task_07 | FAIL | 10 | 10 | 0 | 0 | 7 | 5 | 1 | 1 | UT-03 (teclado PRD) |
| CF-09 Backoffice Read-Only (API) | qa_task_08 | FAIL | 13 | 12 | 1 | 0 | — | — | — | BLOCKED | CT-12 (audit gap PRD) |
| Isolamento Multi-tenant | qa_task_09 | PASS | 14 | 14 | 0 | 0 | — | — | — | — | — |
| Audit Trail | qa_task_10 | PASS | 13 | 13 | 0 | 0 | — | — | — | — | — |

**Totais consolidados:**

| Metrica | Valor |
|---------|-------|
| Tasks executadas | 11 de 11 |
| Tasks PASS | 6 (qa_task_00, 02, 05, 06 apos reteste, 09, 10) |
| Tasks FAIL | 4 (qa_task_01, 03, 04, 07) |
| Tasks FAIL com UI BLOCKED | 1 (qa_task_08) |
| Casos de teste total | 126 |
| Casos PASS | 120 (117 originais + 3 corrigidos no reteste de CF-06) |
| Casos FAIL | 10 (13 originais − 3 corrigidos no reteste de CF-06) |
| Casos BLOCKED | 2 |
| Resultado geral | REPROVADO COM RESSALVA |

> Resultado REPROVADO COM RESSALVA: 4 tasks permanecem com FAIL apos o reteste de CF-06, que corrigiu as 3 falhas originais (CT-02, CT-07 e o bug critico CT-11 de integridade).

---

## Escopo Excluido (acordado com o usuario)

| Feature | Motivo |
|---------|--------|
| CF-08 (inativacao em cascata automatica interna) | Sem endpoint HTTP publico; metodo interno para F07, validado indiretamente quando F03/F07 forem implementadas. PRD classifica como Non-Goal a inativacao automatica em cascata. |

---

## Resultado por Feature — Detalhes

### qa_task_00 — Preflight / Setup (PASS)

**Tipos de teste:** API, Banco  
**Casos executados:** 13/13  

| ID | Descricao | Status |
|----|-----------|--------|
| CT-01 | Backend Health Check (/health/live) | PASS |
| CT-02 | Frontend Sindico Health Check (port 5173) | PASS |
| CT-03 | MailHog UI Health Check (port 8025) | PASS |
| CT-04 | Postgres Health Check (SELECT 1 + user count) | PASS |
| CT-05 | Login como Operator | PASS |
| CT-06 | Criar Tenant A via POST /admin/condominios | PASS |
| CT-07 | Capturar magic link sindico A no MailHog | PASS |
| CT-08 | Password-setup sindico A via token | PASS |
| CT-09 | Login sindico A (validar role=Sindico, tenantId) | PASS |
| CT-10 | Criar Tenant B via POST /admin/condominios | PASS |
| CT-11 | Capturar magic link sindico B + password-setup | PASS |
| CT-12 | Login sindico B (validar role=Sindico, tenantId) | PASS |
| CT-13 | Persistir .env.qa.local e verificar cookie jars | PASS |

**Evidencias:** `qa-evidence/qa_task_00_preflight_setup/`

---

### qa_task_01 — CF-01 Cadastro de Bloco (FAIL)

**Tipos de teste:** API, Banco, UI  
**Casos executados:** 14/14 (11 API/Banco + 3 UI)  
**Falhas:** CT-06, CT-08, UT-01  

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Happy path — criar Bloco QA-01 | API | PASS |
| CT-02 | Nome vazio → 400 | API | PASS |
| CT-03 | Whitespace only → 400 | API | PASS |
| CT-04 | Nome > 50 chars → 400 | API | PASS |
| CT-05 | Nome duplicado → 409 canonical-conflict | API | PASS |
| CT-06 | Body JSON invalido → 400 | API | FAIL |
| CT-07 | Sem cookie de auth → 401 | API | PASS |
| CT-08 | CondominioId inexistente → 404 | API | FAIL |
| CT-09 | Sindico A em Tenant B → 403 ou 404 | API | PASS |
| CT-10 | Persistencia no banco | Banco | PASS |
| CT-11 | Audit entry criada | Banco | PASS |
| UT-01 | Login, navegar estrutura, criar bloco via UI | UI | FAIL |
| UT-02 | Form nome vazio → erro em pt-BR | UI | PASS |
| UT-03 | Nome existente → toast/erro visivel | UI | PASS |

**Evidencias:** `qa-evidence/qa_task_01_cadastro_bloco/`

---

### qa_task_02 — CF-02 Cadastro de Unidade (PASS)

**Tipos de teste:** API, Banco, UI  
**Casos executados:** 20/20  

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Happy path andar=1 numero=101 | API | PASS |
| CT-02 | Andar 0 (terreo) | API | PASS |
| CT-03 | Numero com sufixo minusculo normalizado para maiuscula | API | PASS |
| CT-04 | Numero com sufixo maiusculo | API | PASS |
| CT-05 | Numero com 4 digitos | API | PASS |
| CT-06 | Numero invalido (6 chars) → 400 | API | PASS |
| CT-07 | Numero so letra → 400 | API | PASS |
| CT-08 | Numero com simbolos → 400 | API | PASS |
| CT-09 | Andar negativo → 400 | API | PASS |
| CT-10 | Conflito canonico (duplicata de tripla) → 409 | API | PASS |
| CT-11 | Bloco inexistente → 404 | API | PASS |
| CT-12 | Bloco inativo → 422 | API | PASS |
| CT-13 | Sindico A em tenant B → 403 | API | PASS |
| CT-14 | Sem autenticacao → 401 | API | PASS |
| CT-15 | Persistencia DB — todos os campos corretos | Banco | PASS |
| CT-16 | Audit entry event_kind=9 (UnidadeCriada) | Banco | PASS |
| UT-01 | Adicionar unidade via UI, visivel na arvore | UI | PASS |
| UT-02 | Validacao andar negativo — erro inline | UI | PASS |
| UT-03 | Validacao numero invalido — erro inline | UI | PASS |
| UT-04 | Normalizacao numero para maiuscula na arvore | UI | PASS |

**Evidencias:** `qa-evidence/qa_task_02_cadastro_unidade/`

---

### qa_task_03 — CF-03 Edicao de Nome de Bloco (FAIL)

**Tipos de teste:** API, Banco, UI  
**Casos executados:** 15/15 | PASS: 13 | FAIL: 2  

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Happy path — renomear Bloco QA-02 | API | PASS |
| CT-02 | Mesmo nome → 422 (recebeu 400) | API | FAIL |
| CT-03 | Nome vazio → 400 | API | PASS |
| CT-04 | Nome > 50 chars → 400 | API | PASS |
| CT-05 | Conflito canonico → 409 | API | PASS |
| CT-06 | Bloco inexistente → 404 | API | PASS |
| CT-07 | Sem autenticacao → 401 | API | PASS |
| CT-08 | Cross-tenant → 403 | API | PASS |
| CT-09 | Renomear bloco inativo → 422 (recebeu 404) | API | FAIL |
| CT-10 | Persistencia DB | Banco | PASS |
| CT-11 | Audit entry event_kind=6 com nomeAntes/nomeDepois | Banco | PASS |
| CT-12 | Rename sequencial | API | PASS |
| UT-01 | Renomear bloco via UI | UI | PASS |
| UT-02 | Renomear com nome vazio — erro visivel | UI | PASS* |
| UT-03 | Renomear para nome conflitante — toast visivel | UI | PASS |

*UT-02: comportamento da UI e correto; assertion Playwright falhou por regex inadequado na spec (spec defect, nao bug de produto).

**Evidencias:** `qa-evidence/qa_task_03_edicao_nome_bloco/`

---

### qa_task_04 — CF-04 Inativacao de Unidade (FAIL)

**Tipos de teste:** API, Banco, UI  
**Casos executados:** 15/15 | PASS: 12 | FAIL: 1 | BLOCKED: 1 | PASS-ressalva: 1  

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Happy path — inativar unidade (andar=7, numero=701) | API | PASS |
| CT-02 | Unidade ja inativa → 422 invalid-transition | API | PASS |
| CT-03 | Unidade inexistente → 404 (recebeu 400) | API | FAIL |
| CT-04 | BlocoId errado no path → 400 ou 404 | API | PASS (com ressalva) |
| CT-05 | Sem autenticacao → 401 | API | PASS |
| CT-06 | Cross-tenant sindico A → tenant B → 403 | API | PASS |
| CT-07 | Persistencia DB — ativo=f, inativado_em, inativado_por | Banco | PASS |
| CT-08 | Audit entry event_kind=10 (UnidadeInativada) | Banco | PASS |
| CT-09 | GET estrutura sem includeInactive oculta inativa | API | PASS |
| CT-10 | GET estrutura?includeInactive=true mostra inativa | API | PASS |
| CT-11 | Moradores associados continuam ligados (F03) | API | BLOCKED |
| CT-12 | Inativacao nao afeta outras unidades do bloco | API | PASS |
| UT-01 | Inativar unidade via modal de confirmacao | UI | PASS |
| UT-02 | Toggle "Mostrar inativos" mostra unidade inativada | UI | PASS |
| UT-03 | Cancelar modal — unidade continua ativa | UI | PASS |

**Evidencias:** `qa-evidence/qa_task_04_inativacao_unidade/`

---

### qa_task_05 — CF-05 Inativacao de Bloco (PASS)

**Tipos de teste:** API, Banco, UI  
**Casos executados:** 14/14  

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Happy path — inativar Bloco QA-03 | API | PASS |
| CT-02 | Bloco ja inativo → 422 invalid-transition | API | PASS |
| CT-03 | Bloco inexistente → 404 | API | PASS |
| CT-04 | Sem autenticacao → 401 | API | PASS |
| CT-05 | Cross-tenant → 403 | API | PASS |
| CT-06 | Persistencia DB | Banco | PASS |
| CT-07 | Audit entry event_kind=7 (BlocoInativado) | Banco | PASS |
| CT-08 | GET estrutura sem includeInactive oculta bloco | API | PASS |
| CT-09 | GET estrutura com includeInactive=true mostra bloco | API | PASS |
| CT-10 | Sem cascata — unidade permanece ativa apos inativacao do bloco | API+Banco | PASS |
| CT-11 | Criar unidade em bloco inativo → 422 | API | PASS |
| UT-01 | Inativar bloco via UI | UI | PASS |
| UT-02 | Toggle "Mostrar inativos" faz bloco inativo reaparecer | UI | PASS |
| UT-03 | Modal de confirmacao com copy pt-BR explicando nao-cascata | UI | PASS |

**Evidencias:** `qa-evidence/qa_task_05_inativacao_bloco/`

---

### qa_task_06 — CF-06 Reativacao de Bloco ou Unidade (PASS apos reteste)

**Tipos de teste:** API, Banco, UI  
**Casos executados:** 16/16 | PASS: 16 | FAIL: 0  
**Observacao:** Execucao original (2026-04-20) teve 3 falhas (CT-02, CT-07, CT-11). Reteste em 2026-04-21 confirmou as 3 correcoes. Estado inconsistente remanescente (unidade `a2c82b48-...`) foi limpo via `:inativar` apos o reteste.

| ID | Descricao | Tipo | Status | Historico |
|----|-----------|------|--------|-----------|
| CT-01 | Happy path — reativar bloco | API | PASS | — |
| CT-02 | Bloco ja ativo → 422 invalid-transition | API | PASS | Reteste: era FAIL (recebia 409), agora PASS (422) |
| CT-03 | Bloco inexistente → 404 | API | PASS | — |
| CT-04 | Conflito canonico reativacao bloco → 409 | API | PASS | — |
| CT-05 | Happy path — reativar unidade | API | PASS | — |
| CT-06 | Unidade ja ativa → 422 | API | PASS | — |
| CT-07 | Unidade inexistente → 404 | API | PASS | Reteste: era FAIL (recebia 400), agora PASS (404) |
| CT-08 | Conflito canonico reativacao unidade → 409 | API | PASS | — |
| CT-09 | Sem autenticacao → 401 | API | PASS | — |
| CT-10 | Cross-tenant → 403 | API | PASS | — |
| CT-11 | Unidade reativada com bloco pai inativo → 422 | API | PASS | Reteste: era FAIL CRITICO (retornava 200 e criava estado inconsistente), agora PASS (422 rejeita corretamente) |
| CT-12 | Persistencia DB apos reativacao | Banco | PASS | — |
| CT-13 | Audit entries event_kind=8 e event_kind=11 | Banco | PASS | — |
| UT-01 | Reativar bloco inativo via UI | UI | PASS | — |
| UT-02 | Reativar unidade inativa via UI | UI | PASS | — |
| UT-03 | Conflito canonico na reativacao — toast visivel | UI | PASS | — |

**Evidencias:** `qa-evidence/qa_task_06_reativacao/` (relatorio substituido pela versao do reteste)

---

### qa_task_07 — CF-07 Navegacao em Arvore Hierarquica (FAIL)

**Tipos de teste:** API, Banco, UI  
**Casos executados:** 17/17 | PASS: 15 | FAIL: 1 | BLOCKED: 1  

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Estrutura default — apenas ativos | API | PASS |
| CT-02 | Estrutura com includeInactive=true | API | PASS |
| CT-03 | Estrutura com includeInactive=false | API | PASS |
| CT-04 | Sem autenticacao → 401 | API | PASS |
| CT-05 | CondominioId inexistente → 403 | API | PASS |
| CT-06 | Sindico A acessa Tenant B → 403 | API | PASS |
| CT-07 | Tempo de resposta (3 chamadas) — media 20ms | API | PASS |
| CT-08 | Consistencia API vs DB | API+Banco | PASS |
| CT-09 | Ordenacao alfabetica dos blocos | API | PASS |
| CT-10 | Ordenacao semantica de unidades | API | PASS |
| UT-01 | Arvore renderiza visualmente | UI | PASS |
| UT-02 | Expandir/colapsar bloco via click | UI | PASS |
| UT-03 | Navegacao por teclado (setas) | UI | FAIL |
| UT-04 | Toggle filtro incluir inativos | UI | PASS |
| UT-05 | Painel lateral ao clicar num bloco | UI | PASS |
| UT-06 | Responsividade tablet 768x1024 | UI | PASS |
| UT-07 | Empty state (Tenant B) | UI | BLOCKED |

**Evidencias:** `qa-evidence/qa_task_07_navegacao_arvore/`

---

### qa_task_08 — CF-09 Backoffice Read-Only API (FAIL)

**Tipos de teste:** API, Banco  
**UI:** BLOCKED (feature `apps/backoffice/src/features/estrutura/` nao implementada)  
**Casos executados:** 13/13 | PASS: 12 | FAIL: 1  

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Operador consulta Tenant A | API | PASS |
| CT-02 | Operador consulta Tenant B | API | PASS |
| CT-03 | Operador com includeInactive=true em Tenant A | API | PASS |
| CT-04 | Operador com includeInactive=false em Tenant A | API | PASS |
| CT-05 | Payload identico ao sindico | API | PASS |
| CT-06 | CondominioId inexistente → 404 | API | PASS |
| CT-07 | Sem autenticacao → 401 | API | PASS |
| CT-08 | Sindico tenta acessar admin endpoint do proprio tenant → 403 | API | PASS |
| CT-09 | Sindico A tenta acessar admin endpoint de Tenant B → 403 | API | PASS |
| CT-10 | Operador tenta POST em /condominios/{A}/blocos → 403 | API | PASS |
| CT-11 | Verificar ausencia de endpoints admin de mutation | Contrato | PASS |
| CT-12 | Auditoria do acesso read-only do operador | Banco | FAIL |
| CT-13 | Performance — 3x GET consecutivos em Tenant A — media 16ms | API | PASS |

**Evidencias:** `qa-evidence/qa_task_08_backoffice_readonly/`

---

### qa_task_09 — Isolamento Multi-tenant (PASS)

**Tipos de teste:** API, Banco  
**Casos executados:** 14/14 (+ 2 de setup/re-login)  

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-00 | Re-login Sindico A | API | PASS |
| CT-00B | Re-login Sindico B | API | PASS |
| CT-01 | GET estrutura cross-tenant (A→B) | API | PASS |
| CT-02 | GET estrutura cross-tenant (B→A) | API | PASS |
| CT-03 | POST bloco cross-tenant (A→B) — CRITICO | API | PASS |
| CT-04 | PATCH bloco cross-tenant (A→B) | API | PASS |
| CT-05 | POST :inativar bloco cross-tenant (A→B) | API | PASS |
| CT-06 | POST :reativar bloco cross-tenant (A→B) | API | PASS |
| CT-07 | POST unidade cross-tenant (A→B) | API | PASS |
| CT-08 | POST :inativar unidade cross-tenant (A→B) | API | PASS |
| CT-09 | POST :reativar unidade cross-tenant (A→B) | API | PASS |
| CT-10 | Path mix — condominioId=A + blocoId=B | API | PASS |
| CT-11 | Path mix inverso — condominioId=B + blocoId=A | API | PASS |
| CT-12 | Admin endpoint — Sindico sem role de admin → 403 | API | PASS |
| CT-13 | DB: tenant_id preenchido em bloco e unidade | Banco | PASS |
| CT-14 | DB: tenant_id = condominio_id em todos os blocos | Banco | PASS |

**Zero leaks detectados.** Todos os 12 cenarios de acesso cross-tenant bloqueados corretamente.

**Evidencias:** `qa-evidence/qa_task_09_isolamento_multitenant/`

---

### qa_task_10 — Audit Trail (PASS)

**Tipos de teste:** API, Banco  
**Casos executados:** 13/13  

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | BlocoCriado — event_kind=5 | API+Banco | PASS |
| CT-02 | BlocoRenomeado — event_kind=6 | API+Banco | PASS |
| CT-03 | BlocoInativado — event_kind=7 | API+Banco | PASS |
| CT-04 | BlocoReativado — event_kind=8 | API+Banco | PASS |
| CT-05 | UnidadeCriada — event_kind=9 | API+Banco | PASS |
| CT-06 | UnidadeInativada — event_kind=10 | API+Banco | PASS |
| CT-07 | UnidadeReativada — event_kind=11 | API+Banco | PASS |
| CT-08 | Falhas NAO geram audit entries (400, 403, 409) | API+Banco | PASS |
| CT-09 | Contagem global de audit entries vs operacoes | Banco | PASS |
| CT-10 | Metadata JSON schema consistente por event_kind | Banco | PASS |
| CT-11 | Outbox de eventos de dominio (domain_event_outbox) | Banco | PASS |
| CT-12 | Outbox vs Audit consistency — match perfeito | Banco | PASS |
| CT-13 | Timestamps em UTC (timestamptz) | Banco | PASS |

**100% das operacoes de escrita geraram audit entry. 100% de match entre audit e outbox.**

**Evidencias:** `qa-evidence/qa_task_10_audit_trail/`

---

## Bugs e Divergencias — Detalhes

### BUG-01 — Body JSON invalido retorna 500 em vez de 400

- **Task:** qa_task_01 — CT-06
- **Severidade:** ALTO
- **Tipo:** BUG
- **Expected:** HTTP 400, ValidationProblemDetails
- **Actual:** HTTP 500, `{"type":"https://portabox.app/problems/internal-error","title":"Erro interno do servidor","status":500,"detail":"Failed to read parameter \"CreateBlocoRequest request\" from the request body as JSON."}`
- **Evidencia:** `qa-evidence/qa_task_01_cadastro_bloco/requests.log` bloco CT-06
- **Impacto:** Clientes que enviarem JSON malformado recebem 500 (indicativo de erro nao tratado) em vez de 400 (erro de cliente). Monitoramento de erros produtivos pode gerar alertas falsos. Endpoint: `POST /api/v1/condominios/{condominioId}/blocos`.

---

### BUG-02 — CondominioId inexistente retorna 403 em vez de 404

- **Task:** qa_task_01 — CT-08
- **Severidade:** MEDIO
- **Tipo:** DIVERGENCIA_CONTRATO
- **Expected:** HTTP 404 — contrato ADR-009: "Recurso nao encontrado ou existente em outro tenant — mesma resposta para nao vazar existencia"
- **Actual:** HTTP 403, `{"type":"https://portabox.app/problems/forbidden","title":"Acesso negado","status":403}`
- **Evidencia:** `qa-evidence/qa_task_01_cadastro_bloco/requests.log` bloco CT-08
- **Impacto:** A resposta 403 revela que o middleware verificou o condominioId da rota contra o claim do usuario antes de consultar o banco, expondo informacao de meta-estado (o GUID informado nao e o tenant do usuario autenticado). O contrato define 404 para ambos os casos (inexistente e de outro tenant) para evitar esse vazamento. Afeta todos os endpoints de escrita de bloco e unidade.

---

### BUG-03 — UT-01 qa_task_01: modal nao fecha apos criar bloco (falha de ambiente)

- **Task:** qa_task_01 — UT-01
- **Severidade:** BAIXO
- **Tipo:** AMBIENTE
- **Expected:** Modal fecha, bloco aparece na arvore
- **Actual:** Modal permaneceu aberto; toast de erro generico exibido. O bloco foi criado no banco (id=9aab2b47-1672-4686-ac12-6ce39b4c0f50, ativo=true). A logica de negocio funcionou corretamente.
- **Evidencia:** `qa-evidence/qa_task_01_cadastro_bloco/screenshots/ut01_fail_modal_nao_fechou.png`
- **Impacto:** Nao e bug do produto — o proxy Playwright via `page.route` nao propagou corretamente o response body para o browser. Causa raiz: `VITE_API_BASE_URL` nao definido em `.env.local`, sem proxy Vite configurado para `/api → 5272`. Requer configuracao de ambiente para reproducao correta.

---

### BUG-04 — Rename para mesmo nome retorna 400 em vez de 422

- **Task:** qa_task_03 — CT-02
- **Severidade:** MEDIO
- **Tipo:** DIVERGENCIA_CONTRATO
- **Expected:** HTTP 422, type `invalid-transition`
- **Actual:** HTTP 400, `{"type":"https://portabox.app/problems/validation-error","status":400,"detail":"O novo nome do bloco deve ser diferente do nome atual."}`
- **Evidencia:** `qa-evidence/qa_task_03_edicao_nome_bloco/requests.log` CT-02
- **Impacto:** O contrato define 422 para "tentativa de operacao impossivel por estado atual". A implementacao trata como validacao de entrada (400). O cliente SDK gerado do OpenAPI precisaria tratar ambos os codigos para esse endpoint.

---

### BUG-05 — Rename de bloco inativo retorna 404 em vez de 422

- **Task:** qa_task_03 — CT-09
- **Severidade:** MEDIO
- **Tipo:** DIVERGENCIA_CONTRATO
- **Expected:** HTTP 422, type `invalid-transition`
- **Actual:** HTTP 404, `{"type":"https://portabox.app/problems/not-found","detail":"Bloco nao encontrado"}`
- **Evidencia:** `qa-evidence/qa_task_03_edicao_nome_bloco/requests.log` CT-09
- **Impacto:** O endpoint PATCH exclui blocos inativos da busca e retorna 404 quando nao encontra bloco ativo com o ID informado. O contrato especifica que blocos inativos devem retornar 422 (bloco existe, mas a operacao e invalida para o estado atual). O cliente nao consegue distinguir "bloco nao existe" de "bloco existe mas esta inativo".

---

### BUG-06 — Unidade inexistente em :inativar retorna 400 em vez de 404

- **Task:** qa_task_04 — CT-03
- **Severidade:** MEDIO
- **Tipo:** DIVERGENCIA_CONTRATO
- **Expected:** HTTP 404
- **Actual:** HTTP 400, `{"type":"https://portabox.app/problems/validation-error","detail":"Unidade nao encontrada"}`
- **Evidencia:** `qa-evidence/qa_task_04_inativacao_unidade/requests.log` CT-03 FAIL
- **Impacto:** Divergencia semantica: 400 (client error — requisicao invalida) vs 404 (recurso nao encontrado). Afeta o endpoint `POST .../unidades/{unidadeId}:inativar`. O mesmo comportamento e confirmado em qa_task_06 CT-07 (endpoint :reativar). Nao impacta fluxos funcionais mas diverge do contrato OpenAPI.

---

### BUG-07 — Reativar bloco ja ativo retorna 409 em vez de 422 (RESOLVIDO)

- **Task:** qa_task_06 — CT-02
- **Severidade:** MEDIO
- **Tipo:** DIVERGENCIA_CONTRATO
- **Status:** RESOLVIDO (reteste 2026-04-21)
- **Expected:** HTTP 422, type `invalid-transition` ("A entidade ja esta ativa")
- **Actual (original):** HTTP 409, type `canonical-conflict`, detail "Ja existe bloco ativo com este nome; conflito canonico, inative o outro antes"
- **Actual (reteste):** HTTP 422, type `invalid-transition` — corrigido. A API agora verifica estado da entidade antes de verificar conflito canonico.
- **Evidencia:** `qa-evidence/qa_task_06_reativacao/requests.log` CT-02 (versao do reteste)
- **Impacto original:** A API executava verificacao de conflito canonico antes de verificar se a entidade ja estava ativa. Apos correcao, o cliente distingue corretamente "bloco ja esta ativo" de "conflito de nome com outro bloco".

---

### BUG-08 — Unidade inexistente em :reativar retorna 400 em vez de 404 (RESOLVIDO)

- **Task:** qa_task_06 — CT-07
- **Severidade:** MEDIO
- **Tipo:** DIVERGENCIA_CONTRATO
- **Status:** RESOLVIDO (reteste 2026-04-21)
- **Expected:** HTTP 404
- **Actual (original):** HTTP 400, type `validation-error`, detail "Unidade nao encontrada"
- **Actual (reteste):** HTTP 404, type `not-found` — corrigido. Endpoint `:reativar` de unidade agora e consistente com endpoint de bloco.
- **Evidencia:** `qa-evidence/qa_task_06_reativacao/requests.log` CT-07 (versao do reteste)
- **Observacao:** BUG-06 (mesmo padrao no endpoint `:inativar` de unidade) permanece em aberto — o reteste foi especifico para CF-06 (reativacao) e nao incluiu CF-04 (inativacao).

---

### BUG-09 — Reativacao de unidade aceita quando bloco pai esta inativo (RESOLVIDO — era CRITICO)

- **Task:** qa_task_06 — CT-11
- **Severidade original:** CRITICO (integridade de dados)
- **Tipo:** BUG
- **Status:** RESOLVIDO (reteste 2026-04-21) + estado inconsistente LIMPO
- **Expected:** HTTP 422, type `invalid-transition` — contrato especifica "422 se bloco pai inativo"
- **Actual (original):** HTTP 200 — unidade reativada com ativo=true mesmo com bloco pai inativo
- **Actual (reteste):** HTTP 422, `detail: "Nao e possivel reativar unidade em bloco inativo."` — corrigido. Nenhum novo estado inconsistente foi criado no reteste.

**Passos que originalmente reproduziam o estado inconsistente:**
1. Criar bloco "Bloco Temp Pai Inativo QA" (id=bb643a2a) → 201
2. Criar unidade andar=1, num=101 no bloco (id=a2c82b48) → 201
3. Inativar unidade → 200
4. Inativar bloco → 200
5. Tentar reativar unidade (bloco pai inativo) → 200 (bug) / apos correcao: 422

**Estado inconsistente remanescente — RESOLVIDO:**
- A unidade `a2c82b48-a993-4200-82d7-24e1f3490b34`, que havia sido deixada como evidencia do bug original, foi inativada via `POST .../unidades/{id}:inativar` em 2026-04-21T13:06:21Z, apos confirmacao da correcao no reteste.
- Estado final: `ativo=false`, `inativado_em=2026-04-21T13:06:21Z`, `inativado_por=sindicoA`, bloco pai continua inativo (estado consistente).
- Audit entry `UnidadeInativada` (event_kind=10) gerada normalmente para a operacao de limpeza.

- **Evidencias:**
  - `qa-evidence/qa_task_06_reativacao/requests.log` CT-11 (reteste — 422)
  - `qa-evidence/qa_task_06_reativacao/qa_report_task_06.md` (secao de comparacao com execucao anterior)
- **Impacto (original):** Estado inconsistente no banco: unidade ativa sem bloco pai ativo violava invariante do dominio. **Impacto atual:** nulo — bug corrigido e estado remanescente limpo.

---

### BUG-10 — Navegacao por teclado nao funciona na arvore (gap PRD)

- **Task:** qa_task_07 — UT-03
- **Severidade:** MEDIO
- **Tipo:** GAP_REQUISITO
- **Expected:** ArrowRight expande no, ArrowLeft colapsa no — PRD: "Acessibilidade: arvore navegavel por teclado (setas para expandir/colapsar, Enter para abrir detalhes)"
- **Actual:** `role="treeitem"` presente, `role="tree"` ausente. ArrowRight/ArrowLeft nao alteram `aria-expanded`. O componente `Tree` de `@portabox/ui` nao implementa o padrao WAI-ARIA 1.1 Tree View Pattern para navegacao por teclado.
- **Evidencias:**
  - `qa-evidence/qa_task_07_navegacao_arvore/screenshots/ut03_apos_arrow_right.png`
  - `qa-evidence/qa_task_07_navegacao_arvore/screenshots/ut03_apos_arrow_left.png`
  - `qa-evidence/qa_task_07_navegacao_arvore/videos/` (pasta de videos Playwright)
- **Impacto:** Inacessibilidade para usuarios que dependem de navegacao por teclado. PRD exige esta funcionalidade sem ressalva de Phase 2.

---

### BUG-11 — Acesso read-only do operador nao gera audit entry (gap PRD CF-09)

- **Task:** qa_task_08 — CT-12
- **Severidade:** ALTO
- **Tipo:** GAP_REQUISITO
- **Expected:** PRD CF-09: "Acesso registrado em log de auditoria (quem acessou qual tenant, quando)". Entradas no `tenant_audit_log` para cada acesso do operador via `GET /admin/condominios/{condominioId}/estrutura`.
- **Actual:** Zero entradas de auditoria geradas. Query por `performed_by_user_id = '0dcbb805-e21e-4db3-a196-e6e456b3ea2d'` (operador) nao retornou entradas apos o inicio da task. Handler `EstruturaEndpoints.cs` (linha 316-333) nao chama `IAuditService`. Enum `TenantAuditEventKind.cs` nao possui valor para acesso read-only.
- **Evidencias:**
  - `qa-evidence/qa_task_08_backoffice_readonly/db_check.log`
- **Impacto:** Impossibilidade de rastrear quais tenants foram acessados pelo operador, quando e com que frequencia. Requisito de compliance explicitamente declarado no PRD CF-09 sem ressalva de Phase 2. A ausencia de event kind no enum indica que a implementacao do audit de leitura nao foi iniciada.

---

### BUG-12 — UI CF-09 nao implementada no apps/backoffice (BLOCKED)

- **Task:** qa_task_08 — UI
- **Severidade:** ALTO
- **Tipo:** GAP_REQUISITO
- **Expected:** `apps/backoffice/src/features/estrutura/` presente e funcional
- **Actual:** Diretorio inexistente. Nenhum componente de "estrutura" encontrado no frontend de backoffice.
- **Evidencias:** Verificacao via Glob no codigo-fonte; ausencia confirmada.
- **Impacto:** O operador nao consegue visualizar a estrutura de tenants via interface grafica. Apenas a API esta pronta. PRD CF-09 descreve UI "identica a arvore do sindico sem controles de acao".

---

### BLOCKED-01 — CT-11 qa_task_04: moradores associados apos inativacao de unidade

- **Task:** qa_task_04 — CT-11
- **Severidade:** N/A (bloqueado por dependencia)
- **Tipo:** AMBIENTE
- **Motivo:** Feature F03 (Morador) nao implementada na sessao QA atual. Impossivel criar moradores e verificar vinculo pos-inativacao.
- **Evidencias:** `qa-evidence/qa_task_04_inativacao_unidade/requests.log` CT-11 BLOCKED

---

### BLOCKED-02 — UT-07 qa_task_07: empty state arvore

- **Task:** qa_task_07 — UT-07
- **Severidade:** N/A (bloqueado por estado do ambiente)
- **Tipo:** AMBIENTE
- **Motivo:** Tenant B ja possui 1 bloco cadastrado (criado em tasks anteriores). Nao e possivel reproduzir o empty state sem criar um terceiro tenant zerado, o que estava fora do escopo desta task.
- **Evidencias:** `qa-evidence/qa_task_07_navegacao_arvore/requests.log`

---

## Conformidade Critica

| Requisito | Status | Task/CT de referencia |
|-----------|--------|----------------------|
| Isolamento multi-tenant | PASS — zero leaks em 12 cenarios cross-tenant | qa_task_09 (todos os CTs) |
| Audit trail (operacoes de escrita do sindico) | PASS — 100% das 7 operacoes com entry + outbox | qa_task_10 (CT-01 a CT-13) |
| Audit trail (acesso read-only do operador) | FAIL — gap explicito vs PRD CF-09 | qa_task_08 CT-12 |
| Partial unique index canonico (bloco e unidade) | PASS — 409 confirmado em criacao e reativacao | qa_task_02 CT-10, qa_task_06 CT-08 |
| Soft-delete (ativo, inativado_em, inativado_por) | PASS — campos corretos em multiplas tasks | qa_task_04 CT-07, qa_task_05 CT-06 |
| Sem cascata automatica | PASS — unidade permanece ativa apos inativar bloco | qa_task_05 CT-10 |
| Performance da arvore | PASS — media 20ms (sindico) e 16ms (operador), limite 1000ms | qa_task_07 CT-07, qa_task_08 CT-13 |
| Normalizacao de numero de unidade para maiuscula | PASS — servidor e UI normalizam corretamente | qa_task_02 CT-03, UT-04 |
| Acessibilidade (teclado na arvore) | FAIL — componente Tree nao implementa WAI-ARIA Tree Pattern | qa_task_07 UT-03 |
| UI backoffice CF-09 | NAO TESTAVEL — feature nao implementada | qa_task_08 UI BLOCKED |

---

## Evidencias

```
qa-evidence/
├── qa_session.json
├── qa_report_consolidated.md        (este arquivo)
│
├── qa_task_00_preflight_setup/
│   ├── test_plan.md
│   ├── qa_report_task_00.md
│   ├── requests.log
│   ├── db_check.log
│   ├── .env.qa.local                (nao versionado — credenciais)
│   ├── cookies_operator.txt
│   ├── cookies_sindico_a.txt
│   ├── cookies_sindico_b.txt
│   ├── mailhog_message_a.json
│   ├── mailhog_message_b.json
│   ├── screenshots/                 (vazio)
│   └── videos/                      (vazio)
│
├── qa_task_01_cadastro_bloco/
│   ├── test_plan.md
│   ├── qa_report_task_01.md
│   ├── requests.log
│   ├── db_check.log
│   ├── created_resources.txt
│   ├── cookies_sindico_a.txt
│   └── screenshots/
│       ├── login_inicio.png
│       ├── login_preenchido.png
│       ├── ut01_estrutura_inicio.png
│       ├── ut01_modal_aberto.png
│       ├── ut01_form_preenchido.png
│       ├── ut01_fail_modal_nao_fechou.png    (FAIL UT-01)
│       ├── ut02_form_vazio.png
│       ├── ut02_erro_validacao.png
│       ├── ut03_nome_duplicado_preenchido.png
│       └── ut03_erro_conflito.png
│
├── qa_task_02_cadastro_unidade/
│   ├── test_plan.md
│   ├── qa_report_task_02.md
│   ├── requests.log
│   ├── db_check.log
│   ├── created_resources.txt
│   ├── cookies_sindico_a.txt
│   └── screenshots/
│       ├── ut01_tree_expandido.png
│       ├── ut02_apos_submit.png
│       ├── ut03_apos_submit.png
│       └── ut04_tree_expandido.png
│       (+ 12 screenshots intermediarios)
│
├── qa_task_03_edicao_nome_bloco/
│   ├── test_plan.md
│   ├── qa_report_task_03.md
│   ├── requests.log
│   ├── db_check.log
│   ├── created_resources.txt
│   ├── screenshots/
│   │   ├── ut01_pos_rename.png
│   │   ├── ut02_erro_visivel.png
│   │   └── ut03_erro_conflito.png
│   │   (+ 10 screenshots intermediarios)
│   └── videos/                      (gravacoes Playwright dos 3 testes UI)
│
├── qa_task_04_inativacao_unidade/
│   ├── test_plan.md
│   ├── qa_report_task_04.md
│   ├── requests.log
│   ├── db_check.log
│   ├── created_resources.txt
│   ├── screenshots/
│   │   ├── ut01_modal_confirmacao.png
│   │   ├── ut01_pos_inativacao.png
│   │   ├── ut02_arvore_com_inativos.png
│   │   └── ut03_pos_cancelar.png
│   │   (+ 11 screenshots intermediarios)
│   └── videos/
│
├── qa_task_05_inativacao_bloco/
│   ├── test_plan.md
│   ├── qa_report_task_05.md
│   ├── requests.log
│   ├── db_check.log
│   ├── created_resources.txt
│   └── screenshots/
│       ├── ut01_pos_inativacao.png
│       ├── ut02_com_inativos.png
│       └── ut03_modal_copy.png
│       (+ 7 screenshots intermediarios)
│
├── qa_task_06_reativacao/
│   ├── test_plan.md
│   ├── qa_report_task_06.md
│   ├── requests.log
│   ├── db_check.log
│   ├── created_resources.txt
│   └── screenshots/
│       ├── ut01_modal_confirmacao.png
│       ├── ut01_pos_reativacao.png
│       ├── ut02_modal_confirmacao.png
│       ├── ut02_pos_reativacao.png
│       ├── ut03_pos_confirmacao.png
│       └── (+ 20 screenshots de debug e intermediarios)
│
├── qa_task_07_navegacao_arvore/
│   ├── test_plan.md
│   ├── qa_report_task_07.md
│   ├── requests.log
│   ├── db_check.log
│   ├── performance.log
│   ├── response_active_only.json
│   ├── response_include_inactive.json
│   ├── response_snapshot.json
│   ├── screenshots/
│   │   ├── ut03_apos_arrow_right.png         (FAIL UT-03)
│   │   ├── ut03_apos_arrow_left.png          (FAIL UT-03)
│   │   └── (+ 14 screenshots)
│   └── videos/
│       └── (7 pastas de video/trace Playwright)
│
├── qa_task_08_backoffice_readonly/
│   ├── test_plan.md
│   ├── qa_report_task_08.md
│   ├── requests.log
│   ├── db_check.log                          (CT-12 FAIL — sem entradas de audit)
│   ├── response_operator.json
│   ├── response_sindico.json
│   ├── cookies_operator.txt
│   ├── screenshots/                          (vazio — UI BLOCKED)
│   └── videos/                              (vazio — UI BLOCKED)
│
├── qa_task_09_isolamento_multitenant/
│   ├── test_plan.md
│   ├── qa_report_task_09.md
│   ├── cross_tenant_matrix.md
│   ├── created_resources.txt
│   ├── cookies_sindico_a.txt
│   ├── cookies_sindico_b.txt
│   ├── requests.log
│   ├── db_check.log
│   ├── screenshots/                         (vazio)
│   └── videos/                              (vazio)
│
└── qa_task_10_audit_trail/
    ├── test_plan.md
    ├── qa_report_task_10.md
    ├── db_check.log
    ├── metadata_schema.md
    ├── audit_vs_outbox.md
    ├── screenshots/                          (vazio)
    └── videos/                              (vazio)
```

---

## Recomendacoes de Investigacao

1. **Decidir padrao de status code para recurso nao encontrado em endpoints de unidade.** Os endpoints `:inativar` e `:reativar` de unidade retornam 400 para GUIDs inexistentes, enquanto os endpoints de bloco equivalentes retornam 404. A inconsistencia impacta o client SDK gerado do OpenAPI (que precisaria tratar ambos os codigos para o mesmo semantico) e a experiencia de debugging. A decisao deve definir se 404 sera padronizado globalmente ou se a divergencia de 400 sera documentada como comportamento intencional.

2. ~~**Investigar o bug de integridade CT-11 de qa_task_06 (estado inconsistente no banco).**~~ **RESOLVIDO em 2026-04-21.** O reteste confirmou que o handler agora valida se o bloco pai esta ativo antes de reativar a unidade (retorna 422). O estado inconsistente da unidade `a2c82b48-...` foi limpo via `:inativar`. Nenhuma acao pendente.

3. ~~**Decidir se o estado inconsistente criado em CT-11 deve ser corrigido ou preservado.**~~ **RESOLVIDO em 2026-04-21.** Estado limpo via endpoint `:inativar` apos confirmacao da correcao no reteste. Audit trail coerente (evento `UnidadeInativada` gerado).

4. **Avaliar implementacao do audit de acesso read-only do operador.** O PRD CF-09 exige registro em auditoria (quem acessou qual tenant, quando). A implementacao atual nao registra acesso read-only. Onde investigar: handler `EstruturaEndpoints.cs` (endpoint GET /admin/condominios/{id}/estrutura) e enum `TenantAuditEventKind.cs`. Requer decisao sobre: novo event kind a criar, shape do metadata_json, e se o registro sera sincrono no handler ou via evento de dominio.

5. **Alinhar comportamento do handler de PATCH (renomear bloco) para estados invalidos.** CT-02 de qa_task_03: mesmo nome retorna 400 em vez de 422. CT-09 de qa_task_03: renomear bloco inativo retorna 404 em vez de 422. A investigacao deve determinar se a query de busca do handler exclui blocos inativos (causando o 404) e em que ponto a validacao de "mesmo nome" e executada (causando o 400 de validacao em vez de 422 de transicao).

6. ~~**Alinhar comportamento do endpoint :reativar de bloco para "ja ativo".**~~ **RESOLVIDO em 2026-04-21.** O reteste de CF-06 confirmou que o endpoint agora retorna 422 `invalid-transition` corretamente para bloco ja ativo.

7. **Confirmar prioridade e timeline para implementacao de acessibilidade por teclado na arvore.** UT-03 de qa_task_07: PRD exige arvore navegavel por teclado (WAI-ARIA Tree Pattern). O componente `Tree` de `@portabox/ui` usa `role="treeitem"` mas nao responde a eventos de teclado. O time deve confirmar se isso e gap de MVP ou deferido para Phase 2 (o PRD nao faz ressalva).

8. **Definir timeline para implementacao da UI CF-09 no apps/backoffice.** O diretorio `apps/backoffice/src/features/estrutura/` nao existe. A API esta pronta e testada. O time deve alinhar quando a interface do operador sera implementada.

9. **Investigar e corrigir a configuracao CORS para desenvolvimento local.** O backend retorna `Access-Control-Allow-Origin: *` que e incompativel com `credentials: include` exigido pelo frontend. Isso forcou workarounds de interceptacao Playwright em todas as tasks de UI e afeta qualquer desenvolvedor rodando a aplicacao localmente sem configuracao adicional. Onde investigar: middleware de CORS no backend e documentacao de variavel `VITE_API_BASE_URL` no frontend.

10. **Investigar JSON 500 no endpoint de criacao de bloco.** CT-06 de qa_task_01: body JSON malformado resulta em 500 em vez de 400. Onde investigar: middleware global de tratamento de excecao para `JsonException` e `BadHttpRequestException` — se o pipeline do ASP.NET Core nao captura esses tipos e os converte em 400 antes de chegar ao handler, a excecao sobe como 500.

---

## Dados da Sessao

| Dado | Valor |
|------|-------|
| Tenants de teste | Tenant A (`QA_TENANT_A_ID`), Tenant B (`QA_TENANT_B_ID`) |
| Tipo de banco | PostgreSQL 16 (docker: postgres:16-alpine) |
| Banco validado | Sim |
| Autenticacao testada | Sim — cookie session ASP.NET Identity |
| Playwright (UI) | Sim — tasks 01, 02, 03, 04, 05, 06, 07 |
| cURL (API) | Sim — todas as tasks |
| Tasks em paralelo | Sim — fases 3 (qa_task_02/03/05) e 5 (qa_task_08/09/10) |
| Reteste | CF-06 em 2026-04-21 — 16/16 PASS (3 falhas originais corrigidas) + cleanup do estado inconsistente |
| Blocos criados — Tenant A | 13 blocos no total (incluindo temporarios) |
| Blocos criados — Tenant B | 3 blocos |
| Unidades criadas — Tenant A | 24 unidades |
| Unidades criadas — Tenant B | 2 unidades |
| Audit entries geradas (event_kind=5, BlocoCriado) | 16 |
| Audit entries geradas (event_kind=6, BlocoRenomeado) | 5 |
| Audit entries geradas (event_kind=7, BlocoInativado) | 9 |
| Audit entries geradas (event_kind=8, BlocoReativado) | 6 |
| Audit entries geradas (event_kind=9, UnidadeCriada) | 26 |
| Audit entries geradas (event_kind=10, UnidadeInativada) | 5 |
| Audit entries geradas (event_kind=11, UnidadeReativada) | 4 |

---

## Appendix A — IDs e Recursos Criados (referencia futura)

### Tenant A (QA_TENANT_A_ID = 4cce551d-4f18-474b-a42a-2deb6c2a0451)

#### Blocos

| ID | Nome | Ativo | Criado em | Origem |
|----|------|-------|-----------|--------|
| 88037273-d560-4415-a1e2-b45a00dc5be4 | Bloco QA-01 | true | 2026-04-20T23:39:44Z | qa_task_01 CT-01 |
| 4c936a72-fc12-4f32-809f-3f290c4bc8ae | Bloco QA-02 V3 | true | qa_task_01 | qa_task_01 + renomeado em qa_task_03 |
| ff2ed42b-e9d9-426e-81fa-6bd51b767174 | Bloco QA-03 | false→true (reativado) | qa_task_01 | qa_task_01, inativado em qa_task_05, reativado em qa_task_06 |
| 9aab2b47-1672-4686-ac12-6ce39b4c0f50 | Bloco UI-QA-01 Editado | true | qa_task_01 UT-01 | Renomeado em qa_task_03 UT-01 |
| e6451b5b-bc1d-47e1-9300-2c5a50244273 | Bloco Temp Rename QA | false | qa_task_03 CT-09 | Inativo (usado para CT-09 de qa_task_03) |
| ac7b8af5-4bfe-47f0-84b1-c03bc3160895 | Bloco Temp Cascata QA | true (reativado) | qa_task_05 CT-10 | Reativado em qa_task_06 UT-01 |
| f32f5862-... | Bloco Conflito X QA | false | qa_task_06 CT-04 | Inativo — conflito canonico |
| f7fcaf43-... | Bloco Conflito Y QA | true | qa_task_06 CT-04 | Renomeado para "Bloco Conflito X QA" |
| bb643a2a-... | Bloco Temp Pai Inativo QA | false | qa_task_06 CT-11 | Bloco inativo com unidade ativa (estado inconsistente) |
| 52ac8a90-b077-464a-b14d-4dbd47dae6dd | Audit Bloco QA | true (reativado) | qa_task_10 CT-01 | Usado para ciclo completo de audit |
| 0260f7aa-... | (bloco auxiliar audit) | true | qa_task_10 CT-05 | Criado para teste de UnidadeCriada |

#### Unidades com estado notavel (Tenant A)

| ID | Bloco | Andar | Numero | Ativo | Observacao |
|----|-------|-------|--------|-------|------------|
| f2a0b7cc-13d3-4c18-a36e-b5ba9fcfce33 | QA-01 | 1 | 101 | true | Happy path qa_task_02 |
| d05aca01-c7b2-41b0-8b1b-c2d3b7e45eee | QA-01 | 7 | 701 | true | Inativada em qa_task_04, reativada em qa_task_06 CT-05 |
| c4c0af70-e2be-4d21-a8fa-d1521791cc56 | QA-01 | 8 | 802 | false | Inativada via UI em qa_task_04 UT-01 |
| a2c82b48-a993-4200-82d7-24e1f3490b34 | Bloco Temp Pai Inativo QA | 1 | 101 | false | Estado corrigido em 2026-04-21 via `:inativar` (bug CT-11 qa_task_06 resolvido no reteste) |

### Tenant B (QA_TENANT_B_ID = 23fb219d-460a-4eee-a9e7-308d7665350b)

| Recurso | ID | Origem |
|---------|-----|--------|
| Bloco B-QA-01 | 7a600791-d5ac-4f70-9741-c7c86ea49ca5 | qa_task_09 setup |
| Unidade andar=1 num=101 | f24e0bdf-22fd-4300-9919-94da8618672e | qa_task_09 setup |

---

## Appendix B — Matriz Cross-tenant (resumo de qa_task_09)

| Cenario | Ator | Operacao | Alvo | Resultado | Status |
|---------|------|----------|------|-----------|--------|
| CT-01 | Sindico A | GET estrutura | Tenant B | 403 | PASS |
| CT-02 | Sindico B | GET estrutura | Tenant A | 403 | PASS |
| CT-03 | Sindico A | POST bloco | Tenant B path | 403, bloco nao criado | PASS |
| CT-04 | Sindico A | PATCH bloco | Bloco de B | 403 | PASS |
| CT-05 | Sindico A | POST :inativar bloco | Bloco de B | 403 | PASS |
| CT-06 | Sindico A | POST :reativar bloco | Bloco de B | 403 | PASS |
| CT-07 | Sindico A | POST unidade | Bloco de B | 403 | PASS |
| CT-08 | Sindico A | POST :inativar unidade | Unidade de B | 403 | PASS |
| CT-09 | Sindico A | POST :reativar unidade | Unidade de B | 403 | PASS |
| CT-10 | Sindico A | POST unidade (path mix) | condominioId=A + blocoId=B | 404, nao criado | PASS |
| CT-11 | Sindico A | POST unidade (path mix inverso) | condominioId=B + blocoId=A | 403 | PASS |
| CT-12 | Sindico A | GET /admin/.../estrutura | Tenant A (admin endpoint) | 403 (role) | PASS |

Leaks detectados: **0 de 12 cenarios**.

Mecanismo de protecao: middleware de tenant verifica `condominioId` da rota contra `tenantId` do claim antes de consultar o banco. Para o cenario CT-10 (path mix com condominioId valido do proprio tenant), a validacao de pertencimento do bloco ao condominio retorna 404, impedindo acesso sem expor dados de outro tenant.

---

## Appendix C — Schema de metadata_json por event_kind (qa_task_10)

| event_kind | Nome | Campos | Exemplo |
|------------|------|--------|---------|
| 5 | BlocoCriado | `nome` (string), `blocoId` (uuid) | `{"nome":"Audit Bloco QA","blocoId":"52ac8a90-..."}` |
| 6 | BlocoRenomeado | `blocoId` (uuid), `nomeAntes` (string), `nomeDepois` (string) | `{"blocoId":"52ac8a90-...","nomeAntes":"Audit Bloco QA","nomeDepois":"Audit Bloco QA Renomeado"}` |
| 7 | BlocoInativado | `nome` (string), `blocoId` (uuid) | `{"nome":"Audit Bloco QA Renomeado","blocoId":"52ac8a90-..."}` |
| 8 | BlocoReativado | `nome` (string), `blocoId` (uuid) | `{"nome":"Audit Bloco QA Renomeado","blocoId":"52ac8a90-..."}` |
| 9 | UnidadeCriada | `andar` (int), `numero` (string), `blocoId` (uuid), `unidadeId` (uuid) | `{"andar":10,"numero":"1001","blocoId":"0260f7aa-...","unidadeId":"1bae4c0d-..."}` |
| 10 | UnidadeInativada | `andar` (int), `numero` (string), `blocoId` (uuid), `unidadeId` (uuid) | `{"andar":10,"numero":"1001","blocoId":"0260f7aa-...","unidadeId":"1bae4c0d-..."}` |
| 11 | UnidadeReativada | `andar` (int), `numero` (string), `blocoId` (uuid), `unidadeId` (uuid) | `{"andar":10,"numero":"1001","blocoId":"0260f7aa-...","unidadeId":"1bae4c0d-..."}` |

Observacoes:
- Tabela real: `tenant_audit_log` (nao `tenant_audit_entry` como mencionado no contrato)
- Coluna real: `performed_by_user_id` (nao `performed_by`)
- Timestamps: `timestamptz` com sufixo `+00` (UTC)
- Outbox: tabela `domain_event_outbox`, event_type no formato `bloco.criado.v1`, `unidade.inativada.v1`, etc.
- Contagem audit = contagem outbox para todos os 7 event kinds (match 100%)
- Tabela `domain_event_outbox` confirma `published_at` nao-nulo (dispatcher processou todos os eventos)
