# Matriz de Isolamento Cross-Tenant — qa_task_09

**Data/Hora:** 2026-04-20T23:xx:xxZ
**Tenant A:** 4cce551d-4f18-474b-a42a-2deb6c2a0451 (QA Teste A)
**Tenant B:** 23fb219d-460a-4eee-a9e7-308d7665350b (QA Teste B)

## Legenda

- PASS (403) = Acesso negado com Forbidden — isolamento correto
- PASS (404) = Recurso nao encontrado no escopo do tenant — isolamento correto
- FAIL (201) = CRITICO: recurso criado em tenant errado
- FAIL (200) = CRITICO: dados de outro tenant expostos
- FAIL (500) = BUG: erro interno ao tentar acesso cross-tenant

## Matriz: Sindico A operando em recursos de Tenant B

| CT | Acao | Endpoint | Status Obtido | Resultado |
|----|------|----------|---------------|-----------|
| CT-01 | GET estrutura | GET /condominios/{B}/estrutura | 403 | PASS |
| CT-03 | POST bloco | POST /condominios/{B}/blocos | 403 | PASS |
| CT-04 | PATCH bloco | PATCH /condominios/{B}/blocos/{blocoB} | 403 | PASS |
| CT-05 | Inativar bloco | POST /condominios/{B}/blocos/{blocoB}:inativar | 403 | PASS |
| CT-06 | Reativar bloco | POST /condominios/{B}/blocos/{blocoB}:reativar | 403 | PASS |
| CT-07 | POST unidade | POST /condominios/{B}/blocos/{blocoB}/unidades | 403 | PASS |
| CT-08 | Inativar unidade | POST /condominios/{B}/blocos/{blocoB}/unidades/{uB}:inativar | 403 | PASS |
| CT-09 | Reativar unidade | POST /condominios/{B}/blocos/{blocoB}/unidades/{uB}:reativar | 403 | PASS |
| CT-11 | POST unidade path-mix inv. | POST /condominios/{B}/blocos/{blocoA}/unidades | 403 | PASS |

## Matriz: Sindico B operando em recursos de Tenant A

| CT | Acao | Endpoint | Status Obtido | Resultado |
|----|------|----------|---------------|-----------|
| CT-02 | GET estrutura | GET /condominios/{A}/estrutura | 403 | PASS |

## Cenarios Cirurgicos (Path Mix)

| CT | Acao | condominioId | blocoId | Status Obtido | Resultado |
|----|------|-------------|---------|---------------|-----------|
| CT-10 | POST unidade | Tenant A (proprio) | Bloco B (outro tenant) | 404 | PASS |
| CT-11 | POST unidade | Tenant B (outro tenant) | Bloco A (proprio) | 403 | PASS |

## Endpoints Admin (Role Enforcement)

| CT | Acao | Role Caller | Endpoint | Status Obtido | Resultado |
|----|------|-------------|----------|---------------|-----------|
| CT-12 | GET estrutura admin | Sindico (nao Operator) | GET /admin/condominios/{A}/estrutura | 403 | PASS |

## Resumo

- **Total de cenarios cross-tenant testados:** 12
- **Leaks detectados (201/200 em request cross-tenant):** 0
- **Todos os 9 endpoints de F02 validados:** SIM
- **Mecanismo de protecao observado:** O backend retorna HTTP 403 para todos os acessos cross-tenant em paths /condominios/{tenantId}/* quando o tenantId nao corresponde ao tenantId do sindico autenticado. O unico 404 ocorreu no cenario cirurgico CT-10 (path mix com condominioId proprio + blocoId de outro tenant), indicando que a validacao de pertencimento do bloco ao condominio e feita antes de gravar.

## Conclusao

**ZERO leaks de isolamento detectados.** O sistema implementa isolamento multi-tenant correto em todos os endpoints de F02 testados.
