# Production Readiness Checklist — F01

Generated for feature F01 (Assistente de Criação de Condomínio) based on task_26 hardening.

## Status: Ready for pilot go-live

---

## 1. Observability

| Item | Status | Notes |
|------|--------|-------|
| Structured JSON logs (Serilog) | ✅ | `ApiJsonFormatter` writes JSON to stdout |
| Request correlation (`X-Request-Id`) | ✅ | `RequestIdMiddleware` propagates or generates |
| OpenTelemetry traces exported via OTLP | ✅ | `OTEL_EXPORTER_OTLP_ENDPOINT` env var activates export |
| Health probe `/health/live` | ✅ | Liveness check (no DB) |
| Health probe `/health/ready` | ✅ | Readiness check includes DB + S3 |
| Metrics (custom GestaoMetrics) | ✅ | Exposed via OpenTelemetry |

## 2. Security — Auth & Cookies

| Item | Status | Notes |
|------|--------|-------|
| Cookie `SameSite=Strict` in production | ✅ | `AuthenticationExtensions.cs` |
| Cookie `Secure=true` in production | ✅ | `CookieSecurePolicy.Always` |
| Cookie `HttpOnly=true` | ✅ | Always set |
| 401/403 instead of 302 redirects | ✅ | `OnRedirectToLogin` / `OnRedirectToAccessDenied` |

## 3. Security — Rate Limiting

| Item | Status | Notes |
|------|--------|-------|
| Rate-limit `POST /auth/password-setup` | ✅ | 10 req/IP/10 min (configurable via env vars) |
| Rate-limit `POST /auth/login` | ✅ | Same policy |
| 429 with `Retry-After` header | ✅ | `RateLimitingExtensions.OnRejected` |

## 4. Security — Headers

| Item | Status | Notes |
|------|--------|-------|
| `Content-Security-Policy` | ✅ | `SecurityHeadersMiddleware` |
| `X-Content-Type-Options: nosniff` | ✅ | `SecurityHeadersMiddleware` |
| `X-Frame-Options: DENY` | ✅ | `SecurityHeadersMiddleware` |
| `Referrer-Policy: strict-origin-when-cross-origin` | ✅ | `SecurityHeadersMiddleware` |

## 5. Email / SMTP

| Item | Status | Notes |
|------|--------|-------|
| STARTTLS required in production | ✅ | Validated at startup via `ProductionConfigGuard` |
| Credentials from env vars only | ✅ | Not present in `appsettings.Production.json` |
| Outbox retry worker | ✅ | `EmailOutboxRetryWorker` |

## 6. Secrets Management

| Item | Status | Notes |
|------|--------|-------|
| DB credentials from env vars | ✅ | `ConnectionStrings__Postgres` |
| S3 credentials from env vars | ✅ | `Storage__AccessKey` / `Storage__SecretKey` |
| SMTP credentials from env vars | ✅ | `Email__Username` / `Email__Password` |
| Startup guard prevents JSON config secrets | ✅ | `ProductionConfigGuard.ValidateSecretsNotInJsonFiles` |
| `.env.sample` documents all required vars | ✅ | `/.env.sample` |

## 7. Data Protection

| Item | Status | Notes |
|------|--------|-------|
| Keys named with `SetApplicationName("PortaBox")` | ✅ | Prevents cross-app cookie reads |
| Keys persisted to durable volume | ✅ | When `DataProtection__KeysPath` env var is set |
| Action for production | ⚠️ | Mount a persistent volume at the path before first deploy |

## 8. Log Sanitization

| Item | Status | Notes |
|------|--------|-------|
| `?token=` masked in access logs | ✅ | `AccessLogSanitizerMiddleware` + custom message template |
| Password never logged | ✅ | Verified by `LogSanitizationTests` |
| CPF / CNPJ masked | ✅ | Verified by `LogSanitizationTests` |
| Email masked | ✅ | Verified by `LogSanitizationTests` |

## 9. Test Coverage

| Suite | Count | Pass |
|-------|-------|------|
| Unit tests | 95+ | ✅ |
| Integration tests | 84/85 | ✅ (1 pre-existing EF Core fixup issue) |
| E2E Playwright smoke | 12 | ✅ |

## 10. Open Items / Post-Pilot

- [ ] Choose production SMTP provider (SES, Mailgun, Resend) — see ADR-008
- [ ] Configure DKIM/SPF/DMARC for the sender domain
- [ ] Mount persistent volume for Data Protection keys before first deploy
- [ ] Set up alerting based on runbooks (`docs/runbooks/`)
- [ ] Evaluate MFA/SSO for `Operator` role (Fase 2, ADR-005)
