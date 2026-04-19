# Vision Document — PortaBox

> **Nível 0 da hierarquia de documentação.** Este documento é a âncora de contexto para todos os Domain Docs, PRDs, Tech Specs e Tasks do projeto. Sempre que iniciar uma nova sessão com a IA, forneça este arquivo como contexto.

---

## 1. Visão Geral do Sistema (System Overview)

### Problema de Negócio

Condomínios residenciais de médio e grande porte (100+ unidades) recebem dezenas de encomendas por dia. O registro manual em caderno de protocolo tornou-se inviável nessa escala: gera filas na guarita, atrasa a notificação ao morador, expõe o condomínio a perdas e disputas sem rastreabilidade. O morador frequentemente é notificado pelo próprio e-commerce (Amazon, Mercado Livre) antes de a portaria sequer ter registrado o recebimento — gerando conflito e desconfiança.

O impacto direto da ausência de solução: encomendas acumuladas ocupando espaço na guarita, moradores insatisfeitos, porteiros sobrecarregados com consultas manuais e risco real de perda ou extravio sem evidência.

### Solução Proposta

Uma plataforma multi-tenant de controle inteligente de encomendas para condomínios residenciais. O sistema substitui o caderno de protocolo por um fluxo digital orientado por IA: o porteiro fotografa as etiquetas em lote pelo PWA da portaria no seu próprio ritmo, a IA extrai e valida os dados automaticamente, e o morador recebe uma notificação informativa com um Token de Retirada — sem instalar nenhum aplicativo.

O entregador continua operando exatamente como hoje: deixa os pacotes na guarita e segue em frente. O sistema não impõe nenhuma mudança de comportamento a ele. O morador também não interage com a plataforma: apenas recebe a mensagem informativa e, ao ir retirar, informa o Token de Retirada ao porteiro.

O canal de comunicação é centralizado e plugável: a plataforma opera um WhatsApp corporativo único (não por condomínio) e usa SMS como fallback automático quando o número do morador não tem WhatsApp. A arquitetura de adapters permite adicionar novos canais (Telegram, e-mail) sem refatoração do core.

### Público-Alvo (Target Audience)

| Perfil (Role) | Descrição | Necessidade Principal |
|---|---|---|
| Porteiro / Zelador | Operador físico da guarita, recebe as encomendas e opera o PWA da portaria | Registrar rapidamente pelo app, sem digitação manual, com confirmação automática ao morador |
| Morador / Condômino | Destinatário da encomenda (consumidor passivo de notificações) | Ser notificado imediatamente e receber o Token de Retirada para apresentar na portaria |
| Síndico | Responsável legal e administrativo do condomínio | Visibilidade total do fluxo, relatórios, gestão de moradores e unidades |
| Administradora | Empresa que gerencia um ou mais condomínios | Onboarding e gestão centralizada de múltiplos tenants |
| Entregador | Transportador que entrega a encomenda na portaria | Entregar rapidamente sem mudança de processo — o sistema não exige nenhuma ação dele |

### Contexto de Entrada

- [x] Ideia nova (greenfield)

---

## 2. Domínios Identificados (Domain Map)

> Um domínio é um conjunto coeso de responsabilidades de negócio com fronteiras bem definidas (bounded context).

| # | Domínio | Responsabilidade Principal | Status | Domain Doc |
|---|---|---|---|---|
| D01 | Gestão do Condomínio | Cadastro multi-tenant de condomínios, blocos, unidades e moradores. Importação em massa. Base de verdade para validação de endereços e identidades. | `planned` | `domains/gestao-condominio/domain.md` |
| D02 | Controle de Encomendas | Ciclo de vida completo da encomenda: recebimento → protocolo gerado → Token de Retirada emitido → aguardando retirada → retirada confirmada por PIN → devolução. Token é ao portador: quem apresenta o PIN retira a encomenda. Entrega a terceiros segue o regulamento do condomínio (fora do escopo do sistema). | `planned` | `domains/controle-encomendas/domain.md` |
| D03 | Hub de Comunicação | Canal de notificação plugável e agnóstico de meio. Define a interface padrão de comunicação que qualquer adapter implementa. No MVP: WhatsApp corporativo único da plataforma + SMS como fallback automático quando o número do morador não tem WhatsApp. Credencial centralizada (não por tenant). Integrações são implementadas no serviço .NET e expostas como Tool ao Mastra para uso agêntico. | `planned` | `domains/hub-comunicacao/domain.md` |
| D04 | Reconhecimento de Etiquetas | Extração de dados da foto da etiqueta via VLM (Vision-Language Model), normalização e validação contra o cadastro do condomínio com Fuzzy Matching. Fluxo Human-in-the-Loop obrigatório: quando a confiança da extração for baixa, o porteiro é alertado para confirmar manualmente antes do registro. | `planned` | `domains/reconhecimento-etiquetas/domain.md` |
| D05 | Relatórios & Auditoria | Dashboard do síndico, histórico de encomendas, métricas operacionais e gestão de dados pessoais (LGPD: exclusão sob demanda, exportação). | `planned` | `domains/relatorios-auditoria/domain.md` |

**Status possíveis:** `planned` · `in-progress` · `done` · `out-of-scope`

---

## 3. Mapa de Interdependências (Dependency Map)

```
D03 Hub de Comunicação       ──depende de──→ D01 Gestão do Condomínio (busca morador/tenant)
D03 Hub de Comunicação       ──depende de──→ D02 Controle de Encomendas (aciona notificações)
D02 Controle de Encomendas  ──depende de──→ D01 Gestão do Condomínio (valida unidade)
D02 Controle de Encomendas  ──depende de──→ D04 Reconhecimento de Etiquetas (dados extraídos)
D04 Reconhecimento           ──depende de──→ D01 Gestão do Condomínio (valida endereço)
D05 Relatórios & Auditoria   ──depende de──→ D02 Controle de Encomendas (consome histórico)
```

| Domínio Origem | Depende de | Tipo de Dependência | Risco |
|---|---|---|---|
| D02 Controle de Encomendas | D01 Gestão do Condomínio | Dados (validação de unidade) | Alto — D01 deve ser entregue primeiro |
| D04 Reconhecimento | D01 Gestão do Condomínio | Dados (validação de endereço) | Alto — acurácia depende do cadastro |
| D03 Hub de Comunicação | D01 + D02 | Evento + Dados | Médio — interface desacoplada mitiga |
| D05 Relatórios | D02 | Dados (leitura) | Baixo — pode ser entregue depois |

---

## 4. Roadmap Macro (High-Level Roadmap)

### Fase 1 — MVP: Substituir o Caderno *(D01 + D02 + D03 + D04)*

**Objetivo:** Porteiro consegue registrar encomenda via foto no canal de comunicação do condomínio, a IA valida a unidade, o morador é notificado automaticamente e a retirada é confirmada. O caderno de papel pode ser aposentado.

**Domínios incluídos:** D01, D02, D03 (adapter WhatsApp), D04

**Critério de conclusão:**
- Porteiro fotografa etiquetas em lote pelo PWA da portaria instalado no dispositivo Android da guarita
- IA extrai dados da etiqueta, valida unidade e gera protocolo automaticamente
- Quando confiança da extração é baixa, porteiro recebe alerta no PWA para confirmação manual (Human-in-the-Loop)
- Morador recebe notificação com Token de Retirada (PIN de 6 dígitos) via WhatsApp corporativo da plataforma; se o número não tiver WhatsApp, a plataforma envia por SMS automaticamente
- A notificação informa nome completo do condomínio e é unidirecional (sem interação do morador com a plataforma)
- Porteiro confirma a retirada no PWA registrando o PIN apresentado por quem foi retirar (token ao portador; validação de identidade é regulamento interno do condomínio)
- Porteiro consegue consultar encomendas pendentes por unidade no PWA
- Porteiro e síndico podem reemitir o Token de Retirada quando necessário
- Síndico consegue cadastrar unidades via importação de planilha pelo painel web

**Ambiente de desenvolvimento:** OpenClaw (conta WhatsApp pessoal) para prototipação e testes internos antes de ativar Meta Cloud API em produção.

### Fase 2 — Inteligência Operacional *(D05 + evolução D03 e D04)*

**Objetivo:** Síndico tem visibilidade completa do fluxo e opera o condomínio com base em dados; sistema opera de forma cada vez mais autônoma.

**Domínios incluídos:** D05 (novo), evolução de D03 e D04

**Critério de conclusão:**
- Dashboard do síndico com métricas (tempo médio de retirada, encomendas por período, SLA)
- Reenvio automático do Token de Retirada para encomendas não retiradas após X dias (configurável por tenant)
- Validação de endereço com maior resiliência (variações de escrita na etiqueta)
- Análise agêntica de exceções pelo Mastra (ex.: encomenda sem destinatário óbvio encaminhada ao síndico via Tool de comunicação)

### Fase 3 — Plataforma Multi-tenant e Portaria Remota *(evolução D01 + integrações externas)*

**Objetivo:** Administradoras fazem onboarding self-service de novos condomínios; plataforma suporta portarias remotas (sem porteiro físico) via fluxo de autoatendimento para entregadores.

**Domínios incluídos:** evolução D01, D03 (novos adapters), D04 (fluxo entregador)

**Critério de conclusão:**
- Administradora cadastra novo condomínio sem intervenção técnica
- Fluxo de autoatendimento via QR Code para entregadores em portarias remotas
- Novo adapter de comunicação (ex: Telegram ou e-mail) plugado sem refatoração do core
- API pública documentada para integrações externas (lockers, câmeras, ERPs de administradoras)

---

## 5. Restrições Globais (Global Constraints)

### Restrições Técnicas

- **Stack principal (CRUDs, cadastros, orquestração):** .NET 8 (ASP.NET Core) + PostgreSQL — responsável por D01, D02, D03 e D05. É o owner da encomenda e orquestra o fluxo end-to-end
- **Stack agêntica (reconhecimento de etiquetas):** Mastra (Node.js) — responsável por D04; traz nativamente observabilidade, workflow e tools
- **Frontend do síndico:** React (painel web desktop)
- **Frontend da portaria:** PWA instalável em React, foco Android (99%+ dos dispositivos de guarita no Brasil)
- **Comunicação entre serviços:** assíncrona (evento/fila) — .NET envia foto para o Mastra processar e recebe o resultado; Mastra não mantém estado de negócio
- **Integrações externas (Meta Cloud API, SMS):** implementadas no .NET e expostas ao Mastra como Tool para uso agêntico futuro; o disparo da notificação principal (encomenda recebida) é responsabilidade do .NET após concluir a criação da encomenda
- **Canal de comunicação:** WhatsApp corporativo único da plataforma (Meta Cloud API) + SMS como fallback automático quando o número do morador não tem WhatsApp; detecção automática via API
- **Canal de desenvolvimento:** OpenClaw (conta WhatsApp pessoal) para testes e prototipação
- **Storage de fotos de etiqueta:** MinIO em dev/local; S3 ou Cloudflare R2 em produção
- **Infraestrutura:** serviços gerenciados (cloud-native); sem servidores próprios
- **Multi-tenant:** obrigatório desde o dia 1, mesmo no MVP com um único condomínio
- **Autenticação:** síndico via e-mail+senha no painel web; dispositivo da portaria vinculado ao condomínio via OTP no primeiro uso do PWA (device-bound, não user-bound); moradores não autenticam — são destinatários passivos de notificações

### Restrições de Negócio

- **Prazo:** sem deadline definido
- **Orçamento:** sem restrição declarada; preferência por serviços gerenciados para minimizar custo operacional
- **Regulatório:** LGPD obrigatório — dado mínimo coletado: nome do morador, telefone de celular e unidade (bloco + apartamento). Sem CPF, sem foto do morador. Fotos de etiquetas expurgadas automaticamente após confirmação de retirada ou após 30 dias, o que ocorrer primeiro. Apenas metadados (protocolo, unidade, timestamps) são retidos para auditoria. O opt-in do morador para recebimento de mensagens é coletado em assembleia do condomínio na adesão à plataforma (aprovação da maioria dos moradores + termo assinado pelo síndico).
- **UX:** zero fricção para moradores — sem instalação de app, sem cadastro ativo, sem interação com a plataforma. Apenas recebem mensagens informativas. Porteiro opera via PWA instalado no dispositivo Android da guarita.

### Non-Goals do Sistema

- Controle de acesso de visitantes ou veículos
- Gestão financeira, boletos ou taxas condominiais
- Aplicativo mobile nativo (iOS/Android) — a portaria usa PWA instalável; morador não usa app
- Validação de identidade de quem retira a encomenda — o token é ao portador; a entrega a terceiros é regida pelo regulamento do condomínio
- Interação conversacional do morador com a plataforma — o canal é informativo unidirecional
- Rastreamento de qual porteiro está em plantão no dispositivo — o controle de plantão é externo ao sistema no MVP
- Integração com câmeras de segurança ou reconhecimento facial
- Integração com lockers físicos (smart lockers)
- Rastreamento de encomendas nos Correios ou transportadoras
- Substituição de sistemas de gestão condominial existentes (ERP de condomínio)

---

## 6. Glossário de Negócio (Business Glossary)

| Termo | Definição | Domínio(s) |
|---|---|---|
| Unidade | Apartamento identificado por bloco e número (ex: BL 10 / AP 706). Unidade mínima de endereçamento no condomínio. | D01, D02 |
| Tenant | Cada condomínio como instância isolada de dados e configuração na plataforma. | D01, D03 |
| Protocolo | Número único gerado pelo sistema para cada encomenda registrada. Equivalente digital do número do caderno de protocolo. | D02 |
| Etiqueta | Rótulo físico colado na encomenda pelo remetente ou transportadora, contendo nome e endereço do destinatário. | D04 |
| Guarita / Portaria | Local físico de controle de acesso do condomínio onde as encomendas são recebidas e armazenadas até a retirada. | D01, D02 |
| Síndico | Responsável legal pelo condomínio. Papel administrativo máximo na plataforma para seu tenant. | D01, D05 |
| Administradora | Empresa terceirizada que gerencia um ou mais condomínios. Papel com acesso a múltiplos tenants. | D01 |
| Adapter de Canal | Implementação específica de um meio de comunicação (WhatsApp, SMS e futuros) que segue a interface padrão do Hub de Comunicação. | D03 |
| Ciclo de Vida da Encomenda | Sequência de estados de uma encomenda: Recebida → Aguardando Retirada → Retirada Confirmada → Devolvida. | D02 |
| Token de Retirada | PIN de 6 dígitos gerado automaticamente no momento da notificação ao morador. É ao portador: quem apresentar o PIN ao porteiro retira a encomenda. A validação de identidade de quem retira não é responsabilidade do sistema — segue o regulamento do condomínio. Porteiro e síndico podem reemitir o token quando necessário. | D02, D03 |
| WhatsApp Corporativo da Plataforma | Número único de WhatsApp Business operado pela plataforma (não por condomínio) para envio de notificações a todos os moradores de todos os tenants. O opt-in é coletado em assembleia do condomínio na adesão ao serviço. Toda mensagem identifica o nome completo do condomínio remetente. | D03 |
| App da Portaria | PWA (Progressive Web App) instalável em dispositivo Android da guarita, vinculado ao condomínio via OTP no primeiro uso. Interface exclusiva do porteiro para fotografar etiquetas, confirmar extrações de baixa confiança e registrar retiradas. | D01, D02, D04 |
| Tool (Mastra) | Função exposta ao agente Mastra que encapsula uma capacidade executável (ex.: enviar WhatsApp, enviar SMS, consultar API interna). O .NET implementa a integração; o Mastra pode invocá-la dentro de um workflow agêntico. | D03, D04 |
| Fuzzy Matching | Técnica de busca aproximada que cruza o texto extraído da etiqueta (ex: "Joao S, Ap 101B") com os dados do cadastro (ex: "João Silva, Bloco B, Apto 101"), tolerando variações de grafia, abreviações e acentuação. | D04 |
| Human-in-the-Loop | Fluxo de exceção em D04: quando a confiança da IA na extração é abaixo do limiar aceitável, o porteiro recebe um alerta para revisar e confirmar os dados manualmente antes de registrar a encomenda. | D04 |
| Portaria Remota | Modelo de condomínio sem porteiro físico presencial, operado remotamente. Neste modelo, o fluxo de autoatendimento via QR Code para o entregador se torna o fluxo primário (Fase 3+). | D03 |
| VLM (Vision-Language Model) | Modelo de IA multimodal capaz de analisar imagens e extrair informações textuais com compreensão semântica (ex: GPT-4o, Claude 3.5 Sonnet, Gemini Flash). Usado em D04 para leitura de etiquetas. | D04 |

---

## 7. Premissas e Riscos Globais (Assumptions & Risks)

### Premissas

- O síndico ou administradora fornece planilha de unidades e moradores para importação inicial
- Moradores têm número de celular ativo (WhatsApp preferencial; SMS como fallback automático para números sem WhatsApp)
- A adesão do condomínio à plataforma é aprovada em assembleia pela maioria dos moradores; o síndico assina o termo de consentimento LGPD e opt-in coletivo para envio de mensagens
- A guarita dispõe de celular Android com câmera usável pelo porteiro para instalar o PWA e fotografar as etiquetas
- Entregadores continuam operando como hoje — nenhuma mudança de comportamento é exigida deles

### Riscos Globais

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Mudança de política ou preço da WhatsApp Business API | Média | Alto | Hub de Comunicação com interface plugável — SMS já é fallback ativo; troca/adição de adapter sem refatoração do core |
| Bloqueio ou degradação do número corporativo único da plataforma pela Meta (spam rating) | Média | Alto | Monitoramento ativo do quality rating; pool de números como contingência; conteúdo de template estritamente transacional |
| Foto da etiqueta ilegível (baixa qualidade, embalagem danificada, fita reflexiva) | Alta | Médio | Human-in-the-Loop obrigatório: porteiro confirma manualmente no PWA quando confiança da IA é baixa |
| Fuzzy Matching falha em nomes muito ambíguos ou unidades inexistentes no cadastro | Média | Médio | Limiar de confiança configurável; alerta ao porteiro no PWA; fallback para seleção manual da unidade |
| Número de celular do morador desatualizado no cadastro | Alta | Médio | Dashboard do síndico alerta sobre números com falha de entrega (WhatsApp e SMS); síndico atualiza manualmente no painel web |
| Qualidade ruim dos dados na importação inicial (planilha do síndico) | Alta | Alto | Template de planilha validado + revisão assistida no onboarding |
| Resistência do porteiro à mudança de processo | Média | Alto | UX do PWA extremamente simples — porteiro só fotografa; sistema faz o resto. Treinamento no piloto. |
| Guarita sem dispositivo Android compatível com o PWA | Baixa | Alto | Premissa validada no onboarding; condomínio piloto já dispõe de celular Android na guarita |
| LGPD: dados de terceiros (nome/endereço) na etiqueta transitando por provedores de IA | Média | Médio | DPA com provedores de VLM; expurgo automático de imagens pós-retirada ou em 30 dias |
| Custo consolidado de mensagens (WhatsApp + SMS) não coberto pelo modelo de precificação | Média | Médio | Custo centralizado na plataforma (não no tenant); precificação por unidade ativa cobre o volume estimado (~1.500 mensagens/mês por condomínio de 200 unidades) |

---

## 8. Histórico de Revisões (Revision History)

| Versão | Data | Autor | Alterações |
|---|---|---|---|
| 0.1 | 2026-04-11 | Tasso Gomes | Versão inicial — greenfield, domínios validados, roadmap macro definido |
| 0.2 | 2026-04-11 | Tasso Gomes | Pivot do fluxo: porteiro é o operador primário (não o entregador). Adição de Token de Retirada (D02), Fuzzy Matching e Human-in-the-Loop (D04), política de retenção de imagens LGPD (D05), glossário expandido. |
| 0.3 | 2026-04-17 | Tasso Gomes | Decisões arquiteturais: (1) stack dividida — .NET 8 + PostgreSQL para CRUDs/cadastros (D01, D02, D03, D05) e Mastra (Node.js) para agente de reconhecimento (D04); comunicação assíncrona entre serviços. (2) WhatsApp corporativo único da plataforma (não por tenant) + SMS como fallback automático; detecção via API; integrações expostas como Tool ao Mastra. (3) Porteiro passa a operar PWA instalável em Android (deixa de usar WhatsApp); dispositivo vinculado ao condomínio via OTP (device-bound). (4) Token de Retirada simplificado: 6 dígitos, ao portador; validação de identidade é regulamento do condomínio (fora do escopo). (5) Morador deixa de interagir com a plataforma — apenas recebe notificações informativas; opt-in coletivo via assembleia. (6) Storage: MinIO em dev, S3/R2 em prod. |

---

*Vision Doc gerado com a skill `flow-vision-creator`. Para criar Domain Docs a partir deste documento, use a skill `flow-domain-creator`.*
