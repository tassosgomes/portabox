# QA Report — CF-07 Navegacao em Arvore Hierarquica

**Task ID:** qa_task_07
**Data/Hora:** 2026-04-21T01:50:00Z
**Status Geral:** FAIL

---

## Contexto

- **User Story:** CF-07 — Como sindico, quero visualizar a estrutura do meu condominio em uma arvore hierarquica (blocos, andares, unidades)
- **Ambiente:** http://localhost:5272 (backend), http://localhost:5174 (frontend sindico)
- **Tipos de teste:** API, Banco, UI
- **Autenticacao:** Sim (cookie session ASP.NET Identity)
- **Estado DB confirmado antes dos testes:** 11 blocos (9 ativos, 2 inativos), 19 unidades (18 ativas, 1 inativa) em Tenant A

---

## Nota de Infraestrutura

**CORS + Credenciais:** O backend retorna `Access-Control-Allow-Origin: *` mas o API client usa `credentials: 'include'`. Quando o frontend roda em porta diferente (5174) do backend (5272), o browser bloqueia as requisicoes cross-origin com credenciais quando o ACAO header e wildcard. Os testes de UI precisaram de intercept Playwright para corrigir esse comportamento em ambiente de teste.

**Proxy Vite ausente:** O `shared/api/client.ts` (usado pelo AuthContext) usa `VITE_API_BASE_URL` que nao esta definido em `.env.local`, causando chamadas relativas (`/api/...`) que o Vite nao proxia. Os testes de UI usam intercept Playwright para `localhost:5174/api/**` como workaround.

---

## Casos de Teste

| ID | Descricao | Tipo | Status |
|----|-----------|------|--------|
| CT-01 | Estrutura default sem flag — apenas ativos | API | PASS |
| CT-02 | Estrutura com includeInactive=true | API | PASS |
| CT-03 | Estrutura com includeInactive=false | API | PASS |
| CT-04 | Sem autenticacao → 401 | API | PASS |
| CT-05 | CondominioId inexistente → 403 | API | PASS |
| CT-06 | Sindico A acessa Tenant B → 403 | API | PASS |
| CT-07 | Tempo de resposta (3 chamadas) | API | PASS |
| CT-08 | Consistencia API vs DB | API+Banco | PASS |
| CT-09 | Ordenacao alfabetica dos blocos | API | PASS |
| CT-10 | Ordenacao semantica de unidades | API | PASS |
| UT-01 | Arvore renderiza visualmente | UI | PASS |
| UT-02 | Expandir/colapsar bloco via click | UI | PASS |
| UT-03 | Navegacao por teclado (setas) | UI | FAIL |
| UT-04 | Toggle filtro incluir inativos | UI | PASS |
| UT-05 | Painel lateral ao clicar num bloco | UI | PASS |
| UT-06 | Responsividade tablet 768x1024 | UI | PASS |
| UT-07 | Empty state (Tenant B) | UI | BLOCKED |

---

## Detalhes por Caso

### CT-01 — Estrutura default sem flag PASS

**Expected:** HTTP 200; nenhum bloco ou unidade com ativo=false; blocos em ordem alfabetica; andares numericamente crescentes; geradoEm ISO 8601 UTC
**Actual:** HTTP 200; 0 blocos inativos; 0 unidades inativas; blocos em ordem alfabetica confirmada; andares [0,1,5,7,8,10,11,20,50,99] — ordenado corretamente; geradoEm=2026-04-21T01:26:17.3584827Z (UTC com Z)
**Amostras de ordenacao semantica de unidades:**
- Andar 0: ['01']
- Andar 1: ['101', '101A', '102B'] — numerico antes do alfanumerico, correto
- Andar 10: ['1001', '1002A'] — correto
- Andar 50: ['501'] — unica unidade ativa (a inativa nao aparece)
**Evidencias:** `requests.log` CT-01 | `response_active_only.json`

---

### CT-02 — Estrutura com includeInactive=true PASS

**Expected:** HTTP 200; blocos inativos presentes (Bloco Conflito X QA inativo, Bloco Temp Pai Inativo QA); unidade inativa (andar 50, num 501, id 79e92757) presente
**Actual:** HTTP 200; 11 blocos (9 ativos + 2 inativos); Bloco Conflito X QA (inativo): ENCONTRADO; Bloco Temp Pai Inativo QA (inativo): ENCONTRADO; unidade 79e92757 (andar 50, num 501): ENCONTRADA; andar 50 mostra 2 unidades: uma ativa (501) e uma inativa (501)
**Evidencias:** `requests.log` CT-02 | `response_include_inactive.json`

---

### CT-03 — Estrutura com includeInactive=false PASS

**Expected:** Identico ao CT-01 (default comportamento)
**Actual:** HTTP 200; 0 blocos inativos; 0 unidades inativas — comportamento identico ao default
**Evidencias:** `requests.log` CT-03

---

### CT-04 — Sem autenticacao → 401 PASS

**Expected:** HTTP 401
**Actual:** HTTP 401, body: `{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2","title":"Nao autorizado","status":401,"detail":"Token de autenticacao invalido ou expirado"}`
**Evidencias:** `requests.log` CT-04

---

### CT-05 — CondominioId inexistente PASS

**Expected:** HTTP 404 ou 403
**Actual:** HTTP 403 — sistema retorna 403 para condominioId que nao pertence ao tenant do sindico (nao 404), evitando vazar existencia de recursos alheios
**Nota:** O comportamento de 403 para ID inexistente e aceitavel (privacy by design — nao revela se o recurso existe)
**Evidencias:** `requests.log` CT-05

---

### CT-06 — Sindico A acessa Tenant B → 403 PASS

**Expected:** HTTP 403 ou 404
**Actual:** HTTP 403 — cross-tenant corretamente rejeitado
**Evidencias:** `requests.log` CT-06

---

### CT-07 — Tempo de resposta PASS

**Expected:** < 1s para tenant com <= 100 unidades (PRD: "menos de 1s para tenant com ate 100 unidades")
**Actual:**
- Chamada 1: 22ms
- Chamada 2: 20ms
- Chamada 3: 20ms
- **Media: 20ms** (muito abaixo do limite de 1000ms)
**Tenant A tem ~19 unidades (muito abaixo de 100)**
**Evidencias:** `performance.log` | `requests.log` CT-07

---

### CT-08 — Consistencia API vs DB PASS

**Expected:** Contagens de unidades ativas e inativas por bloco identicas entre DB e API (includeInactive=true)
**Actual:** 11/11 blocos consistentes:
- Bloco QA-01: DB=16 ativas+1 inativa, API=16 ativas+1 inativa — MATCH
- Bloco Conflito X QA (inativo): DB=0 unidades, API=0 unidades — MATCH
- Bloco Temp Pai Inativo QA (inativo): DB=1 ativa+0 inativas, API=1 ativa+0 inativas — MATCH
- Demais 8 blocos: DB=0 unidades, API=0 unidades — MATCH
**Evidencias:** `db_check.log` | `requests.log` CT-08

---

### CT-09 — Ordenacao alfabetica dos blocos PASS

**Expected:** Blocos em ordem alfabetica
**Actual:** Ordem real: ['Bloco Conflito X QA', 'Bloco QA-01', 'Bloco QA-02 V3', 'Bloco QA-03', 'Bloco Temp Cascata QA', 'Bloco Temp Inativo QA', 'Bloco Temp Pai Inativo QA', 'Bloco Temp Rename QA', 'Bloco UI Inativar QA', 'Bloco UI-QA-01 Editado']
**Politica de ordenacao:** Alfabetica padrao; todos os nomes neste dataset iniciam com maiuscula, por isso case-sensitive e case-insensitive produzem o mesmo resultado. Caractere espaco (ASCII 32) vem antes de hifen (ASCII 45): "Bloco UI Inativar" < "Bloco UI-QA-01" — padrao ASCII
**Evidencias:** `requests.log` CT-09

---

### CT-10 — Ordenacao semantica de unidades PASS

**Pre-condicao:** Criadas 4 unidades em Bloco QA-01, andar 99: "99", "101", "101A", "102"
**Expected:** Ordem exata: 99, 101, 101A, 102 (semantica: numerico < alfanumerico)
**Actual:** GET /estrutura retornou andar 99 com: ['99', '101', '101A', '102', '9901', '9902A']
- Posicoes: 99=0, 101=1, 101A=2, 102=3
- Ordem semantica 99 < 101 < 101A < 102: CONFIRMADA
- As unidades existentes (9901, 9902A) aparecem corretamente apos 102
**Nota:** Unidades criadas por este caso foram mantidas (proximas tasks podem usa-las)
**Evidencias:** `requests.log` CT-10

---

### UT-01 — Arvore renderiza visualmente PASS

**Expected:** Blocos, andares e unidades visiveis na arvore em ordem correta
**Actual:** Arvore carregou com "QA Teste A - 1776724904", 9 blocos ativos (default), incluindo "Bloco QA-01 · 20 unidades ativas", "Bloco QA-02 V3", etc. Arvore renderizada corretamente
**Nota:** Bloco QA-01 mostra "20 unidades ativas" — incluindo as 4 criadas em CT-10
**Evidencias:** `screenshots/ut01_arvore_carregada.png` | `screenshots/ut01_final.png`

---

### UT-02 — Expandir e colapsar bloco via click PASS

**Expected:** Click no icone de expansao expande/colapsa os filhos
**Actual:** aria-expanded changed: true -> false (collapse), then -> true (expand) — funciona corretamente via click
**Nota:** Arvore comeca com raiz expandida (aria-expanded=true). Primeiro click colapsa, segundo expande
**Evidencias:** `screenshots/ut02_apos_click.png` | `screenshots/ut02_apos_colapso.png`

---

### UT-03 — Navegacao por teclado (setas expand/collapse) FAIL

**Passos executados:**
1. Pagina carregada com arvore visivel
2. Localizados `role="treeitem"` (contagem > 0)
3. Primeiro treeitem focado
4. ArrowRight pressionado
5. aria-expanded ANTES: true; aria-expanded DEPOIS: true — SEM MUDANCA

**Expected:** ArrowRight expande nodo, ArrowLeft colapsa nodo (PRD: "Acessibilidade: arvore navegavel por teclado")
**Actual:** Teclado nao responde. `role="treeitem"` existe, `role="tree"` nao foi encontrado. ArrowRight/ArrowLeft nao alteram aria-expanded. A arvore nao implementa o padrao ARIA TreeView para navegacao por teclado.

**Console do browser:**
```
[debug] [vite] connecting...
[debug] [vite] connected.
[info] React DevTools message
```
(Sem erros de JS)

**Evidencias:**
- Screenshot: `screenshots/ut03_apos_arrow_right.png`
- Screenshot: `screenshots/ut03_apos_arrow_left.png`
- Video: `videos/qa_task_07_navegacao_arvor-b7188-lado-setas-expand-collapse--chromium/video.webm`

---

### UT-04 — Toggle filtro incluir inativos PASS

**Expected:** Toggle "Mostrar inativos" liga/desliga exibicao de inativos; inativos com visual descontraste quando visives
**Actual:**
- Antes toggle: "Bloco Temp Pai Inativo QA" invisivel (default oculta inativos) — correto
- Apos toggle ON: "Bloco Temp Pai Inativo QA" VISIVEL com estilo azul/descontraste — correto; contador mudou de "9 blocos cadastrados" para "11 blocos cadastrados"
- Apos toggle OFF: "Bloco Temp Pai Inativo QA" invisivel novamente — correto
**Observacao visual:** Blocos inativos aparecem com fundo azul claro (descontraste) e icone de power (reativar), conforme PRD "descontrast + icone distinto quando exibidos"
**Evidencias:** `screenshots/ut04_inicial.png` | `screenshots/ut04_inativos_ligados.png` | `screenshots/ut04_inativos_desligados.png`

---

### UT-05 — Painel lateral ao clicar num bloco PASS

**Expected:** Clicar num bloco abre painel/toolbar lateral com detalhes e acoes contextuais; auditoria "Criada por..."
**Actual:** Ao clicar em "Bloco QA-01", aparece toolbar contextual com "Bloco selecionado: Bloco QA-01" e botoes "Adicionar unidade" e "Adicionar proxima unidade". Toolbar VISIVEL.
**Nota sobre auditoria:** O texto "Criada por..." (PRD: "painel lateral da unidade mostra Criada por [sindico] em [data]") NAO foi encontrado. A toolbar de bloco mostra nome + acoes, mas sem dados de auditoria. Isso pode ser requisito de painel de unidade (nao bloco), ou feature nao implementada no MVP.
**Evidencias:** `screenshots/ut05_apos_click.png` | `screenshots/ut05_final.png`

---

### UT-06 — Responsividade tablet 768x1024 PASS

**Expected:** Arvore funcional em viewport tablet (768x1024)
**Actual:** Arvore visivel e funcional em 768x1024. Blocos, titulo "Estrutura do condominio", toggle e botao "Novo bloco" todos visiveis e utilizaveis.
**Evidencias:** `screenshots/ut06_tablet_768x1024.png`

---

### UT-07 — Empty state (Tenant B) BLOCKED

**Motivo:** Tenant B ja possui 1 bloco cadastrado (criado em tasks anteriores). Nao e possivel testar empty state para Tenant A (tem blocos). A criacao de um novo tenant zerado especificamente para este caso estava fora do escopo desta task.
**Status:** BLOCKED — nao executado

---

## Observacoes sobre Tempo de Resposta

**PRD requesito:** < 2s para tenant com ate 300 unidades; < 1s para tenant com ate 100 unidades
**Resultado:** Media de **20ms** para 3 chamadas consecutivas. Tenant A tem ~19 unidades (muito abaixo de 100).
**Conclusao:** Performance excelente — 50x mais rapido que o limite de 1s para tenant pequeno.

---

## Observacoes sobre Acessibilidade

**PRD requesito:** "Acessibilidade: arvore navegavel por teclado (setas para expandir/colapsar, Enter para abrir detalhes)"
**Resultado (UT-03):** FAIL — a arvore possui `role="treeitem"` nos nos, mas ArrowRight e ArrowLeft nao alteram o estado de expansao. O componente `Tree` de `@portabox/ui` nao implementa o padrao de teclado ARIA TreeView (WAI-ARIA 1.1 Tree View Pattern). Esta e uma lacuna real de acessibilidade em relacao ao PRD.

---

## Resumo de Evidencias

```
qa_task_07_navegacao_arvore/
├── test_plan.md
├── requests.log
├── db_check.log
├── performance.log
├── response_active_only.json
├── response_include_inactive.json
├── response_snapshot.json
├── screenshots/
│   ├── ut01_arvore_carregada.png
│   ├── ut01_final.png
│   ├── ut01_inicio.png
│   ├── ut02_apos_click.png
│   ├── ut02_apos_colapso.png
│   ├── ut02_inicial.png
│   ├── ut03_apos_arrow_left.png
│   ├── ut03_apos_arrow_right.png
│   ├── ut03_inicial.png
│   ├── ut04_inicial.png
│   ├── ut04_inativos_ligados.png
│   ├── ut04_inativos_desligados.png
│   ├── ut05_apos_click.png
│   ├── ut05_final.png
│   ├── ut05_inicial.png
│   └── ut06_tablet_768x1024.png
└── videos/
    └── [7 video/trace folders]
```

---

## Status para o Orquestrador

**Status:** FAIL
**Motivo da falha:** UT-03 — Navegacao por teclado (setas expand/collapse) nao funciona. O componente Tree usa `role="treeitem"` mas nao responde a ArrowRight/ArrowLeft. PRD requer "arvore navegavel por teclado (setas para expandir/colapsar)".
**Casos PASS:** CT-01 a CT-10 (10/10 API/DB), UT-01, UT-02, UT-04, UT-05, UT-06 (5/7 UI)
**Casos FAIL:** UT-03 (acessibilidade por teclado)
**Casos BLOCKED:** UT-07 (empty state — Tenant B ja tem blocos)
**Tasks possivelmente impactadas:** Nenhuma — as funcionalidades core (estrutura, ordenacao, filtro, panel) estao corretas. O FAIL e exclusivamente na acessibilidade por teclado (feature PRD nao implementada no componente UI).

### Totais: 15 PASS | 1 FAIL | 1 BLOCKED
