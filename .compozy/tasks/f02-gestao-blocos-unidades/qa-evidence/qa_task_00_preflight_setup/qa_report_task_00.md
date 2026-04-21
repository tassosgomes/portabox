# Relatorio qa_task_00_preflight_setup

## Status
PASS

## Resumo
Todos os servicos estavam no ar (backend, frontend, MailHog, Postgres). Foram criados dois condominios (Tenant A e Tenant B) via fluxo completo de F01: criacao via operator, captura de magic link no MailHog, password-setup e login validado com role=Sindico e tenantId correto. Credenciais e cookie jars persistidos para reuso pelas tasks subsequentes.

---

## Checklist de servicos

- [x] Backend (port 5272): PASS — GET /health/live retornou HTTP 200, status=Healthy
- [x] Frontend sindico (port 5173): PASS — GET http://localhost:5173/ retornou HTTP 200
- [x] MailHog (port 8025): PASS — GET http://localhost:8025/ retornou HTTP 200 (nota: HEAD retorna 404; MailHog nao suporta HEAD em /; GET funciona corretamente)
- [x] Postgres (port 5432): PASS — SELECT 1 retornou 1; asp_net_users = 4 linhas (snake_case; "AspNetUsers" com pascal case nao existe)

---

## Descoberta do endpoint de F01

- **Path:** POST /api/v1/admin/condominios
- **Autenticacao:** Cookie session, role=Operator obrigatoria
- **Body shape:**
  ```json
  {
    "nomeFantasia": "string",
    "cnpj": "string (formato XX.XXX.XXX/XXXX-XX, validacao algoritmica)",
    "enderecoLogradouro": "string|null",
    "enderecoNumero": "string|null",
    "enderecoComplemento": "string|null",
    "enderecoBairro": "string|null",
    "enderecoCidade": "string|null",
    "enderecoUf": "string|null",
    "enderecoCep": "string (max 8 chars, somente digitos)",
    "administradoraNome": "string|null",
    "optIn": {
      "dataAssembleia": "DateOnly (YYYY-MM-DD)",
      "quorumDescricao": "string",
      "signatarioNome": "string",
      "signatarioCpf": "string (validacao algoritmica CPF)",
      "dataTermo": "DateOnly (YYYY-MM-DD)"
    },
    "sindico": {
      "nome": "string",
      "email": "string",
      "celularE164": "string"
    }
  }
  ```
- **Response:** HTTP 201, body `{ condominioId: Guid, sindicoUserId: Guid }`
- **Erros:** 409 se CNPJ duplicado; 409 se email do sindico duplicado; 400 para validation errors
- **Fluxo de magic link:**
  1. Ao criar condominio, evento `CondominioCadastradoV1` e publicado
  2. Handler `SendSindicoMagicLinkOnCondominioCreated` emite token via `IMagicLinkService.IssueAsync` (purpose=PasswordSetup)
  3. Email enviado ao sindico com link: `{SindicoAppBaseUrl}/password-setup?token={rawToken}` (frontend porta 5174 em dev)
  4. Email subject: "Bem-vindo ao PortaBox — defina sua senha"
  5. Token resgatado via: POST /api/v1/auth/password-setup body `{ token, password }`
  6. Apos setup, login normal via POST /api/v1/auth/login
- **Endpoint /auth/me:** GET /api/v1/auth/me retorna `{ userId, email, roles[], tenantId }` para usuario autenticado

---

## Credenciais geradas (valores em .env.qa.local)

- **Tenant A:** id=4cce551d-4f18-474b-a42a-2deb6c2a0451, sindico=qa-sindico-a-1776724904@portabox.test
- **Tenant B:** id=23fb219d-460a-4eee-a9e7-308d7665350b, sindico=qa-sindico-b-1776724968@portabox.test

---

## Casos de Teste

| ID    | Descricao                                          | Tipo  | Status  |
|-------|----------------------------------------------------|-------|---------|
| CT-01 | Backend Health Check (/health/live)                | API   | PASS    |
| CT-02 | Frontend Sindico Health Check (port 5173)          | API   | PASS    |
| CT-03 | MailHog UI Health Check (port 8025)                | API   | PASS    |
| CT-04 | Postgres Health Check (SELECT 1 + user count)      | Banco | PASS    |
| CT-05 | Login como Operator                                | API   | PASS    |
| CT-06 | Criar Tenant A via POST /admin/condominios         | API   | PASS    |
| CT-07 | Capturar magic link sindico A no MailHog           | API   | PASS    |
| CT-08 | Password-setup sindico A via token                 | API   | PASS    |
| CT-09 | Login sindico A (validar role=Sindico, tenantId)   | API   | PASS    |
| CT-10 | Criar Tenant B via POST /admin/condominios         | API   | PASS    |
| CT-11 | Capturar magic link sindico B + password-setup     | API   | PASS    |
| CT-12 | Login sindico B (validar role=Sindico, tenantId)   | API   | PASS    |
| CT-13 | Persistir .env.qa.local e verificar cookie jars    | N/A   | PASS    |

---

## Detalhes por Caso

### CT-01 — Backend Health Check PASS
**Expected:** HTTP 200, status=Healthy
**Actual:** HTTP 200, `{"status":"Healthy","totalDuration":4.6908,"checks":{"self":{"status":"Healthy",...}}}`
**Evidencias:** `requests.log` bloco "STEP 1A"

---

### CT-02 — Frontend Sindico Health Check PASS
**Expected:** HTTP 200
**Actual:** HTTP 200
**Evidencias:** `requests.log` bloco "STEP 1B"

---

### CT-03 — MailHog Health Check PASS
**Expected:** HTTP 200
**Actual:** HTTP 200 via GET (MailHog nao suporta HEAD em /; retorna 404 para HEAD mas 200 para GET; o servico funciona corretamente e a API /api/v2/messages respondeu com sucesso)
**Evidencias:** `requests.log` bloco "STEP 1C"

---

### CT-04 — Postgres Health Check PASS
**Expected:** SELECT 1 retorna 1; tabela de usuarios existe
**Actual:** SELECT 1 = 1; asp_net_users tem 4 linhas; condominio e demais tabelas confirmadas
**Nota:** Tabelas usam snake_case (asp_net_users) e nao pascal case (AspNetUsers)
**Evidencias:** `db_check.log`

---

### CT-05 — Login Operator PASS
**Expected:** HTTP 200, role=Operator
**Actual:** HTTP 200, `{"userId":"0dcbb805-e21e-4db3-a196-e6e456b3ea2d","role":"Operator","tenantId":null}`
**Evidencias:** `requests.log` bloco "CT-05", `cookies_operator.txt`

---

### CT-06 — Criar Tenant A PASS
**Expected:** HTTP 201 com condominioId e sindicoUserId
**Actual:** HTTP 201, condominioId=4cce551d-4f18-474b-a42a-2deb6c2a0451
**Nota inicial:** Primeira tentativa falhou com HTTP 400 (CNPJ formatado errado com 14 char CEP e CPF invalido). Corrigido com CNPJ/CPF validos e CEP de 8 digitos. Segunda tentativa falhou com HTTP 409 (CNPJ 11.222.333/0001-81 ja existia no banco de dados de outro condominio). Utilizado CNPJ 04.577.786/0001-65 gerado e validado algoritmicamente.
**Evidencias:** `requests.log` blocos "CT-06"

---

### CT-07 — Capturar magic link sindico A PASS
**Expected:** Email encontrado no MailHog com token no body
**Actual:** Email encontrado, token extraido via decodificacao quoted-printable: `CHVLQ2sSBLd4sNBzLrsUpRjm789NnZF2LeYKCpyitfI`
**URL no email:** `http://localhost:5174/password-setup?token=...` (frontend na porta 5174 em ambiente dev)
**Evidencias:** `mailhog_message_a.json`

---

### CT-08 — Password Setup sindico A PASS
**Expected:** HTTP 200
**Actual:** HTTP 200
**Evidencias:** `requests.log` bloco "CT-08"

---

### CT-09 — Login Sindico A PASS
**Expected:** HTTP 200, role=Sindico, tenantId=4cce551d-4f18-474b-a42a-2deb6c2a0451
**Actual:** HTTP 200, role=Sindico, tenantId=4cce551d-4f18-474b-a42a-2deb6c2a0451
**Evidencias:** `requests.log` bloco "CT-09", `cookies_sindico_a.txt`

---

### CT-10 — Criar Tenant B PASS
**Expected:** HTTP 201 com condominioId e sindicoUserId distintos do Tenant A
**Actual:** HTTP 201, condominioId=23fb219d-460a-4eee-a9e7-308d7665350b (diferente do A)
**Evidencias:** `requests.log` bloco "CT-10"

---

### CT-11 — Magic link + password-setup sindico B PASS
**Expected:** Email encontrado; HTTP 200 no password-setup
**Actual:** Email encontrado, token `NG-hJJD7lXA0_H5I-zY4w9dh40b9dck6vFMne2CjdhY`, password-setup HTTP 200
**Evidencias:** `mailhog_message_b.json`, `requests.log` bloco "CT-11"

---

### CT-12 — Login Sindico B PASS
**Expected:** HTTP 200, role=Sindico, tenantId=23fb219d-460a-4eee-a9e7-308d7665350b
**Actual:** HTTP 200, role=Sindico, tenantId=23fb219d-460a-4eee-a9e7-308d7665350b
**Evidencias:** `requests.log` bloco "CT-12", `cookies_sindico_b.txt`

---

### CT-13 — Credenciais e Cookies PASS
**Expected:** .env.qa.local com 9 chaves QA_; 3 cookie jars presentes
**Actual:** 9 chaves QA_ confirmadas; cookies_operator.txt, cookies_sindico_a.txt, cookies_sindico_b.txt todos criados
**Evidencias:** `.env.qa.local`, arquivos de cookie no diretorio de evidencias

---

## Falhas encontradas

Nenhuma falha definitiva. Foram encontradas 2 intercorrencias durante o setup que exigiram ajustes de dados de teste:

1. **Dados invalidos no primeiro request CT-06:** CNPJ sem validacao algoritmica, CEP com pontuacao (9 chars), CPF invalido. Corrigido com dados validos na tentativa seguinte — nao e uma falha do sistema, e validacao correta funcionando.
2. **CNPJ 11.222.333/0001-81 ja existia no banco:** Outro condominio de execucoes anteriores de QA usava este CNPJ. Utilizado CNPJ diferente gerado algoritmicamente. A constraint de unicidade esta funcionando corretamente.
3. **MailHog HEAD / retorna 404:** MailHog nao implementa o verbo HEAD em /. Verificacao via GET confirma servico ativo. Nao e bug do sistema testado.

---

## Avisos para tasks subsequentes

1. **Porta 5174:** O frontend do sindico para password-setup usa porta 5174 (nao 5173). Verificar se o frontend na porta 5173 e o backoffice e o 5174 e o app do sindico, ou vice-versa.
2. **Status do condominio:** Os condominios criados estao com status=1 (Pendente/Cadastrado). As tasks de F02 funcionarao com condominios neste status — o sindico pode criar blocos/unidades sem necessidade de ativar o condominio primeiro (confirmar via techspec de F02).
3. **Tabelas snake_case:** Queries de DB devem usar snake_case (asp_net_users, condominio, bloco, unidade, etc.).
4. **Cookie jars:** Os arquivos de cookie em qa_task_00_preflight_setup/ devem ser referenciados com caminho completo pelas tasks filhas.
5. **Rate limiting:** O endpoint /auth/login tem rate limiting (RateLimitingExtensions.AuthPolicyName). Aguardar entre multiplas chamadas de login se necessario.

---

## Arquivos de evidencia

```
qa_task_00_preflight_setup/
├── test_plan.md
├── qa_report_task_00.md
├── requests.log
├── db_check.log
├── .env.qa.local              (credenciais — nao versionar)
├── cookies_operator.txt
├── cookies_sindico_a.txt
├── cookies_sindico_b.txt
├── mailhog_message_a.json
├── mailhog_message_b.json
├── screenshots/               (vazio — sem testes UI nesta task)
└── videos/                    (vazio — sem testes UI nesta task)
```

---

## Status para o Orquestrador

**Status:** PASS
**Motivo da falha:** N/A
**Tasks desbloqueadas:** qa_task_01 (e todas as tasks dependentes em cascata)
