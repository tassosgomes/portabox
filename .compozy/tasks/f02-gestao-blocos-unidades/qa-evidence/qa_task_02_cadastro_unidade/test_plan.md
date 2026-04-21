# Plano de Testes — CF-02 Cadastro de Unidade

**Task ID:** qa_task_02
**Tipos:** API, Banco, UI

## Ambiente

- Backend: http://localhost:5272/api/v1
- Frontend (sindico): http://localhost:5174
- Tenant A: condominioId=4cce551d-4f18-474b-a42a-2deb6c2a0451
- Bloco pai: 88037273-d560-4415-a1e2-b45a00dc5be4 (Bloco QA-01)

## Casos de Teste — API

### CT-01: Happy path
- **Pre-condicao:** Bloco QA-01 ativo, sindico A autenticado
- **Passos:** POST /condominios/{tenantA}/blocos/{qa01}/unidades com {andar:1, numero:"101"}
- **Expected:** 201 + body {id, blocoId, andar:1, numero:"101", ativo:true, inativadoEm:null} + header Location
- **Tipo:** API

### CT-02: Andar 0 (terreo)
- **Pre-condicao:** Bloco QA-01 ativo
- **Passos:** POST com {andar:0, numero:"01"}
- **Expected:** 201 (terreo permitido conforme PRD)
- **Tipo:** API

### CT-03: Numero com sufixo minusculo
- **Pre-condicao:** Bloco QA-01 ativo
- **Passos:** POST com {andar:1, numero:"101a"}
- **Expected:** 201 + numero retornado em maiuscula "101A"
- **Tipo:** API

### CT-04: Numero com sufixo maiusculo
- **Pre-condicao:** Bloco QA-01 ativo
- **Passos:** POST com {andar:1, numero:"102B"}
- **Expected:** 201
- **Tipo:** API

### CT-05: Numero com 4 digitos
- **Pre-condicao:** Bloco QA-01 ativo
- **Passos:** POST com {andar:20, numero:"2001"}
- **Expected:** 201
- **Tipo:** API

### CT-06: Numero invalido (>5 chars, 5 digitos + sufixo)
- **Pre-condicao:** Bloco QA-01 ativo
- **Passos:** POST com {andar:1, numero:"12345X"}
- **Expected:** 400 + ValidationProblemDetails
- **Tipo:** API

### CT-07: Numero so letra
- **Pre-condicao:** Bloco QA-01 ativo
- **Passos:** POST com {andar:1, numero:"A"}
- **Expected:** 400 + ValidationProblemDetails
- **Tipo:** API

### CT-08: Numero com simbolos
- **Pre-condicao:** Bloco QA-01 ativo
- **Passos:** POST com {andar:1, numero:"101-B"}
- **Expected:** 400 + ValidationProblemDetails
- **Tipo:** API

### CT-09: Andar negativo
- **Pre-condicao:** Bloco QA-01 ativo
- **Passos:** POST com {andar:-1, numero:"01"}
- **Expected:** 400 + ValidationProblemDetails
- **Tipo:** API

### CT-10: Conflito canonico (duplicata de tripla)
- **Pre-condicao:** Bloco QA-01 ativo
- **Passos:** POST {andar:5, numero:"501"} — sucesso; POST novamente mesmos dados
- **Expected:** 1a requisicao: 201; 2a requisicao: 409 + type canonical-conflict
- **Tipo:** API

### CT-11: Bloco inexistente
- **Pre-condicao:** GUID random como blocoId
- **Passos:** POST com blocoId=aaaabbbb-cccc-dddd-eeee-ffff00001111
- **Expected:** 404
- **Tipo:** API

### CT-12: Bloco inativo
- **Pre-condicao:** Criar bloco temporario "Bloco Temp Inativo QA", inativa-lo, tentar criar unidade nele
- **Passos:** POST criar bloco temp; POST :inativar; POST unidade no bloco inativo
- **Expected:** 422 (bloco inativo nao aceita novas unidades)
- **Tipo:** API

### CT-13: Sindico A em tenant B
- **Pre-condicao:** Cookie de sindico A (tenant A)
- **Passos:** POST em condominioId do tenant B
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-14: Sem autenticacao
- **Pre-condicao:** Nenhuma
- **Passos:** POST sem cookie de autenticacao
- **Expected:** 401
- **Tipo:** API

### CT-15: Persistencia DB
- **Pre-condicao:** CT-01 executado com sucesso
- **Passos:** SELECT id, tenant_id, bloco_id, andar, numero, ativo, criado_em, criado_por FROM unidade WHERE id='<ct01>'
- **Expected:** Todos os campos corretos (tenant_id=tenantA, bloco_id=qa01, andar=1, numero="101", ativo=t)
- **Tipo:** Banco

### CT-16: Audit entry
- **Pre-condicao:** CT-01 executado
- **Passos:** SELECT event_kind, metadata_json FROM tenant_audit_log WHERE event_kind=9 ORDER BY occurred_at DESC LIMIT 5
- **Expected:** Registro com event_kind=9 (UnidadeCriada) e metadata_json com id e numero da unidade
- **Tipo:** Banco

## Casos de Teste — UI

### UT-01: Adicionar unidade via UI
- **Pre-condicao:** Sindico A logado no sindico app, Bloco QA-01 visivel na arvore
- **Passos:** Navegar para estrutura; selecionar Bloco QA-01; clicar "Adicionar unidade"; preencher andar=10, numero=1001; submeter
- **Expected:** Modal fecha, unidade 1001 aparece na arvore sob Bloco QA-01 andar 10
- **Tipo:** UI

### UT-02: Validacao de andar negativo
- **Pre-condicao:** Modal de adicionar unidade aberto
- **Passos:** Preencher andar=-1, numero=999; submeter
- **Expected:** Mensagem de erro de validacao visivel (inline no campo andar)
- **Tipo:** UI

### UT-03: Validacao de numero invalido
- **Pre-condicao:** Modal de adicionar unidade aberto
- **Passos:** Preencher andar=1, numero="XX"; submeter
- **Expected:** Mensagem de erro de validacao visivel (inline no campo numero)
- **Tipo:** UI

### UT-04: Normalizacao de numero para maiuscula
- **Pre-condicao:** Modal de adicionar unidade aberto
- **Passos:** Preencher andar=10, numero="1002a"; submeter
- **Expected:** Unidade aparece na arvore com numero "1002A" (maiuscula)
- **Tipo:** UI
