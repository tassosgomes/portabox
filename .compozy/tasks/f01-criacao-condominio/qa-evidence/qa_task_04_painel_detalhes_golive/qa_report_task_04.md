# QA Report — qa_task_04: Painel de Detalhes e Go-live

**Task ID:** qa_task_04
**Data/Hora:** 2026-04-20T17:54:30Z
**Executor:** qa-task-runner (rerun3 — pós BUG-7)
**Status Geral:** PASS

---

## Contexto

- **User Story:** Painel de detalhes do condomínio + ativação go-live
- **Ambiente:** http://localhost:5173 / API http://localhost:5272
- **Tipos de teste:** UI (Playwright) + Banco (psql)
- **Autenticação:** Sim (cookie portabox.auth via login UI)
- **Tenant de teste:** 4a3d87ea-f62f-4d9c-80de-a34237d0dae3 ("Residencial Rerun QA")

---

## Resumo Executivo

Esta re-execução (rerun3) foi realizada após a correção de BUG-7 (`formatDate()` — off-by-one-day em strings `YYYY-MM-DD`). A função agora usa o construtor de data local `new Date(y, m-1, d)` em vez de `new Date(iso)` para strings date-only, eliminando a conversão UTC→local que causava exibição um dia anterior ao correto em fusos UTC-3.

O CT-02 passou: a UI exibe `15/03/2026` para ambos os campos `dataAssembleia` e `dataTermo`, conforme armazenado no banco.

Todos os casos de teste estão em PASS.

---

## Casos de Teste

| ID | Descrição | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Acessar painel de detalhes | UI | PASS (rerun2 anterior) |
| CT-02 | Dados do opt-in exibidos com datas corretas | UI | PASS (rerun3) |
| CT-03 | Situação do síndico exibida | UI | PASS (rerun2 anterior) |
| CT-04 | Ação "Ativar operação" requer confirmação dupla | UI | PASS (rerun2 anterior) |
| CT-05 | Ativação do tenant (go-live) via UI | UI | PASS (rerun2 anterior) |
| CT-06 | Banco — status atualizado após go-live | Banco | PASS (rerun2 anterior) |
| CT-07 | Auditoria da ativação | Banco | PASS (rerun2 anterior) |
| CT-08 | Botão "Ativar operação" desaparece após ativação | UI | PASS (rerun2 anterior) |

---

## Detalhes por Caso

### CT-01 — Acessar painel de detalhes PASS (rerun2 anterior)

**Expected:** Página renderiza sem crash; exibe "Residencial Rerun QA", CNPJ mascarado, badge "pré-ativo"

**Actual:** Página renderizou corretamente. Corpo continha:
- "Residencial Rerun QA" — encontrado
- "****7000161" — encontrado
- "Pré-ativo" — encontrado

**Evidências:** `screenshots/rerun2_ct01_inicio.png`, `screenshots/rerun2_ct01_pass.png`

---

### CT-02 — Dados do opt-in exibidos com datas corretas PASS (rerun3)

**Passos executados:**
1. Login com operator@portabox.dev (sucesso)
2. Navegação para /condominios/4a3d87ea-f62f-4d9c-80de-a34237d0dae3
3. Aguardo de 3s para carregamento da API
4. Leitura do corpo da página
5. Verificação de `15/03/2026` presente e `14/03/2026` ausente

**Expected:**
- Data da assembleia: `15/03/2026`
- Data do termo: `15/03/2026`
- `14/03/2026` ausente (bug anterior)

**Actual (rerun3):**
- Seção "Consentimento LGPD" — presente
- Data da assembleia: `15/03/2026` — CORRETO
- Data do termo: `15/03/2026` — CORRETO
- Data errada `14/03/2026` — ausente

**Trecho do body capturado:**
```
"timento LGPDData da assembleia15/03/2026Quórum60%SignatárioCarlos RerunCPF do signatário***.982.247-**Data do termo15/03/2026"
```

**API retornou:** `"dataAssembleia": "2026-03-15"`, `"dataTermo": "2026-03-15"`

**Correção verificada:** `formatDate()` agora usa `new Date(y, m-1, d)` para strings `YYYY-MM-DD`, renderizando no fuso local sem offset UTC.

**Evidências:**
- Screenshot: `screenshots/rerun3_ct02_inicio.png`
- Screenshot: `screenshots/rerun3_ct02_pre_assert.png`
- Screenshot: `screenshots/rerun3_ct02_pass.png` (DATA DA ASSEMBLEIA: 15/03/2026 visível na UI)
- Log: `requests.log` — entrada `[2026-04-20T17:54:29.467Z] CT-02 (rerun3): PASS`

---

### CT-03 — Situação do síndico exibida PASS (rerun2 anterior)

**Expected:** Nome "Sindico Rerun", email sindico.rerun@portabox.dev, celular mascarado, "Senha definida: Não"

**Actual:**
- Nome "Sindico Rerun" — encontrado
- Email "sindico.rerun@portabox.dev" — encontrado
- Celular "+55 11 9****-4321" — encontrado
- "Não" (Senha definida) — encontrado

**Evidências:** `screenshots/rerun2_ct03_inicio.png`, `screenshots/rerun2_ct03_pass.png`

---

### CT-04 — Ação "Ativar operação" requer confirmação dupla PASS (rerun2 anterior)

**Expected:** Diálogo/modal de confirmação aparece antes de ativar

**Actual:** Ao clicar em "Ativar operação", apareceu modal com título "Confirmar ativação" e botão de confirmação. Body após clique continha: `"Ativar operaçãoConfirmar ativação✕Ao ativar,"` — confirmando modal presente.

**Evidências:** `screenshots/rerun2_ct04_pre_click.png`, `screenshots/rerun2_ct04_apos_click.png`, `screenshots/rerun2_ct04_dialog_pass.png`

---

### CT-05 — Ativação do tenant (go-live) via UI PASS (rerun2 anterior)

**Expected:** Status muda para "Ativo"; mensagem de sucesso; botão "Ativar operação" desaparece

**Actual:**
- Modal detectado (1 dialog element)
- Clique em botão "Ativar" dentro do modal — sucesso
- Após ativação, corpo continha: `"Operação ativadaResidencial Rerun QA****7000161AtivoCriado em 20/04/2026Ativado em 20/04/2026"`
- Status "Ativo" — confirmado
- Mensagem "Operação ativada" — exibida
- Badge "pré-ativo" — desapareceu

**Evidências:** `screenshots/rerun2_ct05_inicio.png`, `screenshots/rerun2_ct05_apos_primeiro_click.png`, `screenshots/rerun2_ct05_apos_ativacao.png`, `screenshots/rerun2_ct05_ct08_pass.png`

---

### CT-06 — Banco: status atualizado após go-live PASS (rerun2 anterior)

**Query executada:**
```sql
SELECT status, activated_at FROM condominio WHERE id = '4a3d87ea-f62f-4d9c-80de-a34237d0dae3'
```

**Expected:** status=2, activated_at preenchido

**Actual:** `2|2026-04-20 16:55:45.05646+00`

Ambas as condições satisfeitas.

**Evidências:** `requests.log` — DB VALIDATION CT-06

---

### CT-07 — Auditoria da ativação PASS (rerun2 anterior)

**Query executada:**
```sql
SELECT event_kind, performed_by_user_id, occurred_at
FROM tenant_audit_log
WHERE tenant_id='4a3d87ea-f62f-4d9c-80de-a34237d0dae3'
ORDER BY id
```

**Expected:** event_kind=1 (criação) e event_kind=2 (ativação)

**Actual:**
```
1|0dcbb805-e21e-4db3-a196-e6e456b3ea2d|2026-04-20 16:33:34.472521+00
2|0dcbb805-e21e-4db3-a196-e6e456b3ea2d|2026-04-20 16:55:45.05646+00
```

event_kind=1 (Criação) e event_kind=2 (Ativação) — ambos presentes e com performed_by_user_id preenchido.

**Evidências:** `requests.log` — DB VALIDATION CT-07

---

### CT-08 — Botão "Ativar operação" desaparece após ativação PASS (rerun2 anterior)

**Expected:** Botão não visível após ativação

**Actual:** `activateBtn count after activation=0` — botão ausente confirmado.

**Evidências:** `screenshots/rerun2_ct05_ct08_pass.png`, `requests.log`

---

## Status das Correções (BUG-1 a BUG-7)

| Bug | Descrição | Status |
|-----|-----------|--------|
| BUG-1 | `details.cnpj` -> `details.cnpjMasked` | CORRIGIDO — verificado |
| BUG-2 | `optIn.signatarioCpf` -> `optIn.signatarioCpfMasked` | CORRIGIDO — verificado |
| BUG-3 | `sindico.celularE164` -> `sindico.celularMasked` | CORRIGIDO — verificado |
| BUG-4 | `sindico.passwordDefined` -> `details.sindicoSenhaDefinida` | CORRIGIDO — verificado |
| BUG-5 | `AuditEntry.performedByEmail` -> `AuditEntry.performedByUserId` | CORRIGIDO — verificado |
| BUG-6 | `details.optInDocuments` -> `details.documentos` | CORRIGIDO — verificado |
| BUG-7 | Off-by-one-day em formatDate() para strings YYYY-MM-DD | CORRIGIDO — verificado (rerun3) |

---

## Evidências

```
qa_task_04_painel_detalhes_golive/
├── test_plan.md
├── qa_report_task_04.md (este arquivo)
├── requests.log                              — inclui DB validations CT-06, CT-07 e log rerun3 CT-02
└── screenshots/
    ├── rerun2_ct01_inicio.png                — CT-01: Página renderizando corretamente
    ├── rerun2_ct01_pass.png                  — CT-01 PASS
    ├── rerun2_ct02_inicio.png                — CT-02 rerun2: data 14/03/2026 visível (FAIL anterior)
    ├── rerun2_ct03_inicio.png                — CT-03 dados síndico
    ├── rerun2_ct03_pass.png                  — CT-03 PASS
    ├── rerun2_ct04_apos_click.png            — CT-04: Modal de confirmação visível
    ├── rerun2_ct04_dialog_pass.png           — CT-04 PASS
    ├── rerun2_ct04_inicio.png                — CT-04 início
    ├── rerun2_ct04_pre_click.png             — Botão "Ativar operação" presente
    ├── rerun2_ct05_apos_ativacao.png         — Status Ativo + "Operação ativada"
    ├── rerun2_ct05_apos_primeiro_click.png   — Dialog visível
    ├── rerun2_ct05_ct08_pass.png             — CT-05 + CT-08 PASS
    ├── rerun2_ct05_inicio.png                — CT-05 início (status pré-ativo)
    ├── rerun3_ct02_inicio.png                — CT-02 rerun3: início da navegação
    ├── rerun3_ct02_pre_assert.png            — CT-02 rerun3: pré-assertion
    └── rerun3_ct02_pass.png                  — CT-02 rerun3: DATA DA ASSEMBLEIA 15/03/2026 visível (PASS)
```

---

## Status para o Orquestrador

**Status:** PASS

**CT-02 — rerun3:** PASS. A correção de BUG-7 em `formatDate()` está funcionando. A UI exibe `15/03/2026` para `dataAssembleia` e `dataTermo` do tenant `4a3d87ea`. A data incorreta `14/03/2026` não aparece em nenhum campo.

**Tenant ativado:** SIM. O tenant `4a3d87ea-f62f-4d9c-80de-a34237d0dae3` está em status=2 (Ativo), activated_at=`2026-04-20 16:55:45+00`.

**Todos os CTs:** PASS (CT-01 a CT-08).

**Tasks impactadas:** Nenhuma — todos os bugs identificados estão corrigidos e verificados.
