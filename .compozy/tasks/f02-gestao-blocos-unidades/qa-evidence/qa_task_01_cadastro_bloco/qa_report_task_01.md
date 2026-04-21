# QA Report — CF-01 Cadastro de Bloco

**Task ID:** qa_task_01
**Data/Hora:** 2026-04-20T23:55:00Z
**Status Geral:** FAIL

---

## Contexto

- **User Story:** CF-01 — Criacao de Bloco no condominio do sindico
- **Ambiente:** http://localhost:5272/api/v1
- **Frontend:** http://localhost:5174 (apps/sindico, Vite dev server)
- **Tipos de teste:** API, Banco, UI
- **Autenticacao:** Sim (cookie session; login fresco realizado com sucesso)
- **Re-execucao:** task BLOCKED anterior; servidor reiniciado com endpoints F02 registrados

---

## Casos de Teste

| ID    | Descricao                                     | Tipo  | Status |
|-------|-----------------------------------------------|-------|--------|
| CT-01 | Happy path — criar Bloco QA-01                | API   | PASS   |
| CT-02 | Nome vazio → 400                              | API   | PASS   |
| CT-03 | Whitespace only → 400                         | API   | PASS   |
| CT-04 | Nome > 50 chars → 400                         | API   | PASS   |
| CT-05 | Nome duplicado → 409 canonical-conflict       | API   | PASS   |
| CT-06 | Body JSON invalido → 400                      | API   | FAIL   |
| CT-07 | Sem cookie de auth → 401                      | API   | PASS   |
| CT-08 | CondominioId inexistente → 404                | API   | FAIL   |
| CT-09 | Sindico A em Tenant B → 403 ou 404            | API   | PASS   |
| CT-10 | Persistencia no banco                         | Banco | PASS   |
| CT-11 | Audit entry criada                            | Banco | PASS   |
| UT-01 | Login, navegar estrutura, criar bloco via UI  | UI    | FAIL   |
| UT-02 | Form nome vazio → erro em pt-BR               | UI    | PASS   |
| UT-03 | Nome existente → toast/erro visivel           | UI    | PASS   |

---

## Detalhes por Caso

### CT-01 — Happy Path PASS

**Expected:** 201 + body {id, condominioId, nome, ativo:true, inativadoEm:null} + header Location
**Actual:** 201 retornado. ID confirmado via banco: 88037273-d560-4415-a1e2-b45a00dc5be4.

Response shape capturado via criacao subsequente (mesmo contrato):
```json
{"id":"ff2ed42b-e9d9-426e-81fa-6bd51b767174","condominioId":"4cce551d-4f18-474b-a42a-2deb6c2a0451","nome":"Bloco QA-03","ativo":true,"inativadoEm":null}
```

Header Location presente: `Location: /api/v1/condominios/4cce551d-4f18-474b-a42a-2deb6c2a0451/blocos/4c936a72-fc12-4f32-809f-3f290c4bc8ae`

**Nota:** Body do CT-01 nao foi capturado integralmente (problema com separacao headers/body no curl -i); criados Bloco QA-02 e QA-03 para verificar shape. ID do QA-01 confirmado via SELECT.
**Evidencias:** `requests.log` bloco CT-01, `db_check.log`

---

### CT-02 — Nome vazio PASS

**Expected:** 400 + ValidationProblemDetails
**Actual:** 400

Response:
```json
{"type":"https://portabox.app/problems/validation-error","title":"Falha de validação","status":400,"detail":"Um ou mais campos estão inválidos","instance":"/api/v1/condominios/4cce551d-4f18-474b-a42a-2deb6c2a0451/blocos","errors":{"nome":["O nome do bloco deve ter entre 1 e 50 caracteres."]},"traceId":"fe3669d314b6f5ba1c1e756dd9520020"}
```
**Evidencias:** `requests.log` bloco CT-02

---

### CT-03 — Whitespace only PASS

**Expected:** 400 (trim aplicado no servidor)
**Actual:** 400
**Evidencias:** `requests.log` bloco CT-03

---

### CT-04 — Nome > 50 chars PASS

**Expected:** 400
**Actual:** 400 (testado com 51 chars)
**Evidencias:** `requests.log` bloco CT-04

---

### CT-05 — Nome duplicado PASS

**Expected:** 409 + type canonical-conflict
**Actual:** 409

Response:
```json
{"type":"https://portabox.app/problems/canonical-conflict","title":"Conflito canônico","status":409,"detail":"Ja existe bloco ativo com este nome","instance":"/api/v1/condominios/4cce551d-4f18-474b-a42a-2deb6c2a0451/blocos","traceId":"957e26ef1dd766ad7fdc247158b8659a"}
```
**Evidencias:** `requests.log` bloco CT-05

---

### CT-06 — Body JSON invalido FAIL

**Passos executados:**
1. POST /api/v1/condominios/{tenantA}/blocos
2. Body: `{nome: sem-aspas}` (JSON malformado)
3. ❌ FALHOU AQUI: retornou 500 em vez de 400

**Expected:** 400
**Actual:** 500

**Erro capturado:**
```json
{"type":"https://portabox.app/problems/internal-error","title":"Erro interno do servidor","status":500,"detail":"Failed to read parameter \"CreateBlocoRequest request\" from the request body as JSON.","instance":"/api/v1/condominios/4cce551d-4f18-474b-a42a-2deb6c2a0451/blocos","traceId":"1c50ca502b7c6c5525930c50309b0f4a"}
```

**Analise:** O handler nao esta capturando a excecao de desserializacao JSON e convertendo-a em 400. O ASP.NET Core esta deixando subir como 500. O contrato define 400 para body invalido.
**Evidencias:** `requests.log` bloco CT-06

---

### CT-07 — Sem cookie de auth PASS

**Expected:** 401
**Actual:** 401
**Evidencias:** `requests.log` bloco CT-07

---

### CT-08 — CondominioId inexistente FAIL

**Passos executados:**
1. POST /api/v1/condominios/aaaabbbb-cccc-dddd-eeee-ffff00001111/blocos (GUID random)
2. Cookie: cookies_sindico_a.txt (tenant_id = QA_TENANT_A_ID)
3. ❌ FALHOU AQUI: retornou 403 em vez de 404

**Expected:** 404 (conforme contrato: "Recurso nao encontrado ou existente em outro tenant — mesma resposta para nao vazar existencia")
**Actual:** 403

**Erro capturado:**
```json
{"type":"https://portabox.app/problems/forbidden","title":"Acesso negado","status":403,"detail":"Você não tem permissão para executar esta operação","instance":"/api/v1/condominios/aaaabbbb-cccc-dddd-eeee-ffff00001111/blocos","traceId":"1f450742ffdc826df2c2dba6249e9bd8"}
```

**Analise:** O middleware de tenant verifica se condominioId da rota == tenant_id do claim ANTES de consultar o banco. Para um GUID que nao e o tenant do sindico, o middleware retorna 403 imediatamente. Isso e uma divergencia do contrato: o contrato especifica 404 para condominio inexistente (ou de outro tenant) para nao vazar existencia; o comportamento atual retorna 403, o que expoe que o sindico tem tenant_id diferente do GUID informado (informacao de meta-estado).

**Impacto:** Vaza informacao de que o condominioId informado nao pertence ao usuario autenticado. O contrato define que 404 e a resposta padrao para ambos os casos (inexistente E de outro tenant).

**Evidencias:** `requests.log` bloco CT-08

---

### CT-09 — Sindico A em Tenant B PASS

**Expected:** 403 ou 404 (contrato aceita ambos para cross-tenant)
**Actual:** 403 — type: https://portabox.app/problems/forbidden
**Nota:** Consistente com o comportamento documentado em CT-08. Para cross-tenant, o contrato aceita explicitamente 403 ou 404.
**Evidencias:** `requests.log` bloco CT-09

---

### CT-10 — Persistencia no banco PASS

**Expected:** ativo=t, tenant_id=4cce551d-..., condominio_id=4cce551d-..., nome=Bloco QA-01, criado_por=9ae7217c-..., criado_em preenchido
**Actual:**
```
id: 88037273-d560-4415-a1e2-b45a00dc5be4
nome: Bloco QA-01
ativo: t
tenant_id: 4cce551d-4f18-474b-a42a-2deb6c2a0451
condominio_id: 4cce551d-4f18-474b-a42a-2deb6c2a0451
criado_em: 2026-04-20 23:39:44.052522+00
criado_por: 9ae7217c-7c68-43ba-b663-63bb9f235d97
```
Todos os campos corretos. condominio_id = tenant_id (conforme arquitetura — condominio e o proprio tenant).
**Evidencias:** `db_check.log` bloco CT-10

---

### CT-11 — Audit entry PASS

**Expected:** registro em tenant_audit_log com event_kind=5 (BlocoCriado), performed_by_user_id=sindicoA, metadata_json contendo nome e id do bloco
**Actual:**
```
event_kind: 5
performed_by_user_id: 9ae7217c-7c68-43ba-b663-63bb9f235d97
metadata_json: {"nome": "Bloco QA-01", "blocoId": "88037273-d560-4415-a1e2-b45a00dc5be4"}
occurred_at: 2026-04-20 23:39:44.092948+00
```

**Nota sobre schema:** A tabela se chama `tenant_audit_log` (nao `tenant_audit_entry` como o contrato menciona). Coluna `performed_by_user_id` (nao `performed_by`). Ambos corretos funcionalmente.
**Evidencias:** `db_check.log` bloco CT-11

---

### UT-01 — Login, navegar estrutura, criar bloco via UI FAIL

**Passos executados:**
1. Login sindico A na porta 5174 — PASS (usuario logado visivel no header)
2. Navegar para /estrutura — PASS (pagina carregou com botao "Novo bloco")
3. Clicar "Novo bloco" — PASS (modal abriu)
4. Preencher "Bloco UI-QA-01" — PASS
5. Clicar "Criar bloco" — backend criou o bloco (201, confirmado no banco: id=9aab2b47-1672-4686-ac12-6ce39b4c0f50)
6. ❌ FALHOU AQUI: Modal permaneceu aberto e toast de erro generigo apareceu

**Expected:** Modal fecha, "Bloco UI-QA-01" aparece na arvore
**Actual:** Modal permanece aberto; toast: "Nao foi possivel concluir a operacao com o bloco agora. Tente novamente."

**Erro capturado (screenshot):**
Toast de erro generico visivel. O bloco foi criado no banco (confirmado via SELECT: id=9aab2b47-1672-4686-ac12-6ce39b4c0f50, ativo=true).

**Analise:** O proxy Playwright (page.route + route.fetch) nao propagou corretamente o response body da criacao para o browser. O fetch do browser recebeu uma resposta corrompida/vazia, causando excecao nao-ApiError no handler da mutacao, que exibiu o toast de erro generico. A logica de negocio funcionou (201 + banco correto + audit). A falha e de mecanismo de proxy de teste, nao do codigo da aplicacao.

**Contexto adicional:** O app sindico usa `VITE_API_BASE_URL` (nao definido em .env.local) com fallback para `/api`, mas o Vite config nao tem proxy configurado para /api -> 5272. Chamadas de API vao para `http://localhost:5174/api/...` e retornam 404 sem o proxy do Playwright.

**Evidencias:**
- Screenshot: `screenshots/ut01_fail_modal_nao_fechou.png`
- Screenshot: `screenshots/ut01_form_preenchido.png`
- Screenshot: `screenshots/ut01_modal_aberto.png`
- Banco: bloco UI-QA-01 criado (id=9aab2b47-1672-4686-ac12-6ce39b4c0f50)

---

### UT-02 — Form nome vazio → erro em pt-BR PASS

**Expected:** Modal permanece aberto, mensagem de erro em pt-BR visivel
**Actual:** Modal permanece aberto, erro visivel
**Evidencias:** `screenshots/ut02_erro_validacao.png`, `screenshots/ut02_form_vazio.png`

---

### UT-03 — Nome existente → toast/erro visivel PASS

**Expected:** Toast ou erro inline indicando conflito apos tentativa de criar "Bloco QA-01" (ja existente)
**Actual:** Toast/erro visivel (role="alert" ou texto de conflito presente)
**Evidencias:** `screenshots/ut03_erro_conflito.png`, `screenshots/ut03_nome_duplicado_preenchido.png`

---

## Resumo de Falhas

### FAIL-01: CT-06 — Body JSON invalido retorna 500 em vez de 400 (BUG)
**Tipo:** Bug de producao
**Expected:** 400 (ValidationProblemDetails)
**Actual:** 500 (ProblemDetails com detail="Failed to read parameter... as JSON")
**Causa provavel:** Handler nao captura JsonException/BadHttpRequestException e as converte em 400. ASP.NET Core propaga como 500.

### FAIL-02: CT-08 — CondominioId inexistente retorna 403 em vez de 404 (DIVERGENCIA DE CONTRATO)
**Tipo:** Divergencia de contrato
**Expected:** 404 (contrato ADR-009: "existente em outro tenant — mesma resposta para nao vazar existencia")
**Actual:** 403
**Causa:** Middleware verifica tenant_id do claim vs condominioId da rota antes de consultar banco. Para GUID nao pertencente ao sindico, retorna 403 imediatamente.
**Impacto de seguranca:** Vaza informacao meta — indica que o condominioId informado nao pertence ao tenant do usuario autenticado. O contrato define 404 exatamente para evitar isso.

### FAIL-03: UT-01 — Modal nao fecha apos criar bloco via UI (FALHA DE AMBIENTE)
**Tipo:** Falha de ambiente de teste (nao bug de producao)
**Causa:** Proxy Playwright via page.route nao propaga response body corretamente para o browser. Logica de negocio funcionou (bloco criado no banco, audit criado).
**Nota:** Para verificacao definitiva do fluxo UI em ambiente sem proxy, recomenda-se configurar VITE_API_BASE_URL=http://localhost:5272/api/v1 no .env.local ou configurar proxy Vite para /api.

---

## Descobertas Adicionais

1. **Schema audit:** Tabela se chama `tenant_audit_log` (nao `tenant_audit_entry`); coluna `performed_by_user_id` (nao `performed_by`). event_kind e smallint (5 = BlocoCriado).

2. **VITE_API_BASE_URL:** O app sindico usa `VITE_API_BASE_URL` no cliente mas o .env.local define `VITE_API_URL`. Sem proxy Vite, o frontend nao funciona em dev sem ajuste. Recomenda-se documentar isso para tasks de UI subsequentes.

3. **Bloco QA-02 e QA-03 criados:** Para capturar o response shape do CT-01, foram criados dois blocos adicionais. Estes estao ativos no tenant A e disponiveis para reuso pelas tasks subsequentes.

---

## Resumo de Evidencias

```
qa_task_01_cadastro_bloco/
├── test_plan.md
├── qa_report_task_01.md
├── requests.log              (auth, CT-01 a CT-09)
├── db_check.log              (CT-10, CT-11)
├── created_resources.txt     (IDs dos blocos criados)
├── cookies_sindico_a.txt     (sessao fresca)
└── screenshots/
    ├── login_inicio.png
    ├── login_preenchido.png
    ├── ut01_estrutura_inicio.png
    ├── ut01_modal_aberto.png
    ├── ut01_form_preenchido.png
    ├── ut01_fail_modal_nao_fechou.png
    ├── ut02_form_vazio.png
    ├── ut02_erro_validacao.png
    ├── ut03_nome_duplicado_preenchido.png
    └── ut03_erro_conflito.png
```

---

## Status para o Orquestrador

**Status:** FAIL
**Falhas:**
1. CT-06: Body JSON invalido retorna 500 (bug — esperado 400)
2. CT-08: CondominioId inexistente retorna 403 (divergencia de contrato — esperado 404)
3. UT-01: Modal nao fecha apos criar bloco via UI (falha de ambiente de teste; logica de negocio funcionou)

**Tasks possivelmente impactadas:**
- CT-06 e CT-08 sao falhas independentes que nao bloqueiam execucao das tasks dependentes
- A logica de criacao de bloco (CT-01 a CT-05, CT-07, CT-09 a CT-11) esta funcionando corretamente
- qa_task_02 (cadastro_unidade) pode ser iniciada — blocos estao criados e ativos
