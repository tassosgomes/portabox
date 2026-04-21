# Plano de Testes — CF-04 Inativacao de Unidade

**Task ID:** qa_task_04
**Tipos:** API | Banco | UI

## Contexto

- Base URL: http://localhost:5272
- Tenant A: 4cce551d-4f18-474b-a42a-2deb6c2a0451
- Bloco QA-01: 88037273-d560-4415-a1e2-b45a00dc5be4
- Endpoint: POST /api/v1/condominios/{condominioId}/blocos/{blocoId}/unidades/{unidadeId}:inativar

## Casos de Teste

### CT-01: Happy path — inativar unidade existente
- **Pre-condicao:** Criar unidade nova andar=7 numero="701" em Bloco QA-01
- **Passos:** POST /:inativar na unidade recem-criada (sem body)
- **Expected:** 200, ativo=false, inativadoEm != null
- **Tipo:** API

### CT-02: Unidade ja inativa (transicao invalida)
- **Pre-condicao:** Unidade do CT-01 ja inativa
- **Passos:** POST /:inativar novamente
- **Expected:** 422, type=invalid-transition
- **Tipo:** API

### CT-03: Unidade inexistente
- **Pre-condicao:** GUID aleatorio como unidadeId
- **Passos:** POST /:inativar com unidadeId invalido
- **Expected:** 404
- **Tipo:** API

### CT-04: BlocoId errado no path
- **Pre-condicao:** Unidade real do QA-01, path com blocoId de outro bloco
- **Passos:** POST /:inativar com blocoId diferente do bloco da unidade
- **Expected:** 404 (unidade nao pertence ao bloco informado) ou 400
- **Tipo:** API

### CT-05: Sem autenticacao
- **Pre-condicao:** Nenhuma
- **Passos:** POST /:inativar sem cookie de sessao
- **Expected:** 401
- **Tipo:** API

### CT-06: Cross-tenant — sindico A tenta inativar unidade de tenant B
- **Pre-condicao:** Criar unidade temporaria em tenant B
- **Passos:** Sindico A tenta POST /:inativar na unidade de tenant B
- **Expected:** 403 ou 404
- **Tipo:** API

### CT-07: Persistencia DB
- **Pre-condicao:** CT-01 executado (unidade inativada)
- **Passos:** SELECT id, ativo, inativado_em, inativado_por FROM unidade WHERE id = <ct01_id>
- **Expected:** ativo=false, inativado_em preenchido, inativado_por = userId do sindico A
- **Tipo:** Banco

### CT-08: Audit entry event_kind=10
- **Pre-condicao:** CT-01 executado
- **Passos:** SELECT event_kind, metadata_json FROM tenant_audit_log WHERE event_kind=10 ORDER BY occurred_at DESC LIMIT 3
- **Expected:** Pelo menos uma entrada com event_kind=10 (UnidadeInativada) referenciando a unidade do CT-01
- **Tipo:** Banco

### CT-09: GET estrutura sem includeInactive oculta unidade inativa
- **Pre-condicao:** CT-01 executado (unidade inativa)
- **Passos:** GET estrutura do condominio sem parametro includeInactive
- **Expected:** Unidade CT-01 nao aparece na resposta
- **Tipo:** API

### CT-10: GET estrutura com includeInactive=true mostra unidade inativa
- **Pre-condicao:** CT-01 executado (unidade inativa)
- **Passos:** GET estrutura com includeInactive=true
- **Expected:** Unidade CT-01 aparece com ativo=false
- **Tipo:** API

### CT-11: Moradores associados continuam ligados (BLOCKED)
- **Pre-condicao:** Feature F03 (morador) implementada — NAO disponivel
- **Passos:** N/A
- **Expected:** Moradores permanecem vinculados apos inativacao da unidade
- **Tipo:** API
- **Status:** BLOCKED — F03 nao implementado

### CT-12: Inativacao nao afeta outras unidades do bloco
- **Pre-condicao:** CT-01 executado, outras unidades ativas no QA-01
- **Passos:** GET estrutura sem includeInactive, verificar que outras unidades (ex: 101, 01, etc.) permanecem ativas
- **Expected:** Outras unidades ativas continuam aparecendo normalmente
- **Tipo:** API

## Casos UI (Playwright)

### UT-01: Inativar unidade via modal de confirmacao
- **Pre-condicao:** Login sindico A, unidade ativa na arvore
- **Passos:** Selecionar unidade, clicar "Inativar", verificar modal com copy sobre moradores, confirmar
- **Expected:** Modal aparece, ao confirmar unidade some da arvore (filtro default sem inativas)
- **Tipo:** UI

### UT-02: Toggle "Incluir inativas" mostra unidade inativada
- **Pre-condicao:** UT-01 executado
- **Passos:** Ativar toggle "Incluir inativas"
- **Expected:** Unidade reaparece com visual descontraste ou badge de inativa
- **Tipo:** UI

### UT-03: Cancelar modal mantem unidade ativa
- **Pre-condicao:** Unidade ativa na arvore
- **Passos:** Clicar "Inativar", modal aparece, clicar "Cancelar"
- **Expected:** Modal fecha, unidade permanece visivel e ativa na arvore
- **Tipo:** UI
