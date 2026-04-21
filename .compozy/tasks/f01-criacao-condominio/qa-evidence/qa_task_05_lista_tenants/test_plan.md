# Plano de Testes — Lista de Tenants no Backoffice (CF5)

**Task ID:** qa_task_05
**Tipos:** UI, API

## Tenant de Referencia

- Nome: Residencial Teste QA
- CNPJ: 11.222.333/0001-81
- ID: f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5
- Status: PreAtivo (status=1)

## Casos de Teste

### TC-01: Lista exibe o tenant criado
- **Pre-condicao:** Tenant "Residencial Teste QA" existe no banco com status PreAtivo
- **Passos:**
  1. Navegar para /condominios (autenticado)
  2. Aguardar carregamento da tabela
  3. Verificar se "Residencial Teste QA" aparece na lista
- **Expected:** Linha com "Residencial Teste QA", CNPJ 11.222.333/0001-81 e status pre-ativo visivel
- **Tipo:** UI

### TC-02: Colunas da tabela
- **Pre-condicao:** Tabela carregada com pelo menos um registro
- **Passos:**
  1. Verificar cabecalhos da tabela
- **Expected:** Colunas Nome, CNPJ, Status, Criado em, Ativado em presentes
- **Tipo:** UI

### TC-03: Filtro por status pre-ativo
- **Pre-condicao:** Tabela carregada
- **Passos:**
  1. Clicar no botao "Pre-ativo"
  2. Aguardar recarga
  3. Verificar se "Residencial Teste QA" aparece
- **Expected:** Tenant pre-ativo aparece; nenhum tenant ativo e exibido
- **Tipo:** UI

### TC-04: Filtro por status ativo
- **Pre-condicao:** Tabela carregada
- **Passos:**
  1. Clicar no botao "Ativo"
  2. Aguardar resultado
- **Expected:** "Residencial Teste QA" NAO aparece (ainda pre-ativo)
- **Tipo:** UI

### TC-05: Busca por nome
- **Pre-condicao:** Tabela carregada, filtro "Todos"
- **Passos:**
  1. Digitar "Residencial Teste" no campo de busca
  2. Aguardar debounce (300ms) e recarga
- **Expected:** "Residencial Teste QA" aparece no resultado
- **Tipo:** UI

### TC-06: Busca por CNPJ
- **Pre-condicao:** Tabela carregada
- **Passos:**
  1. Limpar busca
  2. Digitar "11.222" no campo de busca
  3. Aguardar debounce e recarga
- **Expected:** "Residencial Teste QA" aparece no resultado
- **Tipo:** UI

### TC-07: Link para painel de detalhes
- **Pre-condicao:** "Residencial Teste QA" visivel na lista
- **Passos:**
  1. Clicar no nome "Residencial Teste QA" na lista
  2. Verificar URL resultante
- **Expected:** Redireciona para /condominios/f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5 (nao /undefined)
- **Tipo:** UI

### TC-08: Paginacao (se aplicavel)
- **Pre-condicao:** Quantidade de registros > 20 (PAGE_SIZE)
- **Passos:**
  1. Verificar se controles de paginacao estao visiveis
  2. Se presentes, clicar em "Proxima" e verificar mudanca de pagina
- **Expected:** Se houver mais de 20 registros, controles de paginacao funcionam; com 2 registros, sem paginacao
- **Tipo:** UI
