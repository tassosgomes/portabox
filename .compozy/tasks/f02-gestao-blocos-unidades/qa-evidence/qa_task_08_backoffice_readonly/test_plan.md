# Plano de Testes — CF-09 Leitura Cross-Tenant no Backoffice (API only)

**Task ID:** qa_task_08
**Tipos:** API, Banco
**UI:** BLOCKED — feature "estrutura" nao implementada no apps/backoffice

## Casos de Teste

### CT-01: Operador consulta Tenant A
- **Pre-condicao:** Operador autenticado com cookie valido
- **Passos:** GET /api/v1/admin/condominios/{QA_TENANT_A_ID}/estrutura (sem flag)
- **Expected:** 200 com payload EstruturaDto contendo blocos do Tenant A
- **Tipo:** API

### CT-02: Operador consulta Tenant B
- **Pre-condicao:** Mesmo cookie de operador do CT-01
- **Passos:** GET /api/v1/admin/condominios/{QA_TENANT_B_ID}/estrutura
- **Expected:** 200 com payload EstruturaDto contendo blocos do Tenant B
- **Tipo:** API

### CT-03: Operador com includeInactive=true em Tenant A
- **Pre-condicao:** Operador autenticado
- **Passos:** GET /api/v1/admin/condominios/{QA_TENANT_A_ID}/estrutura?includeInactive=true
- **Expected:** 200, blocos e unidades inativos incluidos na resposta (ativo=false aparece)
- **Tipo:** API

### CT-04: Operador com includeInactive=false em Tenant A
- **Pre-condicao:** Operador autenticado
- **Passos:** GET /api/v1/admin/condominios/{QA_TENANT_A_ID}/estrutura?includeInactive=false
- **Expected:** 200, apenas blocos e unidades com ativo=true
- **Tipo:** API

### CT-05: Payload identico ao sindico
- **Pre-condicao:** CT-01 e task_07 CT-01 disponiveis; sindico A autenticado
- **Passos:** GET endpoint sindico /api/v1/condominios/{QA_TENANT_A_ID}/estrutura com credencial sindico A; comparar com CT-01
- **Expected:** condominioId, nomeFantasia e estrutura de blocos/andares/unidades semanticamente identicos (geradoEm sera diferente)
- **Tipo:** API

### CT-06: CondominioId inexistente
- **Pre-condicao:** Operador autenticado
- **Passos:** GET /api/v1/admin/condominios/00000000-0000-0000-0000-000000000000/estrutura
- **Expected:** 404 com ProblemDetails
- **Tipo:** API

### CT-07: Sem autenticacao
- **Pre-condicao:** Nenhuma
- **Passos:** GET /api/v1/admin/condominios/{QA_TENANT_A_ID}/estrutura sem cookie
- **Expected:** 401 com ProblemDetails
- **Tipo:** API

### CT-08: Sindico tentando acessar admin endpoint do proprio tenant
- **Pre-condicao:** Cookie valido de Sindico A
- **Passos:** GET /api/v1/admin/condominios/{QA_TENANT_A_ID}/estrutura com cookie de sindico
- **Expected:** 403 — role Operator obrigatoria
- **Tipo:** API

### CT-09: Sindico tentando acessar admin endpoint de outro tenant
- **Pre-condicao:** Cookie valido de Sindico A
- **Passos:** GET /api/v1/admin/condominios/{QA_TENANT_B_ID}/estrutura com cookie de sindico A
- **Expected:** 403 — role Operator obrigatoria
- **Tipo:** API

### CT-10: Sem escrita cross-tenant — operador tentando POST em blocos
- **Pre-condicao:** Cookie valido de Operador
- **Passos:** POST /api/v1/condominios/{QA_TENANT_A_ID}/blocos com payload minimo {"nome":"test"}
- **Expected:** 403 — role Operator nao tem permissao de escrita de sindico
- **Tipo:** API

### CT-11: Verificar ausencia de endpoints admin de mutation
- **Pre-condicao:** N/A
- **Passos:** Revisar api-contract.yaml paths que comecam com /admin
- **Expected:** Apenas GET /admin/condominios/{condominioId}/estrutura existe; nenhum POST/PATCH/DELETE admin
- **Tipo:** Revisao de contrato

### CT-12: Auditoria do acesso do operador
- **Pre-condicao:** CTs anteriores executados com operador autenticado
- **Passos:** Query SELECT event_kind, tenant_id, performed_by, occurred_at, metadata_json FROM tenant_audit_log WHERE performed_by = '<operator_user_id>' ORDER BY occurred_at DESC LIMIT 10;
- **Expected:** Entradas de auditoria para os acessos read-only do operador (CF-09 PRD: "Acesso registrado em log de auditoria")
- **Tipo:** Banco

### CT-13: Performance — 3x consecutivos como operador em Tenant A
- **Pre-condicao:** Operador autenticado
- **Passos:** 3 chamadas GET /api/v1/admin/condominios/{QA_TENANT_A_ID}/estrutura medir latencia
- **Expected:** Media < 1000ms; comparar com task_07 CT-07 media de 20ms
- **Tipo:** API
