# PRD — F01: Assistente de Criação de Condomínio

> **Nível 2 da hierarquia de documentação.** Baseado em `vision.md` e `domains/gestao-condominio/domain.md`. Este PRD é entrada para a skill `cy-create-techspec` que definirá a arquitetura de implementação.

**Domínio:** D01 — Gestão do Condomínio
**Feature:** F01 — Assistente de Criação de Condomínio
**Prioridade:** Must Have
**Fase:** 1 — MVP: Substituir o Caderno
**Última revisão:** 2026-04-17

---

## Overview

F01 é a porta de entrada da plataforma: sem ele nenhum outro domínio funciona. É o fluxo interno usado pela equipe da plataforma para provisionar um novo condomínio (tenant) no sistema a partir do momento em que o síndico/administradora formaliza a adesão ao serviço.

**Problema que resolve:** hoje o projeto é greenfield e não há nenhuma forma de criar um tenant. Enquanto não existir um caminho canônico para registrar um condomínio — com seus dados básicos, o opt-in coletivo exigido pela LGPD e o primeiro síndico habilitado — nenhuma outra feature pode ser exercitada. F01 estabelece esse caminho e também a evidência mínima de conformidade legal para começar a enviar mensagens a moradores.

**Para quem é:** um operador interno da equipe da plataforma, responsável por conduzir o onboarding assistido do condomínio-piloto (e, na Fase 1, de qualquer tenant adicional). O síndico não é usuário direto do wizard; ele é o destinatário final do magic link que cria sua conta depois da execução do wizard.

**Por que é valioso:**
- Destrava todas as demais features do MVP (F02, F03, F04, F05, F06, F07) que dependem da existência de um tenant ativo.
- Materializa no produto a política de consentimento LGPD coletivo que o produto declarou no vision doc.
- Serve de "contrato mínimo" entre a equipe da plataforma e o condomínio — tudo que é necessário registrar para considerar o tenant existente na plataforma está aqui.

---

## Goals

Objetivos mensuráveis da feature:

- **G1:** Possibilitar a criação de um tenant completo (condomínio + opt-in coletivo + primeiro síndico) em **menos de 10 minutos** de preenchimento pelo operador, a partir de dados já coletados com o condomínio.
- **G2:** Garantir **100% dos tenants criados na Fase 1** com registro auditável do opt-in coletivo LGPD (data da assembleia, quórum, signatário, data do termo).
- **G3:** Zero cadastros duplicados em produção: todo CNPJ único gera exatamente um tenant ativo.
- **G4:** **100% dos primeiros síndicos** recebem magic link para definição de senha imediatamente após a finalização do wizard.
- **G5:** Nenhum tenant em `pré-ativo` processa encomendas, notificações ou aceita vinculação de dispositivo antes do go-live manual.

---

## User Stories

### Persona primária — Operador da Plataforma (backoffice)

- Como operador, quero cadastrar um novo condomínio em um fluxo guiado para que nenhum dado obrigatório seja esquecido e eu possa concluir o onboarding em uma única sessão.
- Como operador, quero ser impedido de salvar um condomínio cujo CNPJ já exista na plataforma para que eu não crie tenants duplicados por engano.
- Como operador, quero registrar a evidência do opt-in coletivo (metadados obrigatórios; upload opcional de ata/termo) para cumprir a LGPD antes que qualquer mensagem seja enviada.
- Como operador, quero cadastrar o primeiro síndico do condomínio e deixar a plataforma enviar automaticamente o magic link, para não trafegar senha em nenhum canal manual.
- Como operador, quero que o tenant nasça em `pré-ativo` e só passe a `ativo` com um clique explícito meu, para que a operação real só comece após conferência dos cadastros (F02/F04) e vinculação do dispositivo (F06).
- Como operador, quero reenviar o magic link ao síndico quando o link original expirar, para que o onboarding não precise recomeçar.

### Persona secundária — Síndico (destinatário passivo do fluxo)

- Como síndico, quero receber por e-mail um link seguro para definir minha senha e entrar no painel web, sem precisar conhecer o fluxo interno da plataforma.
- Como síndico, quero que o link expire em prazo razoável e possa ser reenviado sob demanda, para que um atraso no primeiro acesso não me deixe fora da plataforma.

### Persona terciária — Auditor/Compliance (leitura futura)

- Como auditor, quero recuperar a evidência do opt-in coletivo de cada tenant (metadados + documentos anexados quando houver) para atestar conformidade com a LGPD em inspeções.

---

## Core Features

### CF1 — Criação do Tenant no Backoffice

- **O que faz:** fluxo web em três etapas lineares que, ao concluir, cria um novo condomínio com status `pré-ativo`, registra o opt-in coletivo e dispara o magic link para o primeiro síndico.
- **Por que é importante:** é o único caminho de provisionamento de tenant no MVP; qualquer outra feature depende dele.
- **Comportamento de alto nível:**
  - Etapa 1 — **Dados do Condomínio:** nome fantasia, CNPJ (com validação de formato e dígito, e bloqueio de duplicata), endereço completo, nome da administradora responsável (campo livre informativo).
  - Etapa 2 — **Opt-in Coletivo LGPD:** metadados obrigatórios (data da assembleia, quórum aprovado, nome completo e CPF do síndico signatário, data de assinatura do termo). Upload opcional de até dois documentos (ata e termo).
  - Etapa 3 — **Primeiro Síndico:** nome completo, e-mail (chave de login), telefone de celular. Sistema valida formato de e-mail e celular.
  - Resumo final com todos os dados para revisão antes de salvar.
  - Após confirmação: tenant criado com status `pré-ativo`, magic link enviado ao e-mail do síndico.

### CF2 — Validação e Deduplicação por CNPJ

- **O que faz:** no momento em que o operador digita o CNPJ, o sistema valida formato, dígito verificador e verifica se o CNPJ já existe na base.
- **Por que é importante:** impede a criação de tenants duplicados, um risco alto sem essa guarda (ADR-003).
- **Comportamento de alto nível:** erro imediato em CNPJ inválido; em caso de duplicata, mensagem clara mostrando o tenant existente (nome, data de criação, status) para o operador decidir como prosseguir. O fluxo não prossegue até que o CNPJ seja válido e único.

### CF3 — Envio Automático de Magic Link ao Primeiro Síndico

- **O que faz:** ao finalizar o wizard, o sistema envia um e-mail ao primeiro síndico contendo um link seguro, de uso único, que permite ao síndico definir sua própria senha e fazer o primeiro login no painel web.
- **Por que é importante:** evita trafegar senhas em canais manuais; mantém o processo auditável.
- **Comportamento de alto nível:**
  - Link tem validade de 72 horas (prazo configurável pelo operador dentro de limites razoáveis).
  - Link só funciona uma vez; após definir senha, torna-se inválido.
  - Após expirar, o operador pode disparar novo link pelo painel de detalhes do tenant.

### CF4 — Painel de Detalhes do Tenant com Ação de Go-live

- **O que faz:** tela interna, acessível ao operador, que consolida todos os dados do tenant criado e oferece a ação "Ativar operação".
- **Por que é importante:** é onde o operador gerencia o ciclo `pré-ativo → ativo` do tenant e acompanha a evolução do onboarding.
- **Comportamento de alto nível:**
  - Exibe status atual (`pré-ativo` ou `ativo`), dados do condomínio, evidência de opt-in e situação do primeiro síndico (senha definida ou pendente).
  - Botão "Ativar operação" transiciona o tenant para `ativo` com registro de auditoria (quem ativou, quando, observação opcional).
  - Botão "Reenviar magic link" disponível enquanto o primeiro síndico ainda não definiu a senha.

### CF5 — Lista de Tenants no Backoffice

- **O que faz:** listagem de todos os tenants existentes, com filtros por status (`pré-ativo`, `ativo`) e busca por nome/CNPJ.
- **Por que é importante:** dá ao operador visibilidade geral do estado de onboarding de todos os condomínios para priorizar ativações pendentes e identificar tenants presos em `pré-ativo`.
- **Comportamento de alto nível:** tabela paginada com colunas: nome, CNPJ, status, data de criação, data da última ativação (quando aplicável). Cada linha leva ao painel de detalhes (CF4).

### CF6 — Log de Auditoria das Transições do Tenant

- **O que faz:** registro permanente, consultável no painel de detalhes, das transições de estado do tenant (criação, ativação, eventuais futuras suspensões/inativações).
- **Por que é importante:** evidência de conformidade e rastreabilidade operacional — quem fez o quê e quando, com observação opcional.
- **Comportamento de alto nível:** log cronológico com entradas imutáveis; cada entrada contém operador responsável, timestamp, tipo de evento e observação livre.

---

## User Experience

### Personas e seus objetivos

- **Operador da plataforma (primária):** quer concluir o onboarding de um condomínio em uma única sessão curta, sem precisar alternar entre sistemas e sem risco de esquecer dados obrigatórios. Já tem em mãos os dados coletados com o síndico/administradora por canais externos (telefone, e-mail, reunião).
- **Síndico (secundária):** quer um primeiro acesso simples — um e-mail, um clique, uma senha e está dentro do painel web.

### Fluxo principal — Criação de um novo tenant

1. Operador faz login no backoffice e acessa "Tenants → Novo condomínio".
2. **Etapa 1 (Dados do Condomínio):** operador preenche nome, CNPJ, endereço, administradora. Validação em tempo real do CNPJ. Em caso de duplicata, o sistema mostra o tenant existente e bloqueia o avanço.
3. **Etapa 2 (Opt-in LGPD):** operador informa metadados obrigatórios da assembleia e do termo; anexa ata e/ou termo em PDF se desejar (opcional).
4. **Etapa 3 (Primeiro Síndico):** operador informa nome, e-mail e celular do síndico.
5. **Revisão:** tela mostra todos os dados preenchidos com opção de voltar a qualquer etapa para edição.
6. **Confirmação:** ao confirmar, o tenant é criado em `pré-ativo`, o magic link é enviado e o operador é redirecionado ao painel de detalhes do tenant.
7. **Pós-wizard:** operador pode ativar o tenant ("Ativar operação") a qualquer momento; pode também reenviar o magic link se necessário.

### Fluxo do síndico (primeira senha)

1. Síndico recebe e-mail com chamada clara ("Bem-vindo ao PortaBox — defina sua senha para acessar o painel do condomínio").
2. Clica no link; chega a uma página segura da plataforma, confirma o e-mail pré-preenchido e define uma senha seguindo política mínima (requisitos serão detalhados em TechSpec).
3. Após definir senha, é direcionado ao login do painel web e pode entrar.
4. Se o link expirou, a página orienta que entre em contato com a equipe da plataforma para reemissão.

### Considerações de UI/UX

- **Fluxo linear com progresso visível:** três etapas claras + revisão + confirmação, com indicador de progresso.
- **Validação em tempo real:** CNPJ, e-mail, celular, datas — todas validadas no momento do preenchimento para evitar descoberta tardia de erro.
- **Mensagens de erro específicas:** "CNPJ já cadastrado — tenant 'Residencial Alfa' criado em 01/03/2026" em vez de "erro de validação".
- **Acessibilidade:** contraste adequado, navegação por teclado, foco visível em todos os campos (padrões WCAG AA — detalhamento em TechSpec).
- **Ações destrutivas ou irreversíveis confirmadas:** ativação do tenant pede confirmação dupla para evitar clique acidental.

### Onboarding e descoberta

- Backoffice é uma UI interna, usada por uma equipe pequena; descoberta é feita por documentação interna da equipe e treinamento. Não há onboarding in-app no MVP.
- Painel de detalhes do tenant contém dicas explícitas ("Para ativar este tenant, confira se: as unidades foram cadastradas em Blocos e Unidades, os moradores foram importados, o dispositivo da portaria foi vinculado") sem automatizar a verificação.

---

## High-Level Technical Constraints

Limites do produto que moldam a feature sem prescrever implementação:

- **Multi-tenant obrigatório desde o MVP** (restrição global do vision.md). Tenants não podem cruzar dados.
- **LGPD:** dados pessoais coletados no F01 são mínimos — do síndico (nome, e-mail, CPF, celular) e metadados do opt-in coletivo. Documentos anexados (ata, termo) seguem política de acesso restrito.
- **CNPJ é o identificador canônico externo do tenant** (ADR-003). Validação de formato e dígito é mandatória.
- **Credenciais em trânsito:** senhas nunca trafegam em canais manuais; magic link com expiração e uso único é o único caminho para o primeiro acesso do síndico.
- **Auditoria:** toda transição de estado do tenant (criação, ativação, futura inativação) precisa ser rastreável com operador, timestamp e observação.
- **Estado `pré-ativo` é vinculante:** todos os demais domínios (D02, D03, D04) devem respeitar esse estado e não operar sobre tenants que ainda não foram ativados.

---

## Non-Goals (Out of Scope)

Explicitamente fora do escopo deste PRD:

- **Self-service de onboarding pelo síndico ou administradora** — diferido para Fase 3 (Roadmap Macro do vision.md).
- **Integração com API da Receita Federal** para pré-preencher razão social a partir do CNPJ — fica como Open Question para futura evolução.
- **Administradora como entidade de primeira classe com papel de acesso multi-tenant** — declarado fora de escopo pelo domain.md; no F01 é apenas campo informativo livre.
- **Cadastro de blocos, unidades e moradores no mesmo fluxo** — responsabilidades de F02 e F04.
- **Vinculação do dispositivo da portaria** — responsabilidade de F06.
- **Checklist automatizado de prontidão para go-live** — rejeitado em ADR-001; conferência é disciplina humana do operador no MVP.
- **Suspensão ou inativação de tenant pelo backoffice** — não previsto no MVP; vai para backlog de pós-MVP quando houver caso de negócio real.
- **Segundo síndico ou múltiplos síndicos no F01** — RN-10 exige ≥1 síndico ativo, mas F01 cadastra somente o primeiro. A adição de síndicos adicionais é responsabilidade de outra feature (possivelmente dentro de F03 ampliada ou nova feature de "Gestão de Síndicos").
- **Política de senha detalhada e política de reset pós-primeiro login** — tratado em F05 (Autenticação do Síndico); F01 apenas dispara o magic link inicial.
- **Rotação, auditoria e alertas LGPD automatizados** — responsabilidade de D05 (Relatórios & Auditoria) na Fase 2.

---

## Phased Rollout Plan

### MVP (Phase 1) — Entrega do F01

- CF1 — Criação do Tenant no Backoffice
- CF2 — Validação e Deduplicação por CNPJ
- CF3 — Envio Automático de Magic Link ao Primeiro Síndico
- CF4 — Painel de Detalhes do Tenant com Ação de Go-live
- CF5 — Lista de Tenants no Backoffice
- CF6 — Log de Auditoria das Transições

**Critérios para considerar a Fase 1 do F01 concluída:**
- Um operador consegue criar um tenant real (condomínio-piloto) do início ao fim pelo backoffice.
- O primeiro síndico recebe e-mail, define senha e consegue logar no painel web (F05).
- O tenant só transita para `ativo` por ação explícita do operador no painel de detalhes.
- Todos os outros domínios (D02, D03, D04) rejeitam operações sobre tenants em `pré-ativo` (integração desta guarda nos domínios é responsabilidade dos PRDs deles).
- Log de auditoria registra corretamente criação e ativação.

### Phase 2 — Hardening e Observabilidade

Evolução do F01 após coleta de feedback do piloto (sem mudar o núcleo):

- Alerta no backoffice sobre tenants que estão em `pré-ativo` há mais de N dias.
- Relatório de conformidade LGPD: lista de tenants, datas de opt-in, presença ou não de documentos anexados.
- Opção de inativação/suspensão do tenant (novo estado e governança), se houver caso de negócio.
- Trilha de auditoria enriquecida (inclusão de IP, user agent do operador).

**Critério de avanço para Phase 3:** ter pelo menos três tenants operando com confiança e identificar padrões de gargalo no onboarding manual.

### Phase 3 — Onboarding Self-service

Alinhado ao Roadmap Macro do vision.md (Fase 3):

- Fluxo self-service para administradoras cadastrarem novos condomínios.
- Evolução do opt-in coletivo para coleta assistida (fluxo no painel do síndico quando o primeiro morador for cadastrado sem evidência).
- Integração com API da Receita Federal para pré-preenchimento de dados.
- API pública documentada para integração com ERPs de administradoras.

---

## Success Metrics

Métricas quantificáveis para avaliar o sucesso do F01:

- **Time-to-tenant:** tempo médio entre o início do wizard e a conclusão (criação + magic link enviado). Meta: ≤ 10 minutos (G1).
- **Taxa de conclusão do wizard:** % de wizards iniciados que são concluídos com sucesso (sem abandono por erro). Meta: ≥ 95% no piloto.
- **Taxa de primeiro login do síndico em até 24h após o envio do magic link.** Meta: ≥ 80%.
- **Taxa de duplicatas criadas em produção:** 0% (G3 — bloqueio por CNPJ garante).
- **Taxa de tenants com opt-in completo (metadados obrigatórios preenchidos):** 100% (G2 — o wizard só deixa concluir se obrigatórios foram preenchidos).
- **Taxa de tenants que ficam presos em `pré-ativo` por mais de 7 dias:** monitorada como sinal de atrito operacional; meta ≤ 20% no piloto, com tendência de queda em Phase 2.
- **Erros reportados pelo operador no wizard por tenant criado:** usar como indicador de fricção — meta ≤ 1 erro reportado a cada 5 tenants criados.

---

## Risks and Mitigations

Riscos não-técnicos que podem afetar o sucesso da feature:

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Operador ativa tenant sem unidades/moradores cadastrados, gerando falhas silenciosas no registro de encomendas | Média | Alto | Painel de detalhes do tenant exibe instrução explícita do que conferir antes de ativar; em Phase 2, alerta automático |
| Síndico não recebe o magic link (filtro anti-spam, e-mail errado) | Média | Médio | Operador tem ação de "Reenviar magic link"; eventualmente contato por canal externo; confirmar e-mail com o síndico antes de finalizar o wizard |
| Dados de opt-in preenchidos sem conferência real da ata/termo existentes | Média | Alto (LGPD) | Operador confirma explicitamente ("li a ata") em registro de auditoria; upload opcional é incentivado como evidência direta |
| Administradora não quer que a plataforma custodie ata/termo | Baixa | Baixo | Upload é opcional por design (ADR-002); metadados são suficientes para o MVP |
| CNPJ digitado errado mas com dígito válido (CNPJ de outro condomínio) | Baixa | Alto | Etapa de revisão mostra os dados antes de salvar; operador deve confirmar com o síndico |
| Adoção do fluxo pelo operador inconsistente (usa canais paralelos para definir senha do síndico) | Média | Alto | Política operacional escrita + treinamento; painel não expõe nenhuma forma alternativa de disparo de senha |
| Condomínio-piloto não aprova opt-in em assembleia a tempo e precisa cadastrar "no escuro" | Baixa | Alto | Condomínio só pode ser cadastrado após opt-in formal — disciplina comercial da equipe; nenhum workaround no produto |
| Gargalo operacional quando o volume de condomínios crescer (todos passam pela equipe) | Alta (longo prazo) | Médio | Não é risco do MVP; mitigado pela Fase 3 (self-service) |

---

## Architecture Decision Records

Decisões registradas durante o brainstorming deste PRD:

- [ADR-001: Onboarding de Tenant no MVP — Operador Interno, Wizard Mínimo e Go-live Manual Independente](adrs/adr-001.md) — Opta por UI de backoffice executada pela equipe da plataforma, sem checklist automatizado e com go-live manual como ação separada do wizard.
- [ADR-002: Registro do Opt-in Coletivo LGPD com Metadados Obrigatórios e Upload Opcional](adrs/adr-002.md) — Exige metadados estruturados do opt-in no wizard; anexar ata/termo é opcional para reduzir fricção no piloto sem abrir mão de conformidade.
- [ADR-003: CNPJ Obrigatório como Identificador Canônico do Condomínio](adrs/adr-003.md) — Fixa o CNPJ como chave canônica externa do tenant com validação de dígito e bloqueio de duplicata.

---

## Open Questions

Itens que precisam de decisão em TechSpec ou de stakeholder externo antes/durante a implementação:

- Prazo exato de expiração do magic link (72h é uma sugestão inicial). Qual a política de segurança desejada?
- Tamanho máximo e tipos aceitos para upload da ata/termo (PDF apenas? JPEG/PNG? Limite em MB?).
- Política de retenção dos documentos anexados quando um tenant eventualmente for inativado (fora do escopo do MVP, mas precisa ser definida antes de entrar em produção).
- Formato exato do campo "quórum aprovado" no opt-in: texto livre, número de presentes + aprovação ou percentual?
- Política de senha mínima para o síndico: regras de complexidade, rotação, MFA opcional? (Provavelmente coordenado com F05.)
- Como o backoffice é protegido: SSO corporativo? Autenticação independente? Controle de acesso por operador (quem pode criar vs quem pode ativar)?
- Integração futura com API da Receita Federal para pré-preenchimento de razão social: Phase 2 ou Phase 3?
- Comportamento em caso de reuso do CNPJ após inativação de um tenant anterior (permitido? bloqueado? regras de reativação?) — tratar junto com definição de inativação de tenant.
- Existem campos adicionais do condomínio exigidos por uma administradora parceira (ex.: código interno do cliente na administradora) que devem ser contemplados desde o MVP?

---

*PRD gerado com a skill `cy-create-prd`. Próximo passo no pipeline: `cy-create-techspec` para traduzir estes requisitos em arquitetura e plano de implementação.*
