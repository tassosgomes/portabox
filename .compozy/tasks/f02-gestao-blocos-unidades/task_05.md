---
status: completed
title: Migration EF AddBlocoAndUnidade (DDL + FKs + partial unique indexes + enum update)
type: backend
complexity: medium
dependencies:
  - task_02
  - task_03
  - task_04
---

# Task 05: Migration EF AddBlocoAndUnidade (DDL + FKs + partial unique indexes + enum update)

## Overview
Materializa no banco de dados o schema resultante das tasks 03 e 04, criando as tabelas `bloco` e `unidade` com todas as constraints, FKs e partial unique indexes declarados em EF Configuration. Como F02 apenas estende o enum C# `EventKind` (sem mudança no schema da coluna `event_kind`), esta migration é focada nas duas tabelas novas.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST gerar migration via `dotnet ef migrations add AddBlocoAndUnidade --project src/PortaBox.Infrastructure --startup-project src/PortaBox.Api`
- MUST revisar SQL gerado e confirmar que ambas partial unique indexes saem como `CREATE UNIQUE INDEX ... WHERE ativo = true` — ajustar manualmente `migrationBuilder.Sql(...)` se EF não gerar corretamente
- MUST garantir FK `bloco.condominio_id → condominio.id ON DELETE RESTRICT`
- MUST garantir FK `unidade.bloco_id → bloco.id ON DELETE RESTRICT`
- MUST garantir FKs de `inativado_por` e `criado_por` para `aspnetusers.id` (ou equivalente na convenção de F01)
- MUST incluir CHECK constraint `andar >= 0` na tabela `unidade`
- MUST atualizar `DbContextModelSnapshot` gerado pela migration
- MUST adicionar testes de integração básicos em task_10 (cobertura total fica lá); nesta task, smoke de migration apenas
- SHOULD rodar migration contra Postgres local (docker-compose dev) e contra Testcontainers para confirmar que `dotnet ef database update` aplica sem erro e que rollback (`Down`) é limpo
</requirements>

## Subtasks
- [x] 05.1 Executar `dotnet ef migrations add AddBlocoAndUnidade` e revisar arquivos gerados
- [x] 05.2 Conferir que partial unique indexes são emitidos corretamente; ajustar via `migrationBuilder.Sql` se necessário
- [x] 05.3 Verificar que FKs (`ON DELETE RESTRICT`) estão presentes em bloco e unidade
- [x] 05.4 Executar `dotnet ef database update` em Postgres local; inspecionar via `\d bloco` e `\d unidade`
- [x] 05.5 Testar rollback `dotnet ef migrations remove` + `database update` para versão anterior; garantir que `Down` remove tabelas sem deixar lixo
- [x] 05.6 Smoke test em Testcontainers: rodar migrations do zero em DB novo e confirmar que fixture do F01 + F02 sobe sem erros

## Implementation Details
Ver TechSpec seção **Data Models** para schema detalhado. Partial unique indexes no Postgres:

```sql
CREATE UNIQUE INDEX idx_bloco_nome_ativo_unique
  ON bloco (tenant_id, condominio_id, nome)
  WHERE ativo = true;

CREATE UNIQUE INDEX idx_unidade_canonica_ativa
  ON unidade (tenant_id, bloco_id, andar, numero)
  WHERE ativo = true;
```

EF Core 8 emite isso via `HasIndex(...).HasFilter("ativo = true").IsUnique()`. Confirmar que o tradutor do provider Npgsql gera a cláusula `WHERE ativo = true` exata; caso emita `WHERE "Ativo" = TRUE` (case-sensitive), não é problema porque Postgres é case-insensitive em unquoted identifiers, mas idealmente usar `snake_case` literal.

Nenhuma alteração de schema para `tenant_audit_entry` — apenas o enum C# expande em task_02.

### Relevant Files
- `src/PortaBox.Infrastructure/Persistence/Migrations/20260418000000_AddBlocoAndUnidade.cs` — migration file (novo)
- `src/PortaBox.Infrastructure/Persistence/Migrations/20260418000000_AddBlocoAndUnidade.Designer.cs` — designer file (novo)
- `src/PortaBox.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs` — snapshot atualizado
- `docker-compose.dev.yml` — referência para Postgres local
- `tests/PortaBox.Api.IntegrationTests/Persistence/PostgresDatabaseFixture.cs` — fixture que roda migrations

### Dependent Files
- Todas as tasks 06–18 dependem do schema estar aplicado
- `BlocoRepository` e `UnidadeRepository` assumem que tabelas existem
- `tenants/f01-criacao-condominio/tasks/...` já rodou suas próprias migrations; F02 apenas acrescenta

### Related ADRs
- [ADR-007: Soft-Delete Padronizado](adrs/adr-007.md) — motiva partial unique indexes
- [ADR-002: Forma Canônica Estrita](adrs/adr-002.md) — CHECK constraint de andar ≥ 0

## Deliverables
- Migration `AddBlocoAndUnidade` commitada e aplicável (`Up`) + reversível (`Down`)
- SQL gerado cobrindo partial unique indexes exatamente como no TechSpec
- Snapshot atualizado
- Smoke test via Testcontainers confirmando que pipeline de migrations roda do zero sem erros
- Unit tests with 80%+ coverage **(REQUIRED)** — aqui, cobertura é sobre o `Up`/`Down` via smoke test de aplicação
- Integration tests para constraints — cobertos em task_10

## Tests
- Unit tests:
  - [x] `Up` aplicado em DB vazio cria `bloco` e `unidade` com as colunas e tipos esperados (inspeção via `information_schema`)
  - [x] `Down` remove as duas tabelas sem deixar objetos órfãos
  - [x] Partial unique indexes aparecem em `pg_indexes.indexdef` com `WHERE ativo = true`
- Integration tests:
  - [x] Testcontainers sobe DB limpo, aplica migrations de F01+F02, retorna sem erro
  - [ ] Inserção de dois blocos ativos com mesmo `(tenant_id, condominio_id, nome)` → Postgres retorna erro de unique violation (coberto em task_10)
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- `dotnet ef database update` roda do zero sem erro em DB limpo
- Partial unique indexes presentes em `pg_indexes`
- FKs com `ON DELETE RESTRICT` protegem integridade (inserção ou delete viola conforme esperado)
- Fixture de integration tests não quebra ao reutilizar DB após respawn
