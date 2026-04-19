---
status: completed
title: UploadOptInDocumentCommandHandler
type: backend
complexity: medium
dependencies:
  - task_05
  - task_08
---

# Task 13: UploadOptInDocumentCommandHandler

## Overview
Cria o handler que recebe a stream de um documento de opt-in (ata ou termo), valida tipo e tamanho, calcula hash SHA-256 em streaming, faz upload para o object storage e persiste os metadados em `opt_in_document`. O endpoint REST correspondente é criado em task_18; aqui temos apenas a lógica de domínio/aplicação.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST definir `UploadOptInDocumentCommand` com `TenantId`, `Kind` (`Ata`/`Termo`/`Outro`), `ContentType`, `FileName`, `Stream` (ou `byte[]` + abstração), `UploadedByUserId`
- MUST validar tipo MIME (`application/pdf`, `image/jpeg`, `image/png`) — reprovar outros
- MUST validar tamanho máximo 10 MB
- MUST calcular SHA-256 em streaming enquanto enviaupload (sem buffer completo em memória)
- MUST compor `storage_key` como `condominios/{tenant_id}/opt-in/{document_id}.{ext}`
- MUST persistir `OptInDocument` com todos os metadados (`storage_key`, `content_type`, `size_bytes`, `sha256`, `uploaded_at`, `uploaded_by_user_id`)
- MUST garantir fluxo "reservar → upload → commit": se `IObjectStorage.UploadAsync` retornar, o insert do metadata deve ocorrer; em falha do insert, marcar o objeto para limpeza (coluna `needs_cleanup` ou log + job posterior — decisão simples: log estruturado)
- MUST retornar `UploadOptInDocumentResult` com `DocumentId`
- MUST exigir que o tenant exista e esteja no estado `PreAtivo` ou `Ativo`
</requirements>

## Subtasks
- [x] 13.1 Definir `UploadOptInDocumentCommand` + `UploadOptInDocumentResult`
- [x] 13.2 Implementar validador (tipo, tamanho, tenant existente)
- [x] 13.3 Implementar handler com streaming hash + upload + persistência
- [x] 13.4 Definir política de órfão (log estruturado `storage.orphan-candidate`)
- [x] 13.5 Registrar handler via DI

## Implementation Details
Conforme ADR-007. O command recebe `Stream` aberta; o handler **não** reposiciona a stream (responsabilidade do chamador). Hash é calculado usando `Sha256StreamHasher` de task_08. Extensão do arquivo derivada do `ContentType`.

### Relevant Files
- `src/PortaBox.Modules.Gestao/Application/Commands/UploadOptInDocument/UploadOptInDocumentCommand.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/UploadOptInDocument/UploadOptInDocumentCommandHandler.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/UploadOptInDocument/UploadOptInDocumentCommandValidator.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Commands/UploadOptInDocument/UploadOptInDocumentResult.cs` (a criar)

### Dependent Files
- `task_08` (`IObjectStorage`, `OptInDocument`) — dependência direta
- `task_18` (controller) expõe `POST /api/v1/admin/condominios/{id}/opt-in-documents`
- `task_23` (backoffice) consome o endpoint

### Related ADRs
- [ADR-002: Registro do Opt-in Coletivo LGPD — upload opcional](../adrs/adr-002.md)
- [ADR-007: Storage de Documentos via IObjectStorage](../adrs/adr-007.md)

## Deliverables
- Command + Handler + Validator + Result
- Política de órfão via log estruturado
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests com MinIO **(REQUIRED)**

## Tests
- Unit tests:
  - [x] Validator reprova `ContentType="text/plain"`
  - [x] Validator reprova `size_bytes > 10*1024*1024`
  - [x] Handler calcula SHA-256 correto para stream conhecida
  - [x] Handler falha quando tenant não existe (retorna `TenantNotFound`)
  - [x] Handler falha quando falha o upload antes de tocar o banco (nenhuma linha em `opt_in_document`)
- Integration tests:
  - [x] Upload de PDF 1 MB grava objeto no MinIO (verificado via `GetObjectAsync`) e linha correspondente em `opt_in_document`
  - [x] Falha simulada no insert (constraint violada artificialmente) emite log `storage.orphan-candidate` com `storage_key`
  - [x] Upload de arquivo 11 MB é rejeitado antes de tocar o MinIO
  - [x] Query filter multi-tenant: tenant `B` não consegue ver documentos do tenant `A` via repositório
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Upload + metadata consistentes em caminho feliz
- Órfãos documentados em log para rotina posterior
- Handler pronto para ser exposto no controller em task_18
