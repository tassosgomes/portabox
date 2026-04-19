---
status: completed
title: Observability (OpenTelemetry + health checks + sanitização de logs)
type: backend
complexity: medium
dependencies:
  - task_02
  - task_04
---

# Task 19: Observability (OpenTelemetry + health checks + sanitização de logs)

## Overview
Configura observabilidade completa da API conforme skill `dotnet-observability`: OpenTelemetry traces/metrics com exporter OTLP configurável, Serilog em JSON com scopes de correlação, sanitização de campos sensíveis (CPF, CNPJ completo, e-mail, token), health checks `/health/live` e `/health/ready`, e métricas customizadas para o F01 (tenants criados/ativados, magic links emitidos/consumidos, outbox age).

<critical>
- ALWAYS READ the PRD and TechSpec before starting
- REFERENCE TECHSPEC for implementation details — do not duplicate here
- FOCUS ON "WHAT" — describe what needs to be accomplished, not how
- MINIMIZE CODE — show code only to illustrate current structure or problem areas
- TESTS REQUIRED — every task MUST include tests in deliverables
</critical>

<requirements>
- MUST adicionar OpenTelemetry ao host (traces + metrics) com exporter OTLP configurado por env (`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME=portabox-api`)
- MUST instrumentar AspNetCore, HttpClient, EF Core e Npgsql automaticamente
- MUST implementar métricas customizadas listadas no TechSpec seção **Monitoring and Observability** (counters: `condominio_created_total`, `condominio_activated_total`, `magic_link_issued_total`, `magic_link_consumed_total`, `magic_link_expired_total`, `email_send_duration_seconds` histogram, `email_outbox_age_seconds` gauge, `domain_event_outbox_pending_count` gauge)
- MUST configurar enrichers Serilog para anexar `request_id`, `trace_id`, `span_id`, `user_id`, `tenant_id` em todo log
- MUST implementar `ILogEnricher` ou middleware que sanitiza campos `password`, `token`, `cpf`, `cnpj` e `email` antes do sink
- MUST expor `GET /health/live` (processo vivo) e `GET /health/ready` com checks: DB (`SELECT 1`), object storage (`HeadBucket`), SMTP (TCP connect sem SEND)
- MUST emitir os logs estruturados listados no TechSpec (`condominio.created`, `condominio.activated`, `magic_link.issued`, `magic_link.consumed`, `email.sent`, `email.failed`, `password-setup.*`)
- MUST garantir que resposta dos health checks segue formato JSON compatível com Kubernetes probes
</requirements>

## Subtasks
- [x] 19.1 Adicionar OpenTelemetry + instrumentações + exporter OTLP
- [x] 19.2 Implementar `ActivitySource` e `Meter` dedicados em `PortaBox.Infrastructure`
- [x] 19.3 Registrar métricas customizadas e conectar com os handlers
- [x] 19.4 Configurar enrichers Serilog (`request_id`, `trace_id`, `span_id`, `user_id`, `tenant_id`)
- [x] 19.5 Implementar middleware/enricher de sanitização de campos sensíveis
- [x] 19.6 Implementar health checks `/live` e `/ready` com subchecks
- [x] 19.7 Validar que os logs estruturados do TechSpec são emitidos corretamente

## Implementation Details
Conforme skill `dotnet-observability`. Métricas são emitidas pelos handlers chamando `Meter.CreateCounter<T>` etc. Sanitização pode usar `Destructurama.Attributed` + regex para campos conhecidos; padrão é deny-list (tudo que casar com lista vira `"***"`).

### Relevant Files
- `src/PortaBox.Api/Extensions/OpenTelemetryExtensions.cs` (a criar)
- `src/PortaBox.Infrastructure/Observability/Diagnostics.cs` — `ActivitySource` e `Meter` centrais (a criar)
- `src/PortaBox.Infrastructure/Observability/GestaoMetrics.cs` — métricas do F01 (a criar)
- `src/PortaBox.Infrastructure/Observability/SensitiveFieldSanitizer.cs` — sanitização (a criar)
- `src/PortaBox.Api/HealthChecks/StorageHealthCheck.cs` (a criar)
- `src/PortaBox.Api/HealthChecks/SmtpHealthCheck.cs` (a criar)
- `src/PortaBox.Api/Program.cs` — registrar OTel, Serilog enrichers, health checks (editar)

### Dependent Files
- Todos os handlers de tasks 12–16 emitem métricas customizadas
- `task_25` (integration tests) verifica que health checks respondem corretamente
- `task_26` (hardening) amarra alertas a essas métricas

### Related ADRs
- Nenhum ADR específico — decisão transversal coberta pelo skill `dotnet-observability`.

## Deliverables
- Bootstrap OpenTelemetry completo com exporter OTLP
- Métricas customizadas do F01 funcionando
- Logs estruturados com correlação
- Sanitização aplicada globalmente
- Health checks `/live` e `/ready`
- Unit tests com 80%+ coverage **(REQUIRED)**
- Integration tests para health checks e sanitização **(REQUIRED)**

## Tests
- Unit tests:
  - [x] `SensitiveFieldSanitizer` mascara `password` em evento com `"password":"hunter2"` → `"password":"***"`
  - [x] `SensitiveFieldSanitizer` mascara `token` em qualquer lugar do payload
  - [x] `SensitiveFieldSanitizer` preserva campos não sensíveis
  - [x] `GestaoMetrics.IncrementCondominioCreated()` incrementa o counter correspondente
- Integration tests:
  - [x] `GET /health/live` retorna 200 com JSON `{ status: "Healthy" }`
  - [x] `GET /health/ready` retorna 200 quando Postgres + MinIO + SMTP alcançáveis
  - [x] `GET /health/ready` retorna 503 quando MinIO inacessível
  - [x] Ao criar condomínio, meter `condominio_created_total` incrementa em 1
  - [x] Logs de criação de condomínio contêm `request_id`, `trace_id`, `tenant_id`
  - [x] Logs não contêm senha nem token em claro mesmo quando trechos suspeitos aparecem no payload
- Test coverage target: >=80%
- All tests must pass

## Success Criteria
- All tests passing
- Test coverage >=80%
- Traces e métricas exportados via OTLP configurável
- Health checks compatíveis com probes Kubernetes
- Zero PII em logs após sanitização
