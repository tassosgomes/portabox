# Plano de Testes — CF1: Wizard Criacao de Tenant

**Task ID:** qa_task_01
**Tipos:** UI + Banco

---

## Casos de Teste

### CT-01: Login e acesso ao formulario de novo condominio
- **Pre-condicao:** Servidor frontend e backend em execucao
- **Passos:**
  1. Navegar para http://localhost:5173/login
  2. Preencher email: operator@portabox.dev e password: PortaBox123!
  3. Submeter login
  4. Navegar para /condominios/novo
- **Expected:** Formulario do wizard exibido com indicador de progresso na etapa 1
- **Tipo:** UI

### CT-02: Etapa 1 — Validacao de campos obrigatorios
- **Pre-condicao:** Autenticado, wizard na etapa 1
- **Passos:**
  1. Clicar em "Avançar" sem preencher nenhum campo
- **Expected:** Mensagem de erro exibida para nome fantasia e CNPJ; wizard nao avanca
- **Tipo:** UI

### CT-03: Etapa 1 — Validacao de CNPJ invalido
- **Pre-condicao:** Autenticado, wizard na etapa 1
- **Passos:**
  1. Preencher nome fantasia: "Residencial Teste QA"
  2. Preencher CNPJ: 00.000.000/0000-00 (invalido)
  3. Clicar em "Avançar"
- **Expected:** Mensagem de erro "CNPJ inválido" exibida; wizard nao avanca
- **Tipo:** UI

### CT-04: Etapa 1 — Preenchimento completo e avanco para etapa 2
- **Pre-condicao:** Autenticado, wizard na etapa 1
- **Passos:**
  1. Preencher nome fantasia: "Residencial Teste QA"
  2. Preencher CNPJ valido: 11.222.333/0001-81
  3. Preencher logradouro: "Rua das Flores"
  4. Preencher numero: "123"
  5. Preencher cidade: "Sao Paulo", UF: "SP", CEP: "01310-100"
  6. Preencher administradora: "Administradora Teste Ltda"
  7. Clicar em "Avançar"
- **Expected:** Wizard avanca para etapa 2; indicador de progresso mostra etapa 2 ativa
- **Tipo:** UI

### CT-05: Etapa 2 — Validacao de campos obrigatorios
- **Pre-condicao:** Wizard na etapa 2
- **Passos:**
  1. Clicar em "Avançar" sem preencher nenhum campo
- **Expected:** Erros para data assembléia, quorum, nome signatario, CPF, data termo; wizard nao avanca
- **Tipo:** UI

### CT-06: Etapa 2 — Preenchimento completo e avanco para etapa 3
- **Pre-condicao:** Wizard na etapa 2
- **Passos:**
  1. Preencher data assembléia: 2026-03-01
  2. Preencher quorum: "75%"
  3. Preencher nome signatario: "Joao da Silva"
  4. Preencher CPF signatario: 529.982.247-25
  5. Preencher data termo: 2026-03-01
  6. Clicar em "Avançar"
- **Expected:** Wizard avanca para etapa 3; indicador de progresso mostra etapa 3 ativa
- **Tipo:** UI

### CT-07: Etapa 3 — Validacao de campos obrigatorios e formato celular E.164
- **Pre-condicao:** Wizard na etapa 3
- **Passos:**
  1. Clicar "Avançar" sem preencher nada
  2. Preencher nome: "Maria Oliveira", email valido, celular em formato nacional "(11) 91234-5678"
  3. Clicar "Avançar"
- **Expected:**
  - Passo 1: erros para nome, email e celular
  - Passo 2: erro de celular (formato E.164 exigido: +5511...); wizard nao avanca
- **Tipo:** UI

### CT-08: Etapa 3 — Preenchimento com celular E.164 e avanco para revisao
- **Pre-condicao:** Wizard na etapa 3
- **Passos:**
  1. Preencher nome: "Maria Oliveira"
  2. Preencher email: sindico.qa@portabox.dev
  3. Preencher celular: +5511912345678
  4. Clicar em "Avançar"
- **Expected:** Wizard avanca para tela de revisao
- **Tipo:** UI

### CT-09: Tela de revisao — dados exibidos corretamente
- **Pre-condicao:** Wizard na tela de revisao
- **Passos:**
  1. Verificar dados do condominio: nome, CNPJ formatado
  2. Verificar dados do opt-in: datas formatadas, CPF formatado
  3. Verificar dados do sindico: nome, email, celular
- **Expected:** Todos os dados preenchidos exibidos com formatacao correta (CNPJ com mascara, CPF com mascara, datas em pt-BR)
- **Tipo:** UI

### CT-10: Botao Voltar da revisao retorna a etapa 3
- **Pre-condicao:** Wizard na tela de revisao
- **Passos:**
  1. Clicar em "Voltar"
- **Expected:** Wizard retorna a etapa 3 (Sindico responsavel)
- **Tipo:** UI

### CT-11: Confirmacao — criacao do condominio e redirecionamento
- **Pre-condicao:** Wizard na tela de revisao (apos retorno para verificar e renavegar)
- **Passos:**
  1. Navegar de volta a revisao
  2. Clicar em "Criar condomínio"
- **Expected:** Feedback visual de sucesso exibido; operador redirecionado para /condominios/{id}
- **Tipo:** UI

### CT-12: Validacao no banco — condominio criado com status pre-ativo
- **Pre-condicao:** CT-11 executado com sucesso
- **Passos:**
  1. Consultar tabela condominio filtrando por nome_fantasia = "Residencial Teste QA"
  2. Verificar status = 1 (pre-ativo)
  3. Verificar CNPJ = "11222333000181"
- **Expected:** Registro encontrado com status correto e dados corretos
- **Tipo:** Banco

### CT-13: Validacao no banco — opt-in registrado
- **Pre-condicao:** CT-11 executado com sucesso, ID do condominio conhecido
- **Passos:**
  1. Consultar tabela opt_in_record pelo tenant_id
  2. Verificar dados: data_assembleia, quorum, signatario, cpf
- **Expected:** Opt-in registrado com todos os dados do wizard
- **Tipo:** Banco

### CT-14: Validacao no banco — sindico cadastrado
- **Pre-condicao:** CT-11 executado com sucesso, ID do condominio conhecido
- **Passos:**
  1. Consultar tabela sindico pelo tenant_id
  2. Verificar nome_completo e user_id referenciando asp_net_users com email correto
- **Expected:** Sindico registrado com dados corretos
- **Tipo:** Banco
