# F01 — Assistente de Criação de Condomínio — Task List

## Tasks

| # | Title | Status | Complexity | Dependencies |
|---|-------|--------|------------|--------------|
| 01 | Scaffold solution .NET + monorepo frontend + docker-compose dev | pending | medium | — |
| 02 | Configuração base da API (Serilog JSON, ProblemDetails, versionamento, CORS) | completed | medium | task_01 |
| 03 | EF Core DbContext + snake_case + fixture Testcontainers | completed | high | task_02 |
| 04 | ASP.NET Identity + roles seed (Operator, Sindico) + operador dev | completed | medium | task_03 |
| 05 | Multi-tenancy baseline (ITenantContext + query filter + teste de isolamento) | completed | critical | task_03 |
| 06 | Entidades Condominio + Sindico + tabelas + repositórios | completed | medium | task_04, task_05 |
| 07 | Entidade OptInRecord + tabela + repositório | completed | low | task_05, task_06 |
| 08 | IObjectStorage + adapter MinIO/S3 + entidade OptInDocument | completed | high | task_05 |
| 09 | IEmailSender (MailKit) + email_outbox + retry worker | completed | high | task_02 |
| 10 | IMagicLinkService + tabela magic_link | completed | medium | task_04 |
| 11 | AggregateRoot + IDomainEvent + outbox + interceptor + publisher NoOp | completed | critical | task_03 |
| 12 | CreateCondominioCommandHandler (orquestra tudo) | completed | high | task_06, task_07, task_10, task_11 |
| 13 | UploadOptInDocumentCommandHandler | completed | medium | task_05, task_08 |
| 14 | ActivateCondominioCommandHandler + tabela tenant_audit_log | completed | medium | task_06, task_11 |
| 15 | ResendMagicLinkCommandHandler | completed | low | task_06, task_09, task_10 |
| 16 | PasswordSetupCommandHandler | pending | medium | task_04, task_10 |
| 17 | Queries ListCondominios + GetCondominioDetails | pending | medium | task_05, task_06 |
| 18 | REST controllers + policies + auth cookie + DTOs Mapster | pending | high | task_04, task_12, task_13, task_14, task_15, task_16, task_17 |
| 19 | Observability (OpenTelemetry + health checks + sanitização de logs) | completed | medium | task_02, task_04 |
| 20 | Monorepo frontend baseline (Vite TS + tokens PortaBox + packages/ui + Lucide) | completed | high | task_01 |
| 21 | Backoffice SPA — autenticação + layout + API client + guards | completed | medium | task_18, task_20 |
| 22 | Backoffice SPA — wizard de criação (3 etapas + revisão + submit) | completed | high | task_18, task_20, task_21 |
| 23 | Backoffice SPA — lista, detalhes, ativação, reenvio, upload | completed | high | task_18, task_20, task_21 |
| 24 | Síndico SPA — setup-password + login + home placeholder | completed | medium | task_18, task_20 |
| 25 | Integration tests E2E do fluxo F01 + Playwright smoke | completed | high | task_18, task_22, task_23, task_24 |
| 26 | Hardening (rate-limit, CSRF/SameSite, STARTTLS, secrets, CSP, mascaramento) | completed | high | task_18, task_19, task_25 |
