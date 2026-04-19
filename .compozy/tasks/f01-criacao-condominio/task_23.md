---
status: completed
title: Backoffice SPA — lista, detalhes, ativação, reenvio, upload
type: frontend
complexity: high
dependencies:
  - task_18
  - task_20
  - task_21
---

# Task 23: Backoffice SPA — lista, detalhes, ativação, reenvio, upload

## Overview
Implementa as demais telas do backoffice (CF4 e CF5 do PRD): lista paginada de tenants com filtros, tela de detalhes consolidando todos os dados do tenant, ação "Ativar operação", reenvio de magic link e upload opcional de documentos de opt-in. Consome os endpoints `GET /api/v1/admin/condominios`, `GET /api/v1/admin/condominios/{id}`, `POST ...:activate`, `POST .../sindicos/{userId}:resend-magic-link` e `POST .../opt-in-documents`.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST implementar tela `/condominios` com tabela paginada, filtro por status (`Todos`/`Pré-ativo`/`Ativo`), busca por nome/CNPJ, link "Novo condomínio"
- MUST implementar tela `/condominios/{id}` com seções:
  - Cabeçalho (nome, CNPJ mascarado, status `Badge`, data de criação, data de ativação)
  - Dados do condomínio (endereço, administradora)
  - Consentimento LGPD (data assembleia, quórum, signatário mascarado, data termo, lista de documentos com download)
  - Síndico (nome, e-mail, celular mascarado, senha definida sim/não)
  - Histórico de auditoria (últimas 20 entradas)
- MUST implementar ação "Ativar operação" com confirmação dupla via `Modal`; confirma chama `POST .../:activate`; sucesso atualiza estado para `Ativo` + toast "Operação ativada"
- MUST implementar ação "Reenviar magic link" apenas quando síndico ainda não definiu senha; confirma chama `POST .../sindicos/{userId}:resend-magic-link`; tratamento de 429 (rate limit) com mensagem pt-BR
- MUST implementar upload de documento (drag-drop ou input file) com validação client-side de tipo/tamanho antes do upload; barra de progresso durante envio; em sucesso, lista de documentos recarrega
- MUST implementar download via presigned URL (`GET .../opt-in-documents/{docId}:download` retorna `{ url, expires_at }`); abrir em nova aba
- MUST lidar com estados de loading/empty/error conforme skill `react-code-quality`
- MUST aplicar copy pt-BR + design system (ADR-010)
</requirements>

## Subtasks
- [x] 23.1 Implementar tela de lista com tabela + paginação + filtros + busca
- [x] 23.2 Implementar tela de detalhes com todas as seções
- [x] 23.3 Implementar ação "Ativar operação" com modal de confirmação
- [x] 23.4 Implementar ação "Reenviar magic link" com tratamento de rate-limit
- [x] 23.5 Implementar upload de documento + listagem + download
- [x] 23.6 Implementar estados empty/loading/error
- [x] 23.7 Integrar ícones Lucide (building-2, check-circle, mail, upload-cloud, power)

## Implementation Details
Consumir os DTOs e tipagens de task_21. Tabela pode usar TanStack Table ou implementação manual simples — esta task foca em funcionalidade, não em flexibilidade de tabela. Modal de confirmação reaproveitado do `packages/ui`.

### Relevant Files
- `apps/backoffice/src/features/condominios/pages/ListaCondominiosPage.tsx` (a criar)
- `apps/backoffice/src/features/condominios/pages/DetalhesCondominioPage.tsx` (a criar)
- `apps/backoffice/src/features/condominios/components/StatusBadge.tsx` (a criar)
- `apps/backoffice/src/features/condominios/components/AuditLogList.tsx` (a criar)
- `apps/backoffice/src/features/condominios/components/UploadOptInDocument.tsx` (a criar)
- `apps/backoffice/src/features/condominios/components/ActivateTenantAction.tsx` (a criar)
- `apps/backoffice/src/features/condominios/components/ResendMagicLinkAction.tsx` (a criar)
- `apps/backoffice/src/features/condominios/api.ts` — editar para adicionar endpoints usados

### Dependent Files
- `task_25` (Playwright) exerce todos os fluxos
- `task_26` (hardening) valida rate-limit por UI

### Related ADRs
- [ADR-001: Onboarding de Tenant no MVP](../adrs/adr-001.md)
- [ADR-002: Opt-in LGPD](../adrs/adr-002.md)
- [ADR-007: Storage de Documentos](../adrs/adr-007.md)
- [ADR-010: Design system PortaBox](../adrs/adr-010.md)

## Deliverables
- Lista + detalhes operacionais
- 3 ações operantes: ativar, reenviar magic link, upload doc
- Download via presigned URL
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests com mock da API **(REQUIRED)**

## Tests
- Unit tests (Vitest + Testing Library):
  - [x] Lista renderiza com paginação, filtro de status e busca
  - [x] `StatusBadge` mostra "Pré-ativo" com cor subtle e "Ativo" com cor sucesso
  - [x] Modal de "Ativar operação" exige segunda confirmação (dois cliques)
  - [x] Upload rejeita arquivo 12 MB client-side antes do POST
  - [x] Download abre nova aba com presigned URL retornada pela API
- Integration tests (MSW):
  - [x] Ativar operação: muda badge para "Ativo" e adiciona entrada no audit log
  - [x] Reenviar magic link: 200 mostra toast de sucesso
  - [x] Reenviar magic link: 429 mostra mensagem pt-BR clara ("Aguarde alguns minutos antes de reenviar")
  - [x] Upload de PDF 500 KB: grava documento + exibe na lista com tamanho formatado (`500 KB`)
  - [x] Empty state da lista: mostra ilustração/texto "Nenhum condomínio cadastrado"
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Operador consegue gerenciar todo o ciclo de vida pré-operacional do tenant
- Estados de loading/empty/error implementados em todas as telas
- Design system consistente com o kit `admin-dashboard` do skill
