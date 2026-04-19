# TechSpec — F01: Assistente de Criação de Condomínio

> **Nível 3 da hierarquia de documentação.** Traduz `_prd.md` em decisões técnicas e plano de implementação. Este TechSpec estabelece também o **baseline arquitetural** do primeiro serviço .NET do projeto (válido para todos os domínios que rodarão em .NET: D01/D02/D03/D05). O próximo passo do pipeline é `cy-create-tasks`.

**Domínio:** D01 — Gestão do Condomínio
**Feature:** F01 — Assistente de Criação de Condomínio
**Última revisão:** 2026-04-17

---

## Executive Summary

F01 materializa o primeiro fluxo operacional da plataforma e, por ser o primeiro serviço a ser construído, fixa o baseline arquitetural para todos os serviços .NET do sistema. A implementação é um **monolito modular** em ASP.NET Core 8 seguindo Clean Architecture em camadas numeradas, com **CQRS nativo** (Commands/Queries/Handlers + Dispatcher, sem MediatR), **Repository Pattern** sobre **EF Core + PostgreSQL** e isolamento multi-tenant por **shared schema com `tenant_id` + global query filter** (ADR-004). A UI do operador é uma **SPA React dedicada** em subdomínio distinto do painel do síndico (ADR-005), e ambas as personas autenticam via **ASP.NET Core Identity** diferenciadas por role (`Operator`, `Sindico`).

O trade-off primário é a preferência por **simplicidade operacional no piloto** em troca de algum trabalho de migração futura: magic link custom com token opaco SHA-256 em tabela dedicada (ADR-006) em vez de Identity tokens; `IObjectStorage` abstraindo MinIO/S3/R2 (ADR-007); `IEmailSender` sobre SMTP genérico em vez de provedor comercial específico (ADR-008); dispatcher in-process + outbox pattern em vez de RabbitMQ desde o dia 1 (ADR-009). Todas as abstrações são plugáveis, sustentando a evolução para a arquitetura-alvo do vision (RabbitMQ + Mastra + múltiplos adapters de comunicação) sem refactor estrutural.

Ambas as SPAs (`apps/backoffice` e `apps/sindico`) aderem ao design system interno **PortaBox**, versionado em `.claude/skills/portabox-design/` e invocável pela skill homônima. Tokens (`colors_and_type.css`), tipografia (Plus Jakarta Sans + Inter + JetBrains Mono), ícones Lucide, paleta navy+laranja e copy pt-BR são fonte única de verdade desde o primeiro componente (ADR-010). Antes de qualquer implementação frontend, o desenvolvedor (humano ou agente) invoca a skill `portabox-design` para carregar o contexto completo.

---

## System Architecture

### Component Overview

| Componente | Responsabilidade | Tipo |
|---|---|---|
| `PortaBox.Api` (ASP.NET Core 8) | API HTTP, autenticação, endpoints de backoffice e síndico | Novo |
| `PortaBox.Modules.Gestao` (módulo do monolito) | Lógica do domínio D01 — Condomínio, Síndico, opt-in, Magic Link, audit log | Novo |
| `PortaBox.Infrastructure` | EF Core DbContext, migrations, adapters (Storage, Email), workers | Novo |
| `apps/backoffice` (React + Vite + TS) | SPA do operador — wizard, lista, detalhes do tenant | Novo |
| `apps/sindico` (React + Vite + TS) | SPA do síndico — login, tela de definição de senha via magic link, home vazia no MVP | Novo (esqueleto mínimo) |
| PostgreSQL 16 | Persistência relacional — schema único multi-tenant | Novo |
| Object Storage (MinIO local / S3 ou R2 prod) | Documentos de opt-in (ata, termo) | Novo |
| SMTP relay (MailHog dev / TBD prod) | Entrega do magic link | Novo |
| Background workers | Outbox publisher, email retry | Novo |

### Data Flow — Principais Cenários

**C1. Criação do tenant (wizard do operador)**
1. Operador autenticado (`Operator` role) envia `POST /api/v1/admin/condominios` com payload completo (dados do condomínio, opt-in metadata, síndico).
2. Handler `CreateCondominioCommandHandler` valida (FluentValidation), normaliza CNPJ, checa duplicata, cria `Condominio` com status `PreAtivo`, cria `Sindico` + `AspNetUser` (sem senha, `PasswordRequired=true`), persiste `OptInRecord`, grava `TenantAuditEntry` e `DomainEventOutbox` (`condominio.cadastrado.v1`) na mesma transação.
3. `SaveChangesInterceptor` do EF Core despacha eventos in-process após commit.
4. Handler in-process `SendSindicoMagicLinkOnCondominioCreated` gera magic link via `IMagicLinkService`, envia e-mail via `IEmailSender`.
5. Resposta 201 com `Location: /api/v1/admin/condominios/{id}`.

**C2. Upload de documento de opt-in (opcional, multipart separado)**
- `POST /api/v1/admin/condominios/{id}/opt-in-documents` (multipart/form-data) → valida tamanho/MIME → stream SHA-256 → `IObjectStorage.UploadAsync` → insere `OptInDocument` com ponteiro.

**C3. Consumo do magic link pelo síndico**
1. Síndico acessa SPA `apps/sindico` com `?token=...`; tela chama `POST /api/v1/auth/password-setup { token, password }`.
2. `PasswordSetupCommandHandler` valida token via `IMagicLinkService.ValidateAndConsumeAsync` (SHA-256 + TTL + uso único).
3. Define senha via `UserManager.AddPasswordAsync` + marca `magic_link.consumed_at` na mesma transação.
4. Resposta 200. SPA redireciona para `/login`.

**C4. Go-live manual do tenant**
- `POST /api/v1/admin/condominios/{id}:activate` → `ActivateCondominioCommandHandler` muda status para `Ativo`, grava `TenantAuditEntry`, emite `condominio.ativado.v1`.

**C5. Reemissão de magic link**
- `POST /api/v1/admin/condominios/{id}/sindicos/{userId}:resend-magic-link` → invalida pendentes (`invalidated_at = now()`), emite novo, envia e-mail. Rate-limit por `user_id+purpose`.

### External System Interactions

- **SMTP relay**: conexão TLS (STARTTLS) para entrega de e-mail transacional. Em dev: MailHog sem TLS.
- **Object Storage**: requests S3-compatible via SDK (MinIO NuGet em dev; `AWSSDK.S3` em prod/R2). Presigned URLs de 5 min para download.
- **Nenhuma integração externa adicional no MVP** (Receita Federal / ANPD ficam fora).

---

## Implementation Design

### Core Interfaces

Interfaces-chave do monolito modular. Exemplos em C# (baseline da stack; o skill padrão menciona Go, mas o projeto é .NET — mesma convenção de "contratos + código curto de referência").

**CQRS base** (em `PortaBox.Application.Abstractions`):

```csharp
public interface ICommand<TResult> { }

public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IQuery<TResult> { }

public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct);
}
```

**Tenant context** (em `PortaBox.Application.MultiTenancy`):

```csharp
public interface ITenantContext
{
    Guid? TenantId { get; }
    IDisposable BeginScope(Guid tenantId);
}

public interface ITenantEntity
{
    Guid TenantId { get; }
}
```

**Magic link** (em `PortaBox.Modules.Gestao.Application`):

```csharp
public interface IMagicLinkService
{
    Task<MagicLinkIssueResult> IssueAsync(
        Guid userId,
        MagicLinkPurpose purpose,
        TimeSpan? ttl,
        CancellationToken ct);

    Task<MagicLinkConsumeResult> ValidateAndConsumeAsync(
        string rawToken,
        MagicLinkPurpose purpose,
        CancellationToken ct);

    Task InvalidatePendingAsync(Guid userId, MagicLinkPurpose purpose, CancellationToken ct);
}
```

**Storage e e-mail** (em `PortaBox.Application.Abstractions`):

```csharp
public interface IObjectStorage
{
    Task<ObjectStorageReference> UploadAsync(
        string key, Stream content, string contentType, CancellationToken ct);
    Task<Uri> GetDownloadUrlAsync(string key, TimeSpan ttl, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
```

**Domain event** (em `PortaBox.Domain.Abstractions`):

```csharp
public interface IDomainEvent
{
    string EventType { get; }
    DateTimeOffset OccurredAt { get; }
}

public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _events;
    protected void AddDomainEvent(IDomainEvent e) => _events.Add(e);
    internal void ClearDomainEvents() => _events.Clear();
}
```

### Data Models

Estrutura persistente (PostgreSQL). Tabelas com `tenant_id` são multi-tenant (filtradas globalmente pelo EF Core, conforme ADR-004). Tabelas globais são explicitamente marcadas.

#### `condominio` (tenant root, global — linha define o próprio tenant)

| Coluna | Tipo | Notas |
|---|---|---|
| `id` | UUID PK | `id` == `tenant_id` das demais entidades |
| `nome_fantasia` | VARCHAR(200) NOT NULL | |
| `cnpj` | CHAR(14) NOT NULL UNIQUE | Apenas dígitos; dedup via UNIQUE INDEX |
| `endereco_logradouro` | VARCHAR(200) | |
| `endereco_numero` | VARCHAR(20) | |
| `endereco_complemento` | VARCHAR(80) | |
| `endereco_bairro` | VARCHAR(80) | |
| `endereco_cidade` | VARCHAR(80) | |
| `endereco_uf` | CHAR(2) | |
| `endereco_cep` | CHAR(8) | |
| `administradora_nome` | VARCHAR(200) NULL | Campo livre |
| `status` | SMALLINT NOT NULL | 1=PreAtivo, 2=Ativo |
| `created_at` | TIMESTAMPTZ NOT NULL | |
| `created_by_user_id` | UUID NOT NULL | FK `AspNetUsers` |
| `activated_at` | TIMESTAMPTZ NULL | |
| `activated_by_user_id` | UUID NULL | FK `AspNetUsers` |

Índices: `idx_condominio_cnpj_unique`, `idx_condominio_status`.

#### `opt_in_record` (multi-tenant, 1..1 com Condominio)

| Coluna | Tipo | Notas |
|---|---|---|
| `id` | UUID PK | |
| `tenant_id` | UUID NOT NULL | FK `condominio.id`, UNIQUE |
| `data_assembleia` | DATE NOT NULL | |
| `quorum_descricao` | VARCHAR(200) NOT NULL | Forma livre |
| `signatario_nome` | VARCHAR(200) NOT NULL | |
| `signatario_cpf` | CHAR(11) NOT NULL | Apenas dígitos |
| `data_termo` | DATE NOT NULL | |
| `registered_by_user_id` | UUID NOT NULL | FK `AspNetUsers` |
| `registered_at` | TIMESTAMPTZ NOT NULL | |

#### `opt_in_document` (multi-tenant, 0..N por Condominio)

| Coluna | Tipo | Notas |
|---|---|---|
| `id` | UUID PK | |
| `tenant_id` | UUID NOT NULL | FK `condominio.id` |
| `kind` | SMALLINT NOT NULL | 1=Ata, 2=Termo, 99=Outro |
| `storage_key` | VARCHAR(512) NOT NULL | Path no object storage |
| `content_type` | VARCHAR(80) NOT NULL | |
| `size_bytes` | BIGINT NOT NULL | Limite 10 MB |
| `sha256` | CHAR(64) NOT NULL | |
| `uploaded_at` | TIMESTAMPTZ NOT NULL | |
| `uploaded_by_user_id` | UUID NOT NULL | FK `AspNetUsers` |

Índice composto: `(tenant_id, uploaded_at DESC)`.

#### `sindico` (multi-tenant, 0..N — no MVP apenas 1 é criado por F01)

| Coluna | Tipo | Notas |
|---|---|---|
| `id` | UUID PK | |
| `tenant_id` | UUID NOT NULL | FK `condominio.id` |
| `user_id` | UUID NOT NULL UNIQUE | FK `AspNetUsers` |
| `nome_completo` | VARCHAR(200) NOT NULL | |
| `celular_e164` | VARCHAR(20) NOT NULL | Formato E.164 |
| `status` | SMALLINT NOT NULL | 1=Ativo, 2=Inativo |
| `created_at` | TIMESTAMPTZ NOT NULL | |

#### `magic_link` (global — não tem tenant_id)

| Coluna | Tipo | Notas |
|---|---|---|
| `id` | UUID PK | |
| `user_id` | UUID NOT NULL | FK `AspNetUsers` |
| `purpose` | SMALLINT NOT NULL | 1=PasswordSetup |
| `token_hash` | CHAR(64) NOT NULL UNIQUE | SHA-256 hex |
| `created_at` | TIMESTAMPTZ NOT NULL | |
| `expires_at` | TIMESTAMPTZ NOT NULL | |
| `consumed_at` | TIMESTAMPTZ NULL | |
| `consumed_by_ip` | INET NULL | |
| `invalidated_at` | TIMESTAMPTZ NULL | Para reemissão |

Índices: `idx_magic_link_token_hash_unique`, `idx_magic_link_user_purpose_open` (parcial: `WHERE consumed_at IS NULL AND invalidated_at IS NULL`).

#### `tenant_audit_log` (global — registra transições do tenant)

| Coluna | Tipo | Notas |
|---|---|---|
| `id` | BIGSERIAL PK | |
| `tenant_id` | UUID NOT NULL | FK `condominio.id` |
| `event_kind` | SMALLINT NOT NULL | 1=Created, 2=Activated, 3=MagicLinkResent, 4=Other |
| `performed_by_user_id` | UUID NOT NULL | FK `AspNetUsers` |
| `occurred_at` | TIMESTAMPTZ NOT NULL | |
| `note` | TEXT NULL | |
| `metadata_json` | JSONB NULL | Extensibilidade |

#### `domain_event_outbox` (global)

| Coluna | Tipo | Notas |
|---|---|---|
| `id` | UUID PK | |
| `tenant_id` | UUID NULL | Para eventos do tenant; NULL em eventos globais |
| `event_type` | VARCHAR(120) NOT NULL | Ex.: `condominio.cadastrado.v1` |
| `aggregate_id` | UUID NOT NULL | |
| `payload` | JSONB NOT NULL | |
| `created_at` | TIMESTAMPTZ NOT NULL | |
| `published_at` | TIMESTAMPTZ NULL | No MVP, worker apenas seta este campo |

Índices: `(published_at, created_at)` para worker scan.

#### `email_outbox` (global, para retry em falha persistente)

| Coluna | Tipo | Notas |
|---|---|---|
| `id` | UUID PK | |
| `to_address` | VARCHAR(254) NOT NULL | |
| `subject` | VARCHAR(200) NOT NULL | |
| `html_body` | TEXT NOT NULL | |
| `text_body` | TEXT NULL | |
| `attempts` | INT NOT NULL DEFAULT 0 | |
| `next_attempt_at` | TIMESTAMPTZ NOT NULL | |
| `last_error` | TEXT NULL | |
| `sent_at` | TIMESTAMPTZ NULL | |

#### Tabelas do ASP.NET Core Identity

Criadas pelas migrations padrão do Identity: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`.

Seed obrigatório: roles `Operator` e `Sindico`; pelo menos um usuário `Operator` seed no ambiente de desenvolvimento.

### API Endpoints

Todos os endpoints são versionados em `/api/v1`. Autorização por policy/role entre colchetes.

| Método | Path | Policy | Descrição |
|---|---|---|---|
| POST | `/api/v1/admin/condominios` | `RequireOperator` | Cria condomínio + opt-in + primeiro síndico. Retorna 201 com `Location`. |
| POST | `/api/v1/admin/condominios/{id}/opt-in-documents` | `RequireOperator` | Upload multipart de ata/termo. Retorna 201 com id do documento. |
| GET | `/api/v1/admin/condominios/{id}/opt-in-documents/{docId}:download` | `RequireOperator` | Retorna presigned URL (5 min) em JSON `{ url, expires_at }`. |
| POST | `/api/v1/admin/condominios/{id}:activate` | `RequireOperator` | Transita `PreAtivo → Ativo`. Body opcional `{ note }`. |
| POST | `/api/v1/admin/condominios/{id}/sindicos/{userId}:resend-magic-link` | `RequireOperator` | Invalida pendentes e emite novo link. Rate-limited. |
| GET | `/api/v1/admin/condominios` | `RequireOperator` | Lista paginada (`?page`, `?pageSize`, `?status`, `?q`). |
| GET | `/api/v1/admin/condominios/{id}` | `RequireOperator` | Detalhes completos (dados, opt-in metadata, docs, status síndico, log de auditoria). |
| POST | `/api/v1/auth/login` | Público | Login cookie-based (Identity). |
| POST | `/api/v1/auth/logout` | Autenticado | Encerra cookie. |
| POST | `/api/v1/auth/password-setup` | Público | Consome magic link + define senha. Body `{ token, password }`. |
| GET | `/health/live` | Público | Liveness probe. |
| GET | `/health/ready` | Público | Readiness probe (DB, storage, SMTP resolve). |

Todos os erros seguem **RFC 7807 (Problem Details)** via `IExceptionHandler` + `ProblemDetails`. Respostas seguem convenções REST do skill `common-restful-api`.

### Frontend Design System (PortaBox)

Decisão completa em **[ADR-010](adrs/adr-010.md)**. Ambas as SPAs (`apps/backoffice` e `apps/sindico`) são implementadas sobre o design system interno **PortaBox**, versionado em `.claude/skills/portabox-design/` (user-invocable skill `portabox-design`).

**Fluxo obrigatório antes de qualquer implementação frontend:**

1. Invocar a skill `portabox-design` (tanto humanos quanto agentes de código devem começar por aqui).
2. Ler `README.md` do skill para brand voice, tom de copy pt-BR e regras de imagery.
3. Inspecionar `preview/component-*.html` e `preview/type-*.html` / `preview/color-*.html` para referência visual dos componentes.
4. Conferir o kit `ui_kits/admin-dashboard/` — é o kit de referência para F01, por ser a outra superfície web desktop do produto.

**Tokens e fontes (consumidos via CSS vars — nunca hardcoded):**

| Categoria | Token-chave | Valor de referência |
|---|---|---|
| Cor primária | `--pb-navy-900` / `--pb-navy-700` | `#0B2B47` / `#1E3A8A` |
| Cor de ação | `--pb-orange-500` / `--pb-orange-700` | `#F97316` / `#EA580C` |
| Papel / surface | `--bg-app` / `--bg-surface` | `#F9FAFB` / `#FFFFFF` |
| Tipografia display/UI | `--font-display` | Plus Jakarta Sans |
| Tipografia body | `--font-body` | Inter |
| Tipografia mono (PIN/ID) | `--font-mono` | JetBrains Mono |
| Espaçamento base | `--sp-1` ... `--sp-20` | Escala 4px |
| Radius padrão (cards) | `--r-lg` | 14px |
| Radius pill (CTAs) | `--r-pill` | 999px |
| Sombras | `--sh-xs` ... `--sh-lg` | Soft, navy-tinted |
| Foco | `--sh-focus` | halo laranja 3px |
| Motion | `--dur-base` + `--ease-standard` | 200ms, `cubic-bezier(0.2,0.7,0.2,1)` |

**Componentes compartilhados** ficam em `packages/ui/` (monorepo) reutilizando os exemplos do skill: Button (pill laranja para primário, navy ghost para secundário), Input (radius 10px, sombra `--sh-sm`), Card (radius 14px, sombra `--sh-md`, sem borda), Badge (status pill), Modal (radius 20px, sombra `--sh-lg`), StepIndicator (para as 3 etapas do wizard).

**Ícones**: [Lucide](https://lucide.dev) via `lucide-react`. Stroke 1.5–2px, tamanhos 16/20/24/32. Sempre com label em navegação e botões primários. Ícones candidatos para F01: `building-2` (condomínio), `file-text` (opt-in), `user-plus` (síndico), `mail` (magic link), `check-circle` (confirmações), `upload-cloud` (upload de docs), `clipboard-check` (audit log), `power` (ativar tenant).

**Copy pt-BR no F01** (exemplos canônicos, aprovar com product antes do piloto):

| Contexto | Copy sugerida |
|---|---|
| Título do wizard | `Novo condomínio` |
| Etapas | `1. Dados do condomínio` · `2. Consentimento LGPD` · `3. Síndico responsável` |
| CTA primário final | `Criar condomínio` |
| CTA de ativação | `Ativar operação` |
| Alerta de duplicata | `Este CNPJ já está cadastrado como “{nome}”, criado em {data}.` |
| Confirmação pós-criação | `Condomínio criado em estado pré-ativo. Enviamos o link de definição de senha para o síndico.` |
| E-mail do síndico — assunto | `Bem-vindo ao PortaBox — defina sua senha` |
| E-mail do síndico — abertura | `Olá, {nome}! Você foi cadastrado como síndico do condomínio {nome_condominio}. Para acessar o painel, defina sua senha: {link}. O link expira em 72 horas.` |

**Restrições herdadas do design system**:
- Sem gradientes, sem acentos secundários, sem ilustrações de pacotes.
- Botões primários = pill laranja; secundários = navy ghost.
- Cards nunca combinam borda + sombra; escolher um.
- Nada de `📦` na UI interna (reservado para mensagens ao morador via D03 futuramente).
- Logo: `assets/logo-portabox.png` (lockup) e `assets/logo-portabox-mark.svg` (favicon / chrome compacto). Nunca recolorir.

**Build-time**: `apps/backoffice/src/styles/tokens.css` importa `../../../.claude/skills/portabox-design/colors_and_type.css` (ou um alias Vite). Google Fonts carregadas via `<link>` em `index.html` de cada app. `lucide-react` é dependência de `packages/ui/`.

---

## Integration Points

| Sistema | Propósito | Autenticação | Estratégia de Erro/Retry |
|---|---|---|---|
| SMTP relay (MailHog dev / TBD prod) | Envio de magic link | SMTP AUTH (user/pass em secrets) | Polly retry exponencial 3 tentativas no handler; falha persistente → `email_outbox` para worker retry |
| Object Storage S3-compatible (MinIO dev / S3 ou R2 prod) | Documentos de opt-in | Access Key + Secret em secrets; HTTPS obrigatório em prod | Polly retry 2 tentativas em 5xx; falha → rollback metadata (operação idempotente via hash) |

Integrações **fora do escopo do MVP**: Receita Federal (pré-preencher razão social), antivírus, D02/D03/D04 (apenas consumirão `condominio.cadastrado.v1` futuramente via RabbitMQ — ADR-009).

---

## Impact Analysis

Todos os componentes são **novos** (projeto greenfield). Não há sistemas pré-existentes para modificar ou depreciar.

| Component | Impact Type | Description and Risk | Required Action |
|---|---|---|---|
| `PortaBox.Api` | new | Primeiro serviço .NET do projeto; define baseline arquitetural (risco alto de desvio em PRs subsequentes) | Code review rígido no primeiro release; publicar convenções em README |
| `PortaBox.Modules.Gestao` | new | Primeiro módulo; padrão que os demais (Encomendas, Hub, Relatórios) seguirão | Documentar estrutura de pastas e CQRS convencionado |
| `PortaBox.Infrastructure` | new | EF Core DbContext, Identity, adapters, workers | Cobrir isolamento de tenant por testes (crítico) |
| `apps/backoffice` | new | SPA React dedicada para operador | Separar bundle do `apps/sindico` via monorepo; aderir ao design system `portabox-design` (ADR-010) |
| `apps/sindico` | new | Esqueleto mínimo (login, setup password via magic link, home vazia) | Não implementar features de D01/F02+ neste TechSpec; reutilizar tokens/components do `packages/ui/` |
| `packages/ui/` | new | Componentes React compartilhados (Button, Input, Card, Badge, Modal, StepIndicator) + ícones Lucide | Implementar baseando-se em `ui_kits/admin-dashboard/` do skill `portabox-design` |
| `.claude/skills/portabox-design/` | reference | Design system versionado (tokens, kits, regras de copy pt-BR) | Obrigatoriamente invocado antes de qualquer implementação frontend |
| PostgreSQL schema | new | Tabelas: `condominio`, `sindico`, `opt_in_record`, `opt_in_document`, `magic_link`, `tenant_audit_log`, `domain_event_outbox`, `email_outbox`, `AspNet*` | Migrations via EF Core; seed de roles e operador em dev |
| Container MinIO | new | Dev/local para docs de opt-in | Adicionar ao `docker-compose.dev.yml` |
| Container MailHog | new | Dev/local para capturar e-mails | Adicionar ao `docker-compose.dev.yml` |
| Secrets (prod) | new | SMTP creds, S3 creds, Identity key ring, ASP.NET Data Protection keys | Definir onde ficam armazenados em prod (decisão de deploy) |

---

## Testing Approach

### Unit Tests

**Stack:** xUnit + AwesomeAssertions + Moq; padrão AAA; naming `Method_Condition_ExpectedBehavior`. Cobertura-alvo ≥ 80% para lógica de negócio.

**Componentes críticos:**
- Validators FluentValidation (CNPJ, CPF, e-mail, celular E.164, CEP).
- `CreateCondominioCommandHandler`: happy path, CNPJ duplicado, síndico com e-mail já existente, validação reprova, rollback em falha de magic link.
- `ActivateCondominioCommandHandler`: transição válida, bloqueio quando já ativo.
- `MagicLinkService`: emissão (hash correto, TTL default), consumo válido, expirado, já usado, inválido, invalidação de pendentes ao reemitir.
- `PasswordSetupCommandHandler`: consome token + define senha + marca consumo na mesma transação.
- `CreateCondominioOnDomainEventOutbox`: interceptor garantindo que evento é enfileirado atomicamente.

**Boundaries mockadas:** `IObjectStorage`, `IEmailSender`, `IUserManager` (via wrapper), `ITenantContext`, `ISystemClock`.

### Integration Tests

**Stack:** `WebApplicationFactory<Program>` + Testcontainers (PostgreSQL + MinIO) + fake SMTP em memória.

**Cenários obrigatórios:**
- **Fluxo completo do wizard**: POST com payload válido → 201 → e-mail registrado no fake SMTP → consumo do magic link → síndico consegue login com a senha definida.
- **Deduplicação**: criar tenant com CNPJ X → repetir → esperar 409 com detalhes do tenant existente.
- **Isolamento multi-tenant (teste de segurança)**: criar dois tenants A e B → autenticar como síndico de A → request de leitura enxerga apenas dados do tenant A mesmo em endpoints que retornam listas.
- **Upload de documento**: upload de PDF 1 MB → MinIO recebe → metadata persistida → download via presigned URL funciona.
- **Reemissão de magic link**: dois links consecutivos → apenas o segundo consome com sucesso.
- **Magic link expirado**: forçar expiração via relógio de teste → consumo retorna 400 genérico.
- **Go-live**: tenant em `PreAtivo` → activate → estado `Ativo` + entrada no audit log.
- **Outbox**: após criar tenant, `domain_event_outbox` tem linha `condominio.cadastrado.v1`; worker marca `published_at`.

**Política:** nenhum teste de integração usa mock de banco. Testcontainers é padrão oficial (skill `dotnet-testing`).

### Observações sobre QA manual

- Smoke test do backoffice React contra API em dev via Playwright (skill `playwright-cli`) antes do piloto.
- Verificar render do e-mail real em clientes (Gmail/Outlook/Apple Mail) antes do primeiro go-live.

---

## Development Sequencing

### Build Order

Sequência ordenada por dependências. Cada passo após o primeiro declara explicitamente do que depende.

1. **Skeleton da solution .NET + monorepo frontend** — sem dependências.
   Projetos `PortaBox.Api`, `PortaBox.Application`, `PortaBox.Domain`, `PortaBox.Infrastructure`, `PortaBox.Modules.Gestao`; `apps/backoffice` e `apps/sindico` via Vite.
2. **Configuração base** — depende do passo 1.
   `appsettings.*.json`, OpenTelemetry bootstrap, Serilog JSON, `IExceptionHandler` global, versionamento `/api/v1`, CORS.
3. **EF Core + PostgreSQL + Testcontainers** — depende do passo 2.
   `AppDbContext` com convenções (snake_case), connection pooling, fixture base de integração.
4. **ASP.NET Core Identity + migrations iniciais + seed `Operator`** — depende do passo 3.
5. **`ITenantContext` + global query filter base + `ITenantEntity`** — depende do passo 3.
6. **Tabela `condominio` + entidade `Condominio` + repositório** — depende do passo 5.
7. **`IMagicLinkService` + tabela `magic_link`** — depende dos passos 4 e 5.
8. **`IEmailSender` SMTP + `email_outbox` + worker retry** — depende do passo 3. Adapter MailKit + `FakeEmailSender` para tests.
9. **`IObjectStorage` (MinIO adapter) + `opt_in_document`** — depende do passo 3.
10. **`CreateCondominioCommandHandler` completo (condomínio + opt-in + síndico + magic link)** — depende dos passos 6, 7, 8.
11. **Endpoint de upload de documento de opt-in** — depende dos passos 9, 10.
12. **`ActivateCondominioCommandHandler` + `tenant_audit_log`** — depende do passo 6.
13. **Queries (lista paginada, detalhes)** — depende do passo 6.
14. **`PasswordSetupCommandHandler` + endpoint `/auth/password-setup`** — depende dos passos 7 e 4.
15. **Endpoints admin REST (controllers + Mapster DTOs)** — depende dos passos 10, 11, 12, 13.
16. **Outbox interceptor + `DomainEventOutboxPublisher` BackgroundService** — depende dos passos 3 e 10.
17. **Observability: health checks, métricas OTel, eventos de log-chave** — depende do passo 15.
18. **Backoffice React: wizard (3 etapas), lista, detalhes com go-live, reenvio de magic link** — depende do passo 15. **Pré-requisito adicional:** invocar a skill `portabox-design` e importar `colors_and_type.css` da skill em `apps/backoffice/src/styles/tokens.css`; adicionar `lucide-react` + Google Fonts (Plus Jakarta Sans, Inter, JetBrains Mono). Componentes reutilizáveis vão em `packages/ui/`. Toda cor/tipografia via CSS vars (ADR-010).
19. **Síndico React (esqueleto): login + setup-password + home placeholder** — depende do passo 15 e reutiliza tokens + componentes de `packages/ui/` (passo 18).
20. **Integration tests end-to-end + Playwright smoke** — depende dos passos 15, 17, 18, 19.
21. **Hardening: secrets em prod, STARTTLS SMTP, CSP, rate-limit do `/auth/password-setup`** — depende dos passos 15 e 20.
22. **Piloto go-live interno** — depende de todos os anteriores.

### Technical Dependencies

Dependências bloqueantes que precisam estar resolvidas:

- **Container registry para imagens dev** (Docker local é suficiente no início).
- **Domínio e subdomínios configurados** (`app.PortaBox.com`, `admin.PortaBox.com`, `api.PortaBox.com`) antes do piloto real.
- **Provedor SMTP de produção escolhido** (ADR-008 — Open Question) antes do primeiro e-mail real sair.
- **Credenciais S3/R2 de produção** antes do primeiro upload real.
- **Política de senha do Identity** alinhada com F05 (Open Question do PRD).
- **Chaves de ASP.NET Data Protection** persistentes entre deploys em prod (volume ou provider dedicado).

---

## Monitoring and Observability

Seguindo skill `dotnet-observability`.

### Metrics (OpenTelemetry)

- `condominio_created_total` (counter) — total de tenants criados; label `status_outcome`.
- `condominio_activated_total` (counter).
- `magic_link_issued_total`, `magic_link_consumed_total`, `magic_link_expired_total` (counters).
- `email_send_duration_seconds` (histogram) — latência do envio SMTP.
- `email_outbox_age_seconds` (gauge) — idade do e-mail mais antigo ainda não enviado.
- `domain_event_outbox_pending_count` (gauge) — fila pendente.
- `http_server_duration_seconds` (padrão ASP.NET Core OTel) por rota.

### Logs (Serilog JSON estruturado)

- Campos base: `timestamp`, `level`, `request_id`, `trace_id`, `span_id`, `user_id`, `tenant_id` (quando aplicável), `event`.
- Eventos-chave (com campos específicos):
  - `condominio.created` (`condominio_id`, `cnpj_suffix` — últimos 4 dígitos apenas).
  - `condominio.activated` (`condominio_id`, `activated_by`).
  - `magic_link.issued` (`user_id`, `token_id`, `purpose`, `expires_at`).
  - `magic_link.consumed` (`user_id`, `token_id`, `ip`).
  - `magic_link.invalidated` (`user_id`, `count`).
  - `email.sent` (`template`, `to_hash` — SHA-256 do e-mail para não logar PII).
  - `email.failed` (`template`, `to_hash`, `error_kind`).
- **Sanitização obrigatória**: nunca logar token em claro, senha, CPF, CNPJ completo ou e-mail em texto aberto.

### Health Checks

- `/health/live` — processo vivo.
- `/health/ready` — DB (`SELECT 1`), object storage (`HeadBucket`), SMTP (TCP connect ao host, sem enviar e-mail).
- Probes Kubernetes compatíveis (payload JSON com estado).

### Alertas (no mínimo, no primeiro dia de prod)

- `email_send_failure_rate > 5%` em janela de 5 min.
- `email_outbox_age_seconds > 900` (15 min).
- `domain_event_outbox_pending_count > 1000`.
- Qualquer resposta 5xx sustentada por mais de 3 requests em 1 minuto.

---

## Technical Considerations

### Key Decisions (resumo executivo)

| Decisão | O que foi escolhido | Principal trade-off | ADR |
|---|---|---|---|
| Isolamento multi-tenant | Shared schema + `tenant_id` + EF Core query filter | Risco de vazamento se filter for ignorado; mitigado por testes de isolamento | [ADR-004](adrs/adr-004.md) |
| UI do backoffice | SPA React separado em subdomínio | Dois pipelines de build vs isolamento de bundle | [ADR-005](adrs/adr-005.md) |
| Autenticação | ASP.NET Identity com roles `Operator`/`Sindico` | Uma base única de usuários simplifica auditoria; SSO fica para Fase 2 | [ADR-005](adrs/adr-005.md) |
| Magic link | Token opaco SHA-256 hashed, TTL 72h, uso único | Mais código que usar Identity token, em troca de controle fino | [ADR-006](adrs/adr-006.md) |
| Storage de documentos | `IObjectStorage` + adapter MinIO/S3 + metadata em Postgres | Dois storages a manter, em troca de não poluir DB transacional | [ADR-007](adrs/adr-007.md) |
| E-mail transacional | `IEmailSender` + SMTP genérico (MailKit) | Provedor comercial adiado; abstração permite troca sem refactor | [ADR-008](adrs/adr-008.md) |
| Eventos de domínio | Dispatcher in-process + outbox pattern desde já | RabbitMQ adiado até Mastra; outbox garante atomicidade | [ADR-009](adrs/adr-009.md) |
| Design system do frontend | Adesão integral ao skill `portabox-design` (tokens + tipografia + Lucide + copy pt-BR) | Disciplina de consumo via CSS vars vs velocidade de biblioteca pronta | [ADR-010](adrs/adr-010.md) |

### Known Risks

| Risco | Probabilidade | Mitigação |
|---|---|---|
| Query filter multi-tenant esquecido em query raw ou migration (vaza dados entre tenants) | Média | Testes de isolamento obrigatórios; code review; analyzer futuro pode flagar `IgnoreQueryFilters()` |
| Token do magic link vazando em logs de servidor (URL completa na access log) | Média | Interceptor que mascara `?token=`; smoke test em staging antes do primeiro release |
| Deliverability ruim do SMTP (e-mail em spam, atraso) | Alta | SPF/DKIM/DMARC no domínio; provedor de produção com reputação conhecida antes do piloto |
| Órfãos no object storage (upload ok, insert de metadata falhou) | Baixa | Fluxo reservar→upload→commit; job periódico opcional de consolidação |
| Backoffice exposto na internet com usuário `Operator` com senha fraca | Média | Política de senha forte; rate-limit no `/auth/login`; em Fase 2 avaliar MFA/SSO |
| Migração da outbox para RabbitMQ mais tarde quebra consumers implícitos | Baixa | Eventos versionados (`.v1`); changelog de eventos documentado por domínio |
| Performance do EF Core com query filter em tabelas crescendo | Baixa (piloto) | Índices compostos começando por `tenant_id`; monitorar em Fase 2 |

### Pesquisa/prototipagem pendente

- Confirmar provedor SMTP de prod (shortlist: SES, Mailgun, Postmark, Resend).
- Prototipar a UX do backoffice com Vite + React Router + algum design system leve (shadcn/ui) — decisão detalhada fica fora deste TechSpec (competência do tech lead de frontend).
- Verificar comportamento de presigned URL do Cloudflare R2 em comparação a S3 para confirmar compatibilidade.

---

## Architecture Decision Records

Decisões registradas durante o PRD e o TechSpec, em ordem cronológica:

- [ADR-001: Onboarding de Tenant no MVP — Operador Interno, Wizard Mínimo e Go-live Manual Independente](adrs/adr-001.md) — Wizard assistido pela equipe; tenant nasce `PreAtivo`; go-live é ação manual separada.
- [ADR-002: Registro do Opt-in Coletivo LGPD com Metadados Obrigatórios e Upload Opcional](adrs/adr-002.md) — Metadados estruturados são obrigatórios; anexar ata/termo é opcional.
- [ADR-003: CNPJ Obrigatório como Identificador Canônico do Condomínio](adrs/adr-003.md) — Validação de dígito e bloqueio de duplicata por UNIQUE INDEX.
- [ADR-004: Isolamento Multi-tenant via Shared Schema com `tenant_id` e EF Core Query Filters](adrs/adr-004.md) — Padrão único para todos os serviços .NET; entidades implementam `ITenantEntity`.
- [ADR-005: Backoffice como SPA React Separado; Autenticação via ASP.NET Core Identity com Roles `Operator` e `Sindico`](adrs/adr-005.md) — Dois bundles, uma base de usuários, discriminação por role.
- [ADR-006: Magic Link com Token Opaco (SHA-256) em Tabela Dedicada, TTL 72h e Uso Único](adrs/adr-006.md) — Persistência custom em `magic_link`; invalidação explícita na reemissão.
- [ADR-007: Storage de Documentos via `IObjectStorage` (MinIO/S3) com Metadados em Postgres](adrs/adr-007.md) — Adapters por ambiente, metadata em `opt_in_document`, presigned URL 5 min.
- [ADR-008: `IEmailSender` sobre SMTP Genérico no MVP (Provedor Comercial Adiado)](adrs/adr-008.md) — MailKit + MailHog em dev; provedor prod em Open Question.
- [ADR-009: Eventos de Domínio In-process no MVP com Outbox Pattern para Publicação Futura](adrs/adr-009.md) — Atomicidade estado+evento desde o dia 1; RabbitMQ chega quando Mastra entrar.
- [ADR-010: Frontend Adere ao Design System `portabox-design` (Tokens, Tipografia, Componentes e Copy pt-BR)](adrs/adr-010.md) — Fonte única de verdade visual e de voz para todas as SPAs; obrigatório invocar a skill antes de implementar frontend.

---

*TechSpec gerado com a skill `cy-create-techspec`. Próximo passo no pipeline: `cy-create-tasks` para quebrar este TechSpec em tarefas de implementação executáveis por agentes de código.*
