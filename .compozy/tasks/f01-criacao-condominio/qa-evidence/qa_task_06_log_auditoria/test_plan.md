# Plano de Testes — CF6: Log de Auditoria das Transicoes do Tenant

**Task ID:** qa_task_06
**Tipos:** Banco, API, UI

## Casos de Teste

### TC-01: Entrada de criacao no audit log (Banco)
- **Pre-condicao:** Tenant f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5 criado na task_01
- **Passos:**
  1. Conectar ao banco portabox via psql
  2. Consultar tenant_audit_log WHERE tenant_id = 'f6d3cc9d-...' AND event_kind = 1
- **Expected:** Registro existe com event_kind=1, performed_by_user_id preenchido, occurred_at preenchido
- **Tipo:** Banco

### TC-02: Entrada de ativacao no audit log (Banco)
- **Pre-condicao:** Tenant ativado na task_04
- **Passos:**
  1. Conectar ao banco portabox via psql
  2. Consultar tenant_audit_log WHERE tenant_id = 'f6d3cc9d-...' AND event_kind = 2
- **Expected:** Registro existe com event_kind=2, performed_by_user_id preenchido, occurred_at preenchido, note com texto da ativacao
- **Tipo:** Banco

### TC-03: Endpoint de API para audit log
- **Pre-condicao:** Usuario operator@portabox.dev autenticado
- **Passos:**
  1. POST /api/v1/auth/login com credenciais do operador
  2. Tentar GET /api/v1/admin/condominios/{id}/audit-log e variacoes
  3. Tentar GET /api/v1/admin/condominios/{id} e verificar campo auditLog
- **Expected:** Audit log retornado via algum endpoint, com entradas de criacao e ativacao
- **Tipo:** API

### TC-04: Log de auditoria visivel no painel de detalhes (UI)
- **Pre-condicao:** Aplicacao rodando em http://localhost:5173
- **Passos:**
  1. Navegar para /condominios/f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5
  2. Verificar se secao de log de auditoria esta presente
  3. Verificar se entradas de criacao e ativacao estao visiveis
- **Expected:** Secao de auditoria com entradas cronologicas, operador responsavel e timestamp
- **Tipo:** UI
- **Nota:** Bug conhecido de crash nesta pagina documentado em tasks anteriores

### TC-05: Imutabilidade do log (Banco)
- **Pre-condicao:** Tabela tenant_audit_log existe no banco
- **Passos:**
  1. Verificar schema da tabela: colunas updated_at, deleted_at, is_active, is_deleted
  2. Verificar triggers customizados de bloqueio de UPDATE/DELETE
  3. Verificar regras (rules) de banco
- **Expected:** Sem colunas de mutabilidade, tabela de estrutura append-only
- **Tipo:** Banco
