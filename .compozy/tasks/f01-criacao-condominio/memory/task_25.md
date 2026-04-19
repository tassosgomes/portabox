# Task Memory: task_25.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot

Task completed. All required E2E integration tests and Playwright smoke suite implemented and passing.

## Important Decisions

- Implemented task_18 REST endpoints (auth + condominios) as prerequisite since they were pending.
- Added `.AddSignInManager()` to `DependencyInjection.cs` — required for `ISecurityStampValidator` to be registered for cookie auth; was missing and caused 500 on all business endpoints.
- Added `OnRedirectToLogin`/`OnRedirectToAccessDenied` event handlers to cookie auth to return 401/403 instead of 302 redirects — required for REST API contract.
- `AppFactoryFixture` uses `Email__UseStartTls=false` (not `Email__UseSsl`) to match `EmailOptions.UseStartTls` property.
- MailHog v2 API MIME parts have QP-encoded bodies. `ExtractMagicLinkTokensAsync` must decode QP (and optionally base64) before applying regex; otherwise `token=` becomes `token=3D` prefix capture.
- `GetLatestMagicLinkTokenAsync` polls MailHog up to 5 × 400ms instead of relying on a fixed delay.

## Learnings

- EF Core one-to-one fixup silently detaches a newly Added dependent when a second one with the same FK is added to the same context. `OptInRecordPersistenceTests.SavingSecondOptInRecordForSameTenant` fails because of this; pre-existing test design issue unrelated to task_25.
- `ConfigureWarnings(ignore ManyServiceProvidersCreatedWarning)` must be added to `IdentityIntegrationTests.IntegrationIdentityContext` to prevent test ordering failures when the process exceeds 20 EF Core internal service providers.
- MailHog v2 JSON: use `Content.MIME.Parts[N].Body` with QP decoding, not top-level `Content.Body`, for reliable token extraction.

## Files / Surfaces

New files:
- `src/PortaBox.Api/Endpoints/AuthEndpoints.cs`
- `src/PortaBox.Api/Endpoints/CondominiosEndpoints.cs`
- `tests/PortaBox.Api.IntegrationTests/Fixtures/AppFactoryFixture.cs`
- `tests/PortaBox.Api.IntegrationTests/Features/Condominios/CreateCondominioEndToEndTests.cs`
- `tests/PortaBox.Api.IntegrationTests/Features/Condominios/CnpjDeduplicationTests.cs`
- `tests/PortaBox.Api.IntegrationTests/MultiTenancy/TenantIsolationEndToEndTests.cs`
- `tests/PortaBox.Api.IntegrationTests/Features/OptInDocuments/UploadTests.cs`
- `tests/PortaBox.Api.IntegrationTests/Features/MagicLinks/MagicLinkFlowTests.cs`
- `tests/PortaBox.Api.IntegrationTests/Features/Outbox/OutboxAtomicityTests.cs`
- `tests/PortaBox.Api.IntegrationTests/Observability/LogSanitizationTests.cs`
- `tests/e2e/package.json`, `tests/e2e/playwright.config.ts`, `tests/e2e/specs/smoke-wizard.spec.ts`

Modified:
- `src/PortaBox.Api/Program.cs` — register endpoint groups
- `src/PortaBox.Api/Extensions/AuthenticationExtensions.cs` — 401/403 event handlers
- `src/PortaBox.Infrastructure/DependencyInjection.cs` — `.AddSignInManager()`
- `pnpm-workspace.yaml`, `package.json`, `.github/workflows/ci.yml`
- `tests/PortaBox.Api.IntegrationTests/IdentityIntegrationTests.cs` — `ConfigureWarnings` added

## Errors / Corrections

- `Email__UseSsl` → `Email__UseStartTls` (property name in EmailOptions)
- `ISecurityStampValidator` missing → added `.AddSignInManager()`
- Cookie auth 302 redirect → added 401/403 event handlers
- MailHog QP encoding corrupts token extraction → added QP/base64 decoder
- URL interpolation bug: `{condominioA}` → `{condominioA.CondominioId}` in TenantIsolationEndToEndTests

## Ready for Next Run

Task completed. 84/85 integration tests pass (1 pre-existing failure in `OptInRecordPersistenceTests`). All 95 unit tests pass. All 12 new E2E tests pass.
