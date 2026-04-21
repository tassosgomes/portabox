# Plano de Testes — CF-07 Navegacao em Arvore Hierarquica

**Task ID:** qa_task_07
**Tipos:** API, Banco, UI

## Estado DB confirmado antes dos testes

### Blocos Tenant A (4cce551d-...)
| nome | ativo |
|------|-------|
| Bloco Conflito X QA | false |
| Bloco Conflito X QA | true |
| Bloco QA-01 | true |
| Bloco QA-02 V3 | true |
| Bloco QA-03 | true |
| Bloco Temp Cascata QA | true |
| Bloco Temp Inativo QA | true |
| Bloco Temp Pai Inativo QA | false |
| Bloco Temp Rename QA | true |
| Bloco UI Inativar QA | true |
| Bloco UI-QA-01 Editado | true |

### Unidades Tenant A
- Bloco QA-01 (88037273): 13 ativas + 1 inativa (andar 50 num 501)
- Bloco Temp Cascata (ac7b8af5): 1 ativa (andar 1 num 101)
- Bloco Temp Pai Inativo (bb643a2a, inativo): 1 unidade ativa (andar 1 num 101)

---

## Casos de Teste

### CT-01: Estrutura default sem flag (apenas ativos)
- **Pre-condicao:** Sindico A autenticado via cookie
- **Passos:** GET /api/v1/condominios/4cce551d-.../estrutura (sem query param)
- **Expected:** HTTP 200; nenhum bloco com ativo=false; nenhuma unidade com ativo=false; blocos em ordem alfabetica; andares numericamente crescentes; geradoEm em ISO 8601 UTC
- **Tipo:** API

### CT-02: Estrutura com includeInactive=true
- **Pre-condicao:** Sindico A autenticado
- **Passos:** GET .../estrutura?includeInactive=true
- **Expected:** HTTP 200; inclui blocos inativos (Bloco Conflito X QA inativo, Bloco Temp Pai Inativo QA); inclui unidade inativa (andar 50 num 501 em Bloco QA-01)
- **Tipo:** API

### CT-03: Estrutura com includeInactive=false
- **Pre-condicao:** Sindico A autenticado
- **Passos:** GET .../estrutura?includeInactive=false
- **Expected:** HTTP 200; mesmo comportamento que CT-01 (nenhum inativo)
- **Tipo:** API

### CT-04: Sem autenticacao
- **Pre-condicao:** Nenhuma
- **Passos:** GET .../estrutura sem cookies/token
- **Expected:** HTTP 401
- **Tipo:** API

### CT-05: CondominioId inexistente
- **Pre-condicao:** Sindico A autenticado
- **Passos:** GET /api/v1/condominios/00000000-0000-0000-0000-000000000001/estrutura
- **Expected:** HTTP 404 (ou 403)
- **Tipo:** API

### CT-06: Sindico A requisita estrutura do Tenant B
- **Pre-condicao:** Sindico A autenticado, usando condominioId do Tenant B
- **Passos:** GET /api/v1/condominios/23fb219d-.../estrutura com cookies de Sindico A
- **Expected:** HTTP 403 ou 404
- **Tipo:** API

### CT-07: Tempo de resposta (3 chamadas)
- **Pre-condicao:** Sindico A autenticado
- **Passos:** 3x GET .../estrutura; medir tempo de cada resposta
- **Expected:** tempo medio < 1s (tenant com << 100 unidades)
- **Tipo:** API

### CT-08: Consistencia API vs DB
- **Pre-condicao:** Sindico A autenticado
- **Passos:** query direta no DB (blocos + contagem unidades) vs estrutura?includeInactive=true
- **Expected:** contagens consistentes; todos blocos presentes
- **Tipo:** API + Banco

### CT-09: Ordenacao alfabetica dos blocos
- **Pre-condicao:** Sindico A autenticado
- **Passos:** GET .../estrutura?includeInactive=true; extrair lista de nomes de blocos
- **Expected:** lista em ordem alfabetica (documentar politica case)
- **Tipo:** API

### CT-10: Ordenacao semantica de unidades (criar 4 unidades andar 99)
- **Pre-condicao:** Sindico A autenticado; Bloco QA-01 ativo
- **Passos:** criar unidades andar=99 com numeros "99", "101", "101A", "102"; GET estrutura; validar ordem no andar 99
- **Expected:** ordem exata: 99, 101, 101A, 102 (semantica: numerico < alfanumerico crescente)
- **Tipo:** API

### UT-01: Arvore renderiza visualmente
- **Pre-condicao:** Sindico A logado na UI
- **Passos:** navegar para pagina de estrutura; verificar que arvore exibe blocos, andares, unidades em ordem
- **Expected:** arvore visivel; screenshot capturado
- **Tipo:** UI

### UT-02: Expandir/colapsar bloco via click
- **Pre-condicao:** Sindico A logado; arvore visivel
- **Passos:** clicar no icone de expansao de um bloco; verificar que filhos aparecem; clicar novamente; filhos somem
- **Expected:** expand/collapse funcional
- **Tipo:** UI

### UT-03: Navegacao por teclado
- **Pre-condicao:** Sindico A logado; arvore visivel
- **Passos:** focar em bloco; pressionar seta direita; pressionar seta esquerda
- **Expected:** seta direita expande; seta esquerda colapsa (PRD: acessibilidade)
- **Tipo:** UI

### UT-04: Toggle filtro incluir inativos
- **Pre-condicao:** Sindico A logado; arvore visivel
- **Passos:** marcar toggle "Incluir inativos"; verificar que inativos aparecem com visual diferenciado; desmarcar; verificar que somem
- **Expected:** inativos aparecem/somem conforme toggle
- **Tipo:** UI

### UT-05: Painel lateral ao clicar num bloco
- **Pre-condicao:** Sindico A logado; arvore visivel
- **Passos:** clicar num bloco; verificar painel lateral
- **Expected:** painel lateral abre com detalhes; informacao de auditoria presente ("Criada por... em...")
- **Tipo:** UI

### UT-06: Responsividade tablet (768x1024)
- **Pre-condicao:** Sindico A logado
- **Passos:** redimensionar viewport para 768x1024; navegar para estrutura
- **Expected:** arvore funcional em tablet
- **Tipo:** UI

### UT-07: Empty state
- **Pre-condicao:** (verificar se Tenant B tem blocos)
- **Passos:** navegar para estrutura de Tenant B (se vazia); verificar empty state
- **Expected:** empty state exibido ou BLOCKED se Tenant B ja tem blocos
- **Tipo:** UI
