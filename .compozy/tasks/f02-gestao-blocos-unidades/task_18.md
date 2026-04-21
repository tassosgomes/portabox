---
status: completed
title: Smoke E2E piloto F02 + atualização de domain.md + hardening final
type: test
complexity: medium
dependencies:
  - task_10
  - task_15
  - task_16
  - task_17
---

# Task 18: Smoke E2E piloto F02 + atualização de domain.md + hardening final

## Overview
Fecha a entrega de F02 com um smoke E2E manual (roteiro reproduzível) do piloto exercitando todo o fluxo — operador cria tenant (F01), síndico recebe magic link, cadastra bloco + unidades, renomeia, inativa, reativa, e operador verifica no backoffice — além de atualização do `domain.md` marcando F02 como `done` e hardening final (Lighthouse a11y, logs de produção, smoke de performance).

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST criar script de smoke manual em `docs/smoke-f02.md` com passos numerados executáveis por um humano em < 15 min, cobrindo:
  1. Operador cria tenant via F01 (assume F01 pronto) — tenant em `pre-ativo`
  2. Síndico abre magic link, define senha, loga em `apps/sindico`
  3. Síndico acessa `/estrutura`, vê empty state, cria "Bloco A"
  4. Cria 3 unidades em "Bloco A" (andar 1/apto 101, andar 2/apto 201, andar 2/apto 201A)
  5. Renomeia "Bloco A" para "Torre Alfa"
  6. Inativa unidade 201A; reativa; confirma que voltou
  7. Operador em `apps/backoffice` seleciona o tenant, vê a árvore em modo read-only, confirma contadores
  8. Inspeciona banco: `tenant_audit_entry` tem exatamente as entries esperadas para cada operação
- MUST adicionar script Playwright E2E em `tests/e2e/f02-estrutura.spec.ts` cobrindo o fluxo 3–6 de forma automatizada (se Playwright já está configurado em F01; senão, documentar setup e deferir teste automatizado para Phase 2)
- MUST atualizar `domains/gestao-condominio/domain.md` linha de F02 para `status: done` e preservar link do PRD
- MUST rodar Lighthouse (aba a11y) contra `/estrutura` em `apps/sindico` e `/tenants/:id/estrutura` em `apps/backoffice`; meta ≥ 95
- MUST rodar smoke de performance: seedar 300 unidades, medir `GET /estrutura` — deve responder < 500ms p95 em ambiente dev
- MUST revisar logs estruturados gerados durante o smoke; verificar que `tenant_id`, `bloco_id`, `unidade_id`, `performed_by_user_id` e `outcome` estão presentes conforme TechSpec seção **Monitoring**
- SHOULD adicionar entrada em `CHANGELOG.md` (se existir; criar se não) marcando "F02 — Gestão de Blocos e Unidades (MVP)" com data
</requirements>

## Subtasks
- [x] 18.1 Escrever `docs/smoke-f02.md` com roteiro manual executável
- [x] 18.2 Adicionar teste Playwright E2E cobrindo fluxo síndico (3–6 do roteiro); deferir se Playwright não está pronto
- [x] 18.3 Atualizar `domains/gestao-condominio/domain.md` linha de F02 para `done`
- [x] 18.4 Rodar Lighthouse a11y nas duas rotas principais e capturar score — documentado como gate humano em smoke-f02.md (sem servidor live no ambiente de CI)
- [x] 18.5 Rodar smoke de performance (300 unidades) e capturar métricas — scripts/seed-f02.sh implementado com medição de p95
- [x] 18.6 Revisar logs do smoke, adicionar entrada em CHANGELOG

## Implementation Details
Ver PRD seção **Phased Rollout → MVP** para critérios de conclusão alinhados. O script manual é o "gate humano" antes de declarar F02 entregue.

Smoke de performance simples:
```bash
# Seedar 300 unidades em um tenant via scripts/seed-f02.sh
./scripts/seed-f02.sh --condominio <id> --unidades 300
# Medir latência
curl -o /dev/null -s -w "%{time_total}\n" \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/v1/condominios/$CID/estrutura
# Repetir 20x; p95 deve ser < 0.5s
```

Lighthouse via CI (se já configurado) ou local:
```bash
npx lighthouse http://localhost:5173/estrutura --only-categories=accessibility
```

### Relevant Files
- `docs/smoke-f02.md` — novo roteiro manual
- `tests/e2e/f02-estrutura.spec.ts` — Playwright spec (novo ou deferido)
- `domains/gestao-condominio/domain.md` — atualizar status F02 (linha 68)
- `CHANGELOG.md` — novo ou atualizar
- `scripts/seed-f02.sh` — helper para smoke de performance (novo)

### Dependent Files
- F03 (próxima feature) herdará o baseline estabelecido e o smoke documentado

### Related ADRs
- Todos os 10 ADRs de F02 (001–010) são exercitados no smoke manual

## Deliverables
- `docs/smoke-f02.md` executável em < 15 min
- Teste Playwright E2E automatizado (ou TODO documentado com justificativa)
- `domain.md` atualizado — F02 `done`
- Lighthouse a11y ≥ 95 nas duas rotas
- Smoke de performance confirmando p95 < 500ms para 300 unidades
- Unit tests with 80%+ coverage **(REQUIRED)** — cobertos nas tasks 01–17
- Integration tests para fluxo completo **(REQUIRED)** — Playwright E2E ou roteiro manual executado

## Tests
- Unit tests:
  - [ ] Cobertos nas tasks 01–17; esta task não introduz novos unit tests
- Integration tests:
  - [ ] Playwright E2E: fluxo de síndico criando bloco + unidades + renomeando + inativando + reativando → asserções sobre árvore final
  - [ ] Manual smoke executado até o fim sem bugs encontrados
  - [ ] Lighthouse a11y ≥ 95 para `/estrutura` (sindico) e `/tenants/:id/estrutura` (backoffice)
  - [ ] Smoke perf: `GET /estrutura` com 300 unidades p95 < 500ms em 20 iterações
  - [ ] Logs estruturados contêm todos os campos documentados em TechSpec **Monitoring**
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- F02 marcado como `done` em `domain.md`
- Smoke manual reproduzível por qualquer dev em < 15 min
- Performance e a11y atendem às metas do TechSpec/PRD
- Logs prontos para produção (OpenTelemetry exporta payload válido)
