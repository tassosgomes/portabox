# Task Memory: task_12.md

Keep only task-local execution context here. Do not duplicate facts that are obvious from the repository, task file, PRD documents, or git history.

## Objective Snapshot
- Implementar a orquestração completa de criação do condomínio com validação, persistência atômica, evento de domínio e disparo pós-commit do magic link/e-mail.

## Important Decisions
- O handler ficará em `PortaBox.Modules.Gestao`, então a criação do usuário do síndico será mediada por uma abstração de aplicação implementada na infraestrutura para preservar a direção de dependências e evitar referência direta a `AppUser`/`UserManager`.
- A atomicidade final foi implementada com um único `SaveChangesAsync` no mesmo `AppDbContext` escopado; o provisionamento do `AppUser` do síndico apenas anexa `AppUser` + `IdentityUserRole<Guid>` ao contexto antes da persistência, preservando o disparo do evento in-process somente após o commit efetivo.
- A task 12 vai bootstrapar `TenantAuditEntry`/`TenantAuditEventKind` no nível mínimo necessário para registrar `Created`, deixando a evolução de ativação e cobertura adicional para a task 14.

## Learnings
- `PortaBox.Modules.Gestao` ainda não referencia `FluentValidation`, `Options` nem qualquer abstração de Identity; isso precisa ser adicionado de forma mínima para suportar command validator, link de setup e provisionamento do síndico.
- Para testar rollback sem expor o handler a detalhes de provider, o cenário de falha de `OptInRecord` foi coberto em integração via substituição scoped do repositório por um dublê que lança `DbUpdateException` antes do `SaveChanges`; isso valida que `Condominio`, `AppUser` e e-mail não ficam persistidos quando a orquestração quebra antes do commit.

## Files / Surfaces
- `src/PortaBox.Application.Abstractions`
- `src/PortaBox.Modules.Gestao`
- `src/PortaBox.Infrastructure`
- `tests/PortaBox.Modules.Gestao.UnitTests`
- `tests/PortaBox.Api.IntegrationTests`

## Errors / Corrections
- A primeira tentativa usou transação explícita, mas isso permitia que o dispatcher in-process rodasse antes do `CommitAsync`; o fluxo foi simplificado para `SaveChangesAsync` único para manter a garantia "e-mail só após commit" descrita no tech spec.

## Ready for Next Run
