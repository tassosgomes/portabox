# Plano de Testes — CF-03 Edicao do Nome de Bloco

**Task ID:** qa_task_03
**Data:** 2026-04-20
**Tipos:** API, Banco, UI

## Variaveis de Ambiente

- BASE_URL: http://localhost:5272/api/v1
- TENANT_A_ID: 4cce551d-4f18-474b-a42a-2deb6c2a0451
- TENANT_B_ID: 23fb219d-460a-4eee-a9e7-308d7665350b
- BLOCO_QA02_ID: 4c936a72-fc12-4f32-809f-3f290c4bc8ae (Bloco QA-02)
- BLOCO_UI_QA01_ID: 9aab2b47-1672-4686-ac12-6ce39b4c0f50 (Bloco UI-QA-01)
- BLOCO_B_ID: 849b6750-0cca-4cef-a798-5d90d04246ff (Bloco B-01 em Tenant B)

## Casos de Teste — API

### CT-01: Happy path — renomear Bloco QA-02
- **Pre-condicao:** Bloco QA-02 existe e esta ativo
- **Passos:** PATCH /condominios/{tenantA}/blocos/{blocoQA02} com nome "Bloco QA-02 Renomeado"
- **Expected:** HTTP 200, body com nome = "Bloco QA-02 Renomeado", ativo = true
- **Tipo:** API

### CT-02: Mesmo nome (422)
- **Pre-condicao:** Bloco QA-02 tem nome "Bloco QA-02 Renomeado" (apos CT-01)
- **Passos:** PATCH com mesmo nome "Bloco QA-02 Renomeado"
- **Expected:** HTTP 422, type contendo "invalid-transition"
- **Tipo:** API

### CT-03: Novo nome vazio
- **Pre-condicao:** Bloco QA-02 ativo
- **Passos:** PATCH com body {"nome": ""}
- **Expected:** HTTP 400, errors.nome presente
- **Tipo:** API

### CT-04: Nome > 50 chars
- **Pre-condicao:** Bloco QA-02 ativo
- **Passos:** PATCH com nome de 51 caracteres
- **Expected:** HTTP 400
- **Tipo:** API

### CT-05: Conflito canonico (409)
- **Pre-condicao:** Bloco QA-01 existe com nome "Bloco QA-01"
- **Passos:** PATCH Bloco QA-02 com nome "Bloco QA-01"
- **Expected:** HTTP 409, type contendo "canonical-conflict"
- **Tipo:** API

### CT-06: Bloco inexistente (404)
- **Pre-condicao:** nenhuma
- **Passos:** PATCH com blocoId UUID aleatorio inexistente
- **Expected:** HTTP 404
- **Tipo:** API

### CT-07: Sem autenticacao (401)
- **Pre-condicao:** nenhuma
- **Passos:** PATCH sem cookie de sessao
- **Expected:** HTTP 401
- **Tipo:** API

### CT-08: Cross-tenant (403 ou 404)
- **Pre-condicao:** Sindico A autenticado; bloco em Tenant B existe
- **Passos:** Sindico A tenta PATCH /condominios/{tenantB}/blocos/{blocoB}
- **Expected:** HTTP 403 ou 404
- **Tipo:** API

### CT-09: Renomear bloco inativo (422)
- **Pre-condicao:** nenhuma
- **Passos:** criar "Bloco Temp Rename QA", inativar, tentar renomear
- **Expected:** HTTP 422, type contendo "invalid-transition"
- **Tipo:** API

### CT-10: Persistencia DB
- **Pre-condicao:** CT-01 executado com sucesso
- **Passos:** SELECT id, nome, ativo FROM bloco WHERE id = '4c936a72-fc12-4f32-809f-3f290c4bc8ae'
- **Expected:** nome = "Bloco QA-02 Renomeado" (ou nome mais recente apos CT-12)
- **Tipo:** Banco

### CT-11: Audit entry com diff (event_kind=6)
- **Pre-condicao:** CT-01 executado com sucesso
- **Passos:** SELECT event_kind, metadata_json FROM tenant_audit_log WHERE event_kind = 6 ORDER BY occurred_at DESC LIMIT 3
- **Expected:** registro com event_kind=6, metadata_json contendo nomeAntes e nomeDepois
- **Tipo:** Banco

### CT-12: Rename idempotente (sequencia)
- **Pre-condicao:** CT-01 executado
- **Passos:** PATCH Bloco QA-02 para "Bloco QA-02 V3"
- **Expected:** HTTP 200, nome = "Bloco QA-02 V3"; audit deve ter 2 entries distintas de rename
- **Tipo:** API + Banco

## Casos de Teste — UI

### UT-01: Renomear Bloco UI-QA-01 via interface
- **Pre-condicao:** Frontend em http://localhost:5173, Sindico A logado
- **Passos:** navegar para estrutura, clicar Acoes > Renomear em Bloco UI-QA-01, inserir novo nome, submeter
- **Expected:** novo nome aparece na arvore imediatamente
- **Tipo:** UI

### UT-02: Renomear com nome vazio
- **Pre-condicao:** Modal de renomear aberto
- **Passos:** limpar campo nome, submeter
- **Expected:** erro de validacao visivel no campo
- **Tipo:** UI

### UT-03: Renomear para nome conflitante (409)
- **Pre-condicao:** Existe outro bloco ativo com nome diferente
- **Passos:** tentar renomear UI-QA-01 para nome de bloco ja existente
- **Expected:** toast/mensagem de erro visivel indicando conflito
- **Tipo:** UI
