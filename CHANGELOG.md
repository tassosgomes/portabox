# Changelog

All notable changes to this project will be documented in this file.

---

## [Unreleased]

---

## [F02] — 2026-04-20

### F02 — Gestão de Blocos e Unidades (MVP)

**Domínio:** D01 — Gestão do Condomínio  
**Feature:** F02 — Gestão de Blocos e Unidades  
**Status:** Done

#### Added

**Backend**
- `ISoftDeletable` marker interface and `SoftDeleteableAggregateRoot` base class (ADR-007)
- Global soft-delete query filter applied via reflection in `AppDbContext.OnModelCreating`
- `Bloco` aggregate: entity, domain events (`BlocoCriado/Renomeado/Inativado/ReativadoV1`), EF configuration, partial unique index `(tenant_id, condominio_id, nome) WHERE ativo = true`
- `Unidade` aggregate: entity, domain events (`UnidadeCriada/Inativada/ReativadaV1`), EF configuration, canonical partial unique index `(tenant_id, bloco_id, andar, numero) WHERE ativo = true`
- `IBlocoRepository` / `BlocoRepository` and `IUnidadeRepository` / `UnidadeRepository`
- Commands + handlers + FluentValidation validators for: `CreateBloco`, `RenameBloco`, `InativarBloco`, `ReativarBloco`, `CreateUnidade`, `InativarUnidade`, `ReativarUnidade`
- `GetEstruturaQuery` + handler returning the full hierarchical tree (`EstruturaDto`)
- 9 minimal API endpoints under `/api/v1` — 8 síndico + 1 operator admin (ADR-009)
- EF Core migration `AddBlocoAndUnidade` (DDL, FKs, partial unique indexes, `EventKind` enum extension)
- `TenantAuditEntry.EventKind` values 5–11: `BlocoCriado`, `BlocoRenomeado`, `BlocoInativado`, `BlocoReativado`, `UnidadeCriada`, `UnidadeInativada`, `UnidadeReativada` (ADR-008)
- Structured logs per handler: `event`, `tenant_id`, `condominio_id`, `bloco_id`, `unidade_id`, `performed_by_user_id`, `outcome`, `duration_ms`

**Frontend — packages**
- `packages/api-client`: baseline HTTP client, query keys, typed modules (`estrutura`, `blocos`, `unidades`)
- `packages/ui`: `<Tree>`, `<TreeNode>`, `<ConfirmModal>` generic components (ADR-010)

**Frontend — apps/sindico**
- TanStack Query setup (`QueryClientProvider`, devtools)
- `EstruturaPage`: full tree view with bloco + unit management
- Hooks: `useEstrutura`, `useCriarBloco`, `useRenomearBloco`, `useInativarBloco`, `useReativarBloco`, `useCriarUnidade`, `useInativarUnidade`, `useReativarUnidade`
- Forms: `BlocoForm` (create/rename), `UnidadeForm` (single + batch mode)
- Optimistic updates and cache invalidation via TanStack Query

**Frontend — apps/backoffice**
- TanStack Query setup
- `EstruturaReadOnlyPage`: cross-tenant read-only tree view for operators
- `TenantSelector` dropdown for operator navigation
- `useEstruturaAdmin` hook (operator role, explicit tenant scope)

**Tests**
- Backend unit tests: `SoftDeleteableAggregateRootTests`, `BlocoTests`, `UnidadeTests`, command handler tests
- Backend integration tests: endpoint coverage, soft-delete filter, cross-tenant isolation, audit entry verification
- Frontend unit + integration tests: `useEstrutura`, mutation hooks, `EstruturaPage`, `EstruturaReadOnlyPage`
- E2E Playwright spec: `tests/e2e/specs/f02-estrutura.spec.ts` (API-level smoke of steps 3–7)

**Docs & Scripts**
- `docs/smoke-f02.md`: manual smoke roteiro (< 15 min, 9 steps, gate humano)
- `scripts/seed-f02.sh`: seeds 300 units and measures p95 latency of `GET /estrutura`
- `domains/gestao-condominio/domain.md`: F02 status updated to `done`

#### Architecture Decisions

- ADR-006: F02 reuses `PortaBox.Modules.Gestao` (no new module)
- ADR-007: Soft-delete via `ISoftDeletable` + global filter + partial unique index
- ADR-008: Audit via `TenantAuditEntry.EventKind` extension + `MetadataJson`
- ADR-009: Single `GET /estrutura` endpoint returning complete tree
- ADR-010: TanStack Query + React Context as frontend baseline for all subsequent features

---

## [F01] — 2026-04-17

### F01 — Assistente de Criação de Condomínio (MVP)

Initial tenant setup wizard: condomínio creation, opt-in registration, síndico registration, magic link, backoffice management.
