# Plano de Testes — CF-01 Cadastro de Bloco

**Task ID:** qa_task_01
**Tipos:** API, Banco, UI
**Re-execucao:** Backend reiniciado com endpoints de F02 registrados

---

## Casos de Teste API

### CT-01: Happy path — criar Bloco QA-01
- **Pre-condicao:** Sindico A autenticado, tenant A ativo, tabela bloco vazia
- **Passos:** POST /api/v1/condominios/{tenantA}/blocos {"nome":"Bloco QA-01"}
- **Expected:** 201 + body {id, condominioId, nome, ativo:true, inativadoEm:null} + header Location
- **Tipo:** API

### CT-02: Nome vazio → 400
- **Pre-condicao:** Autenticado
- **Passos:** POST com {"nome":""}
- **Expected:** 400 + ValidationProblemDetails com errors.nome
- **Tipo:** API

### CT-03: Whitespace only → 400
- **Pre-condicao:** Autenticado
- **Passos:** POST com {"nome":"   "}
- **Expected:** 400 (trim aplicado no servidor)
- **Tipo:** API

### CT-04: Nome > 50 chars → 400
- **Pre-condicao:** Autenticado
- **Passos:** POST com nome de 51 chars
- **Expected:** 400
- **Tipo:** API

### CT-05: Nome duplicado entre ativos → 409
- **Pre-condicao:** Bloco QA-01 ativo existe
- **Passos:** POST com {"nome":"Bloco QA-01"} segunda vez
- **Expected:** 409 + type=https://portabox.app/problems/canonical-conflict
- **Tipo:** API

### CT-06: Body JSON invalido → 400
- **Pre-condicao:** Autenticado
- **Passos:** POST com body malformado {nome: sem-aspas}
- **Expected:** 400
- **Tipo:** API

### CT-07: Sem cookie de auth → 401
- **Pre-condicao:** Nenhum cookie
- **Passos:** POST sem cookie
- **Expected:** 401
- **Tipo:** API

### CT-08: CondominioId inexistente → 404
- **Pre-condicao:** Autenticado como sindico A
- **Passos:** POST com GUID random (aaaabbbb-cccc-dddd-eeee-ffff00001111)
- **Expected:** 404 (contrato)
- **Tipo:** API

### CT-09: Sindico A em Tenant B → 403 ou 404
- **Pre-condicao:** Autenticado como sindico A
- **Passos:** POST com QA_TENANT_B_ID na rota
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-10: Persistencia no banco
- **Pre-condicao:** Bloco QA-01 criado no CT-01
- **Passos:** SELECT no banco por id do bloco criado
- **Expected:** ativo=t, tenant_id=QA_TENANT_A_ID, condominio_id=QA_TENANT_A_ID, nome=Bloco QA-01, criado_por=sindicoUserId, criado_em preenchido
- **Tipo:** Banco

### CT-11: Audit entry criada
- **Pre-condicao:** Bloco QA-01 criado
- **Passos:** SELECT em tenant_audit_log por event_kind=5 e blocoId
- **Expected:** registro com performed_by_user_id=sindicoA, metadata_json contendo nome e id do bloco
- **Tipo:** Banco

---

## Casos de Teste UI

### UT-01: Login, navegar estrutura, criar bloco via UI
- **Pre-condicao:** Sindico A com credenciais validas, frontend 5174
- **Passos:** Login > navegar /estrutura > clicar "Novo bloco" > preencher > submeter
- **Expected:** Modal fecha, bloco aparece na arvore
- **Tipo:** UI

### UT-02: Form com nome vazio → erro em pt-BR
- **Pre-condicao:** Modal de criacao aberto
- **Passos:** Submeter sem preencher nome
- **Expected:** Modal permanece aberto, mensagem de erro em pt-BR visivel
- **Tipo:** UI

### UT-03: Nome existente → toast/erro visivel
- **Pre-condicao:** Bloco QA-01 ativo existe
- **Passos:** Preencher "Bloco QA-01" e submeter
- **Expected:** Toast ou erro inline visivel indicando conflito
- **Tipo:** UI
