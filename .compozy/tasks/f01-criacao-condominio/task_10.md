---
status: completed
title: IMagicLinkService + tabela magic_link
type: backend
complexity: medium
dependencies:
  - task_04
---

# Task 10: IMagicLinkService + tabela magic_link

## Overview
Implementa o mecanismo de magic link descrito em ADR-006: token opaco de 32 bytes em Base64URL, apenas o hash SHA-256 persistido em `magic_link`, TTL 72h configurável, uso único e invalidação de pendentes ao reemitir. É a infraestrutura consumida pelo fluxo do primeiro login do síndico (tasks 12 e 15) e pela definição de senha (task 16).

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST definir `IMagicLinkService` com `IssueAsync(userId, purpose, ttl?)`, `ValidateAndConsumeAsync(rawToken, purpose)` e `InvalidatePendingAsync(userId, purpose)` conforme TechSpec seção Core Interfaces
- MUST criar entidade `MagicLink` e tabela `magic_link` com colunas do TechSpec (seção Data Models)
- MUST gerar token via `RandomNumberGenerator.GetBytes(32)` + Base64URL sem padding
- MUST armazenar `SHA256(UTF8(token))` em hex minúsculo como `CHAR(64)` com `UNIQUE INDEX`
- MUST considerar token válido apenas quando `consumed_at IS NULL AND invalidated_at IS NULL AND expires_at > now()`
- MUST marcar `consumed_at` + `consumed_by_ip` em consumo bem-sucedido, na mesma transação do handler consumidor
- MUST invalidar (`invalidated_at = now()`) tokens pendentes do mesmo `(user_id, purpose)` em reemissão
- MUST aplicar rate-limit de emissão por `(user_id, purpose)` — default 5 por 24h (configurável via `MagicLinkOptions`)
- MUST devolver resposta genérica em falha (não revelar se token é inválido, expirado ou já usado) — erro detalhado vai para log estruturado
- MUST emitir logs estruturados `magic-link.issued`, `magic-link.consumed`, `magic-link.invalidated` sem expor token em claro
</requirements>

## Subtasks
- [x] 10.1 Definir `IMagicLinkService`, `MagicLinkPurpose`, `MagicLinkIssueResult`, `MagicLinkConsumeResult`
- [x] 10.2 Implementar entidade `MagicLink` + configuração EF + migração
- [x] 10.3 Implementar `MagicLinkService` com geração segura, hashing, persistência
- [x] 10.4 Implementar validação/consumo com checagem atômica + marcação `consumed_at`
- [x] 10.5 Implementar invalidação em massa de pendentes
- [x] 10.6 Implementar rate-limit por `(user_id, purpose)`
- [x] 10.7 Adicionar logs estruturados sanitizados (sem token em claro)

## Implementation Details
Conforme ADR-006. Tabela `magic_link` é **global** (sem `tenant_id`, fica fora de `ITenantEntity`). Índice parcial `WHERE consumed_at IS NULL AND invalidated_at IS NULL` acelera validação e invalidação em massa.

### Relevant Files
- `src/PortaBox.Application.Abstractions/MagicLinks/IMagicLinkService.cs` (a criar)
- `src/PortaBox.Application.Abstractions/MagicLinks/MagicLinkPurpose.cs` (a criar)
- `src/PortaBox.Application.Abstractions/MagicLinks/MagicLinkOptions.cs` (a criar)
- `src/PortaBox.Infrastructure/MagicLinks/MagicLinkService.cs` (a criar)
- `src/PortaBox.Infrastructure/MagicLinks/MagicLink.cs` — entidade (a criar)
- `src/PortaBox.Infrastructure/Persistence/Configurations/MagicLinkConfiguration.cs` (a criar)
- `src/PortaBox.Infrastructure/Persistence/Migrations/*_MagicLink.cs` (a criar)

### Dependent Files
- `task_12` (CreateCondominio) dispara `IssueAsync` via handler in-process
- `task_15` (Resend magic link) usa `InvalidatePendingAsync` + `IssueAsync`
- `task_16` (PasswordSetup) usa `ValidateAndConsumeAsync`
- `task_26` (Hardening) adiciona rate-limit do endpoint público

### Related ADRs
- [ADR-006: Magic Link com Token Opaco (SHA-256) em Tabela Dedicada](../adrs/adr-006.md) — especificação completa.

## Deliverables
- `IMagicLinkService` + implementação `MagicLinkService`
- Tabela `magic_link` + migração + índices
- Log estruturado sanitizado
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para emissão/consumo/invalidação **(REQUIRED)**

## Tests
- Unit tests:
  - [ ] `IssueAsync` retorna token Base64URL de 43 caracteres e hash hex de 64 caracteres
  - [ ] Hash armazenado é `SHA256(UTF8(token))` (verificado com vetor conhecido)
  - [ ] `ValidateAndConsumeAsync` retorna sucesso apenas quando todas as condições são true
  - [ ] Rate-limit: 6ª emissão em 24h retorna erro específico `RateLimited`
  - [ ] Log `magic-link.issued` não contém o token em claro (apenas id/hash)
- Integration tests:
  - [ ] Ciclo completo: emitir → consumir retorna sucesso e marca `consumed_at`
  - [ ] Segundo consumo com mesmo token retorna falha genérica
  - [ ] Token expirado (forçar `expires_at < now()`) retorna falha genérica
  - [ ] `InvalidatePendingAsync` marca todos os pendentes do `(user_id, purpose)` em uma única UPDATE
  - [ ] Reemissão após invalidação permite consumir apenas o novo token
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Tokens nunca persistidos em claro
- Respostas de validação não vazam informação
- Implementação pronta para ser consumida pelos handlers seguintes
