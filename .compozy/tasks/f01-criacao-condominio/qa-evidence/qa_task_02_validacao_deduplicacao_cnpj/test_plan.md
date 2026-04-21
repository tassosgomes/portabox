# Plano de Testes — Validação e Deduplicação de CNPJ

**Task ID:** qa_task_02
**Tipos:** UI, API

## Contexto Arquitetural

- Validação de formato e dígito verificador: local (frontend, `validateCnpj`)
- Verificação de duplicata: retorno HTTP 409 da API no POST `/v1/admin/condominios`
- A duplicata é detectada na submissão final (etapa 4 — Revisão), não na etapa 1
- A mensagem de erro de duplicata é montada a partir das extensões do ProblemDetails 409

## Casos de Teste

### TC-01: CNPJ com formato inválido (incompleto)
- **Pré-condição:** usuário autenticado no wizard `/condominios/novo`
- **Passos:**
  1. Preencher nome fantasia com valor válido
  2. Digitar CNPJ incompleto: `11.222.333/0001` (sem os dígitos verificadores)
  3. Clicar no botão "Avançar"
- **Expected:** mensagem "CNPJ inválido" exibida; campo marcado como inválido; permanece na etapa 1
- **Tipo:** UI

### TC-02: CNPJ com formato correto mas dígito verificador inválido
- **Pré-condição:** usuário autenticado no wizard `/condominios/novo`
- **Passos:**
  1. Preencher nome fantasia com valor válido
  2. Digitar CNPJ `11.222.333/0001-00` (formato correto, dígito errado)
  3. Clicar no botão "Avançar"
- **Expected:** mensagem de erro de CNPJ inválido exibida; permanece na etapa 1
- **Tipo:** UI

### TC-03: CNPJ válido e duplicado — duplicata detectada
- **Pré-condição:** tenant "Residencial Teste QA" com CNPJ 11.222.333/0001-81 existe no banco
- **Passos:**
  1. Preencher nome com valor qualquer
  2. Digitar CNPJ `11.222.333/0001-81`
  3. Avançar todas as etapas com dados válidos
  4. Na etapa 4 (Revisão), clicar "Criar condomínio"
- **Expected:** mensagem de duplicata com nome "Residencial Teste QA" e data de criação; botão de submit indica erro
- **Tipo:** UI + API (HTTP 409)

### TC-04: CNPJ válido e único — permite avançar
- **Pré-condição:** usuário autenticado; CNPJ 11.444.777/0001-61 não existe no banco
- **Passos:**
  1. Preencher nome fantasia
  2. Digitar CNPJ `11.444.777/0001-61`
  3. Clicar "Avançar"
- **Expected:** sem mensagem de erro; avança para a etapa 2
- **Tipo:** UI
