# QA Report — qa_task_03: Magic Link do Síndico

**Status geral:** PASS
**Data da re-execução TC-03 (RERUN5):** 2026-04-20T18:05:19Z
**Data da re-execução TC-03 (RERUN4):** 2026-04-20T18:00:00Z
**Data da re-execução TC-03 (RERUN3):** 2026-04-20T17:49:06Z
**Data da re-execução TC-06 (RERUN2):** 2026-04-20T17:05:53Z
**Data da re-execução TC-03 (RERUN2):** 2026-04-20T16:58:43Z
**Data da re-execução anterior (TC-03 RERUN1):** 2026-04-20T00:00:00Z
**Data da execução original:** 2026-04-19T01:30:00Z
**Executor:** qa-task-runner

---

## Contexto

- **User Story:** CF3 — Envio automático de magic link ao primeiro síndico após criação do wizard
- **Ambiente:** http://localhost:5173 (backoffice), http://localhost:5272 (API), http://localhost:8025 (Mailpit), http://localhost:5174 (app síndico)
- **Tipos de teste:** API, Email, UI, Banco
- **Tenant original (execução anterior):** Residencial Teste QA — ID `f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5`
- **Síndico original:** Maria Oliveira — email `sindico.qa@portabox.dev` — user_id `547b2417-fac5-4b3c-9cc8-69c855a95924`
- **Tenant rerun:** Residencial Rerun QA — ID `4a3d87ea-f62f-4d9c-80de-a34237d0dae3`
- **Síndico rerun:** email `sindico.rerun@portabox.dev` — user_id `85c256c2-3958-4b69-b6c8-8104adb805a7`

---

## Casos de Teste

| ID | Descrição | Tipo | Status |
|----|-----------|------|--------|
| TC-01 | E-mail recebido após conclusão do wizard | Email | PASS (execução original) |
| TC-02 | Magic link contém token e é bem formado | Email/API | PASS (execução original) |
| TC-03 | Página de definição de senha acessível via magic link | UI | PASS (RERUN5) |
| TC-04 | Definição de senha com o magic link | API | PASS (execução original) |
| TC-05 | Magic link inválido após uso | API | PASS (execução original) |
| TC-06 | Botão "Reenviar magic link" disponível no painel de detalhes | UI | PASS (re-execução — botão visível, reenvio confirmado) |
| TC-07 | Banco — registro do magic link | Banco | PASS (execução original) |

---

## Detalhes por Caso

### TC-01 — E-mail recebido após conclusão do wizard — PASS (execução original)

**Expected:** E-mail enviado de `no-reply@portabox.dev` para `sindico.qa@portabox.dev` com assunto relacionado a acesso/senha.

**Actual:** E-mail encontrado no Mailpit.
- From: `no-reply@portabox.dev`
- To: `sindico.qa@portabox.dev`
- Subject: `Bem-vindo ao PortaBox — defina sua senha`
- Criado em: `2026-04-19T01:13:13Z`

**Evidências:** `requests.log` — seção TC-01

---

### TC-02 — Magic link contém token e é bem formado — PASS (execução original)

**Expected:** URL válida com parâmetro `token` não trivial, apontando para página de definição de senha.

**Actual:**
- URL extraída: `http://localhost:5174/password-setup?token=GSmVY1TW_q6ThuPXyFsuGHJYRPSNiEYt5OvnMjS4-HA`
- Formato: scheme + host:port + path `/password-setup` + query param `token`
- Token length: 43 caracteres

**Evidências:** `requests.log` — seção TC-02

---

### TC-03 — Página de definição de senha acessível via magic link — PASS (RERUN5)

**Re-executado em (RERUN5):** 2026-04-20T18:05:19Z
**Tenant usado:** Residencial Rerun QA (ID `4a3d87ea-f62f-4d9c-80de-a34237d0dae3`)
**Síndico:** `sindico.rerun@portabox.dev` (user_id `85c256c2-3958-4b69-b6c8-8104adb805a7`)

**Preparação:**
- Resend de magic link executado via `POST /api/v1/admin/condominios/.../sindicos/...:resend-magic-link` — HTTP 200
- Novo e-mail recebido no Mailpit (ID: `zuMRZrJG3LhRgwmJE_GxS0KOSfB6HX6QAAUF49SBNiY=@mailhog.example`, criado: `2026-04-20T18:04:13Z`)
- Token decodificado de quoted-printable: `gcEBtNw2q44PdiYPsdldzpPAXLgWVzOvuMGD43x1qW8`

**Passos executados:**

1. Playwright navegou para `http://localhost:5174/password-setup?token=gcEBtNw2q44PdiYPsdldzpPAXLgWVzOvuMGD43x1qW8`
2. Aguardou `domcontentloaded` + 3s adicionais para estabilização
3. Verificada URL final, conteúdo, campos de senha e botão

**Expected:**
- URL permanece em `/password-setup?token=...` — sem redirecionamento para `/login`
- Ao menos 1 campo `input[type="password"]` visível
- Botão "Definir senha" visível

**Actual:**
- URL final: `http://localhost:5174/password-setup?token=gcEBtNw2q44PdiYPsdldzpPAXLgWVzOvuMGD43x1qW8` — CORRETO
- Não redirecionou para /login: CORRETO
- Título da página: `PortaBox — Painel do Síndico`
- Body text length: 120 chars (formulário renderizado)
- Campos `input[type="password"]` encontrados: **2**
- Total de inputs: 2
- Botão "Definir senha" encontrado: **1**

**Console do browser (RERUN5):**
```
[debug] [vite] connecting...
[info] Download the React DevTools for a better development experience
[debug] [vite] connected.
```

Nenhum erro de runtime. Nenhum crash do React. O fix de remoção do `ReactQueryDevtools` de `apps/sindico/src/providers/QueryProvider.tsx` resolveu o problema identificado no RERUN4.

**Evidências:**
- Screenshot: `screenshots/rerun5_tc03_inicio.png`
- Screenshot: `screenshots/rerun5_tc03_apos_wait.png`
- Screenshot: `screenshots/rerun5_tc03_pre_assert.png`
- Screenshot: `screenshots/rerun5_tc03_pass.png`
- Vídeo: `videos/rerun5/`
- Log: `requests.log` — seção "TC-03 RERUN5"

---

### TC-04 — Definição de senha com o magic link — PASS (execução original)

**Expected:** HTTP 200, senha definida com sucesso.

**Actual:** HTTP 200, body vazio (sucesso). DB confirmou `consumed_at = 2026-04-19 01:28:54`.

**Nota:** Na execução anterior, foi necessário usar um segundo token (após resend) pois a primeira tentativa retornou HTTP 429 (rate limit, Retry-After: 600s).

**Evidências:** `requests.log` — seção TC-04 RETRY

---

### TC-05 — Magic link inválido após uso — PASS (execução original)

**Expected:** Token rejeitado com erro 4xx após uso.

**Actual:** HTTP 400 — `{"title": "Invalid or expired token.", "status": 400}`

**Evidências:** `requests.log` — seção TC-05

---

### TC-06 — Botão "Reenviar magic link" no painel de detalhes — PASS (re-execução — botão visível, reenvio confirmado)

**Re-executado em (RERUN2):** 2026-04-20T17:05:53Z
**Tenant usado:** Residencial Rerun QA (ID `4a3d87ea-f62f-4d9c-80de-a34237d0dae3`)
**Síndico:** `sindico.rerun@portabox.dev`
**Pré-condição verificada via API:** `sindicoSenhaDefinida: false` — confirmado (GET HTTP 200)

**Passos executados:**

1. Login com `operator@portabox.dev` — sucesso, redirecionado para `http://localhost:5173/condominios`
2. Navegação para `http://localhost:5173/condominios/4a3d87ea-f62f-4d9c-80de-a34237d0dae3`
3. Página renderizou com conteúdo (body text length: 1063 chars) — sem tela branca
4. Botão encontrado via seletor `button:has-text("Reenviar magic link")` — 1 elemento
5. Botão visível — assertion `toBeVisible()` passou
6. Clique no botão executado
7. Feedback de sucesso apareceu: `[role="status"]` com texto "Magic link reenviado com sucesso."

**Expected:**
- Botão "Reenviar magic link" visível (sindicoSenhaDefinida=false)
- Após clique: feedback de sucesso exibido

**Actual:**
- Botão "Reenviar magic link" encontrado e visível — PASS
- Feedback após clique: `"Magic link reenviado com sucesso."` via `[role="status"]` — PASS

**Evidências:**
- Screenshots: `screenshots/rerun2_tc06_*.png`
- Log: `requests.log` — seção "TC-06 RERUN2"

---

### TC-07 — Banco — registro do magic link — PASS (execução original)

**Query executada:** `SELECT * FROM magic_link WHERE user_id = '547b2417-fac5-4b3c-9cc8-69c855a95924' ORDER BY created_at DESC`

**Expected:** Registro com token_hash, created_at, expires_at (72h), consumed_at e invalidated_at.

**Actual — estado final do banco:**

| id | created_at | expires_at | consumed_at | invalidated_at |
|----|-----------|------------|-------------|----------------|
| 1522bf70 (novo) | 2026-04-19 01:28:07 | 2026-04-22 01:28:07 | 2026-04-19 01:28:54 | NULL |
| 528dc6a1 (original) | 2026-04-19 01:13:13 | 2026-04-22 01:13:13 | NULL | 2026-04-19 01:28:07 |

- Validade: 3 dias (72 horas) — correto
- Token original: invalidado no momento do resend
- Token novo: consumido após definição de senha (TC-04)
- `token_hash` presente: YES (não é o token em texto puro, apenas o hash)

**Evidências:** `requests.log` — seções TC-07 e DB STATE AFTER TC-04 AND TC-05

---

## Funcionalidades Verificadas com Sucesso

- E-mail enviado automaticamente ao síndico após criação do tenant
- Remetente correto: `no-reply@portabox.dev`
- Token presente no e-mail, formato URL correto (`/password-setup?token=...`)
- Token armazenado com hash no banco (não em texto puro)
- Validade de 72 horas (3 dias) correta
- Definição de senha via token funciona (HTTP 200) — validado via API direta
- Token marcado como consumido no banco após uso (`consumed_at`)
- Token rejeitado após uso (HTTP 400 `Invalid or expired token`)
- Reenvio de magic link via API funciona (HTTP 200)
- Ao reenviar: token antigo é invalidado (`invalidated_at`), novo token criado, novo e-mail enviado
- Painel de detalhes do condomínio renderiza corretamente
- Botão "Reenviar magic link" visível quando `sindicoSenhaDefinida=false`
- Após clicar em "Reenviar magic link": feedback de sucesso "Magic link reenviado com sucesso." exibido via `[role="status"]`
- Rota `/password-setup` reconhecida pelo router do app do síndico (não redireciona para `/login`) — CORRIGIDO
- Formulário de definição de senha renderiza com 2 campos `input[type="password"]` e botão "Definir senha" — CORRIGIDO (RERUN5)

---

## Histórico de Bugs Corrigidos

### BUG-A — Rota /password-setup redirecionava para /login — CORRIGIDO

- **Comportamento observado (RERUN2):** Rota registrada como `/setup-password` — redirecionava para `/login`
- **Fix aplicado:** path ajustado para `/password-setup` em `routes.tsx`
- **Verificado:** RERUN3, RERUN4, RERUN5 confirmaram que o redirecionamento não ocorre mais

### BUG-B — App crashava com `Cannot read properties of undefined (reading 'replace')` — CORRIGIDO

- **Comportamento observado (RERUN3):** Bootstrap sem `VITE_API_URL` causava crash antes dos providers
- **Fix aplicado:** `.env.local` criado com `VITE_API_URL=http://localhost:5272/api/v1`
- **Verificado:** RERUN4 e RERUN5 confirmaram que esse erro não ocorre mais

### BUG-C — ReactQueryDevtools crashava com `No QueryClient set` — CORRIGIDO

- **Comportamento observado (RERUN4):** `ReactQueryDevtools` renderizado fora do contexto do `QueryClientProvider` lançava exceção, desmontando a árvore React inteira
- **Fix aplicado:** `ReactQueryDevtools` removido de `apps/sindico/src/providers/QueryProvider.tsx`
- **Verificado (RERUN5):** Console do browser sem erros de runtime; formulário renderizado com 2 campos de senha e botão "Definir senha"

### P-03 (histórico) — Endpoint `/api/v1/auth/password-setup` retornou HTTP 429 (rate limit) — execução original

- **Comportamento observado:** Primeira tentativa de definição de senha retornou HTTP 429 (`Retry-After: 600`)
- **Impacto:** TC-04 só passou após executar o resend e usar o novo token

---

## Evidências

```
qa_task_03_magic_link_sindico/
├── qa_report_task_03.md              (este arquivo)
├── requests.log                      (log completo API + DB + todas as reruns)
├── screenshots/
│   ├── rerun5_tc03_inicio.png        (RERUN5: imediato após navegação)
│   ├── rerun5_tc03_apos_wait.png     (RERUN5: após 3s de espera)
│   ├── rerun5_tc03_pre_assert.png    (RERUN5: antes das assertions)
│   ├── rerun5_tc03_pass.png          (RERUN5: estado final — PASS)
│   ├── rerun4_tc03_inicio.png        (RERUN4: página branca)
│   ├── rerun4_tc03_apos_wait.png     (RERUN4: página branca após 3s)
│   ├── rerun4_tc03_pre_assert.png    (RERUN4: antes das assertions — branca)
│   ├── rerun4_tc03_fail_no_form.png  (RERUN4: estado de falha — branca)
│   ├── rerun3_tc03_inicio.png
│   ├── rerun3_tc03_apos_wait.png
│   ├── rerun3_tc03_pre_assert.png
│   ├── rerun3_tc03_final.png
│   ├── rerun2_tc03_inicio.png
│   ├── rerun2_tc03_apos_load.png
│   ├── rerun2_tc03_pre_assert.png
│   ├── rerun2_tc03_pass.png
│   ├── rerun2_tc06_login_inicio.png
│   ├── rerun2_tc06_login_preenchido.png
│   ├── rerun2_tc06_apos_login.png
│   ├── rerun2_tc06_painel_carregado.png
│   ├── rerun2_tc06_pre_assert_botao.png
│   ├── rerun2_tc06_botao_visivel.png
│   ├── rerun2_tc06_apos_clique.png
│   ├── tc06_login_inicio.png
│   ├── tc06_login_pre_submit.png
│   ├── tc06_apos_login.png
│   ├── tc06_painel_detalhes.png
│   ├── tc06_pre_assert.png
│   └── tc06_fail_playwright.png
└── videos/
    ├── rerun5/                       (vídeo gravado pelo Playwright RERUN5)
    └── rerun4/                       (vídeo gravado pelo Playwright RERUN4)
```

---

## Status para o Orquestrador

**Status:** PASS

Todos os 7 casos de teste passaram. TC-03 passou na RERUN5 (2026-04-20T18:05:19Z) após remoção do `ReactQueryDevtools` de `apps/sindico/src/providers/QueryProvider.tsx`.

**Resumo da RERUN5:**
- Token fresco obtido via resend (`POST .../sindicos/...:resend-magic-link` — HTTP 200)
- URL navegada: `http://localhost:5174/password-setup?token=gcEBtNw2q44PdiYPsdldzpPAXLgWVzOvuMGD43x1qW8`
- URL final: mantida em `/password-setup` (sem redirecionamento)
- Formulário visível: 2 campos `input[type="password"]`, 1 botão "Definir senha"
- Erros de console: nenhum (apenas mensagens de debug do Vite HMR)
- Playwright: 1 passed, 0 failed
