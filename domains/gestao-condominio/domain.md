# Domain Document — Gestão do Condomínio

> **Nível 1 da hierarquia de documentação.** Este documento detalha o bounded context do domínio D01. Sempre forneça o `vision.md` junto com este arquivo ao iniciar sessões de PRD ou Tech Spec dentro deste domínio.

**Domínio:** D01 — Gestão do Condomínio
**Responsável:** a definir
**Status:** `planned`
**Fase do Roadmap:** Fase 1 — MVP: Substituir o Caderno
**Última revisão:** 2026-04-17

---

## 1. Propósito do Domínio (Domain Purpose)

### Responsabilidade Principal

Manter o cadastro canônico multi-tenant de condomínios, blocos, unidades e moradores — sendo a única fonte de verdade consultada por todos os outros domínios para identificar destinatários e validar endereços.

### Problema que Resolve

Sem um cadastro estruturado e confiável, é impossível determinar se um endereço de etiqueta pertence ao condomínio, qual unidade corresponde a variações como "BL A AP 201" ou "301B", e para qual número de WhatsApp enviar a notificação. Este domínio é a fundação que torna possível toda a automação dos outros domínios.

### Fora do Escopo deste Domínio (Out of Scope)

- **Fuzzy matching de texto de etiqueta** → responsabilidade de D04 (Reconhecimento de Etiquetas); D01 apenas provê a forma canônica das unidades
- **Envio de notificações e mensagens** → responsabilidade de D03 (Hub de Comunicação); D01 apenas armazena o número de celular do morador e resolve o destino (escolhendo WhatsApp ou SMS)
- **Ciclo de vida de encomendas** → responsabilidade de D02 (Controle de Encomendas)
- **Autorizado a Retirar (pré-cadastrado ou ad-hoc)** → fora do escopo da plataforma; a entrega a terceiros é regida pelo regulamento do condomínio (token é ao portador)
- **Administradora como papel próprio com acesso multi-tenant** → fora do escopo desta versão; síndico é o papel administrativo máximo por tenant
- **Histórico de moradores anteriores por unidade** → não rastreado; apenas estado atual importa
- **Interação conversacional do morador com a plataforma** → morador é destinatário passivo de notificações; alterações de cadastro são responsabilidade do síndico via painel web
- **Rastreamento de qual porteiro está em plantão** → externo ao sistema no MVP; a portaria mantém esse controle por meios próprios
- **Múltiplas portarias como entidade distinta** → não afeta o MVP; o produto opera na portaria que recebe a encomenda

---

## 2. Usuários do Domínio (Domain Users)

| Perfil (Role) | O que faz neste domínio | Frequência de uso | Canal de acesso |
|---|---|---|---|
| Síndico | Cria e configura o condomínio; gerencia blocos, unidades e moradores; gera OTPs para vincular dispositivos da portaria; revoga dispositivos | Baixa (setup inicial) + Esporádica (manutenção) | Painel web |
| Porteiro | Consulta indiretamente o cadastro via D02 ao registrar encomendas no PWA; não acessa dados de contato dos moradores | Diária | PWA da Portaria (exclusivo) |
| Morador | Destinatário passivo de notificações informativas; não interage com a plataforma | — | — |
| Sistema (D02, D03, D04) | Consulta a API interna para validar unidades e obter o destino de notificação do morador (resolvido internamente com canal já escolhido) | A cada encomenda registrada | API interna |

---

## 3. Entidades Principais (Core Entities)

> Entidades são os objetos de negócio centrais deste domínio. Não é um schema de banco de dados — é o vocabulário do domínio.

| Entidade | Descrição | Atributos Principais | Relacionamentos |
|---|---|---|---|
| Condomínio | Tenant raiz. Representa um condomínio residencial como instância isolada na plataforma. O opt-in coletivo para envio de mensagens é registrado na adesão (aprovação em assembleia + termo do síndico). | Nome, endereço completo, data da adesão, status (ativo/inativo) | Contém: Blocos, Síndico(s), Dispositivos da Portaria |
| Bloco | Agrupamento lógico de unidades dentro de um condomínio (ex: Bloco A, Torre 1, Bloco 2). | Código/nome do bloco, condomínio ao qual pertence | Pertence a: Condomínio. Contém: Unidades |
| Unidade | Apartamento identificado na forma canônica: bloco + andar + número (ex: Bloco A / Andar 2 / Apto 201). É a referência que D04 tenta mapear ao ler uma etiqueta. | Bloco, andar, número do apartamento, status (ocupada/vaga) | Pertence a: Bloco. Possui: Moradores |
| Morador | Residente associado a uma unidade. Pode haver mais de um por unidade; todos são notificados quando uma encomenda chega. O canal de entrega (WhatsApp ou SMS) é determinado automaticamente pela plataforma a partir do número de celular. | Nome completo, número de celular, canal resolvido (WhatsApp/SMS), status (ativo/inativo), unidade | Pertence a: Unidade |
| Síndico | Administrador do tenant. Acessa o sistema exclusivamente via painel web. Responsável por todas as operações de cadastro, incluindo alterações solicitadas por moradores (que são comunicadas por canais externos à plataforma). | Nome, e-mail, número de celular, condomínio | Administra: Condomínio |
| Dispositivo da Portaria | Celular Android da guarita com o PWA instalado e vinculado a um condomínio via OTP. É a identidade operacional na portaria — não há vínculo com uma pessoa física (o controle de qual porteiro está em plantão é externo ao sistema). | Identificador único do dispositivo, data de vinculação, status (ativo/revogado) | Vinculado a: Condomínio |

---

## 4. Features Previstas (Planned Features)

| # | Feature | Descrição | Prioridade | Status | PRD |
|---|---|---|---|---|---|
| F01 | Assistente de Criação de Condomínio | Wizard guiado (web) para configuração inicial do tenant: dados do condomínio, registro do opt-in coletivo (assembleia + termo assinado pelo síndico) e cadastro do primeiro síndico. No piloto, iniciado manualmente pela equipe do produto. | Must Have | `done` | [`.compozy/tasks/f01-criacao-condominio/_prd.md`](/.compozy/tasks/f01-criacao-condominio/_prd.md) |
| F02 | Gestão de Blocos e Unidades | CRUD de blocos e unidades via interface web. Definição da estrutura canônica do condomínio (bloco + andar + número). | Must Have | `done` | [`.compozy/tasks/f02-gestao-blocos-unidades/_prd.md`](/.compozy/tasks/f02-gestao-blocos-unidades/_prd.md) |
| F03 | Gestão Individual de Moradores | Incluir, editar e inativar moradores via web. Associação a uma unidade. Validação de formato do número de celular no cadastro. | Must Have | `planned` | — |
| F04 | Importação em Massa de Moradores | Upload de planilha (template fornecido) com moradores em lote. Dados existentes não são sobrescritos sem confirmação explícita. Relatório de erros pós-importação. | Must Have | `planned` | — |
| F05 | Autenticação do Síndico | Login via painel web (e-mail + senha). | Must Have | `planned` | — |
| F06 | Vinculação do Dispositivo da Portaria via OTP | Síndico gera OTP na interface web; porteiro insere o OTP no primeiro uso do PWA instalado no dispositivo Android da guarita; sistema vincula o dispositivo ao condomínio até revogação explícita. | Must Have | `planned` | — |
| F07 | API Interna de Busca de Unidade e Morador | Endpoint interno consumido por D02, D03 e D04 para: (a) validar se uma unidade existe no condomínio; (b) retornar moradores da unidade (sem expor o número de celular ao chamador — D03 recebe o destino já resolvido com o canal escolhido automaticamente: WhatsApp ou SMS). | Must Have | `planned` | — |
| F08 | Gestão de Dispositivos da Portaria pelo Síndico | Síndico pode listar, desvincular e revogar acesso de dispositivos da portaria via interface web. | Should Have | `planned` | — |
| F09 | Dashboard de Ocupação | Visão do síndico com unidades sem morador cadastrado e moradores com número de celular com falha de entrega (WhatsApp e SMS). | Could Have | `planned` | — |

**Prioridades (MoSCoW):** `Must Have` · `Should Have` · `Could Have` · `Won't Have`
**Status possíveis:** `planned` · `prd-ready` · `in-progress` · `done` · `out-of-scope`

---

## 5. Dependências (Domain Dependencies)

### Depende de (Upstream)

Nenhuma. D01 é o domínio raiz — não consome dados de outros domínios do sistema.

### Fornece para (Downstream)

| Domínio | O que fornece | Tipo | Criticidade |
|---|---|---|---|
| D02 — Controle de Encomendas | Validação de existência de unidade; lista de moradores da unidade (sem número de celular exposto) | API interna (leitura) | Crítica — D02 não registra encomenda sem unidade válida |
| D04 — Reconhecimento de Etiquetas | Base canônica de unidades para Fuzzy Matching (bloco + andar + número de todos os apartamentos do condomínio) | API interna (leitura) | Crítica — acurácia do D04 depende da qualidade do cadastro |
| D03 — Hub de Comunicação | Destino resolvido da notificação ao morador — D01 escolhe o canal (WhatsApp ou SMS) a partir do número de celular e entrega a D03 o destino pronto, sem expor o dado bruto | API interna (evento) | Crítica — sem o destino resolvido, a notificação não é enviada |
| D05 — Relatórios & Auditoria | Estrutura do condomínio (blocos, unidades, moradores) para cruzamento com histórico de encomendas | API interna (leitura) | Média |

### Integrações Externas

Nenhuma no MVP. Futuro: integração com ERPs de administradoras para importação automática de cadastros.

---

## 6. Regras de Negócio (Business Rules)

| ID | Regra | Origem |
|---|---|---|
| RN-01 | Cada condomínio é um tenant isolado — dados de tenants distintos nunca se cruzam em nenhuma operação | Arquitetura multi-tenant |
| RN-02 | Uma unidade pertence a exatamente um bloco e um condomínio; não pode ser movida entre blocos | Modelo de dados |
| RN-03 | Uma unidade pode ter 1 a N moradores ativos; todos recebem a notificação de encomenda recebida | Decisão de produto |
| RN-04 | O número de celular do morador é dado privado — nunca exposto ao porteiro, entregador ou a outros domínios em texto aberto; D01 resolve o destino (canal + identificador opaco) internamente antes de passar para D03 | LGPD + Decisão de produto |
| RN-05 | A forma canônica da unidade (bloco + andar + número) é definida no cadastro do D01; variações de escrita encontradas em etiquetas são de responsabilidade do D04 para resolução | Separação de responsabilidades |
| RN-06 | Apenas o síndico pode criar, editar ou inativar blocos, unidades e moradores | Controle de acesso |
| RN-07 | Um morador está associado a uma única unidade no estado atual; não há rastreamento de unidades anteriores | Escopo do MVP |
| RN-08 | A importação em massa não sobrescreve dados de moradores já cadastrados sem confirmação explícita do síndico na interface | Integridade de dados |
| RN-09 | A portaria acessa o sistema exclusivamente via PWA instalado no dispositivo Android vinculado ao condomínio; o dispositivo é a identidade operacional (não a pessoa); o rastreamento de qual porteiro está em plantão é externo ao sistema no MVP | Privacidade + Decisão de produto |
| RN-10 | Um condomínio deve ter ao menos um síndico ativo; não é possível inativar o último síndico sem designar um substituto | Governança do tenant |
| RN-11 | A escolha do canal de notificação (WhatsApp ou SMS) é automática: D01 detecta via API se o número do morador tem WhatsApp ativo e, caso contrário, resolve para SMS. A escolha pode ser recalculada a qualquer momento (ex: morador passa a ter WhatsApp) | Decisão de produto |
| RN-12 | A vinculação de um dispositivo da portaria ao condomínio requer OTP de uso único gerado pelo síndico na interface web; o OTP expira em 30 minutos; um dispositivo revogado precisa de novo OTP para ser revinculado | Segurança de acesso |

---

## 7. Eventos do Domínio (Domain Events)

### Produz (Publishes)

- `condominio.cadastrado` — quando um novo condomínio (tenant) é criado na plataforma
- `morador.cadastrado` — quando um novo morador é associado a uma unidade
- `morador.atualizado` — quando dados de um morador são alterados pelo síndico (inclui mudança de número de celular, que pode impactar o canal de notificação resolvido)
- `morador.inativado` — quando um morador é desativado no cadastro
- `dispositivo-portaria.vinculado` — quando um dispositivo da portaria é autenticado via OTP e vinculado ao condomínio
- `dispositivo-portaria.revogado` — quando um dispositivo tem seu acesso revogado pelo síndico

### Consome (Subscribes)

Nenhum. D01 é upstream de todos os outros domínios.

---

## 8. Estratégia de Desenvolvimento (Development Strategy)

### Ordem de Implementação Sugerida

1. **F01** — Assistente de criação do condomínio (tenant setup); sem isso nada existe
2. **F02** — Blocos e Unidades; estrutura canônica que D04 vai consumir
3. **F05 + F06** — Autenticação do síndico (web) e vinculação do dispositivo da portaria (OTP no PWA); sem acesso controlado, o cadastro não pode ser operado com segurança
4. **F03 + F04** — Gestão individual e importação em massa de moradores
5. **F07** — API interna de busca; habilita D02, D03 e D04 a entrarem em desenvolvimento
6. **F08** — Gestão de dispositivos da portaria pelo síndico (revogação)
7. **F09** — Dashboard de ocupação (observabilidade do cadastro)

### Riscos do Domínio

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Planilha de importação com dados inconsistentes (nomes errados, blocos inexistentes, números inválidos) | Alta | Alto | Template rígido com validação automática + relatório de erros pós-importação antes de confirmar |
| Cadastro incompleto de unidades compromete acurácia do D04 | Alta | Alto | Checklist de onboarding: importação deve cobrir 100% das unidades antes de ativar o sistema |
| OTP do dispositivo da portaria interceptado ou compartilhado indevidamente | Baixa | Alto | OTP expira em 30 minutos e é de uso único; síndico pode revogar acesso do dispositivo a qualquer momento (F08) |
| Morador com número de celular errado no cadastro | Alta | Médio | Validação de formato no cadastro; F09 (dashboard) alerta síndico sobre números com falha de entrega (WhatsApp e SMS) |
| Detecção WhatsApp/SMS via API incorreta (falso negativo gera SMS desnecessário; falso positivo impede entrega) | Média | Médio | Reprocessar detecção periodicamente; monitorar taxa de falha de entrega por canal e reavaliar o destino resolvido |

---

## 9. Questões em Aberto (Open Questions)

- [ ] Como o condomínio-piloto será criado na prática? Quem da equipe executa o setup inicial e qual o checklist mínimo para considerar o tenant pronto para operar?
- [ ] Há um número máximo de dispositivos da portaria por condomínio, ou é ilimitado? Qual o comportamento quando um novo dispositivo é vinculado enquanto outro ainda está ativo (coexistência vs substituição)?
- [ ] Qual o SLA de detecção de mudança de canal (WhatsApp ↔ SMS) quando o morador passa a ter ou a não ter mais WhatsApp no número cadastrado? Reavaliação proativa periódica ou reativa (por falha de entrega)?

---

*Domain Doc gerado com a skill `flow-domain-creator`. Para criar PRDs das features deste domínio, use a skill `flow-prd-creator` fornecendo o `vision.md` e este `domain.md` como contexto.*
