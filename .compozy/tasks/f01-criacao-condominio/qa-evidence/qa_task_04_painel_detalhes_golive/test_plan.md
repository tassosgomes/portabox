# Plano de Testes — Painel de Detalhes e Go-live

**Task ID:** qa_task_04
**Tipos:** UI | API | Banco

## Casos de Teste

### CT-01: Acessar painel de detalhes do tenant
- **Pre-condicao:** Tenant f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5 existe com status PreAtivo
- **Passos:** Navegar para /condominios/f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5
- **Expected:** Pagina carrega com nome "Residencial Teste QA", CNPJ mascarado, status pre-ativo
- **Tipo:** UI

### CT-02: Dados do opt-in exibidos
- **Pre-condicao:** Tenant com opt-in registrado
- **Passos:** Verificar secao de opt-in na pagina de detalhes
- **Expected:** Data assembleia (2026-03-01), quorum (75%), signatario (Joao da Silva), data termo (2026-03-01)
- **Tipo:** UI

### CT-03: Situacao do primeiro sindico exibida
- **Pre-condicao:** Sindico Maria Oliveira criado, senha nao definida
- **Passos:** Verificar secao do sindico na pagina de detalhes
- **Expected:** Nome Maria Oliveira, email sindico.qa@portabox.dev, status senha pendente
- **Tipo:** UI

### CT-04: Botao "Ativar operacao" requer confirmacao dupla
- **Pre-condicao:** Tenant com status PreAtivo
- **Passos:** Clicar no botao "Ativar operacao", verificar dialogo de confirmacao
- **Expected:** Dialogo de confirmacao aparece antes de executar a acao
- **Tipo:** UI

### CT-05: Ativacao do tenant (go-live)
- **Pre-condicao:** Dialogo de confirmacao aberto
- **Passos:** Confirmar ativacao no dialogo
- **Expected:** Status muda para "ativo" na UI, feedback visual de sucesso
- **Tipo:** UI

### CT-06: Banco — status atualizado apos go-live
- **Pre-condicao:** Ativacao concluida (CT-05)
- **Passos:** Consultar banco, verificar campo Status do tenant
- **Expected:** Status = 2 (Ativo) no banco
- **Tipo:** Banco

### CT-07: Registro de auditoria da ativacao
- **Pre-condicao:** Ativacao concluida
- **Passos:** Consultar banco (tabela de audit log), verificar entrada de ativacao; verificar UI
- **Expected:** Entrada com eventKind = ativacao, operador ID, timestamp
- **Tipo:** Banco | UI

### CT-08: Botao "Ativar operacao" indisponivel apos ativacao
- **Pre-condicao:** Tenant com status Ativo
- **Passos:** Verificar presenca/estado do botao de ativacao
- **Expected:** Botao ausente ou desabilitado apos ativacao
- **Tipo:** UI
