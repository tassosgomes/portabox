# Task Memory: task_17.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Implementar `ListCondominiosQuery` e `GetCondominioDetailsQuery` com paginação, filtros, mascaramento de PII e comportamento cross-tenant controlado para operador.

## Important Decisions
- Manter os handlers de query no módulo `PortaBox.Modules.Gestao` desacoplados da infraestrutura: a leitura ficará exposta via `ICondominioRepository`, sem injetar `AppDbContext` no projeto de aplicação.
- O bypass controlado do filtro multi-tenant ficou implícito no `tenantContext`: `Operator` roda sem tenant ativo e o repositório usa `IgnoreQueryFilters()` apenas nesse caso; com tenant ativo (`Sindico`), `GetCondominioDetails` restringe explicitamente o root `Condominio` ao tenant corrente.
- Para carregar detalhes completos sem expor `storage_key`, o repositório projeta `CondominioDetailsDto` a partir do root `Condominio` com `AsNoTracking()` + `AsSplitQuery()` + includes controlados, e busca o estado de senha do síndico via `AspNetUsers` separado.

## Learnings
- `Condominio` é entidade global e não recebe query filter; para leituras de detalhes em contexto `Sindico`, o escopo precisa ser imposto explicitamente na query do root para evitar vazamento cross-tenant.
- O mascaramento de CNPJ adotado nesta task segue o exemplo do task file (`****8000195`): prefixo fixo `****` e últimos 7 dígitos visíveis. CPF e celular seguem os formatos explicitados na task.
- Os testes focados da task passaram (`PortaBox.Modules.Gestao.UnitTests` e `CondominioQueriesIntegrationTests`), mas a verificação global da suíte de integração segue instável por queda do container PostgreSQL compartilhado fora do escopo desta task.

## Files / Surfaces
- `src/PortaBox.Modules.Gestao/Application/Repositories/ICondominioRepository.cs`
- `src/PortaBox.Infrastructure/Repositories/CondominioRepository.cs`
- `src/PortaBox.Modules.Gestao/Application/Queries/*`
- `src/PortaBox.Modules.Gestao/Application/Common/*`
- `src/PortaBox.Modules.Gestao/Domain/Condominio.cs`
- `src/PortaBox.Infrastructure/Persistence/*Configuration.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/ListCondominiosQueryAndMaskingTests.cs`
- `tests/PortaBox.Modules.Gestao.UnitTests/GetCondominioDetailsQueryHandlerTests.cs`
- `tests/PortaBox.Api.IntegrationTests/CondominioQueriesIntegrationTests.cs`

## Errors / Corrections
- Corrigido o teste de detalhes para anexar `OptInDocument` com um `uploaded_by_user_id` realmente persistido (`SindicoUserId` criado pelo fluxo), evitando violação de FK no setup da integração.
- Corrigido o teste cross-tenant para abrir o `ITenantContext.BeginScope(...)` no momento da execução da query, reproduzindo o comportamento do middleware e evitando falso positivo por escopo perdido.

## Ready for Next Run
- Se for necessário fechar a task como `completed`, primeiro estabilizar ou isolar a falha preexistente da suíte global de integração/Postgres fixture; hoje a evidência limpa disponível é task-specific, não da pipeline completa.
