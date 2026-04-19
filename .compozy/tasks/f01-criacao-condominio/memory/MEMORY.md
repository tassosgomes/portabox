# Workflow Memory

Keep only durable, cross-task context here. Do not duplicate facts that are obvious from the repository, PRD documents, or git history.

## Current State

## Shared Decisions
- `Program.cs` limpa `builder.Configuration.Sources` no bootstrap e recarrega apenas `appsettings*.json` + environment variables; em testes de integração, overrides críticos devem entrar por environment variables (`ConnectionStrings__Postgres`, etc.), não por `AddInMemoryCollection`.
- O bootstrap da API aplica migrations/seed no startup por padrão, mas pode ser desligado via `Persistence:ApplyMigrationsOnStartup=false` para testes ou hosts sem banco.
- O baseline do ASP.NET Identity neste serviço usa tabelas físicas em `snake_case` (`asp_net_users`, `asp_net_roles`, etc.), mesmo mantendo o modelo padrão do Identity.
- O query filter multi-tenant do EF Core deve referenciar um membro do `AppDbContext` (`CurrentTenantId`) e não capturar `ITenantContext` diretamente dentro de `HasQueryFilter`; isso evita congelar o tenant errado no modelo cacheado.
- A fixture compartilhada `PostgresDatabaseFixture` usa `WithReuse(true)` no container PostgreSQL para estabilizar execuções repetidas da suite de integração entre processos.
- `PortaBox.Infrastructure` referencia `PortaBox.Modules.Gestao` para que `AppDbContext` exponha entidades do módulo, aplique seus mapeamentos EF e registre os repositórios concretos de `Gestao` no DI.
- `IObjectStorage` ficou em `PortaBox.Application.Abstractions`, mas a seleção do provider acontece centralmente em `PortaBox.Infrastructure.AddInfrastructure` via `Storage:Provider` (`Minio` ou `S3`); o mapeamento EF de `OptInDocument` também permanece na infraestrutura porque o `AppDbContext` só descobre configurações desse assembly.
- O helper `Sha256StreamHasher` agora vive em `PortaBox.Application.Abstractions.Storage`, e os adapters de storage reutilizam uma `HashingReadStream` recebida em `UploadAsync`; handlers podem calcular hash em streaming antes do commit sem duplicar leitura nem acoplar `Gestao` à infraestrutura.
- A infraestrutura de e-mail usa `IEmailTemplateRenderer` em `PortaBox.Application.Abstractions` e templates embutidos em `PortaBox.Infrastructure/Email/Resources/EmailTemplates`; handlers futuros devem renderizar os corpos por essa abstração, não ler arquivo físico direto.
- O DI de e-mail seleciona `FakeEmailSender` automaticamente quando `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT` é `Testing` ou quando `Email:Provider=Fake`; fora disso registra `SmtpEmailSender` + `EmailOutboxRetryWorker`.
- O baseline de eventos de domínio usa `DomainEventOutboxInterceptor` registrado por DI no `AddDbContext`; ele persiste a `domain_event_outbox` no `SavingChanges`, restaura os eventos se o commit falhar e despacha handlers in-process apenas em `SavedChanges`.
- O publisher do outbox segue o mesmo padrão do e-mail: `DomainEvents:Publisher:Enabled` controla se `DomainEventOutboxPublisher` entra no host; em testes/ambientes específicos a fila pode crescer intencionalmente com o worker desligado.
- O provisionamento do primeiro síndico foi encapsulado em `IIdentityUserProvisioningService` dentro de `PortaBox.Application.Abstractions`; a implementação concreta na infraestrutura anexa `AppUser` + `IdentityUserRole<Guid>` diretamente ao `AppDbContext` para que a criação do usuário participe do mesmo `SaveChanges` do caso de uso e mantenha o disparo de eventos/e-mail somente após commit.
- Fluxos administrativos que precisam localizar `Sindico` fora do contexto autenticado do tenant devem usar consulta explícita com `IgnoreQueryFilters()` na infraestrutura; o filtro global continua válido para leitura do síndico autenticado, mas backoffice cross-tenant não pode depender dele.
- Reemissão de magic link não deve invalidar links pendentes antes de checar rate-limit; o baseline do serviço agora expõe `IMagicLinkService.CanIssueAsync(...)` para o handler fazer preflight e preservar o link ainda válido quando a nova emissão é bloqueada.
- `AddInfrastructure` configura o `DbContext` com `ConfigureWarnings(...Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))`; a suíte de integração cria muitos `ServiceProvider`s descartáveis por teste e, sem essa supressão explícita, o EF Core passa a derrubar o run completo após dezenas de contextos mesmo quando o comportamento funcional está correto.

## Shared Learnings
- **Frontend (tasks 21–25):** `apiClient` needs `noRedirectOn401: true` for the initial session check (`/v1/auth/me`). Without it, the 401 interceptor redirects before `AuthContext` can set `isAuthenticated: false`, causing a redirect loop.
- **Frontend (tasks 22–25):** Success toast messages from wizard/action pages are passed via `navigate('/path', { state: { successMessage } })` — the receiving page (list or details) reads `location.state.successMessage` and renders the toast. Task_23 must implement this on the details/list page.
- **Frontend (tasks 22–25):** `<input type="date">` in jsdom tests requires `fireEvent.change(el, { target: { value: 'YYYY-MM-DD' } })` — `userEvent.type` does not work reliably with date inputs in jsdom.
- **Frontend (tasks 22–25):** `tsc -b` (used in the `build` script) type-checks test files; unused variables declared in test files cause TS errors that break `pnpm build`. Keep test-only variables used or removed.
- **Frontend (tasks 21–25):** MSW v2 (`msw/node`) in vitest requires absolute URL handlers. Relative fetch calls (`/api/...`) are not resolved in Node.js context. Fix: add `define: { 'import.meta.env.VITE_API_BASE_URL': '"http://localhost/api"' }` to `vitest.config.ts`; integration tests use `http://localhost/api` as MSW base.
- `AppUser.SindicoTenantId` agora tem FK real para `condominio.id`; qualquer seed/teste que preencha esse campo precisa persistir o `Condominio` antes ou no mesmo `SaveChanges` do usuário para evitar violação de FK.
- **Frontend (tasks 21–25):** apps que rodam `tsc -b && vite build` precisam de um `vitest.config.ts` separado do `vite.config.ts`; a propriedade `test` no `vite.config.ts` não passa na verificação TypeScript (`UserConfigExport` não inclui `test`) e quebra o build.
- **Frontend (tasks 21–25):** vitest faz hash nos nomes de classe de CSS Modules por padrão (`_card_b294cf`). Para testes com `toHaveClass()`, configurar `test.css.modules.classNameStrategy: 'non-scoped'` no `vitest.config.ts`.
- **Frontend (tasks 21–25):** pnpm não hóiste `@vitejs/plugin-react` automaticamente para `packages/ui`. A lib usa `vitest.config.ts` próprio com `esbuild: { jsx: 'automatic', jsxImportSource: 'react' }` em vez de depender do plugin.
- Algumas tasks instruem ler `AGENTS.md` e `CLAUDE.md`, mas esses arquivos ainda não existem no workspace; enquanto isso, o contexto operacional confiável vem dos docs da PRD (`_techspec.md`, `_tasks.md`, ADRs) e das skills obrigatórias do fluxo.
- A fixture `MinioFixture` sobe um MinIO real via Testcontainers e usa `AmazonS3Client` apontando para o endpoint S3-compatible do container para validar upload/download/delete tanto do adapter `MinioObjectStorage` quanto do `S3ObjectStorage`.
- `IMagicLinkService.ValidateAndConsumeAsync` já faz a marcação atômica de `consumed_at`/`consumed_by_ip` no próprio `magic_link` via `ExecuteUpdateAsync` em providers relacionais; handlers futuros que precisarem manter isso na mesma transação do caso de uso devem compartilhar o mesmo `AppDbContext`/transação antes do `SaveChanges` final.
- Como o evento concreto de negócio ainda não existe nas tasks anteriores, os testes de atomicidade do outbox usam `Condominio` com eventos injetados via reflexão no `AggregateRoot`; tasks futuras podem emitir eventos reais sem mudar a infraestrutura.
- A suíte de integração com `PostgresDatabaseFixture`/Testcontainers não tolera execuções concorrentes confiavelmente; rode `dotnet test` dessas integrações de forma sequencial, ou o container compartilhado pode cair/resetar no meio do run.
- Mesmo em execução sequencial, a suíte completa de integração pode derrubar o PostgreSQL reutilizado (`WithReuse(true)`) no meio do run com `terminating connection due to administrator command` / `connection refused`; quando isso acontecer, valide a task pelo subconjunto de integrações afetado pela mudança e trate a falha global como instabilidade preexistente do fixture.

## Open Risks

## Handoffs
