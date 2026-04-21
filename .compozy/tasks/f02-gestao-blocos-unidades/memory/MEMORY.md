# Workflow Memory

Keep only durable, cross-task context here. Do not duplicate facts that are obvious from the repository, PRD documents, or git history.

## Current State

- Task 01 introduziu a infra base de soft-delete no backend: `ISoftDeletable`, `SoftDeleteableAggregateRoot` e filtro global combinado em `AppDbContext`.

## Shared Decisions

- O dominio passou a expor `PortaBox.Domain.Result` nao generico para guard clauses internas sem criar dependencia de `PortaBox.Domain` para `PortaBox.Application.Abstractions`.
- Tasks backend de F02 devem seguir o baseline real do repositorio com `TimeProvider`; os task specs que mencionam `IClock` precisam ser adaptados a esse padrao existente em vez de introduzir uma abstracao paralela fora de escopo.
- Para F02 backend, os caminhos autoritativos do repositorio sao os reais: EF configurations em `src/PortaBox.Infrastructure/Persistence`, repositorios em `src/PortaBox.Infrastructure/Repositories` e registros em `src/PortaBox.Infrastructure/DependencyInjection.cs`, mesmo quando a task spec lista caminhos ideais dentro de `PortaBox.Modules.Gestao`.
- Enquanto nao existir uma abstracao injetavel de usuario atual na camada de aplicacao, commands backend de F02 devem carregar `performedByUserId` explicitamente; os endpoints continuam responsaveis por extrair o `NameIdentifier` do `HttpContext`.
- Para leituras agregadas de F02, preferir estender os contratos de repositorio da aplicacao (ex.: `IUnidadeRepository.ListByCondominioAsync`) em vez de injetar `AppDbContext` diretamente nos handlers; isso preserva o boundary da camada de aplicacao e ainda permite queries otimizadas na infrastructure.
- No frontend de F02, o identificador confiavel do condominio para o sindico no estado atual do repo e `tenantId` retornado por `/v1/auth/me`; rotas/features que precisarem do condominio devem consumir esse campo do auth context ou aceitar fallback por path parametrizado.

## Shared Learnings

- Em F02 frontend, a regra ESLint `react-hooks/refs` bloqueia o acesso a `ref.current` dentro de funções passadas em JSX (mesmo que o acesso só ocorra em evento). Solução padrão: usar estado booleano + `useEffect` para deslocar o acesso ao ref para fora do caminho de render.
- Em F02 frontend, a regra ESLint `react-hooks/set-state-in-effect` bloqueia o padrão `useEffect → setState`. Substituir por `useMemo` que deriva o valor diretamente de outras dependências é a abordagem correta — elimina o efeito por completo.
- Testes de integração MSW para features que usam `@portabox/api-client` exigem `configure({ baseUrl: 'http://localhost/api/v1' })` no `beforeAll`. Sem isso o cliente usa `VITE_API_BASE_URL = http://localhost/api` (sem `/v1`) e os handlers MSW registrados com `/v1` não correspondem, causando requests não interceptados e testes que travam no timeout.
- Em RTL, assertions síncronas após `waitFor(elementoEstático)` falham se os dados ainda não chegaram. Mover todas as assertions dependentes de dados async para dentro do mesmo bloco `waitFor` é o padrão correto para testes de integração do backoffice.
- `screen.getByText(texto)` falha com "Found multiple elements" quando o mesmo texto aparece em `<h2>` e em `<option>` (ex: nome do condomínio aparece no tree card E no TenantSelector). Usar `screen.getByRole('heading', { name: texto, level: 2 })` resolve a ambiguidade.

- Para validar migrations do EF Core contra o Postgres do `docker-compose.dev.yml`, exporte `ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=portabox;Username=portabox;Password=portabox`; o `AppDbContextFactory` ainda cai no default `postgres/postgres` quando a variavel nao e fornecida.
- Em F02 frontend, testes de contrato com Prism no Node 18 exigem fixar `@faker-js/faker@5.5.3` via `pnpm.overrides`; sem isso o `@stoplight/prism-cli` falha ao subir por incompatibilidade ESM/CJS.
- No estado atual do repo, verificacoes de lint para workspaces frontend podem exigir `NODE_OPTIONS=--experimental-default-type=module pnpm exec eslint ... --format json`; o script `pnpm lint` com formatter padrao ainda quebra no ambiente local antes de reportar findings.
- Em F02 frontend, `apps/sindico` e `apps/backoffice` passaram a configurar `@portabox/api-client` via `VITE_API_URL` no bootstrap do app, mas os fluxos de autenticacao existentes ainda usam os clientes HTTP locais em `src/shared/api/client.ts` com `VITE_API_BASE_URL` e cookies; futuras tasks devem conviver com ambos ate uma migracao explicita de auth.
- O `@portabox/api-client` precisa enviar `credentials: 'include'` por padrao no frontend atual, porque a autenticacao real do backend continua baseada em cookie de sessao do Identity; depender apenas de `Authorization` quebra leituras autenticadas de F02.

## Open Risks

- Validacoes de integracao baseadas em `Testcontainers` dependem de Docker disponivel no host; sem `unix:///var/run/docker.sock` acessivel nao e possivel produzir evidencia de Postgres real para tasks backend que exigem esse fixture.

## Handoffs
