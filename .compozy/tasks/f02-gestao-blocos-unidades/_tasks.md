# F02 — Gestão de Blocos e Unidades — Task List

## Tasks

| # | Title | Status | Complexity | Dependencies |
|---|-------|--------|------------|--------------|
| 01 | Infra soft-delete: ISoftDeletable + SoftDeleteableAggregateRoot + global filter por reflection | pending | high | — |
| 02 | Extensão TenantAuditEntry.EventKind + StructuralAuditMetadata + IAuditService.RecordStructuralAsync | pending | low | — |
| 03 | Agregado Bloco: entity + eventos + EF Configuration + IBlocoRepository + BlocoRepository | pending | medium | task_01 |
| 04 | Agregado Unidade: entity + eventos + EF Configuration + IUnidadeRepository + UnidadeRepository | pending | medium | task_01 |
| 05 | Migration EF AddBlocoAndUnidade (DDL + FKs + partial unique indexes + enum update) | pending | medium | task_02, task_03, task_04 |
| 06 | Bloco: Commands + Handlers + Validators (Create/Rename/Inativar/Reativar) | pending | medium | task_02, task_03 |
| 07 | Unidade: Commands + Handlers + Validators (Create/Inativar/Reativar) | pending | medium | task_02, task_03, task_04 |
| 08 | GetEstruturaQuery + handler + DTOs (árvore completa) | pending | medium | task_03, task_04 |
| 09 | Registro DI + EstruturaEndpoints + mapeamento em Program.cs (8 rotas) | pending | high | task_05, task_06, task_07, task_08 |
| 10 | Integration tests F02 (endpoints + soft-delete filter + cross-tenant + audit) | pending | high | task_09 |
| 11 | packages/api-client baseline (http + queryKeys + ApiError + módulos tipados) | pending | medium | task_09 |
| 12 | Componentes Tree + TreeNode + ConfirmModal em packages/ui | pending | medium | — |
| 13 | Setup TanStack Query em apps/sindico e apps/backoffice | pending | low | task_11 |
| 14 | apps/sindico: EstruturaPage + hook useEstrutura (leitura da árvore) | pending | medium | task_11, task_12, task_13 |
| 15 | apps/sindico: mutações de Bloco (hooks + BlocoForm + modais) | pending | medium | task_14 |
| 16 | apps/sindico: mutações de Unidade (hooks + UnidadeForm + modais) | pending | medium | task_14 |
| 17 | apps/backoffice: estrutura read-only cross-tenant + seletor de tenant + log de acesso | pending | medium | task_11, task_12, task_13 |
| 18 | Smoke E2E piloto + atualização de domain.md + hardening final | pending | medium | task_10, task_15, task_16, task_17 |
