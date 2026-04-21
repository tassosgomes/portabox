# Audit vs Outbox — Comparacao de Contagens F02

**Coletado em:** 2026-04-21T02:01:49Z
**Tenants incluidos:** Tenant A + Tenant B

## Tabela Comparativa

| Event Kind (Audit) | Nome | Audit Count | Outbox Event Type | Outbox Count | Match? |
|---|---|---|---|---|---|
| 5 | BlocoCriado | 16 | bloco.criado.v1 | 16 | EXATO |
| 6 | BlocoRenomeado | 5 | bloco.renomeado.v1 | 5 | EXATO |
| 7 | BlocoInativado | 9 | bloco.inativado.v1 | 9 | EXATO |
| 8 | BlocoReativado | 6 | bloco.reativado.v1 | 6 | EXATO |
| 9 | UnidadeCriada | 26 | unidade.criada.v1 | 26 | EXATO |
| 10 | UnidadeInativada | 5 | unidade.inativada.v1 | 5 | EXATO |
| 11 | UnidadeReativada | 4 | unidade.reativada.v1 | 4 | EXATO |

**Resultado: 7/7 event kinds com contagem exatamente igual entre audit e outbox.**

## Contagens Adicionais de Entidades

| Entidade | Tenant A | Tenant B | Total |
|---|---|---|---|
| Blocos (todos, incluindo inativos) | 13 | 3 | 16 |
| Unidades (todas, incluindo inativas) | 24 | 2 | 26 |

**Observacao:** O numero total de blocos no banco (16) corresponde exatamente ao numero de entries `event_kind=5` e ao numero de entradas `bloco.criado.v1` no outbox — confirmando que cada bloco criado gerou exatamente 1 audit entry e 1 outbox event.

Idem para unidades (26 blocos = 26 entries kind=9 = 26 outbox `unidade.criada.v1`).

## Published_at

Verificacao adicional: todos os 7 outbox events dos CT-01 a CT-07 desta sessao de QA possuem `published_at` nao-nulo, confirmando que o dispatcher in-process executou apos commit.

| CT | Outbox Event | Created_at | Published_at | Lag |
|---|---|---|---|---|
| CT-01 | bloco.criado.v1 | 2026-04-21 01:57:49.963 | 2026-04-21 01:57:55.441 | ~5.5s |
| CT-02 | bloco.renomeado.v1 | 2026-04-21 01:58:10.346 | 2026-04-21 01:58:10.451 | ~0.1s |
| CT-03 | bloco.inativado.v1 | 2026-04-21 01:58:27.114 | 2026-04-21 01:58:40.254 | ~13s |
| CT-04 | bloco.reativado.v1 | 2026-04-21 01:58:43.443 | 2026-04-21 01:58:55.078 | ~11.6s |
| CT-05 | unidade.criada.v1 | 2026-04-21 01:59:09.274 | 2026-04-21 01:59:10.082 | ~0.8s |
| CT-06 | unidade.inativada.v1 | 2026-04-21 01:59:28.580 | 2026-04-21 01:59:39.941 | ~11.4s |
| CT-07 | unidade.reativada.v1 | 2026-04-21 01:59:45.856 | 2026-04-21 01:59:54.829 | ~9s |

Nota: O lag variavel (0.1s a 13s) sugere que o dispatcher roda em background poll com intervalo; nao e sinal de falha.
