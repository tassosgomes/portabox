# Plano de Testes — Audit Trail (qa_task_10)

**Task ID:** qa_task_10
**Tipos:** API + Banco (DB)
**Data:** 2026-04-20

## Contexto

- Audit table real: `tenant_audit_log` (coluna `performed_by_user_id`, NAO `performed_by`)
- Outbox table real: `domain_event_outbox` (coluna `event_type`, `aggregate_id`, `payload`)
- Tenant A ID (= condominio A ID): `4cce551d-4f18-474b-a42a-2deb6c2a0451`
- Sindico A userId: `9ae7217c-7c68-43ba-b663-63bb9f235d97`
- Tenant B ID: `23fb219d-460a-4eee-a9e7-308d7665350b`
- Auth: cookie session (`portabox.auth`), login via POST /api/v1/auth/login

## Mapeamento event_kind

| event_kind | Nome | Operacao |
|---|---|---|
| 5 | BlocoCriado | POST /blocos |
| 6 | BlocoRenomeado | PATCH /blocos/{id} |
| 7 | BlocoInativado | POST /blocos/{id}:inativar |
| 8 | BlocoReativado | POST /blocos/{id}:reativar |
| 9 | UnidadeCriada | POST .../unidades |
| 10 | UnidadeInativada | POST .../unidades/{id}:inativar |
| 11 | UnidadeReativada | POST .../unidades/{id}:reativar |

## Casos de Teste

### CT-01: BlocoCriado — event_kind=5
- **Pre-condicao:** Sindico A autenticado
- **Passos:** POST /api/v1/condominios/{condominioId}/blocos com {"nome":"Audit Bloco QA"}; capturar timestamp antes
- **Expected:** 1 entry em tenant_audit_log com event_kind=5, tenant_id correto, performed_by_user_id=sindicoId, metadata_json com blocoId e nome
- **Tipo:** API + Banco

### CT-02: BlocoRenomeado — event_kind=6
- **Pre-condicao:** Bloco criado em CT-01
- **Passos:** PATCH /api/v1/condominios/{condominioId}/blocos/{blocoId} com {"nome":"Audit Bloco QA Renomeado"}
- **Expected:** 1 nova entry event_kind=6, metadata_json com nomeAntes e nomeDepois
- **Tipo:** API + Banco

### CT-03: BlocoInativado — event_kind=7
- **Pre-condicao:** Bloco renomeado em CT-02
- **Passos:** POST /api/v1/condominios/{condominioId}/blocos/{blocoId}:inativar
- **Expected:** 1 nova entry event_kind=7, metadata_json com blocoId/nome
- **Tipo:** API + Banco

### CT-04: BlocoReativado — event_kind=8
- **Pre-condicao:** Bloco inativado em CT-03
- **Passos:** POST /api/v1/condominios/{condominioId}/blocos/{blocoId}:reativar
- **Expected:** 1 nova entry event_kind=8
- **Tipo:** API + Banco

### CT-05: UnidadeCriada — event_kind=9
- **Pre-condicao:** Bloco ativo existente (criar novo bloco para isolamento)
- **Passos:** POST /api/v1/condominios/{condominioId}/blocos/{blocoId}/unidades com {"andar":10,"numero":"1001"}
- **Expected:** 1 nova entry event_kind=9, metadata_json com unidadeId, blocoId, andar, numero
- **Tipo:** API + Banco

### CT-06: UnidadeInativada — event_kind=10
- **Pre-condicao:** Unidade criada em CT-05
- **Passos:** POST .../unidades/{unidadeId}:inativar
- **Expected:** 1 nova entry event_kind=10
- **Tipo:** API + Banco

### CT-07: UnidadeReativada — event_kind=11
- **Pre-condicao:** Unidade inativada em CT-06
- **Passos:** POST .../unidades/{unidadeId}:reativar
- **Expected:** 1 nova entry event_kind=11
- **Tipo:** API + Banco

### CT-08: Falhas NAO geram audit entry
- **Pre-condicao:** Sindico A autenticado
- **Passos:**
  1. POST bloco com nome vazio → esperado 400; 0 novas entries
  2. Sindico A tenta operar em tenant B → esperado 403/404; 0 novas entries
  3. POST bloco com nome duplicado ativo → esperado 409; 0 novas entries
- **Expected:** Nenhuma nova entry nos 3 casos
- **Tipo:** API + Banco

### CT-09: Contagem global de audit entries vs operacoes
- **Passos:** SELECT event_kind, count(*) da tabela; comparar com totais historicos das tasks 01-07
- **Expected:** Contagens coerentes (documentar diferencas)
- **Tipo:** Banco

### CT-10: Metadata JSON schema consistente
- **Passos:** SELECT event_kind, metadata_json para amostras de cada kind
- **Expected:** Schema consistente por kind; documentar shape real
- **Tipo:** Banco

### CT-11: Outbox de eventos de dominio
- **Passos:** Apos cada operacao CT-01 a CT-07, verificar entrada em domain_event_outbox
- **Expected:** 1 entrada por operacao com event_type correto
- **Tipo:** Banco

### CT-12: Outbox vs Audit consistency
- **Passos:** Comparar contagens de outbox vs audit_log por tipo de operacao de F02
- **Expected:** Contagens aproximadamente iguais
- **Tipo:** Banco

### CT-13: Timestamps em UTC
- **Passos:** SELECT occurred_at, pg_typeof(occurred_at) FROM tenant_audit_log LIMIT 3
- **Expected:** Tipo timestamptz; valores em UTC sem drift absurdo
- **Tipo:** Banco
