# Task Memory: task_13.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Implemented opt-in document upload command flow with streaming SHA-256, tenant state gating, object storage upload, metadata persistence, and orphan-candidate error logging.

## Important Decisions
- Moved `Sha256StreamHasher` into `PortaBox.Application.Abstractions.Storage` so both `Gestao` and storage adapters can share the same streaming hash wrapper without adding a module-to-infrastructure dependency.
- Kept orphan handling as structured error logging (`storage.orphan-candidate`) on metadata commit failure, matching the task's simpler fallback instead of adding cleanup persistence now.
- Updated MinIO/S3 adapters to reuse an incoming `Sha256StreamHasher.HashingReadStream` when provided, avoiding double hashing while preserving the existing `IObjectStorage.UploadAsync` contract.

## Learnings
- `CondominioRepository.GetByIdAsync` reads the global tenant root without query filters, so it is the right place to validate tenant existence/state before creating tenant-scoped `OptInDocument` rows.
- The existing full-solution suite had one storage unit test tied to the old `PortaBox.Infrastructure.Common` namespace; moving the hasher required updating that reference to keep the global verification green.

## Files / Surfaces
- `src/PortaBox.Modules.Gestao/Application/Commands/UploadOptInDocument/*`
- `src/PortaBox.Modules.Gestao/DependencyInjection.cs`
- `src/PortaBox.Application.Abstractions/Storage/Sha256StreamHasher.cs`
- `src/PortaBox.Infrastructure/Storage/MinioObjectStorage.cs`
- `src/PortaBox.Infrastructure/Storage/S3ObjectStorage.cs`
- `src/PortaBox.Modules.Gestao/PortaBox.Modules.Gestao.csproj`
- `src/PortaBox.Infrastructure/PortaBox.Infrastructure.csproj`
- `tests/PortaBox.Modules.Gestao.UnitTests/UploadOptInDocumentCommand*Tests.cs`
- `tests/PortaBox.Api.IntegrationTests/UploadOptInDocumentCommandIntegrationTests.cs`
- `tests/PortaBox.Api.UnitTests/StorageInfrastructureTests.cs`

## Errors / Corrections
- Fixed a NuGet downgrade after adding logging abstractions by aligning `Microsoft.Extensions.Logging.Abstractions` to `8.0.3` in `PortaBox.Infrastructure`.
- Fixed the legacy storage unit test import after relocating `Sha256StreamHasher`.

## Ready for Next Run
- Task 13 implementation and its explicit tests are in place; next task can expose the handler through the REST controller without changing the application-layer upload flow.
