# Metadata JSON Schema Real — tenant_audit_log

Coletado em 2026-04-21T02:01:49Z via SELECT nas entradas de tenant A.

## event_kind=5 — BlocoCriado

**Schema observado:**
```json
{
  "nome": "string (nome do bloco)",
  "blocoId": "uuid"
}
```

**Exemplos:**
```json
{"nome": "Audit Bloco QA", "blocoId": "52ac8a90-b077-464a-b14d-4dbd47dae6dd"}
{"nome": "Audit Bloco Unidades QA", "blocoId": "0260f7aa-8148-40e7-9280-7fd6ca93e4ba"}
{"nome": "Bloco QA-01", "blocoId": "88037273-d560-4415-a1e2-b45a00dc5be4"}
```

**Consistencia:** 100% — todos os 16 entries (tenants A+B) seguem este shape exato.

---

## event_kind=6 — BlocoRenomeado

**Schema observado:**
```json
{
  "blocoId": "uuid",
  "nomeAntes": "string",
  "nomeDepois": "string"
}
```

**Exemplos:**
```json
{"blocoId": "52ac8a90-...", "nomeAntes": "Audit Bloco QA", "nomeDepois": "Audit Bloco QA Renomeado"}
{"blocoId": "9aab2b47-...", "nomeAntes": "Bloco UI-QA-01", "nomeDepois": "Bloco UI-QA-01 Editado"}
```

**Consistencia:** 100% — todos os 5 entries seguem este shape com diff completo.

---

## event_kind=7 — BlocoInativado

**Schema observado:**
```json
{
  "nome": "string (nome atual do bloco no momento da inativacao)",
  "blocoId": "uuid"
}
```

**Exemplos:**
```json
{"nome": "Audit Bloco QA Renomeado", "blocoId": "52ac8a90-b077-464a-b14d-4dbd47dae6dd"}
{"nome": "Bloco Temp Pai Inativo QA", "blocoId": "bb643a2a-8e4c-49c6-b76f-c98ad440a79e"}
```

**Consistencia:** 100% — identico ao shape de BlocoCriado (nome + blocoId).

---

## event_kind=8 — BlocoReativado

**Schema observado:**
```json
{
  "nome": "string (nome atual do bloco no momento da reativacao)",
  "blocoId": "uuid"
}
```

**Exemplos:**
```json
{"nome": "Audit Bloco QA Renomeado", "blocoId": "52ac8a90-b077-464a-b14d-4dbd47dae6dd"}
{"nome": "Bloco QA-03", "blocoId": "ff2ed42b-e9d9-426e-81fa-6bd51b767174"}
```

**Consistencia:** 100% — mesmo shape de BlocoInativado.

---

## event_kind=9 — UnidadeCriada

**Schema observado:**
```json
{
  "andar": "int",
  "numero": "string",
  "blocoId": "uuid",
  "unidadeId": "uuid"
}
```

**Exemplos:**
```json
{"andar": 10, "numero": "1001", "blocoId": "0260f7aa-...", "unidadeId": "1bae4c0d-..."}
{"andar": 99, "numero": "101A", "blocoId": "88037273-...", "unidadeId": "c0f931cf-..."}
{"andar": 1, "numero": "101", "blocoId": "bb643a2a-...", "unidadeId": "a2c82b48-..."}
```

**Consistencia:** 100% — todos os 26 entries seguem este shape. Inclui blocoId e unidadeId como esperado.

---

## event_kind=10 — UnidadeInativada

**Schema observado:**
```json
{
  "andar": "int",
  "numero": "string",
  "blocoId": "uuid",
  "unidadeId": "uuid"
}
```

**Nota:** Shape identico ao UnidadeCriada. Nao ha campo de "motivo" ou "inativadoPor" separado (esse dado esta em `performed_by_user_id`).

---

## event_kind=11 — UnidadeReativada

**Schema observado:**
```json
{
  "andar": "int",
  "numero": "string",
  "blocoId": "uuid",
  "unidadeId": "uuid"
}
```

**Nota:** Shape identico ao UnidadeInativada.

---

## Observacoes vs TechSpec

A TechSpec descreve risco de "Drift entre schema de MetadataJson e consumidores futuros". Nao foi detectado drift nos dados existentes — todos os schemas sao consistentes internamente.

- Para BlocoRenomeado: a techspec menciona `{nomeAntes, nomeDepois}` — CONFIRMADO, com adicional `blocoId` (nao mencionado na spec, mas util).
- Para BlocoInativado/Reativado: techspec menciona `{nome, blocoId}` — CONFIRMADO.
- Para UnidadeCriada: techspec menciona `{andar, numero, blocoId, unidadeId}` — CONFIRMADO.
- Para Unidade{Inativada/Reativada}: nao especificado detalhadamente; shape consistente com UnidadeCriada.
