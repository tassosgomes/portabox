# Relatório QA Consolidado — F01: Assistente de Criação de Condomínio

**Data da Sessão:** 2026-04-18T00:00:00Z  
**Ambiente:** http://localhost:5173 (backoffice) | http://localhost:5272 (API) | http://localhost:8025 (Mailpit)  
**PRD:** `.compozy/tasks/f01-criacao-condominio/_prd.md`  
**Techspec:** não fornecida  
**Status Geral:** REPROVADO

---

## Sumário Executivo

A sessão de testes da feature F01 (Assistente de Criação de Condomínio) cobriu 6 user stories, executou 42 casos de teste e identificou falhas críticas na camada de UI do backoffice. O backend (API + banco de dados) funciona corretamente em todas as operações testadas — criação, ativação, magic link e auditoria. O problema dominante é um conjunto de mismatches entre o contrato da API e os tipos TypeScript do frontend: a API retorna campos como `cnpjMasked`, `signatarioCpfMasked`, `celularMasked` e `sindicoSenhaDefinida`, enquanto o frontend acessa `cnpj`, `signatarioCpf`, `celularE164` e `sindico.passwordDefined`. Esses mismatches causam crashes de tela branca em dois componentes centrais (`DetalhesCondominioPage` e `ListaCondominiosPage`), tornando a interface do backoffice inoperante para as features CF4 e CF5. O fluxo de criação do wizard (CF1) também falha no redirecionamento pós-criação por um mismatch de campo análogo (`id` vs `condominioId`). Resultado geral: REPROVADO.

---

## Resultado por Feature

| Feature / User Story | Task | Status | TCs Aprovados | TCs Reprovados | TCs Não Executados |
|----------------------|------|--------|---------------|----------------|--------------------|
| CF1 — Wizard de criação de tenant | qa_task_01 | FAIL | 13 | 1 | 0 |
| CF2 — Validação e deduplicação de CNPJ | qa_task_02 | PASS | 4 | 0 | 0 |
| CF3 — Magic link do síndico | qa_task_03 | PARTIAL | 5 | 2 | 0 |
| CF4 — Painel de detalhes + go-live | qa_task_04 | FAIL | 2 | 4 | 2 |
| CF5 — Lista de tenants | qa_task_05 | FAIL | 0 | 1 | 7 |
| CF6 — Log de auditoria | qa_task_06 | PARTIAL | 2 | 1 | 0 + 2 PARTIAL |

**Totais:**

| Métrica | Resultado |
|---------|-----------|
| Tasks executadas | 6 de 6 |
| Tasks PASS | 1 |
| Tasks FAIL | 3 |
| Tasks PARTIAL | 2 |
| Tasks BLOCKED | 0 |
| Casos de teste total | 42 |
| Casos PASS | 26 |
| Casos FAIL | 9 |
| Casos não executados / PARTIAL | 7 |
| **Resultado geral** | **REPROVADO** |

### Escopo Excluído

Nenhuma feature foi excluída do escopo durante a sessão. Todas as 6 user stories foram executadas.

---

## Resultado por Feature — Detalhamento

### qa_task_01 — CF1: Wizard de Criação de Tenant — FAIL

**Tipos de teste:** UI + API + Banco  
**Casos executados:** 14/14

| Caso | Descrição | Status |
|------|-----------|--------|
| TC-01 | Login e acesso ao wizard | PASS |
| TC-02 | Etapa 1 — Validação de campos obrigatórios | PASS |
| TC-03 | Etapa 1 — CNPJ inválido bloqueado | PASS |
| TC-04 | Etapa 1 — Avanço para etapa 2 | PASS |
| TC-05 | Etapa 2 — Validação de campos obrigatórios | PASS |
| TC-06 | Etapa 2 — Avanço para etapa 3 | PASS |
| TC-07 | Etapa 3 — Validação de celular E.164 | PASS |
| TC-08 | Etapa 3 — Avanço para revisão | PASS |
| TC-09 | Revisão — dados exibidos corretamente | PASS |
| TC-10 | Revisão — botão Voltar funciona | PASS |
| TC-11 | Confirmação — criação e redirecionamento | FAIL |
| TC-12 | Banco — condomínio com status pré-ativo | PASS |
| TC-13 | Banco — opt-in registrado | PASS |
| TC-14 | Banco — síndico cadastrado + magic link gerado | PASS |

**Evidências:** `qa-evidence/qa_task_01_wizard_criacao_tenant/`

---

### qa_task_02 — CF2: Validação e Deduplicação de CNPJ — PASS

**Tipos de teste:** UI + API  
**Casos executados:** 4/4

| Caso | Descrição | Status |
|------|-----------|--------|
| TC-01 | CNPJ com formato inválido (incompleto) | PASS |
| TC-02 | CNPJ formato correto com dígito verificador inválido | PASS |
| TC-03 | CNPJ válido duplicado — duplicata detectada e bloqueada | PASS |
| TC-04 | CNPJ válido e único — permite avançar | PASS |

**Observação:** TC-03 passou (409 recebido, fluxo bloqueado), mas há divergência com o PRD: a mensagem de duplicata não exibe o nome do tenant existente, a data de criação nem o status. A API não retorna os campos `extensions.nomeExistente` e `extensions.criadoEm` previstos no PRD CF2. O frontend está preparado para exibir essas informações, mas elas não chegam da API.

**Evidências:** `qa-evidence/qa_task_02_validacao_deduplicacao_cnpj/`

---

### qa_task_03 — CF3: Magic Link do Síndico — PARTIAL

**Tipos de teste:** UI + API + Email + Banco  
**Casos executados:** 7/7 (2 falharam)

| Caso | Descrição | Status |
|------|-----------|--------|
| TC-01 | E-mail recebido após conclusão do wizard | PASS |
| TC-02 | Magic link contém token bem formado | PASS |
| TC-03 | Página de definição de senha acessível via magic link (UI) | FAIL |
| TC-04 | Definição de senha com o magic link (API) | PASS |
| TC-05 | Magic link rejeitado após uso | PASS |
| TC-06 | Botão "Reenviar magic link" visível no painel de detalhes (UI) | FAIL |
| TC-07 | Banco — registro do magic link com hash e timestamps | PASS |

**Motivo dos FAILs:**
- TC-03: O app do síndico (porta 5174) não estava em execução no ambiente de teste. O magic link aponta corretamente para `localhost:5174`, mas a conexão foi recusada.
- TC-06: A sessão Playwright não persistiu para as chamadas de API da SPA após o login. A página do painel de detalhes exibiu tela em branco com erros 401 — o mesmo crash transversal documentado em qa_task_04.

**Evidências:** `qa-evidence/qa_task_03_magic_link_sindico/`

---

### qa_task_04 — CF4: Painel de Detalhes e Go-live — FAIL

**Tipos de teste:** UI + API + Banco  
**Casos executados:** 8 (4 FAIL, 2 PASS via API/Banco, 2 não executados)

| Caso | Descrição | Status |
|------|-----------|--------|
| CT-01 | Acessar painel de detalhes do tenant (UI) | FAIL |
| CT-02 | Dados do opt-in exibidos (UI) | FAIL |
| CT-03 | Situação do primeiro síndico exibida (UI) | FAIL |
| CT-04 | Ação "Ativar operação" requer confirmação dupla (UI) | FAIL |
| CT-05 | Ativação do tenant (go-live) via UI | Não executado |
| CT-06 | Banco — status atualizado após go-live | PASS (via API) |
| CT-07 | Registro de auditoria da ativação | PASS |
| CT-08 | Botão "Ativar operação" desaparece após ativação (UI) | Não executado |

**Motivo dos FAILs de UI:** A página `DetalhesCondominioPage` crasha com tela branca ao ser carregada. Foram identificados 5 mismatches de contrato API x frontend (detalhados na seção de bugs). O crash ocorre em `DetalhesCondominioPage.tsx:83` com `Cannot read properties of undefined (reading 'replace')`.

**Evidências:** `qa-evidence/qa_task_04_painel_detalhes_golive/`

---

### qa_task_05 — CF5: Lista de Tenants — FAIL

**Tipos de teste:** UI  
**Casos executados:** 1/8 (1 FAIL, 7 não executados por bloqueio do crash)

| Caso | Descrição | Status |
|------|-----------|--------|
| TC-01 | Lista exibe o tenant criado | FAIL |
| TC-02 | Colunas da tabela estão presentes | Não executado |
| TC-03 | Filtro por status pré-ativo | Não executado |
| TC-04 | Filtro por status ativo não exibe tenant pré-ativo | Não executado |
| TC-05 | Busca por nome retorna tenant correto | Não executado |
| TC-06 | Busca por CNPJ retorna tenant correto | Não executado |
| TC-07 | Clicar na linha redireciona para o painel correto | Não executado |
| TC-08 | Paginação presente e funcional | Não executado |

**Motivo do FAIL:** O componente `ListaCondominiosPage` crasha ao receber dados da API. A causa é análoga ao crash de qa_task_04: a API retorna `cnpjMasked` mas o frontend acessa `item.cnpj`. TC-02 a TC-08 não foram executados por bloqueio do crash em TC-01.

**Evidências:** `qa-evidence/qa_task_05_lista_tenants/`

---

### qa_task_06 — CF6: Log de Auditoria — PARTIAL

**Tipos de teste:** UI + API + Banco  
**Casos executados:** 5/5 (2 PASS, 1 FAIL, 2 PARTIAL)

| Caso | Descrição | Status |
|------|-----------|--------|
| TC-01 | Entrada de criação (event_kind=1) no banco | PASS |
| TC-02 | Entrada de ativação (event_kind=2) no banco | PASS |
| TC-03 | Endpoint de API retorna audit log | PARTIAL |
| TC-04 | Log de auditoria visível no painel de detalhes (UI) | FAIL |
| TC-05 | Imutabilidade da tabela (estrutura append-only) | PARTIAL |

**Motivo do FAIL de UI (TC-04):** O mesmo crash transversal de `DetalhesCondominioPage.tsx:83` bloqueia a visualização do log na interface.  
**Motivo dos PARTIALs:** TC-03 — o log é retornado embutido no endpoint de detalhes, sem endpoint dedicado e sem nome do operador (apenas UUID). TC-05 — a tabela não possui colunas de mutabilidade, mas também não possui triggers, rules nem RLS que impeçam UPDATE/DELETE diretamente no banco.

**Evidências:** `qa-evidence/qa_task_06_log_auditoria/`

---

## Detalhes das Falhas

### BUG-01 — Redirecionamento para `/condominios/undefined` após criação de tenant (Severidade: Crítica)

- **Feature afetada:** CF1 — Wizard de criação de tenant (qa_task_01, TC-11)
- **Componentes envolvidos:** `NovoCondominioPage.tsx` | `types.ts` (interface `CreateCondominioResponse`) | `PortaBox.Api/Endpoints/CondominiosEndpoints.cs`
- **Comportamento esperado:** Após POST bem-sucedido (HTTP 201), o operador é redirecionado para `/condominios/{uuid-valido}` com mensagem de sucesso.
- **Comportamento observado:** O operador é redirecionado para `/condominios/undefined`. A página exibe "Condomínio não encontrado." A API retorna `{ "condominioId": "uuid", "sindicoUserId": "uuid" }`, mas o frontend acessa `result.id` (que é `undefined`) e executa `navigate('/condominios/undefined')`.

**Passos até a falha:**
1. Wizard preenchido nas 3 etapas com dados válidos
2. Revisão conferida — dados exibidos corretamente
3. Clique em "Criar condomínio"
4. API retornou HTTP 201 com body `{ "condominioId": "f6d3cc9d-..." }`
5. Frontend executou `navigate('/condominios/${result.id}')` — `result.id` = `undefined`
6. Página exibiu "Condomínio não encontrado."

**Observação:** O backend cria o tenant corretamente (confirmado no banco com status pré-ativo, opt-in registrado e síndico cadastrado). O bug é exclusivamente no mapeamento do campo de retorno da API no frontend.

**Evidências:**
- Screenshot: `qa-evidence/qa_task_01_wizard_criacao_tenant/screenshots/ct11_fail_no_redirect.png`
- Screenshot: `qa-evidence/qa_task_01_wizard_criacao_tenant/screenshots/ct11_revisao_pre_submit.png`
- Log: `qa-evidence/qa_task_01_wizard_criacao_tenant/requests.log` — seção "API DIRECT TEST — Confirming response shape"

---

### BUG-02 — Crash de tela branca em `DetalhesCondominioPage` — campo `cnpj` vs `cnpjMasked` (Severidade: Crítica)

- **Feature afetada:** CF4 — Painel de detalhes (qa_task_04, CT-01 a CT-04); CF3 — Magic link (qa_task_03, TC-06); CF6 — Log de auditoria (qa_task_06, TC-04)
- **Componente:** `DetalhesCondominioPage.tsx` linha 83 (linha 129 conforme stack trace de qa_task_04)
- **Comportamento esperado:** Página carrega e exibe os dados do tenant.
- **Comportamento observado:** Página exibe tela completamente em branco. O componente crasha com `Cannot read properties of undefined (reading 'replace')` ao tentar executar `formatCnpjMasked(details.cnpj)`. A API retorna o campo como `cnpjMasked`, não como `cnpj`.

**Erro capturado no browser console:**
```
[PAGE_ERROR] Cannot read properties of undefined (reading 'replace')
[BROWSER_ERROR] The above error occurred in the <DetalhesCondominioPage> component:
  at DetalhesCondominioPage (http://localhost:5173/src/features/condominios/pages/DetalhesCondominioPage.tsx:83:18)
```

**Evidências:**
- Screenshot: `qa-evidence/qa_task_04_painel_detalhes_golive/screenshots/ct01_painel_inicio.png` (tela branca)
- Screenshot: `qa-evidence/qa_task_06_log_auditoria/screenshots/tc04_fail_browser_crash.png`
- Log: `qa-evidence/qa_task_04_painel_detalhes_golive/requests.log` linhas 59-89

---

### BUG-03 — Campos ausentes em `DetalhesCondominioPage` — mismatches adicionais de contrato (Severidade: Alta)

- **Feature afetada:** CF4 — Painel de detalhes (qa_task_04)
- **Comportamento esperado:** Após correção do BUG-02, os campos de CPF do signatário, celular do síndico, status de senha do síndico e log de auditoria seriam exibidos corretamente.
- **Comportamento observado (projetado a partir da análise de código e contrato da API):** Quatro campos adicionais seriam exibidos como `undefined`:

| Campo acessado pelo frontend | Campo retornado pela API | Efeito |
|------------------------------|--------------------------|--------|
| `details.optIn.signatarioCpf` | `signatarioCpfMasked` | CPF do signatário exibe `undefined` |
| `sindico.celularE164` | `celularMasked` | Celular do síndico exibe `undefined` |
| `sindico.passwordDefined` | `sindicoSenhaDefinida` (nível raiz) | Botão "Reenviar magic link" sempre visível, independente do estado real |
| `auditEntry.performedByEmail` | `performedByUserId` (UUID) | Log de auditoria exibiria UUID do operador em vez do e-mail |

**Evidências:** Análise estática do código-fonte referenciada em `qa-evidence/qa_task_04_painel_detalhes_golive/requests.log`

---

### BUG-04 — Crash de tela branca em `ListaCondominiosPage` — campo `cnpj` vs `cnpjMasked` (Severidade: Crítica)

- **Feature afetada:** CF5 — Lista de tenants (qa_task_05, TC-01 — bloqueou TC-02 a TC-08)
- **Componente:** `ListaCondominiosPage.tsx` linha 39
- **Comportamento esperado:** Página exibe tabela com os tenants cadastrados.
- **Comportamento observado:** Página exibe tela em branco. O componente crasha com `TypeError: Cannot read properties of undefined (reading 'replace')` ao chamar `formatCnpj(item.cnpj)`. A API retorna `cnpjMasked`, não `cnpj`.

**Erro capturado no browser console:**
```
[PAGE_ERROR] Cannot read properties of undefined (reading 'replace')
[BROWSER_CONSOLE_ERROR] The above error occurred in the <ListaCondominiosPage> component:
    at ListaCondominiosPage (http://localhost:5173/src/features/condominios/pages/ListaCondominiosPage.tsx:39:20)
```

**Evidências:**
- Screenshot: `qa-evidence/qa_task_05_lista_tenants/screenshots/tc01_fail_blank_page.png`
- Log: `qa-evidence/qa_task_05_lista_tenants/requests.log` — entradas TC-01 com [PAGE_ERROR]

---

### BUG-05 — Paginação da lista de tenants inoperante — campo `total` vs `totalCount` (Severidade: Crítica)

- **Feature afetada:** CF5 — Lista de tenants (qa_task_05)
- **Componente:** `ListaCondominiosPage.tsx` (tipo `PagedResult`)
- **Comportamento esperado:** Paginação calcula corretamente o total de páginas.
- **Comportamento observado:** O frontend calcula `Math.ceil(result.total / PAGE_SIZE)` onde `result.total` é `undefined` (a API retorna `totalCount`), resultando em `NaN`. A paginação fica inoperante.

**Resposta real da API:**
```json
{ "items": [...], "page": 1, "pageSize": 20, "totalCount": 2, "totalPages": 1 }
```

**Evidências:** Log `qa-evidence/qa_task_05_lista_tenants/requests.log`

---

### BUG-06 — Mensagem de duplicata de CNPJ não exibe contexto do tenant existente (Severidade: Média)

- **Feature afetada:** CF2 — Validação e deduplicação de CNPJ (qa_task_02, TC-03 — divergência, status mantido PASS)
- **Comportamento esperado (PRD CF2):** Mensagem de duplicata exibe nome do tenant existente, data de criação e status ("pré-ativo").
- **Comportamento observado:** Mensagem exibe apenas "Este CNPJ já está cadastrado." sem nome, data ou status.

**Resposta real da API (HTTP 409):**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "CNPJ já cadastrado.",
  "status": 409,
  "detail": "CnpjAlreadyExists",
  "instance": "/api/v1/admin/condominios"
}
```

O campo `extensions` com `nomeExistente` e `criadoEm` — previstos no PRD e mapeados no frontend em `NovoCondominioPage.tsx` linhas 71-80 — não foi retornado pela API.

**Evidências:** `qa-evidence/qa_task_02_validacao_deduplicacao_cnpj/requests.log` linhas 26-43

---

### BUG-07 — App do síndico (porta 5174) não disponível no ambiente de teste (Severidade: Média — Infraestrutura)

- **Feature afetada:** CF3 — Magic link do síndico (qa_task_03, TC-03)
- **Comportamento esperado:** O magic link gerado (`http://localhost:5174/password-setup?token=...`) leva a uma página de definição de senha acessível.
- **Comportamento observado:** `http://localhost:5174` recusou conexão. Timeout de 15 segundos no Playwright.

**Erro capturado:**
```
page.goto: Timeout 15000ms exceeded.
navigating to "http://localhost:5174/password-setup?token=GSmVY1TW_q6ThuPXyFsuGHJYRPSNiEYt5OvnMjS4-HA", waiting until "load"
```

**Evidências:** Playwright test result em `tests/e2e/specs/qa_task_03_magic_link.spec.ts`

---

### BUG-08 — `status=0` na API de listagem retorna lista vazia (Severidade: Média)

- **Feature afetada:** CF5 — Lista de tenants (qa_task_05)
- **Comportamento esperado:** `GET /api/v1/admin/condominios?status=0` retorna todos os tenants (status=0 = "Todos").
- **Comportamento observado:** `totalCount: 0` — lista vazia. O frontend contorna o problema omitindo o parâmetro `status` quando o valor é 0, o que funciona. O comportamento do backend com `status=0` é incorreto.

**Evidências:** Log `qa-evidence/qa_task_05_lista_tenants/requests.log`

---

### BUG-09 — Imutabilidade do log de auditoria sem garantia em banco (Severidade: Baixa)

- **Feature afetada:** CF6 — Log de auditoria (qa_task_06, TC-05)
- **Comportamento esperado:** Tabela `tenant_audit_log` com garantia de append-only em nível de banco (trigger, rule ou RLS bloqueando UPDATE/DELETE).
- **Comportamento observado:** A tabela não possui colunas de mutabilidade, mas também não possui nenhum mecanismo de banco que impeça UPDATE ou DELETE diretamente. A imutabilidade depende exclusivamente da lógica da aplicação.

**Evidências:** `qa-evidence/qa_task_06_log_auditoria/requests.log` linhas 104-114

---

### BUG-10 — event_kind=3 não documentado no log de auditoria (Severidade: Informativa)

- **Feature afetada:** CF6 — Log de auditoria (qa_task_06)
- **Comportamento observado:** O banco contém um registro `id=4, event_kind=3, occurred_at=2026-04-19T01:28:07` na tabela `tenant_audit_log`. O PRD menciona apenas criação (event_kind=1) e ativação (event_kind=2). A origem deste evento é desconhecida.

**Evidências:** `qa-evidence/qa_task_06_log_auditoria/requests.log`

---

## Recomendações de Investigação

### 1. Mismatch de contrato entre API e frontend — responsável por todos os crashes de tela branca

- **Contexto:** Os componentes `DetalhesCondominioPage` e `ListaCondominiosPage` crasham porque a API retorna nomes de campo diferentes dos esperados pelos tipos TypeScript do frontend.
- **Comportamento observado:** Tela branca com `TypeError: Cannot read properties of undefined (reading 'replace')` nos dois componentes.
- **Onde investigar:** Contrato de resposta dos endpoints `GET /api/v1/admin/condominios` (lista) e `GET /api/v1/admin/condominios/{id}` (detalhes) versus as interfaces TypeScript `CondominioListItem`, `CondominioDetails`, `OptInRecord`, `SindicoInfo` e `AuditEntry` em `apps/backoffice/src/features/condominios/types.ts`.
- **Campos divergentes identificados:** `cnpj` vs `cnpjMasked` | `signatarioCpf` vs `signatarioCpfMasked` | `celularE164` vs `celularMasked` | `sindico.passwordDefined` vs `sindicoSenhaDefinida` (nível raiz) | `performedByEmail` vs `performedByUserId` | `total` vs `totalCount`.
- **Evidências:** `qa-evidence/qa_task_04_painel_detalhes_golive/screenshots/ct01_painel_inicio.png`, `qa-evidence/qa_task_05_lista_tenants/screenshots/tc01_fail_blank_page.png`, `qa-evidence/qa_task_04_painel_detalhes_golive/requests.log`

---

### 2. Campo de retorno do POST de criação de tenant — `condominioId` vs `id`

- **Contexto:** O endpoint `POST /api/v1/admin/condominios` retorna `{ "condominioId": "uuid" }`, mas a interface `CreateCondominioResponse` em `apps/backoffice/src/features/condominios/types.ts` espera `{ id: string }`.
- **Comportamento observado:** O operador é redirecionado para `/condominios/undefined` após criar um tenant.
- **Onde investigar:** Record `CreateCondominioResponse` em `PortaBox.Api/Endpoints/CondominiosEndpoints.cs` e interface homônima em `types.ts`. O comando `navigate()` em `NovoCondominioPage.tsx` usa `result.id`.
- **Evidências:** `qa-evidence/qa_task_01_wizard_criacao_tenant/screenshots/ct11_fail_no_redirect.png`, `qa-evidence/qa_task_01_wizard_criacao_tenant/requests.log`

---

### 3. Ausência de contexto do tenant existente na resposta 409 de CNPJ duplicado

- **Contexto:** O PRD CF2 especifica que a mensagem de duplicata deve incluir nome do tenant existente, data de criação e status.
- **Comportamento observado:** A API retorna HTTP 409 sem as extensões `nomeExistente` e `criadoEm`. A mensagem exibida ao operador é genérica ("Este CNPJ já está cadastrado.").
- **Onde investigar:** Handler do endpoint `POST /api/v1/admin/condominios` para o caso `CnpjAlreadyExists` — construção do objeto `ProblemDetails` retornado e se o campo `extensions` é populado com os dados do tenant existente.
- **Evidências:** `qa-evidence/qa_task_02_validacao_deduplicacao_cnpj/requests.log` linhas 26-43

---

### 4. App do síndico (porta 5174) ausente no ambiente de testes

- **Contexto:** O magic link gerado aponta para `http://localhost:5174/password-setup?token=...`. O app do síndico não estava em execução durante a sessão.
- **Comportamento observado:** Timeout de conexão ao tentar acessar o magic link via navegador.
- **Onde investigar:** Configuração do ambiente de testes para inicialização do app `apps/sindico` antes da execução da suite CF3. Verificar se `localhost:5174` é o endereço correto de produção ou apenas de desenvolvimento local.
- **Evidências:** Playwright test result referenciado em `qa-evidence/qa_task_03_magic_link_sindico/requests.log`

---

### 5. Rate limiting de login ativado durante execução sequencial de testes de UI

- **Contexto:** O backend aplica rate limiting por IP nas tentativas de login. Múltiplos testes que realizam login em sequência curta (qa_task_04, qa_task_05) ativaram o bloqueio.
- **Comportamento observado:** HTTP 429 nos endpoints de login com `Retry-After: 600s` durante a execução de CT-03, CT-04 e CT-05 de qa_task_04.
- **Onde investigar:** Configuração dos limites de rate limiting para o endpoint de autenticação e se há mecanismo de bypass ou de identidade de sessão para ambientes de teste.
- **Evidências:** `qa-evidence/qa_task_04_painel_detalhes_golive/screenshots/ct01_rate_limit_fail.png`, `qa-evidence/qa_task_04_painel_detalhes_golive/requests.log` linhas 132-139

---

### 6. Comportamento do endpoint de listagem com `status=0`

- **Contexto:** O parâmetro `status=0` passado explicitamente ao endpoint `GET /api/v1/admin/condominios` retorna lista vazia em vez de todos os tenants.
- **Comportamento observado:** `totalCount: 0` quando `status=0` é enviado. Sem o parâmetro, a listagem retorna normalmente.
- **Onde investigar:** Lógica de filtragem do endpoint de listagem no backend — como o valor `0` é interpretado (ausência de filtro vs. status inválido).
- **Evidências:** `qa-evidence/qa_task_05_lista_tenants/requests.log`

---

### 7. Imutabilidade do log de auditoria sem constraint de banco

- **Contexto:** A tabela `tenant_audit_log` não possui mecanismo de banco (trigger, rule ou RLS) que impeça UPDATE ou DELETE.
- **Comportamento observado:** A integridade do log depende exclusivamente da camada de aplicação.
- **Onde investigar:** Estratégia de proteção da tabela `tenant_audit_log` no banco PostgreSQL — se a ausência de constraint é uma decisão de design ou uma lacuna.
- **Evidências:** `qa-evidence/qa_task_06_log_auditoria/requests.log` linhas 104-114

---

### 8. event_kind=3 não documentado no log de auditoria

- **Contexto:** O banco contém um registro com `event_kind=3` para o tenant testado. O PRD menciona apenas os eventos de criação (1) e ativação (2).
- **Comportamento observado:** Registro `id=4, event_kind=3, occurred_at=2026-04-19T01:28:07` presente na tabela `tenant_audit_log`.
- **Onde investigar:** Definição do enum `TenantEventKind` no backend e se o evento 3 corresponde a alguma ação documentada (ex.: reenvio de magic link, alteração de dados).
- **Evidências:** `qa-evidence/qa_task_06_log_auditoria/requests.log`

---

## Índice de Evidências

```
qa-evidence/
├── qa_session.json
├── qa_report_consolidated.md
│
├── qa_task_01_wizard_criacao_tenant/
│   ├── test_plan.md
│   ├── qa_report_task_01.md
│   ├── requests.log
│   └── screenshots/
│       ├── ct01_login_page.png
│       ├── ct01_login_filled.png
│       ├── ct01_after_login.png
│       ├── ct01_wizard_page.png
│       ├── ct02_inicio.png
│       ├── ct02_errors.png
│       ├── ct03_cnpj_invalido_filled.png
│       ├── ct03_cnpj_invalido_error.png
│       ├── ct04_etapa1_inicio.png
│       ├── ct04_etapa1_filled.png
│       ├── ct04_etapa2_inicio.png
│       ├── ct05_etapa2_errors.png
│       ├── ct06_etapa2_filled.png
│       ├── ct06_etapa3_inicio.png
│       ├── ct07_etapa3_errors_empty.png
│       ├── ct07_etapa3_celular_nacional_error.png
│       ├── ct08_etapa3_e164_filled.png
│       ├── ct08_revisao_inicio.png
│       ├── ct09_revisao_dados.png
│       ├── ct10_apos_voltar.png
│       ├── ct11_revisao_pre_submit.png
│       └── ct11_fail_no_redirect.png
│
├── qa_task_02_validacao_deduplicacao_cnpj/
│   ├── test_plan.md
│   ├── qa_report_task_02.md
│   ├── requests.log
│   └── screenshots/
│       ├── tc01_inicio.png, tc01_filled.png, tc01_apos_click.png
│       ├── tc02_inicio.png, tc02_filled.png, tc02_apos_click.png
│       ├── tc03_inicio.png ... tc03_apos_submit.png
│       └── tc04_inicio.png, tc04_filled.png, tc04_apos_click.png
│
├── qa_task_03_magic_link_sindico/
│   ├── requests.log
│   ├── qa_report_task_03.md
│   └── screenshots/
│       ├── tc06_login_inicio.png
│       ├── tc06_login_pre_submit.png
│       ├── tc06_apos_login.png        (branco — sessão não carregou)
│       ├── tc06_painel_detalhes.png   (branco — 401 errors)
│       ├── tc06_pre_assert.png
│       └── tc06_fail_playwright.png
│
├── qa_task_04_painel_detalhes_golive/
│   ├── test_plan.md
│   ├── qa_report_task_04.md
│   ├── requests.log
│   └── screenshots/
│       ├── ct01_painel_inicio.png     (tela branca — crash UI)
│       ├── ct01_painel_resultado.png  (tela branca)
│       ├── ct01_rate_limit_fail.png   (rate limit segunda execução)
│       ├── ct02_optin_inicio.png      (tela branca)
│       ├── ct02_optin_resultado.png
│       ├── ct04_fail_playwright.png
│       └── ct05_08_fail_playwright.png
│
├── qa_task_05_lista_tenants/
│   ├── test_plan.md
│   ├── qa_report_task_05.md
│   ├── requests.log
│   ├── auth_state.json
│   └── screenshots/
│       ├── setup_login.png
│       ├── setup_login_preenchido.png
│       ├── setup_login_sucesso.png
│       ├── tc01_inicio.png
│       ├── tc01_fail_blank_page.png   (tela em branco — crash React)
│       └── tc03_antes_filtro.png
│
└── qa_task_06_log_auditoria/
    ├── test_plan.md
    ├── requests.log
    ├── qa_report_task_06.md
    └── screenshots/
        ├── tc04_fail_browser_crash.png
        └── tc04_page_load.png         (tela em branco)
```

---

## Informações da Sessão

| Campo | Valor |
|-------|-------|
| Banco validado | Sim |
| Tipo de banco | PostgreSQL 16 (Docker: postgres:16-alpine) |
| Autenticação testada | Sim (Bearer + cookie HttpOnly) |
| Playwright (UI) | Sim |
| cURL (API) | Sim |
| Email catcher | Sim (Mailpit em localhost:8025) |
| Tasks em paralelo | Sim (Fase 2: qa_task_02, 03, 04, 05 em paralelo) |
| Orchestrator version | 1.0 |
