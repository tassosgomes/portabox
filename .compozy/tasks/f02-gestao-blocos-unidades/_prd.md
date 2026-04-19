# PRD — F02: Gestão de Blocos e Unidades

> **Nível 2 da hierarquia de documentação.** Baseado em `vision.md` e `domains/gestao-condominio/domain.md`. Este PRD é entrada para a skill `cy-create-techspec` que definirá a arquitetura de implementação.

**Domínio:** D01 — Gestão do Condomínio
**Feature:** F02 — Gestão de Blocos e Unidades
**Prioridade:** Must Have
**Fase:** 1 — MVP: Substituir o Caderno
**Última revisão:** 2026-04-18

---

## Overview

F02 é a feature que transforma um tenant recém-criado em F01 em um condomínio endereçável: sem blocos e unidades cadastradas, nenhuma outra operação do MVP funciona. F03 (cadastro individual de morador) exige uma unidade; F04 (importação em massa) rejeita linhas sem unidade canônica; F07 (API interna) não tem o que retornar a D02/D04; D04 (reconhecimento de etiquetas) não tem base de verdade para fazer fuzzy matching.

**Problema que resolve:** ao final de F01, o tenant está em `pre-ativo` com apenas os dados do condomínio e o primeiro síndico cadastrados. A estrutura física do prédio — blocos, andares, apartamentos — precisa ser representada digitalmente antes que qualquer encomenda possa ser associada a um destinatário. F02 é o passo que materializa essa representação.

**Para quem é:** o síndico do condomínio, após receber o magic link de F01 e ativar a conta no `apps/sindico`. É ele quem conhece a estrutura real do prédio (quantos blocos, nomenclatura, unidades por andar) e é o dono do cadastro. A equipe da plataforma, via `apps/backoffice`, tem visibilidade read-only cross-tenant para apoio em suporte.

**Por que é valioso:**
- Destrava F03, F04 e F07 do MVP — é bloqueador linear da Fase 1.
- Fornece a forma canônica (bloco + andar + número) sobre a qual D04 faz fuzzy matching; quanto mais consistente o cadastro, maior a acurácia da IA.
- Materializa a política de propriedade do síndico sobre o cadastro do seu condomínio (RN-06), preparando a cultura operacional do produto.
- Estabelece a base de auditoria: toda unidade tem um criador, uma data e um histórico de status — pré-requisito para rastrear encomendas ao longo do tempo.

---

## Goals

Objetivos mensuráveis da feature:

- **Cadastro completo do piloto**: 100% das unidades físicas do condomínio-piloto representadas no sistema em no máximo 2h de trabalho do síndico.
- **Zero fricção no onboarding de F03/F04**: após F02 concluído, F04 aceita pelo menos 95% das linhas da primeira importação de moradores (erros residuais são de dados de morador, não de unidade inexistente).
- **Propriedade clara do cadastro**: 100% das operações de escrita em F02 atribuídas a um síndico identificado; nenhuma escrita cross-tenant no MVP.
- **Auditabilidade total**: para qualquer bloco ou unidade, é possível responder "quem criou?", "quem alterou o nome do bloco?", "quando foi inativada?".
- **Suporte desbloqueado**: o operador da plataforma consegue diagnosticar problemas estruturais de qualquer tenant sem precisar solicitar prints ao síndico.

Meta de entrega: F02 pronto para uso antes do início de F03 no cronograma do MVP. Sem data absoluta; o que importa é a ordenação dentro da Fase 1.

---

## User Stories

### Síndico (persona primária, escrita)

- **Como síndico**, depois de ativar minha conta via magic link, **quero** cadastrar o primeiro bloco do meu condomínio, **para** começar a representar a estrutura do prédio.
- **Como síndico**, **quero** cadastrar unidades dentro de um bloco informando andar e número, **para** que moradores possam ser posteriormente associados.
- **Como síndico** de um condomínio com torre única, **quero** criar um único bloco (por convenção, "Bloco Único" ou equivalente) **para** satisfazer a forma canônica estrita do sistema sem precisar me adaptar a um modelo que não reflete a realidade do meu prédio.
- **Como síndico**, **quero** renomear um bloco se errei na digitação ou se a convenção interna mudou (ex.: "Bloco A" → "Torre Alfa"), **para** que a exibição no sistema fique consistente com a comunicação interna do condomínio.
- **Como síndico**, **quero** cadastrar unidades com sufixo alfabético no número (101A, 706B), **para** refletir fielmente a numeração que aparece nas etiquetas das transportadoras.
- **Como síndico** que cometeu um erro no cadastro de uma unidade (andar ou número errado), **quero** inativar a unidade errada e criar uma nova com os dados corretos, **para** corrigir o cadastro sem perder histórico auditável.
- **Como síndico**, **quero** inativar um bloco inteiro quando ele é desativado administrativamente (ex.: reforma estrutural, desocupação), **para** que novas encomendas não sejam associadas a unidades daquele bloco.
- **Como síndico**, **quero** reativar uma unidade inativada por engano, **para** voltá-la ao uso sem precisar recriar (e sem duplicar a forma canônica).
- **Como síndico**, **quero** visualizar a estrutura do meu condomínio em uma árvore hierárquica (blocos → andares → unidades), **para** entender rapidamente o que já está cadastrado e o que falta.
- **Como síndico**, **quero** filtrar unidades inativas da visualização padrão, **para** me concentrar no que está operacional sem que o cadastro fique visualmente poluído.

### Operador Backoffice (persona secundária, leitura)

- **Como operador** da plataforma, **quero** visualizar a estrutura de qualquer tenant (blocos, andares, unidades, status), **para** atender dúvidas ou investigar problemas reportados por síndicos sem precisar solicitar prints ou acesso ao banco.
- **Como operador**, **quero** saber quantas unidades ativas existem em um tenant específico, **para** apoiar a decisão de transição de `pre-ativo` → `ativo`.

### Sistema (persona técnica, consumidores)

- **Como sistema F04** (Importação em Massa de Moradores), **preciso** validar se uma tripla `(bloco, andar, número)` existe e está ativa em um tenant, **para** aceitar ou rejeitar cada linha da planilha de moradores com feedback preciso ao síndico.
- **Como sistema D04** (Reconhecimento de Etiquetas), **preciso** receber a lista de unidades ativas de um tenant, **para** executar fuzzy matching contra texto extraído de etiquetas.
- **Como sistema F03** (Gestão Individual de Moradores), **preciso** listar unidades ativas de um bloco, **para** apresentar seletor ao síndico durante o cadastro do morador.

---

## Core Features

### CF-01 — Cadastro de Bloco

**O que faz:** cria um novo bloco vinculado ao tenant atual do síndico.

**Por que é importante:** unidade não existe sem bloco (ADR-002); todo cadastro começa aqui.

**Comportamento:**
- Entrada: nome do bloco (string, 1–50 caracteres, único dentro do tenant).
- Saída: bloco persistido com status `ativo`, associado ao tenant, auditoria registrando o síndico criador e o timestamp.
- Validações: unicidade do nome dentro do tenant; rejeita nomes que conflitem com blocos inativos da mesma tripla canônica somente se a reativação daquele inativo seria possível (heurística: se já existe inativo com mesmo nome, oferece ao síndico reativar em vez de criar).

### CF-02 — Cadastro de Unidade

**O que faz:** cria uma nova unidade dentro de um bloco existente do tenant.

**Por que é importante:** é o endereço canônico que todos os outros domínios consumirão.

**Comportamento:**
- Entrada: bloco (seleção), andar (inteiro não-negativo), número (string `^[0-9]{1,4}[A-Z]?$`, normalizado para caixa alta).
- Saída: unidade persistida com status `ocupada` se imediatamente associada a morador em F03, ou `vaga` por default; auditoria de criação.
- Validações: unicidade da tripla `(bloco_id, andar, número)` entre unidades ativas do tenant; forma canônica conforme ADR-002.
- Restrição: bloco precisa existir e estar ativo. Criação de unidade em bloco inativo é rejeitada.

### CF-03 — Edição do Nome de Bloco

**O que faz:** permite ao síndico alterar o nome de exibição de um bloco.

**Por que é importante:** corrige erros de digitação e absorve mudanças de convenção interna do condomínio sem gerar entradas inativas desnecessárias.

**Comportamento:**
- Entrada: bloco alvo, novo nome (validado como em CF-01).
- Saída: bloco com novo nome; auditoria registra alteração (antes/depois + autor).
- Propagação: a nova exibição aparece em toda a UI e relatórios imediatamente; referências internas (ID do bloco) permanecem estáveis.
- Restrição: unidade não pode ter nenhum atributo editado (ADR-003). Nem bloco vinculado, nem andar, nem número.

### CF-04 — Inativação de Unidade

**O que faz:** marca uma unidade como inativa (soft-delete).

**Por que é importante:** único caminho para remover uma unidade do uso corrente preservando auditoria.

**Comportamento:**
- Entrada: unidade alvo.
- Pré-condição: síndico confirma a ação em modal (copy explica que histórico é preservado).
- Saída: unidade com `ativo = false` e `InativadaEm = now`; auditoria registra autor + motivo opcional.
- Efeito em domínios vizinhos: moradores associados continuam ligados à unidade (F03 deve ser executado pelo síndico para remigrar ou inativar moradores se for o caso); F07 deixa de retornar a unidade em consultas default; D04 não considera em fuzzy matching contra cadastro ativo; encomendas históricas mantêm referência intacta.

### CF-05 — Inativação de Bloco

**O que faz:** marca um bloco como inativo.

**Por que é importante:** permite representar desativação administrativa de um bloco inteiro (reforma, desocupação, desmembramento).

**Comportamento:**
- Entrada: bloco alvo.
- Pré-condição: síndico confirma em modal que explica que unidades ativas do bloco precisarão ser inativadas separadamente (não há cascata automática no MVP — ver Open Questions).
- Saída: bloco com `ativo = false`; auditoria registra autor.
- Efeito: criação de novas unidades no bloco é bloqueada; unidades existentes não são afetadas pela inativação do bloco em si.

### CF-06 — Reativação de Bloco ou Unidade

**O que faz:** reverte inativação anterior.

**Por que é importante:** permite corrigir inativação equivocada sem recriar a entidade.

**Comportamento:**
- Entrada: bloco ou unidade alvo (atualmente inativo).
- Validação: para unidade, verifica se a reativação não cria conflito canônico com outra unidade ativa de mesma tripla. Se conflito, rejeita com mensagem acionável ("Já existe unidade ativa Bloco A / Andar 2 / Apto 201; inative-a antes de reativar esta").
- Saída: `ativo = true`, `InativadaEm = null`; auditoria registra reativação (autor + timestamp).

### CF-07 — Navegação em Árvore Hierárquica

**O que faz:** apresenta o condomínio como árvore expansível no `apps/sindico` e no `apps/backoffice`.

**Por que é importante:** é a única visualização do MVP; precisa ser funcional para cadastro rápido, diagnóstico e suporte.

**Comportamento:**
- Estrutura: Condomínio (raiz) → Blocos (lista) → Andares (agrupamento interno do bloco) → Unidades (folhas).
- Indicadores visuais por nó: nome, contagem de filhos ativos, badge de status (ativo/inativo).
- Interação: expandir/colapsar ramos; clicar em um nó abre painel lateral com detalhes e ações contextuais (no `apps/sindico`); no `apps/backoffice`, o painel lateral é read-only.
- Filtros: toggle para incluir/excluir nós inativos (default: oculta inativos).
- Tratamento visual de inativos: descontrast + ícone distinto quando exibidos.

### CF-08 — Exposição para F07 (API Interna)

**O que faz:** F07 (API Interna de Busca de Unidade e Morador) ganha endpoints de leitura que F02 alimenta.

**Por que é importante:** é o ponto de integração para F04, F03 e domínios externos (D02, D03, D04).

**Comportamento (perspectiva de produto):**
- Dada uma tripla `(bloco_nome_ou_id, andar, número)` e um tenant, responde se a unidade existe e está ativa; retorna ID canônico.
- Dado um tenant, lista todas as unidades ativas (para consumo de D04).
- Dado um bloco ativo, lista andares e unidades ativas (para UI de F03).
- Todos os retornos omitem dados de contato de moradores; F07 apenas valida existência e agrega dados estruturais.

### CF-09 — Leitura Cross-Tenant no Backoffice (Read-Only)

**O que faz:** permite ao operador do `apps/backoffice` visualizar a árvore de qualquer tenant com os mesmos dados que o síndico vê, sem qualquer ação de escrita.

**Por que é importante:** desbloqueia suporte ágil sem comprometer propriedade do síndico (ADR-005).

**Comportamento:**
- UI idêntica à árvore do síndico, mas com todos os controles de ação ocultos.
- Seletor de tenant no topo do backoffice; o operador escolhe o condomínio a visualizar.
- Acesso registrado em log de auditoria (quem acessou qual tenant, quando).

---

## User Experience

### Jornada do Síndico (fluxo primário)

**Pré-condição:** síndico recebeu magic link de F01, definiu senha, fez login no `apps/sindico`. Tenant está em `pre-ativo`.

1. **Entrada na feature**: ao entrar no painel, o síndico vê a home do condomínio. Uma seção "Estrutura do Condomínio" exibe estado vazio com CTA claro ("Cadastrar primeiro bloco").
2. **Primeiro bloco**: síndico clica no CTA, modal pede nome do bloco, confirma. Bloco aparece na árvore como único nó filho do condomínio.
3. **Primeira unidade**: sobre o bloco recém-criado, o síndico vê "Cadastrar primeira unidade" contextual. Modal pede andar + número; ao confirmar, a unidade aparece como folha na árvore.
4. **Loop de cadastro**: síndico adiciona unidades uma a uma. UI mantém foco no bloco atual; atalho "Adicionar próxima unidade" mantém o bloco selecionado e apenas pede andar + número na sequência.
5. **Correção de erro**: ao notar unidade com número errado, síndico clica na unidade, escolhe "Inativar", confirma em modal; em seguida clica em "Cadastrar unidade" e entra a correta.
6. **Múltiplos blocos**: síndico volta à raiz, clica "Novo bloco", preenche. Nova árvore de cadastro começa.
7. **Revisão**: síndico navega na árvore com filtro "Ocultar inativos" ligado; percorre cada bloco conferindo a contagem de unidades ativas.
8. **Saída**: síndico sai da feature e avança para F03 (ou aguarda F04 quando estiver pronto).

**Tempo esperado para condomínio-piloto (~200 unidades):** 1h30 a 2h em sessão única; sessões interrompidas são seguras (cada criação é persistente).

### Jornada do Operador Backoffice (fluxo de suporte)

**Pré-condição:** síndico reportou problema via canal externo ("meu bloco A sumiu", "cadastrei 201 mas não consigo criar 201A").

1. **Entrada**: operador abre `apps/backoffice`, usa seletor de tenant para escolher o condomínio reportado.
2. **Navegação**: operador vê a mesma árvore que o síndico veria, sem controles de edição.
3. **Diagnóstico**: operador expande blocos, verifica contagens, identifica o status das unidades mencionadas, localiza inativos relevantes.
4. **Comunicação**: operador responde ao síndico pelo canal original com explicação ("Sua unidade 201 está inativa desde ontem; reative-a pelo painel ou inative-a explicitamente antes de criar 201A").

### UI/UX — Princípios

- **Design system**: todo o visual aderente a `portabox-design` (ADR-010 de F01); sem estilos ad-hoc.
- **Empty states didáticos**: quando o condomínio não tem bloco, quando um bloco não tem unidade, a UI orienta o próximo passo.
- **Confirmações em operações irreversíveis-na-prática**: modal explicando implicações antes de inativar blocos ou unidades com morador associado.
- **Acessibilidade**: árvore navegável por teclado (setas para expandir/colapsar, Enter para abrir detalhes).
- **Responsividade**: painel web desktop é o alvo; a árvore precisa funcionar em tablet (o síndico pode usar durante vistoria no prédio), mas não é prioridade de MVP.
- **Auditoria visível**: painel lateral da unidade mostra "Criada por [síndico] em [data]"; painel lateral do bloco mostra histórico de mudança de nome com antes/depois.

### Onboarding e Descoberta

F02 não tem onboarding próprio além do empty state inicial. É uma feature profundamente instrumental; o síndico chega nela pelo fluxo natural de ativação do tenant. A primeira interação já é o próprio "tutorial" — CTA claro, modal de cadastro direto, árvore autoexplicativa.

---

## High-Level Technical Constraints

Estas restrições são herdadas de F01 e da arquitetura global; não prescrevem implementação de F02 em si, apenas os limites nos quais a techspec deve operar.

- **Multi-tenant obrigatório**: toda entidade de F02 deve carregar `tenant_id` e ser filtrada automaticamente por consulta de tenant; uma unidade de um tenant nunca pode ser acessada por outro.
- **Stack fixa**: .NET 8 (ASP.NET Core) + PostgreSQL para o módulo `PortaBox.Modules.Gestao`; React + Vite no `apps/sindico` e `apps/backoffice`.
- **Design system obrigatório**: `portabox-design` (ADR-010 de F01); nenhum componente ou token customizado.
- **Auditoria**: toda operação de escrita (criação, edição de nome, inativação, reativação) registra autor, timestamp e diff onde aplicável; mecanismo padronizado já estabelecido em F01.
- **Eventos de domínio**: criação e alteração de estado de bloco/unidade emitem eventos (`bloco.criado`, `bloco.renomeado`, `bloco.inativado`, `bloco.reativado`, `unidade.criada`, `unidade.inativada`, `unidade.reativada`) usando a infraestrutura outbox estabelecida em F01 ADR-009.
- **LGPD**: F02 não manipula dados pessoais — apenas estrutura física. Nenhuma exigência específica adicional além de auditoria de quem operou.
- **Performance (expectativa do usuário)**: árvore de um condomínio de 300 unidades deve carregar em menos de 2s; criação de unidade deve persistir e atualizar a árvore em menos de 1s.

---

## Non-Goals (Out of Scope)

Explicitamente fora do escopo de F02:

- **Gerador de unidades em lote** ("criar andares 1–20 com 4 unidades cada"). Cadastro é manual por decisão explícita (ADR-001).
- **Importação de estrutura via planilha** — não há upload de .xlsx/.csv para criar blocos e unidades. F04 importa moradores; F02 é manual.
- **Auto-criação de estrutura por F04** — F04 rejeita linhas com unidade inexistente; nunca cria estrutura implicitamente (ADR-004).
- **Unidades comerciais, garagens, sobrelojas, coberturas diferenciadas** — o produto é residencial; todas as unidades são tratadas como residenciais no MVP.
- **Lojas térreas sem andar** — andar é obrigatório (ADR-002).
- **Condomínios sem bloco** — bloco é obrigatório; condomínios de torre única criam "Bloco Único" como workaround (ADR-002).
- **Tipo de unidade como atributo** — não há categorização residencial/comercial/outro.
- **Número do apartamento puramente numérico** — suporta sufixo alfabético (101A, 706B) sem restrição contrária (ADR-002).
- **Edição de atributos canônicos da unidade** (bloco vinculado, andar, número). Unidade é imutável; erro exige inativar + criar nova (ADR-003).
- **Hard-delete (exclusão física)** — nem síndico nem operador podem excluir. Apenas inativação (ADR-003).
- **Escrita do operador backoffice** — operador tem acesso read-only cross-tenant; não edita em nome do síndico (ADR-005).
- **Inativação em cascata automática** — inativar bloco não inativa suas unidades no MVP (ver Open Questions).
- **Checklist automatizado de prontidão** (contadores e alertas dedicados a indicar que o tenant está "pronto" para ativar). Responsabilidade do operador + F09 quando disponível (ADR-001 de F01 e ADR-001 de F02).
- **Versionamento do nome do bloco** (histórico de nomes como linha do tempo). Auditoria registra alterações, mas UI não expõe linha do tempo no MVP.
- **Andares negativos / subsolo** — ver Open Questions; provavelmente Phase 2.
- **Reordenação manual de blocos na árvore** — ordem default (alfabética ou por data de criação, a definir em techspec).
- **Migração de morador entre unidades via F02** — operação de F03; F02 apenas inativa/reativa unidades.
- **Papel administrativo elevado da plataforma com escrita cross-tenant** — não existe no MVP.
- **Acesso de morador ou porteiro à estrutura** — morador não acessa a plataforma; porteiro acessa via PWA (D02), não via F02.

---

## Phased Rollout Plan

### MVP (Phase 1)

**Escopo:** todas as 9 Core Features (CF-01 a CF-09) descritas acima.

**Critério de conclusão:**
- Síndico consegue executar cadastro completo do condomínio-piloto (≥ 100 unidades) via `apps/sindico` em menos de 2h.
- F04 consegue importar moradores com ≥ 95% de aceitação após F02 estar completo.
- Operador backoffice consegue navegar em modo read-only pela estrutura de qualquer tenant.
- Todos os eventos de domínio (`bloco.*`, `unidade.*`) emitidos e consumíveis pelo outbox.
- Auditoria registra autor e timestamp de toda operação de escrita.

**Saída para Phase 2:** síndico do piloto cadastrou estrutura e pelo menos 1 importação F04 bem-sucedida; operador reportou ao menos 1 caso resolvido via suporte read-only.

### Phase 2 — Conveniência e Resiliência

**Candidatos a escopo** (a ser validado após feedback do piloto):

- **Gerador de unidades em lote**: assistente "N andares × M unidades por andar" com prévia e confirmação, se o piloto demonstrar tédio real no onboarding.
- **Importação de estrutura via planilha**: para condomínios futuros com 300+ unidades.
- **Reativação em cascata**: reativar um bloco oferece reativação de suas unidades anteriormente ativas.
- **Inativação em cascata**: inativar um bloco oferece inativação de suas unidades em uma única confirmação.
- **Views agregadas no backoffice**: listagem cross-tenant com filtros (condomínios com menos de X unidades, condomínios sem bloco ativo, etc.) para apoiar priorização do suporte.
- **Suporte a andares negativos** (subsolo) — útil para condomínios que tratam garagens como unidades formais em fase futura.

**Saída para Phase 3:** padrões de uso estabelecidos; feedback de múltiplos condomínios consolidado.

### Phase 3 — Expansão do Modelo

**Candidatos a escopo:**

- **Tipo de unidade** (residencial/comercial/garagem) como atributo, se o produto expandir para condomínios mistos.
- **Estruturas alternativas** (torre única sem bloco nominal, lojas térreas sem andar) com migração de dados existente.
- **Reordenação manual de blocos** e personalização de labels de andar (ex.: "Térreo", "Cobertura").
- **Histórico visual de alterações** (linha do tempo de mudanças de nome de bloco, inativações/reativações).
- **Papel administrativo elevado** com escrita cross-tenant para casos excepcionais, com trilha de auditoria diferenciada.

---

## Success Metrics

**Métricas de adoção e operação:**

- **Tempo médio de cadastro inicial**: < 2h para condomínio de até 300 unidades (medido do primeiro bloco criado à última unidade cadastrada pelo síndico em sessões contíguas).
- **Taxa de rejeição em F04 por "unidade inexistente"**: < 5% na primeira importação; convergindo para 0 após correção em F02.
- **Abandono no meio do cadastro**: < 10% de sessões iniciadas sem completar todos os blocos previstos para o tenant; medido via log de eventos.
- **Tempo de resposta da árvore**: < 2s para carregar tenant com até 300 unidades; < 1s para tenant com até 100 unidades.
- **Tempo de operação unitária**: < 1s para criar/inativar/reativar uma unidade ou bloco (do clique de confirmação à atualização visual).

**Métricas de qualidade do cadastro:**

- **Taxa de unidades inativadas no primeiro dia**: < 15% das criadas (indicador de erros iniciais de cadastro; valores altos indicam necessidade de melhoria de UI ou adição de gerador em lote).
- **Taxa de renomeação de bloco pós-criação**: < 20% dos blocos criados (valores altos sugerem placeholder inicial confuso ou falta de orientação).

**Métricas de suporte:**

- **Redução do tempo médio de resposta a tickets estruturais**: medido antes/depois de F02 como indicador de que o backoffice read-only está sendo utilizado efetivamente.

**Métricas de confiabilidade:**

- **Zero incidentes de cross-tenant leak**: nenhuma unidade ou bloco de tenant A visível em operações de tenant B.
- **100% das operações de escrita com audit trail completo** (autor + timestamp + diff onde aplicável).

---

## Risks and Mitigations

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Síndico desiste a meio do cadastro pela tediosidade de 200+ unidades | Média | Alto | Cada criação é persistente e imediata; sessões interrompidas são seguras. UX de "cadastrar próxima unidade" mantém contexto do bloco. Se piloto reportar fadiga real, Phase 2 entrega gerador em lote. |
| Competidor (Superlógica, TownSq) oferece gerador em lote; síndico compara e vê fricção | Média | Médio | F02 é pré-piloto, não em concorrência direta com ferramentas estabelecidas. Mensagem do produto foca em IA de reconhecimento de etiqueta e notificação WhatsApp — não em eficiência de cadastro. Aceitar trade-off no MVP. |
| Síndico cadastra estrutura inconsistente com as etiquetas reais das transportadoras (ex.: cadastra "Bloco 1" mas as etiquetas dizem "Torre 1"), degradando acurácia de D04 | Alta | Alto | Documentação de onboarding orienta o síndico a verificar as etiquetas antes de cadastrar; D04 tem fuzzy matching tolerante (até certo ponto); F09 (Phase 2) poderá sinalizar unidades que nunca foram alvo de encomenda como possível inconsistência. |
| Suporte read-only não resolve casos reais (síndico bloqueado, precisa de intervenção urgente) | Baixa | Médio | Scripts administrativos pré-aprovados para intervenção em banco sob solicitação formal; se frequência alta no piloto, Phase 2 avalia papel administrativo com escrita cross-tenant auditada. |
| F02 atrasa e trava F03/F04 no cronograma do MVP | Baixa | Alto | Escopo mínimo (ADR-001) escolhido justamente para reduzir esse risco; apenas CF-01 a CF-09, todas diretas. |
| Árvore com muitas unidades inativas pollui visualmente a UI | Média | Baixo | Filtro default oculta inativos; indicador sutil no header do bloco mostra "N inativas" sem forçar exibição. |
| Síndico renomeia bloco pensando que está movendo unidades | Baixa | Médio | Modal de confirmação explica que alteração afeta apenas o nome de exibição; unidades não se movem. |
| Condomínio de torre única estranha ter que criar "Bloco Único" | Alta | Baixo | Placeholder sugestivo no campo de nome do primeiro bloco; help contextual explica decisão; em Phase 3 considerar bloco opcional. |
| Operador identifica problema que exigiria escrita mas não tem permissão; cria fricção de processo | Média | Baixo | Documentar claramente o limite do suporte read-only; intervenções de escrita seguem pelo processo administrativo externo. |

---

## Architecture Decision Records

- [ADR-001: F02 — Abordagem MVP Pura (CRUD Manual + Árvore Hierárquica)](adrs/adr-001.md) — escopo mínimo; sem gerador em lote; sem indicadores de prontidão.
- [ADR-002: Forma Canônica Estrita — Bloco e Andar Obrigatórios; Número com Sufixo Alfabético](adrs/adr-002.md) — uniformidade vence flexibilidade; unidade comercial e loja térrea fora do escopo.
- [ADR-003: Remoção Exclusivamente por Inativação; Edição Restrita](adrs/adr-003.md) — soft-delete; bloco com nome editável; unidade imutável após criação.
- [ADR-004: F02 como Única Fonte de Estrutura; F04 Valida e Rejeita Linhas com Unidade Inexistente](adrs/adr-004.md) — separação estrita de responsabilidades; F04 nunca cria estrutura.
- [ADR-005: Escrita Exclusiva do Síndico; Backoffice Operador com Read-Only Cross-Tenant](adrs/adr-005.md) — modelo de permissões alinhado ao de F01.

---

## Open Questions

Itens que permanecem em aberto e devem ser resolvidos na techspec ou no refinamento de UX imediato:

- [ ] **Inativação em cascata (bloco → unidades)**: inativar um bloco deve oferecer opção de inativar suas unidades em uma única confirmação, ou força o síndico a inativá-las uma a uma? MVP leans para "sem cascata" (explícito nos Non-Goals), mas a UX do modal de confirmação pode oferecer o atalho sem automatizar. **Decisão diferida para refinamento da techspec.**
- [ ] **Reativação de bloco com unidades previamente inativas**: reativar o bloco mantém unidades como estavam (não as reativa automaticamente)? Assumir que sim, mas confirmar na UI com copy claro.
- [ ] **Andares negativos (subsolo)**: permitir valores < 0 no campo de andar para cobrir subsolos? No MVP, assumir **apenas inteiros não-negativos** (0 permitido? ver próxima questão). Reavaliar se piloto demandar.
- [ ] **Andar zero (térreo)**: andar = 0 é permitido para apartamentos térreos? Provável sim, mas precisa validar com o síndico do piloto se há apartamentos térreos residenciais no prédio.
- [ ] **Limite máximo de blocos e unidades por tenant**: existe um teto prático (ex.: 50 blocos, 5000 unidades) para fins de performance e sanity check? Provavelmente não necessário no MVP, mas vale documentar decisão.
- [ ] **Exportação da estrutura em planilha**: síndico pode precisar "baixar" a estrutura cadastrada para fins de conferência ou auditoria externa. Não é Must Have, mas pode surgir como necessidade óbvia pós-piloto. **Deferido para Phase 2 a menos que o piloto demande.**
- [ ] **Ordenação default da árvore**: blocos exibidos em ordem alfabética do nome ou por data de criação? Unidades dentro do andar ordenadas por número? Definir na techspec seguindo convenção natural (alfabética + numérica crescente).
- [ ] **Log de auditoria visível ao síndico**: o síndico consegue ver no painel lateral da unidade/bloco um histórico de operações ("Criada por mim em 2026-04-18; Inativada por mim em 2026-04-20")? É razoável no MVP, mas precisa de confirmação de escopo.

---

*PRD gerado com a skill `cy-create-prd`. Próximo passo: criar a TechSpec (`cy-create-techspec`) a partir deste PRD para detalhar a arquitetura de implementação.*
