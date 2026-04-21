# Plano de Testes — CF-06 Reativacao de Bloco ou Unidade (RETESTE)

**Task ID:** qa_task_06_reativacao (RETESTE)
**Versao:** 2 (Reteste em 2026-04-21)
**Tipos:** API, Banco, UI

## Falhas Anteriores a Reverificar

| Caso | Falha Anterior | Expectativa no Reteste |
|------|----------------|------------------------|
| CT-02 | Retornou 409 em vez de 422 ao reativar bloco ja ativo | Verificar se foi corrigido para 422 |
| CT-07 | Retornou 400 em vez de 404 para unidade inexistente | Verificar se foi corrigido para 404 |
| CT-11 | Retornou 200 ao reativar unidade com bloco pai inativo (critico) | Verificar se foi corrigido para 422 |

## Estado Inicial do Banco (antes do reteste)

- Bloco QA-03 (ff2ed42b): ATIVO
- Bloco Conflito X QA (f32f5862): INATIVO
- Bloco Temp Pai Inativo QA (bb643a2a): INATIVO
- Unidade 501 andar=50 (79e92757): INATIVA
- Unidade CT-11 (a2c82b48): ATIVA em bloco INATIVO — estado inconsistente legado

## Casos de Teste

### CT-01: Happy path — criar bloco, inativar, reativar
- Pre-condicao: Bloco "Bloco Retest Reativar QA" nao existe
- Passos: criar, inativar, reativar
- Expected: 200, ativo=true, inativadoEm=null
- Tipo: API

### CT-02: Bloco ja ativo — esperar 422 (REVERIFICACAO DE FALHA anterior: 409)
- Pre-condicao: Bloco de CT-01 esta ativo
- Expected: 422, type=invalid-transition
- Tipo: API

### CT-03: Bloco inexistente — 404
- Expected: 404, type=not-found
- Tipo: API

### CT-04: Conflito canonico na reativacao de bloco
- Criar A, criar B, inativar A, renomear B para nome de A, reativar A
- Expected: 409, type=canonical-conflict
- Tipo: API

### CT-05: Happy path — criar unidade, inativar, reativar
- Pre-condicao: Bloco QA-01 ativo, andar=60, numero=601
- Expected: 200, ativo=true, inativadoEm=null
- Tipo: API

### CT-06: Unidade ja ativa — 422
- Expected: 422, type=invalid-transition
- Tipo: API

### CT-07: Unidade inexistente — 404 (REVERIFICACAO DE FALHA anterior: 400)
- Expected: 404, type=not-found
- Tipo: API

### CT-08: Conflito canonico em reativacao de unidade
- Criar X {andar:61, numero:611}, inativar, criar Y igual, reativar X
- Expected: 409, type=canonical-conflict
- Tipo: API

### CT-09: Sem autenticacao — 401
- Expected: 401
- Tipo: API

### CT-10: Cross-tenant — 403
- Expected: 403 ou 404
- Tipo: API

### CT-11: Unidade reativada com bloco pai inativo (REVERIFICACAO DE FALHA CRITICO anterior: 200)
- Criar bloco novo, criar unidade, inativar unidade, inativar bloco, reativar unidade
- Expected: 422, type=invalid-transition
- Tipo: API

### CT-12: Persistencia DB pos-reativacao
- Expected: ativo=t, inativado_em=NULL, inativado_por=NULL
- Tipo: Banco

### CT-13: Audit entries
- Expected: event_kind=8 (BlocoReativado), event_kind=11 (UnidadeReativada)
- Tipo: Banco

### CT-14: Estado inconsistente legado (apenas observacao)
- Query para unidade a2c82b48 — apenas documentar
- Tipo: Banco

### UT-01: Toggle inativos, reativar bloco via UI
- Tipo: UI

### UT-02: Reativar unidade inativa via UI
- Tipo: UI

### UT-03: Conflito canonico — toast de erro visivel
- Tipo: UI
