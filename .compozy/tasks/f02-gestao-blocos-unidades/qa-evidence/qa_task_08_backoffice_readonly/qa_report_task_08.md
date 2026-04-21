# QA Report — CF-09 Leitura Cross-Tenant no Backoffice (API only)

**Task ID:** qa_task_08
**Data/Hora:** 2026-04-21T03:00:00Z
**Status Geral:** FAIL

---

## Contexto

- **User Story:** CF-09 — Como operador da plataforma, quero visualizar a estrutura de qualquer tenant (blocos, andares, unidades, status) para atender duvidas ou investigar problemas reportados por sindicos sem precisar solicitar prints ou acesso ao banco.
- **Ambiente:** http://localhost:5272
- **Tipos de teste:** API, Banco
- **Autenticacao:** Sim (cookie session ASP.NET Identity, endpoint /api/v1/auth/login)
- **Operator user_id:** 0dcbb805-e21e-4db3-a196-e6e456b3ea2d
- **Tenant A:** 4cce551d-4f18-474b-a42a-2deb6c2a0451
- **Tenant B:** 23fb219d-460a-4eee-a9e7-308d7665350b

---

## Casos UI — BLOCKED

A feature "estrutura" nao esta implementada no frontend apps/backoffice. Verificacao via Glob:

- Caminho esperado: `apps/backoffice/src/features/estrutura/` — NAO EXISTE
- Nenhum arquivo encontrado sob este diretorio

Consequencia: todos os casos de teste de UI para CF-09 sao BLOCKED. Nenhum teste Playwright foi executado.

---

## Casos de Teste

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Operador consulta Tenant A | API | PASS |
| CT-02 | Operador consulta Tenant B | API | PASS |
| CT-03 | Operador com includeInactive=true em Tenant A | API | PASS |
| CT-04 | Operador com includeInactive=false em Tenant A | API | PASS |
| CT-05 | Payload identico ao sindico | API | PASS |
| CT-06 | CondominioId inexistente — 404 | API | PASS |
| CT-07 | Sem autenticacao — 401 | API | PASS |
| CT-08 | Sindico tenta acessar admin endpoint do proprio tenant — 403 | API | PASS |
| CT-09 | Sindico A tenta acessar admin endpoint de Tenant B — 403 | API | PASS |
| CT-10 | Operador tenta POST em /condominios/{A}/blocos — 403 | API | PASS |
| CT-11 | Verificar ausencia de endpoints admin de mutation | Contrato | PASS |
| CT-12 | Auditoria do acesso read-only do operador | Banco | FAIL |
| CT-13 | Performance — 3x GET consecutivos em Tenant A | API | PASS |

---

## Detalhes por Caso

### CT-01 — Operador consulta Tenant A PASS

**Expected:** GET /api/v1/admin/condominios/{TENANT_A_ID}/estrutura com cookie de operador retorna 200 com EstruturaDto
**Actual:** HTTP 200. Payload contem condominioId=4cce551d-4f18-474b-a42a-2deb6c2a0451, nomeFantasia="QA Teste A - 1776724904", 9 blocos ativos, estrutura completa de andares e unidades.
**Evidencias:** `requests.log` (entrada CT-01), `response_operator.json`

---

### CT-02 — Operador consulta Tenant B PASS

**Expected:** GET /api/v1/admin/condominios/{TENANT_B_ID}/estrutura retorna 200 com condominioId do Tenant B
**Actual:** HTTP 200. condominioId=23fb219d-460a-4eee-a9e7-308d7665350b confirmado na resposta. Cross-tenant read funcionando corretamente.
**Evidencias:** `requests.log` (entrada CT-02)

---

### CT-03 — Operador com includeInactive=true PASS

**Expected:** 200, blocos e unidades com ativo=false aparecem na resposta
**Actual:** HTTP 200. Encontrados: blocos_inativos=2, unidades_inativas=1 em Tenant A. Consistente com estado DB confirmado em task_07.
**Evidencias:** `requests.log` (entrada CT-03)

---

### CT-04 — Operador com includeInactive=false PASS

**Expected:** 200, nenhum bloco ou unidade com ativo=false
**Actual:** HTTP 200. Contagem: blocos_inativos=0, unidades_inativas=0. Flag respeitada corretamente.
**Evidencias:** `requests.log` (entrada CT-04)

---

### CT-05 — Payload identico ao sindico PASS

**Expected:** Response do operador (CT-01) e response do sindico A (via endpoint de sindico) sao semanticamente identicos exceto geradoEm
**Actual:** Apos remover campo geradoEm de ambos os payloads, comparacao Python retornou "IDENTICAL". Todos os campos: condominioId, nomeFantasia, blocos (ids, nomes, ativo, andares, unidades) identicos.
**Evidencias:** `response_operator.json`, `response_sindico.json`, `requests.log` (entrada CT-05)

---

### CT-06 — CondominioId inexistente PASS

**Expected:** 404 com ProblemDetails
**Actual:** HTTP 404. GUID 00000000-0000-0000-0000-000000000000 retornou 404.
**Evidencias:** `requests.log` (entrada CT-06)

---

### CT-07 — Sem autenticacao PASS

**Expected:** 401 sem cookie de sessao
**Actual:** HTTP 401 confirmado.
**Evidencias:** `requests.log` (entrada CT-07)

---

### CT-08 — Sindico tenta acessar admin endpoint do proprio tenant PASS

**Expected:** 403 — endpoint /admin/... requer role Operator; sindico tem role Sindico
**Actual:** HTTP 403. Autorizacao por role funcionando corretamente. Sindico nao pode elevar acesso via path admin mesmo para seu proprio tenant.
**Evidencias:** `requests.log` (entrada CT-08)

---

### CT-09 — Sindico A tenta acessar admin endpoint de Tenant B PASS

**Expected:** 403
**Actual:** HTTP 403. Mesmo resultado que CT-08 — a restricao e de role, nao de tenant_id, o que e correto.
**Evidencias:** `requests.log` (entrada CT-09)

---

### CT-10 — Operador tenta POST em /condominios/{A}/blocos PASS

**Expected:** 403 — endpoint sindico requer role Sindico; operador tem role Operator
**Actual:** HTTP 403. Payload minimo {"nome":"test-operator-write"} rejeitado. Nenhum bloco criado. ADR-005 cumprido: operador nao tem permissao de escrita via endpoints de sindico.
**Evidencias:** `requests.log` (entrada CT-10)

---

### CT-11 — Verificar ausencia de endpoints admin de mutation PASS

**Expected:** Apenas GET /admin/condominios/{condominioId}/estrutura no contrato; nenhum POST/PATCH/DELETE admin
**Actual:** Revisao de api-contract.yaml confirma: unico path /admin/* e `GET /admin/condominios/{condominioId}/estrutura` com operationId `getEstruturaAdmin`. Nenhum endpoint admin de mutation existe no contrato nem no codigo (EstruturaEndpoints.cs revisado).
**Evidencias:** `requests.log` (entrada CT-11), `api-contract.yaml` secao paths `/admin/...`

---

### CT-12 — Auditoria do acesso read-only do operador FAIL

**Expected:** PRD CF-09 exige: "Acesso registrado em log de auditoria (quem acessou qual tenant, quando)". Esperado encontrar entradas de audit para os acessos read-only realizados nos CTs anteriores.

**Actual:** Nenhuma entrada de auditoria gerada para acesso read-only.

**Passos executados:**
1. Query `SELECT event_kind, tenant_id, performed_by_user_id, occurred_at, metadata_json FROM tenant_audit_log WHERE performed_by_user_id = '0dcbb805-e21e-4db3-a196-e6e456b3ea2d' ORDER BY occurred_at DESC LIMIT 10;`
2. Resultado: 10 entradas encontradas, mas todas de operacoes anteriores (criacao de condominios em qa_task_00, criacao/ativacao em outros tenants)
3. Nenhuma entrada com timestamp posterior ao inicio desta task (2026-04-21T03:xx:xx)
4. Revisao do handler `EstruturaEndpoints.cs` linha 316-333: o handler GET /admin/condominios/{id}/estrutura NAO chama `IAuditService`. Apenas executa `tenantContext.BeginScope(condominioId)` e `handler.HandleAsync`.
5. Enum `TenantAuditEventKind.cs`: nao existe valor para "EstruturaAcessada", "ReadOnlyAccess" ou equivalente.

**Erro capturado:**
```
Nenhuma entrada de auditoria gerada para os seguintes acessos do operador:
- CT-01: GET /admin/condominios/4cce551d.../estrutura
- CT-02: GET /admin/condominios/23fb219d.../estrutura
- CT-03: GET /admin/condominios/4cce551d.../estrutura?includeInactive=true
- CT-04: GET /admin/condominios/4cce551d.../estrutura?includeInactive=false
- CT-05: GET /admin/condominios/4cce551d.../estrutura (comparacao)
```

**Analise de gap:**
- PRD CF-09 (secao "Comportamento"): "Acesso registrado em log de auditoria (quem acessou qual tenant, quando)" — requisito explicito, sem ressalva de Phase 2 no PRD.
- TechSpec nao documenta deferimento especifico desta auditoria para Phase 2. A nota de Phase 2 em techspec refere-se a "audit viewer" UI, nao ao registro em si.
- api-contract.yaml nao menciona Open Question sobre auditoria de read-only.
- **Conclusao:** gap de conformidade — PRD CF-09 exige registro de auditoria mas a implementacao nao o faz.

**Evidencias:** `db_check.log`, `src/PortaBox.Api/Features/Estrutura/EstruturaEndpoints.cs` linha 316-333, `src/PortaBox.Modules.Gestao/Domain/TenantAuditEventKind.cs`

---

### CT-13 — Performance PASS

**Expected:** Media < 1000ms para tenant com ate 100 unidades (PRD threshold); comparar com task_07 CT-07 media 20ms
**Actual:**
- Call 1: 15ms (HTTP 200)
- Call 2: 17ms (HTTP 200)
- Call 3: 18ms (HTTP 200)
- Media: 16ms

Media do endpoint admin (16ms) e ligeiramente inferior a media do endpoint de sindico (20ms de task_07). Diferenca dentro da variabilidade normal — ambos usam o mesmo `GetEstruturaQueryHandler`.
**Evidencias:** `requests.log` (entrada CT-13)

---

## Resumo de Evidencias

```
qa-evidence/qa_task_08_backoffice_readonly/
├── test_plan.md
├── requests.log           — todos os CTs de API
├── db_check.log           — CT-12 auditoria banco
├── response_operator.json — payload operador Tenant A (CT-01)
├── response_sindico.json  — payload sindico A (CT-05)
├── cookies_operator.txt   — arquivo de sessao (NAO CONTEM TOKEN EM PLAIN TEXT)
├── screenshots/           — vazio (UI BLOCKED)
└── videos/                — vazio (UI BLOCKED)
```

---

## Status para o Orquestrador

**Status:** FAIL

**Motivo da falha:** CT-12 — implementacao do endpoint `GET /admin/condominios/{condominioId}/estrutura` nao registra acesso do operador em `tenant_audit_log`. PRD CF-09 exige explicitamente "Acesso registrado em log de auditoria (quem acessou qual tenant, quando)". Nenhuma entrada gerada nos acessos executados nesta task. O enum `TenantAuditEventKind` nao possui valor para acesso read-only e o handler nao chama `IAuditService`.

**UI BLOCKED:** Feature `apps/backoffice/src/features/estrutura/` inexistente no frontend. CF-09 UI completamente nao implementada.

**Tasks possivelmente impactadas:**
- qa_task_10 (audit_trail) — o gap de auditoria de CT-12 e relevante para a task de validacao de audit trail; recomenda-se que qa_task_10 documente este gap tambem.
- CF-09 UI BLOCKED afeta qualquer teste de UI futuro que dependa da feature de estrutura do backoffice.

---

## Conformidade CF-09 — Conclusao

| Requisito CF-09 | Status |
|---|---|
| Operador visualiza estrutura de qualquer tenant (cross-tenant GET) | CONFORME |
| Mesmo shape de EstruturaDto do endpoint de sindico | CONFORME |
| Acesso diferenciado por role Operator (sindico recebe 403) | CONFORME |
| Operador nao tem escrita (POST retorna 403) | CONFORME |
| Nenhum endpoint admin de mutation existe | CONFORME |
| includeInactive funciona identicamente ao endpoint de sindico | CONFORME |
| Acesso registrado em log de auditoria (PRD CF-09) | NAO CONFORME — gap de implementacao |
| UI identica a arvore do sindico sem controles de acao (backoffice) | NAO TESTAVEL — UI nao implementada |
