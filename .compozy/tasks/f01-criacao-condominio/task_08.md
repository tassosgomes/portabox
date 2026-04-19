---
status: completed
title: IObjectStorage + adapter MinIO/S3 + entidade OptInDocument
type: backend
complexity: high
dependencies:
  - task_05
---

# Task 08: IObjectStorage + adapter MinIO/S3 + entidade OptInDocument

## Overview
Introduz a abstração `IObjectStorage` com adapters plugáveis (MinIO em dev, S3/R2 em prod), junto da entidade multi-tenant `OptInDocument` que armazena metadados (`storage_key`, `sha256`, `content_type`, `size_bytes`). Todo upload de arquivo no projeto passa por aqui — F01 usa para opt-in, D04 (reconhecimento de etiquetas) usará para fotos futuramente.

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST definir `IObjectStorage` em `PortaBox.Application.Abstractions` com `UploadAsync`, `GetDownloadUrlAsync` (presigned URL) e `DeleteAsync`
- MUST implementar `MinioObjectStorage` (NuGet `Minio`) para dev
- MUST implementar `S3ObjectStorage` (NuGet `AWSSDK.S3`) para prod e R2 (endpoint S3-compat)
- MUST selecionar adapter via configuração `Storage:Provider = Minio|S3`
- MUST implementar entidade `OptInDocument` implementando `ITenantEntity`
- MUST criar migração EF Core para tabela `opt_in_document` com índice `(tenant_id, uploaded_at DESC)`
- MUST validar tipos MIME (`application/pdf`, `image/jpeg`, `image/png`) e tamanho máximo (10 MB) na camada de handler (task_13) — a interface só recebe os bytes
- MUST calcular hash SHA-256 em streaming durante o upload
- MUST gerar presigned URL com TTL configurável (default 5 min)
- SHOULD expor `StorageOptions` bindado de `IOptions<StorageOptions>`
</requirements>

## Subtasks
- [x] 08.1 Definir `IObjectStorage`, `ObjectStorageReference`, `StorageOptions`
- [x] 08.2 Implementar `MinioObjectStorage`
- [x] 08.3 Implementar `S3ObjectStorage` (compatível com R2)
- [x] 08.4 Registrar adapter escolhido via DI conforme `Storage:Provider`
- [x] 08.5 Implementar entidade `OptInDocument` + configuração EF Core
- [x] 08.6 Gerar migração com índice composto
- [x] 08.7 Implementar helper de SHA-256 em streaming reutilizável

## Implementation Details
Seguir ADR-007 para convenção de chave (`condominios/{tenant_id}/opt-in/{document_id}.pdf`) e bucket único por ambiente (`portabox-{env}`). Usar Testcontainers MinIO para testes de integração reais, e `FakeObjectStorage` em memória para testes unitários de handlers.

### Relevant Files
- `src/PortaBox.Application.Abstractions/Storage/IObjectStorage.cs` (a criar)
- `src/PortaBox.Application.Abstractions/Storage/StorageOptions.cs` (a criar)
- `src/PortaBox.Infrastructure/Storage/MinioObjectStorage.cs` (a criar)
- `src/PortaBox.Infrastructure/Storage/S3ObjectStorage.cs` (a criar)
- `src/PortaBox.Infrastructure/Storage/StorageServiceCollectionExtensions.cs` (a criar)
- `src/PortaBox.Infrastructure/Common/Sha256StreamHasher.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Domain/OptInDocument.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Domain/OptInDocumentKind.cs` — enum `Ata`, `Termo`, `Outro` (a criar)
- `src/PortaBox.Modules.Gestao/Infrastructure/Persistence/OptInDocumentConfiguration.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Application/Repositories/IOptInDocumentRepository.cs` (a criar)
- `src/PortaBox.Modules.Gestao/Infrastructure/Repositories/OptInDocumentRepository.cs` (a criar)
- `src/PortaBox.Infrastructure/Persistence/Migrations/*_OptInDocument.cs` (a criar)
- `tests/PortaBox.Api.IntegrationTests/Fixtures/MinioFixture.cs` (a criar)

### Dependent Files
- `task_13` (UploadOptInDocumentCommandHandler) consome `IObjectStorage` e persiste `OptInDocument`
- `task_17` e `task_23` (frontend) geram presigned URLs para download

### Related ADRs
- [ADR-007: Storage de Documentos via IObjectStorage (MinIO/S3) com Metadados em Postgres](../adrs/adr-007.md) — especifica a abstração, adapters, bucket e política de presigned URL.

## Deliverables
- `IObjectStorage` + dois adapters plugáveis por configuração
- Entidade `OptInDocument` + migração + índice composto
- Helper `Sha256StreamHasher` reutilizável
- Fixture `MinioFixture` para testes de integração
- `FakeObjectStorage` em memória para testes unitários
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para upload e presigned URL **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `Sha256StreamHasher` calcula hash correto de um stream conhecido sem consumir a stream para o consumidor downstream
  - [x] Convenção de chave `condominios/{tenant_id}/opt-in/{doc_id}.pdf` coberta por `ObjectStorageKeyFactory`
  - [x] `StorageServiceCollectionExtensions` registra `MinioObjectStorage` quando `Provider=Minio`
  - [x] `StorageServiceCollectionExtensions` registra `S3ObjectStorage` quando `Provider=S3`
- Integration tests:
  - [x] Upload de PDF 1 MB via `MinioObjectStorage` grava objeto recuperável via `GetObjectAsync` do bucket
  - [x] `GetDownloadUrlAsync` retorna presigned URL com query string Amazon/MinIO válida expirando em 5 min
  - [x] `DeleteAsync` remove objeto e `GetObjectAsync` subsequente falha com 404
  - [x] Migração cria `opt_in_document` com índice `(tenant_id, uploaded_at DESC)`
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- `IObjectStorage` consumível por qualquer handler sem conhecimento do provider
- MinIO operacional em dev via Testcontainers
- `OptInDocument` preparada para receber registros do task_13
