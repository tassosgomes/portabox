# QA Report — qa_task_06: Log de Auditoria

**Task ID:** qa_task_06
**Data/Hora (relatorio final):** 2026-04-20T17:05:00Z
**Status Geral:** PARTIAL (TC-04 corrigido para PASS na re-execucao)

---

## Contexto

- **User Story:** CF6 — Log de auditoria das transicoes do tenant (criacao e ativacao)
- **Ambiente:** Backend http://localhost:5272 | Frontend http://localhost:5173
- **Tipos de teste:** Banco, API, UI
- **Autenticacao:** Sim (cookie-based, portabox.auth HttpOnly)

---

## Casos de Teste

| ID    | Descricao                                                  | Tipo  | Status                    |
|-------|------------------------------------------------------------|-------|---------------------------|
| TC-01 | Entrada de criacao (event_kind=1) no banco                 | Banco | PASS (execucao original)  |
| TC-02 | Entrada de ativacao (event_kind=2) no banco                | Banco | PASS (execucao original)  |
| TC-03 | Endpoint de API retorna audit log                          | API   | PARTIAL (execucao original — sem endpoint dedicado) |
| TC-04 | Log de auditoria visivel no painel de detalhes (UI)        | UI    | PASS (re-execucao 2026-04-20) |
| TC-05 | Imutabilidade da tabela (sem colunas de update/delete)     | Banco | PARTIAL (execucao original — sem garantia de imutabilidade no banco) |

---

## Detalhes por Caso

### TC-01 — Entrada de criacao (event_kind=1) no banco — PASS (execucao original)

**Expected:** Registro com event_kind=1, operador preenchido, timestamp preenchido

**Actual:**
```
id=1 | event_kind=1 | occurred_at=2026-04-19 01:13:12.70591+00 | note=(vazio) | performed_by_user_id=0dcbb805-e21e-4db3-a196-e6e456b3ea2d
```

Usuario confirmado: operator@portabox.dev (JOIN com asp_net_users)

**Evidencias:** `requests.log` linhas 88-96

---

### TC-02 — Entrada de ativacao (event_kind=2) no banco — PASS (execucao original)

**Expected:** Registro com event_kind=2, operador preenchido, timestamp preenchido, note com descricao da ativacao

**Actual:**
```
id=3 | event_kind=2 | occurred_at=2026-04-19 01:22:51.638297+00 | note=Ativacao via QA test runner | performed_by_user_id=0dcbb805-e21e-4db3-a196-e6e456b3ea2d
```

Operador confirmado: operator@portabox.dev

**Evidencias:** `requests.log` linhas 97-103

**Observacao:** Existe tambem um registro id=4 com event_kind=3 (ocorrido em 2026-04-19 01:28:07). Este event_kind nao estava documentado nos requisitos testados.

---

### TC-03 — Endpoint de API para audit log — PARTIAL (execucao original)

**Expected:** Endpoint dedicado de audit log retornando lista de entradas

**Actual:** Nao ha endpoint dedicado de audit log. O log e retornado embutido no payload do endpoint de detalhes do condominio, no campo `auditLog`:

```json
"auditLog": [
  { "id": 4, "eventKind": 3, "performedByUserId": "0dcbb805-...", "occurredAt": "2026-04-19T01:28:07...", "note": null },
  { "id": 3, "eventKind": 2, "performedByUserId": "0dcbb805-...", "occurredAt": "2026-04-19T01:22:51...", "note": "Ativacao via QA test runner" },
  { "id": 1, "eventKind": 1, "performedByUserId": "0dcbb805-...", "occurredAt": "2026-04-19T01:13:12...", "note": null }
]
```

**Avaliacao:** O audit log e acessivel via API (embutido no endpoint de detalhes). Os dados estao corretos. Nao ha endpoint dedicado e `performedByEmail` nao e retornado — apenas o UUID do usuario.

**Evidencias:** `requests.log` linhas 1-86

---

### TC-04 — Log de auditoria visivel no painel de detalhes (UI) — PASS (re-execucao 2026-04-20)

**Tenant testado:** Residencial Rerun QA — ID: 4a3d87ea-f62f-4d9c-80de-a34237d0dae3 (Status: Ativo)

**Passos executados:**
1. Login via POST /api/v1/auth/login — Status 200, cookie portabox.auth obtido
2. Navegou para http://localhost:5173/condominios/4a3d87ea-f62f-4d9c-80de-a34237d0dae3
3. Aguardou 4 segundos pelo carregamento completo
4. Verificou ausencia de tela em branco e de erros JavaScript
5. Localizou heading "Historico de auditoria" (h3) e lista (aria-label="Historico de auditoria")
6. Contou e leu os itens da lista

**Expected:**
- Secao "Historico de auditoria" visivel na pagina
- Ao menos dois registros: evento de criacao e evento de ativacao
- Entradas mostram tipo do evento (Criado/Ativado), performedByUserId, data/hora
- Pagina nao exibe tela branca nem erros

**Actual:**
- Pagina carregou completamente (body text: 1063 chars, sem erros JavaScript)
- Heading "Historico de auditoria" visivel: SIM
- Lista de auditoria (aria-label) visivel: SIM
- Registros encontrados: 2

```
Item 1: Ativado | 0dcbb805-e21e-4db3-a196-e6e456b3ea2d · 20/04/2026, 13:55
Item 2: Criado  | 0dcbb805-e21e-4db3-a196-e6e456b3ea2d · 20/04/2026, 13:33
```

- Texto "Criado" na pagina: PRESENTE
- Texto "Ativado" na pagina: PRESENTE
- Erros JavaScript (page errors): NENHUM
- Console do browser: apenas mensagens de inicializacao normais (vite, React DevTools)

**Contexto da correcao:** Na execucao original (2026-04-19), a pagina crashava com `Cannot read properties of undefined (reading 'replace')` em `DetalhesCondominioPage.tsx:83` — mismatch entre nomes de campo da API (cnpjMasked, signatarioCpfMasked) e o que o frontend tentava acessar. O bug foi corrigido antes desta re-execucao.

**Evidencias:**
- Screenshot inicio: `screenshots/rerun_tc04_inicio.png`
- Screenshot pos-carga: `screenshots/rerun_tc04_pos_carga.png`
- Screenshot secao auditoria: `screenshots/rerun_tc04_audit_section.png`
- Screenshot final: `screenshots/rerun_tc04_final.png`
- Log: `requests.log` (bloco TC-04 RERUN, ultimo bloco do arquivo)

---

### TC-05 — Imutabilidade da tabela (estrutura append-only) — PARTIAL (execucao original)

**Expected:** Sem colunas de mutabilidade; tabela de estrutura append-only garantida por banco

**Actual:**

Colunas da tabela `tenant_audit_log`: `id | tenant_id | event_kind | performed_by_user_id | occurred_at | note | metadata_json`

Colunas de mutabilidade encontradas: NENHUMA

Triggers customizados: NENHUM (apenas RI_ConstraintTrigger de FK automaticos)

Regras de banco: NENHUMA

Constraints: apenas PRIMARY KEY e FOREIGN KEYs (sem CHECK impedindo UPDATE/DELETE)

**Avaliacao:** Sem mecanismo de banco (trigger, rule ou RLS) que impeca UPDATE/DELETE. Imutabilidade depende exclusivamente da logica da aplicacao.

**Evidencias:** `requests.log` linhas 104-114

---

## Problemas Encontrados

### Problema 1 — TC-03: Sem endpoint dedicado de audit log

**Comportamento observado:** Nao existe endpoint `/api/v1/admin/condominios/{id}/audit-log`. O audit log e retornado como campo embutido no endpoint de detalhes.

**Impacto:** Nao e possivel paginar ou filtrar o audit log independentemente dos dados do tenant.

---

### Problema 2 — TC-05: Imutabilidade sem garantia em banco

**Comportamento observado:** A tabela nao possui triggers, rules ou RLS que bloqueiem UPDATE/DELETE diretamente no banco.

**Impacto:** Um acesso direto ao banco (fora da aplicacao) poderia alterar ou excluir registros de auditoria.

---

### Observacao — event_kind=3 nao documentado

O banco contem um registro (id=4, event_kind=3, occurred_at=2026-04-19T01:28:07) gerado durante execucao da task_04.

---

## Resumo de Evidencias

```
qa_task_06_log_auditoria/
├── test_plan.md
├── requests.log
├── qa_report_task_06.md
└── screenshots/
    ├── tc04_fail_browser_crash.png         (execucao original — crash)
    ├── tc04_page_load.png                  (execucao original — tela em branco)
    ├── rerun_tc04_inicio.png               (re-execucao — inicio da navegacao)
    ├── rerun_tc04_pos_carga.png            (re-execucao — apos carregamento)
    ├── rerun_tc04_audit_section.png        (re-execucao — secao de auditoria visivel)
    └── rerun_tc04_final.png               (re-execucao — estado final PASS)
```

---

## Status para o Orquestrador

**Status:** PARTIAL

**Resumo:**
- TC-01 (Banco criacao): PASS — entrada event_kind=1 confirmada com operador e timestamp corretos
- TC-02 (Banco ativacao): PASS — entrada event_kind=2 confirmada com operador, timestamp e nota corretos
- TC-03 (API): PARTIAL — audit log acessivel via campo embutido no endpoint de detalhes; sem endpoint dedicado; sem nome do operador (apenas UUID)
- TC-04 (UI): PASS — secao "Historico de auditoria" visivel, 2 registros exibidos (Ativado + Criado), sem erros JavaScript
- TC-05 (Imutabilidade): PARTIAL — sem colunas de mutabilidade, mas sem trigger/rule de banco garantindo append-only

**Classificacao PARTIAL mantida por:**
TC-03 sem endpoint dedicado e TC-05 sem garantia de imutabilidade em nivel de banco. TC-04 passou apos correcao do crash no DetalhesCondominioPage.

**Tasks possivelmente impactadas:** Nenhuma dependente de qa_task_06 identificada no qa_session.json.
