# QA Report — Audit Trail (qa_task_10)

**Task ID:** qa_task_10
**Data/Hora:** 2026-04-21T02:05:00Z
**Status Geral:** PASS

---

## Contexto

- **User Story:** Validacao de audit trail completo para cada operacao de escrita em F02 (blocos e unidades)
- **PRD Goal:** "Auditabilidade total"; Success Metric: "100% das operacoes de escrita com audit trail completo"
- **Ambiente:** http://localhost:5272
- **Tipos de teste:** API + Banco (DB)
- **Autenticacao:** Sim — cookie session ASP.NET Identity

### Descobertas de Setup

- Tabela de audit real: `tenant_audit_log` (coluna `performed_by_user_id`, NAO `performed_by` como sugere a spec de nomenclatura)
- Tabela de outbox real: `domain_event_outbox`
- Tenant A ID = Condominio A ID: `4cce551d-4f18-474b-a42a-2deb6c2a0451`
- Sindico A userId: `9ae7217c-7c68-43ba-b663-63bb9f235d97`
- Auth endpoint real: `/api/v1/auth/login` (NAO `/auth/login` como documentado na session)

---

## Casos de Teste

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | BlocoCriado — event_kind=5 | API + Banco | PASS |
| CT-02 | BlocoRenomeado — event_kind=6 | API + Banco | PASS |
| CT-03 | BlocoInativado — event_kind=7 | API + Banco | PASS |
| CT-04 | BlocoReativado — event_kind=8 | API + Banco | PASS |
| CT-05 | UnidadeCriada — event_kind=9 | API + Banco | PASS |
| CT-06 | UnidadeInativada — event_kind=10 | API + Banco | PASS |
| CT-07 | UnidadeReativada — event_kind=11 | API + Banco | PASS |
| CT-08 | Falhas NAO geram audit entries (400, 403, 409) | API + Banco | PASS |
| CT-09 | Contagem global de audit entries vs operacoes | Banco | PASS |
| CT-10 | Metadata JSON schema consistente | Banco | PASS |
| CT-11 | Outbox de eventos de dominio | Banco | PASS |
| CT-12 | Outbox vs Audit consistency | Banco | PASS |
| CT-13 | Timestamps em UTC (timestamptz) | Banco | PASS |

**Total: 13/13 PASS**

---

## Detalhes por Caso

### CT-01 — BlocoCriado (event_kind=5) PASS

**Operacao:** POST /api/v1/condominios/4cce551d-.../blocos com `{"nome":"Audit Bloco QA"}`
**Expected:** HTTP 201 + exatamente 1 audit entry com event_kind=5, tenant_id correto, performed_by_user_id=sindicoId, metadata com blocoId e nome
**Actual:** HTTP 201; 1 entry criada:
```
id=74 | event_kind=5 | tenant_id=4cce551d-4f18-474b-a42a-2deb6c2a0451
performed_by_user_id=9ae7217c-7c68-43ba-b663-63bb9f235d97
occurred_at=2026-04-21 01:57:49.962912+00
metadata_json={"nome": "Audit Bloco QA", "blocoId": "52ac8a90-b077-464a-b14d-4dbd47dae6dd"}
```
**Evidencias:** `db_check.log` (CT-01)

---

### CT-02 — BlocoRenomeado (event_kind=6) PASS

**Operacao:** PATCH .../blocos/52ac8a90-... com `{"nome":"Audit Bloco QA Renomeado"}`
**Expected:** HTTP 200 + 1 audit entry event_kind=6 com nomeAntes e nomeDepois no metadata
**Actual:** HTTP 200; 1 entry:
```
id=75 | event_kind=6
metadata_json={"blocoId": "52ac8a90-...", "nomeAntes": "Audit Bloco QA", "nomeDepois": "Audit Bloco QA Renomeado"}
```
**Evidencias:** `db_check.log` (CT-02)

---

### CT-03 — BlocoInativado (event_kind=7) PASS

**Operacao:** POST .../blocos/52ac8a90-...:inativar
**Expected:** HTTP 200 + 1 audit entry event_kind=7 com metadata {nome, blocoId}
**Actual:** HTTP 200; 1 entry:
```
id=76 | event_kind=7
metadata_json={"nome": "Audit Bloco QA Renomeado", "blocoId": "52ac8a90-..."}
```
**Evidencias:** `db_check.log` (CT-03)

---

### CT-04 — BlocoReativado (event_kind=8) PASS

**Operacao:** POST .../blocos/52ac8a90-...:reativar
**Expected:** HTTP 200 + 1 audit entry event_kind=8
**Actual:** HTTP 200; 1 entry:
```
id=77 | event_kind=8
metadata_json={"nome": "Audit Bloco QA Renomeado", "blocoId": "52ac8a90-..."}
```
**Evidencias:** `db_check.log` (CT-04)

---

### CT-05 — UnidadeCriada (event_kind=9) PASS

**Operacao:** POST .../blocos/0260f7aa-.../unidades com `{"andar":10,"numero":"1001"}`
**Expected:** HTTP 201 + 1 audit entry event_kind=9 com metadata {andar, numero, blocoId, unidadeId}
**Actual:** HTTP 201; 2 entries no periodo (1 para o bloco auxiliar criado + 1 para a unidade):
```
id=78 | event_kind=5 (bloco auxiliar)
id=79 | event_kind=9
  metadata_json={"andar": 10, "numero": "1001", "blocoId": "0260f7aa-...", "unidadeId": "1bae4c0d-..."}
```
Exatamente 1 entry event_kind=9 — CORRETO.
**Evidencias:** `db_check.log` (CT-05)

---

### CT-06 — UnidadeInativada (event_kind=10) PASS

**Operacao:** POST .../unidades/1bae4c0d-...:inativar
**Expected:** HTTP 200 + 1 audit entry event_kind=10
**Actual:** HTTP 200; 1 entry:
```
id=80 | event_kind=10
metadata_json={"andar": 10, "numero": "1001", "blocoId": "0260f7aa-...", "unidadeId": "1bae4c0d-..."}
```
**Evidencias:** `db_check.log` (CT-06)

---

### CT-07 — UnidadeReativada (event_kind=11) PASS

**Operacao:** POST .../unidades/1bae4c0d-...:reativar
**Expected:** HTTP 200 + 1 audit entry event_kind=11
**Actual:** HTTP 200; 1 entry:
```
id=81 | event_kind=11
metadata_json={"andar": 10, "numero": "1001", "blocoId": "0260f7aa-...", "unidadeId": "1bae4c0d-..."}
```
**Evidencias:** `db_check.log` (CT-07)

---

### CT-08 — Falhas NAO geram audit entries PASS

**CT-08a: POST bloco nome vazio → 400**
- Actual HTTP: 400 com ProblemDetails (validation-error)
- Novas audit entries: 0
- PASS

**CT-08b: Sindico A → Tenant B → 403**
- Actual HTTP: 403 com ProblemDetails (forbidden)
- Novas audit entries de sindicoA: 0
- PASS

**CT-08c: POST bloco nome duplicado ativo → 409**
- Actual HTTP: 409 com ProblemDetails (canonical-conflict)
- Novas audit entries: 0
- PASS

---

### CT-09 — Contagem global PASS

**Contagens audit (tenant A + B, event_kinds 1-11):**
```
event_kind 1 (F01): 2
event_kind 5: 16
event_kind 6: 5
event_kind 7: 9
event_kind 8: 6
event_kind 9: 26
event_kind 10: 5
event_kind 11: 4
```

**Entidades no banco:**
- Blocos A: 13 | Blocos B: 3 = 16 total — bate com event_kind=5 count
- Unidades A: 24 | Unidades B: 2 = 26 total — bate com event_kind=9 count

Interpretacao: cada bloco criado em qualquer tenant gerou exatamente 1 BlocoCriado entry. Idem para unidades. As contagens de inativacao/reativacao sao menores porque nem todos os recursos foram inativados/reativados.

**Evidencias:** `db_check.log` (CT-09)

---

### CT-10 — Metadata JSON schema consistente PASS

Schema real documentado em `metadata_schema.md`. Resumo:

| Event Kind | Campos Presentes | Conformidade com TechSpec |
|---|---|---|
| 5 BlocoCriado | nome, blocoId | CONFORME + extra blocoId (nao especificado mas util) |
| 6 BlocoRenomeado | blocoId, nomeAntes, nomeDepois | CONFORME |
| 7 BlocoInativado | nome, blocoId | CONFORME |
| 8 BlocoReativado | nome, blocoId | CONFORME |
| 9 UnidadeCriada | andar, numero, blocoId, unidadeId | CONFORME |
| 10 UnidadeInativada | andar, numero, blocoId, unidadeId | CONFORME (shape igual ao UnidadeCriada) |
| 11 UnidadeReativada | andar, numero, blocoId, unidadeId | CONFORME |

100% de consistencia interna para todos os event kinds — sem drift detectado.

**Evidencias:** `metadata_schema.md`, `db_check.log` (CT-10)

---

### CT-11 — Outbox de eventos de dominio PASS

Tabela `domain_event_outbox` verificada. Todos os 7 tipos de evento de F02 possuem entradas:

| event_type | count |
|---|---|
| bloco.criado.v1 | 16 |
| bloco.renomeado.v1 | 5 |
| bloco.inativado.v1 | 9 |
| bloco.reativado.v1 | 6 |
| unidade.criada.v1 | 26 |
| unidade.inativada.v1 | 5 |
| unidade.reativada.v1 | 4 |

Bloco CT-01 (52ac8a90): 4 entradas outbox com `published_at` nao-nulo — criado, renomeado, inativado, reativado.
Unidade CT-05 (1bae4c0d): 3 entradas outbox — criada, inativada, reativada.

**Evidencias:** `db_check.log` (CT-11), `audit_vs_outbox.md`

---

### CT-12 — Outbox vs Audit consistency PASS

Para todos os 7 event kinds de F02: audit count == outbox count. Match perfeito.
Detalhes em `audit_vs_outbox.md`.

---

### CT-13 — Timestamps em UTC PASS

- Tipo da coluna `occurred_at`: `timestamp with time zone` (timestamptz) — CORRETO
- Valores armazenados com sufixo `+00` (UTC)
- Drift check: 2 entries de event_kind=5 criadas nesta sessao encontradas na janela "last 1 hour" — CORRETO
- Lag entre `created_at` e `published_at` no outbox: 0.1s a 13s (variavel, comportamento esperado de dispatcher com poll)

**Evidencias:** `db_check.log` (CT-13)

---

## Conformidade do Audit Trail

**Operacoes bem-sucedidas com audit entry:** 7/7 tipos de operacao = **100%**
**Falhas sem audit entry:** 3/3 casos = **100%**
**Outbox vs Audit match:** 7/7 = **100%**
**Metadata schema consistente:** 7/7 = **100%**
**Timestamps UTC:** CONFIRMADO

## Resumo de Evidencias

```
qa_task_10_audit_trail/
├── test_plan.md
├── db_check.log          (todas as queries e resultados)
├── metadata_schema.md    (schema real de cada metadata_json)
├── audit_vs_outbox.md    (comparativo de contagens)
├── qa_report_task_10.md  (este arquivo)
├── screenshots/          (vazio — testes de DB nao requerem screenshots)
└── videos/               (vazio — testes de DB nao requerem videos)
```

---

## Status para o Orquestrador

**Status:** PASS
**Motivo da falha:** N/A — todos os 13 casos passaram
**Conformidade audit trail:** 100% das operacoes de escrita de F02 geram exatamente 1 TenantAuditEntry com campos corretos + 1 DomainEventOutbox entry correspondente
**Tasks possivelmente impactadas:** Nenhuma — qa_task_10 e o ultimo passo do pipeline de QA de F02
