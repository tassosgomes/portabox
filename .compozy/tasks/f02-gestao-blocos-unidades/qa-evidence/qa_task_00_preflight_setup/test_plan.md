# Plano de Testes — qa_task_00_preflight_setup

**Task ID:** qa_task_00
**Tipos:** API, Banco

## Casos de Teste

### CT-01: Backend Health Check
- **Pre-condicao:** Servico iniciado
- **Passos:** GET /health/live
- **Expected:** HTTP 200, status Healthy
- **Tipo:** API

### CT-02: Frontend Sindico Health Check
- **Pre-condicao:** Vite dev server rodando na porta 5173
- **Passos:** GET http://localhost:5173/
- **Expected:** HTTP 200
- **Tipo:** API

### CT-03: MailHog UI Health Check
- **Pre-condicao:** MailHog container rodando
- **Passos:** GET http://localhost:8025/ (verbo GET, nao HEAD)
- **Expected:** HTTP 200 com HTML da UI
- **Tipo:** API

### CT-04: Postgres Health Check
- **Pre-condicao:** Container postgres rodando
- **Passos:** psql SELECT 1; SELECT count(*) FROM asp_net_users
- **Expected:** Retorna 1; retorna numero de usuarios existentes
- **Tipo:** Banco

### CT-05: Login como Operator
- **Pre-condicao:** Seed executou com usuario operator@portabox.dev / PortaBox123!
- **Passos:** POST /api/v1/auth/login com credenciais do operador
- **Expected:** HTTP 200, role=Operator, cookie de sessao definido
- **Tipo:** API

### CT-06: Criar Tenant A (condominio + sindico A)
- **Pre-condicao:** Autenticado como Operator
- **Passos:** POST /api/v1/admin/condominios com dados do condominio e sindico A
- **Expected:** HTTP 201, retorna condominioId e sindicoUserId
- **Tipo:** API

### CT-07: Capturar magic link do sindico A no MailHog
- **Pre-condicao:** Email enviado apos criacao do condominio A
- **Passos:** GET /api/v2/messages; filtrar por email do sindico A; extrair token
- **Expected:** Email encontrado com link contendo ?token=...
- **Tipo:** API

### CT-08: Redeem magic link do sindico A (password-setup)
- **Pre-condicao:** Token valido extraido do email
- **Passos:** POST /api/v1/auth/password-setup com token e senha QaTestPass123!
- **Expected:** HTTP 200
- **Tipo:** API

### CT-09: Login como sindico A
- **Pre-condicao:** Senha definida via magic link
- **Passos:** POST /api/v1/auth/login com email e senha do sindico A
- **Expected:** HTTP 200, role=Sindico, tenantId == condominioId A
- **Tipo:** API

### CT-10: Criar Tenant B (condominio + sindico B)
- **Pre-condicao:** Autenticado como Operator (cookie operator reutilizado)
- **Passos:** POST /api/v1/admin/condominios com dados do condominio e sindico B
- **Expected:** HTTP 201, retorna condominioId e sindicoUserId distintos do tenant A
- **Tipo:** API

### CT-11: Capturar magic link do sindico B e fazer password-setup
- **Pre-condicao:** Email enviado apos criacao do condominio B
- **Passos:** GET /api/v2/messages; filtrar por email do sindico B; POST password-setup
- **Expected:** HTTP 200
- **Tipo:** API

### CT-12: Login como sindico B
- **Pre-condicao:** Senha definida via magic link
- **Passos:** POST /api/v1/auth/login com email e senha do sindico B
- **Expected:** HTTP 200, role=Sindico, tenantId == condominioId B (diferente do A)
- **Tipo:** API

### CT-13: Persistir credenciais em .env.qa.local
- **Pre-condicao:** Todos IDs e emails capturados
- **Passos:** Gerar arquivo .env.qa.local com todas as variaveis
- **Expected:** Arquivo criado com 9 chaves necessarias
- **Tipo:** N/A (setup)
