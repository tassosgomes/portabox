# Plano de Testes — CF-05 Inativacao de Bloco

**Task ID:** qa_task_05
**Data:** 2026-04-20
**Tipos:** API, Banco, UI

## Recursos fixos

- BLOCO_QA03_ID = ff2ed42b-e9d9-426e-81fa-6bd51b767174
- TENANT_A_ID   = 4cce551d-4f18-474b-a42a-2deb6c2a0451
- BASE_URL       = http://localhost:5272/api/v1
- Cookies        = qa_task_01_cadastro_bloco/cookies_sindico_a.txt

## Casos de Teste API

### CT-01: Happy path — inativar Bloco QA-03
- **Pre-condicao:** Bloco ff2ed42b... ativo no tenant A
- **Passos:** POST /api/v1/condominios/{tenantA}/blocos/{blocoQA03}:inativar com cookie sindico A
- **Expected:** 200, body { id, condominioId, nome, ativo:false, inativadoEm: timestamp ISO8601 }
- **Tipo:** API

### CT-02: Bloco ja inativo → 422 invalid-transition
- **Pre-condicao:** Bloco QA-03 ja inativo (pos CT-01)
- **Passos:** POST :inativar novamente
- **Expected:** 422, type contains "invalid-transition"
- **Tipo:** API

### CT-03: Bloco inexistente → 404
- **Pre-condicao:** nenhuma
- **Passos:** POST /blocos/00000000-0000-0000-0000-000000000099:inativar
- **Expected:** 404
- **Tipo:** API

### CT-04: Sem autenticacao → 401
- **Pre-condicao:** nenhuma
- **Passos:** POST sem cookie/token
- **Expected:** 401
- **Tipo:** API

### CT-05: Cross-tenant — sindico A inativar bloco de tenant B → 403 ou 404
- **Pre-condicao:** bloco de outro tenant
- **Passos:** Sindico A POST usando tenant B ID no path
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-06: Persistencia DB — campos ativo, inativado_em, inativado_por
- **Pre-condicao:** CT-01 executado com sucesso
- **Passos:** SELECT id, ativo, inativado_em, inativado_por FROM bloco WHERE id = 'ff2ed42b-...'
- **Expected:** ativo=false, inativado_em preenchido, inativado_por=sindico user_id
- **Tipo:** Banco

### CT-07: Audit entry event_kind=7 (BlocoInativado)
- **Pre-condicao:** CT-01 executado com sucesso
- **Passos:** SELECT event_kind, metadata_json FROM tenant_audit_log WHERE event_kind=7 ORDER BY occurred_at DESC LIMIT 3
- **Expected:** registro com event_kind=7, metadata_json contendo bloco id/nome
- **Tipo:** Banco

### CT-08: GET estrutura sem includeInactive oculta bloco inativo
- **Pre-condicao:** CT-01 executado com sucesso
- **Passos:** GET /condominios/{tenantA}/estrutura (sem includeInactive)
- **Expected:** 200, blocoQA03 NAO aparece na lista
- **Tipo:** API

### CT-09: GET estrutura com includeInactive=true mostra bloco inativo
- **Pre-condicao:** CT-01 executado com sucesso
- **Passos:** GET /condominios/{tenantA}/estrutura?includeInactive=true
- **Expected:** 200, blocoQA03 aparece com ativo:false
- **Tipo:** API

### CT-10: Sem cascata — unidades permanecem ativas apos inativacao de bloco
- **Pre-condicao:** criar bloco temporario "Bloco Temp Cascata QA", criar 1 unidade, inativar bloco
- **Passos:** criar bloco, criar unidade, inativar bloco, SELECT ativo FROM unidade WHERE bloco_id=:newBlocoId
- **Expected:** unidade permanece com ativo=true (sem cascata automatica)
- **Tipo:** API + Banco

### CT-11: Criar unidade em bloco inativo → 422 invalid-transition
- **Pre-condicao:** Bloco QA-03 inativo (pos CT-01)
- **Passos:** POST /blocos/{blocoQA03}/unidades com payload valido
- **Expected:** 422, type contains "invalid-transition"
- **Tipo:** API

## Casos de Teste UI

### UT-01: Inativar bloco via UI — modal confirma, bloco some da arvore padrao
- **Pre-condicao:** Login sindico A, arvore com bloco ativo
- **Passos:** criar "Bloco UI Inativar QA" via API, navegar /estrutura, clicar Acoes > Inativar, confirmar modal
- **Expected:** bloco some da arvore (default sem inativos)
- **Tipo:** UI

### UT-02: Toggle "Mostrar inativos" faz bloco inativo reaparecer
- **Pre-condicao:** UT-01 executado
- **Passos:** marcar checkbox "Mostrar inativos"
- **Expected:** bloco inativo aparece (visual diferenciado ou badge)
- **Tipo:** UI

### UT-03: Modal de confirmacao tem copy pt-BR explicando nao-cascata
- **Pre-condicao:** Login sindico A, bloco ativo na arvore
- **Passos:** clicar Acoes > Inativar, observar modal
- **Expected:** modal visivel com texto pt-BR mencionando que unidades nao sao inativadas em cascata / precisam ser inativadas separadamente
- **Tipo:** UI
