# Integration Test Database Fixture

`PostgresDatabaseFixture` é a fixture base para testes de integração com PostgreSQL real via Testcontainers.

## Como usar

1. Adicione `[Collection(nameof(PostgresDatabaseCollection))]` na classe de teste.
2. Receba `PostgresDatabaseFixture` no construtor da classe.
3. Chame `await fixture.ResetAsync()` no início de cada teste que grava no banco.
4. Use `fixture.ConnectionString` para configurar `WebApplicationFactory`, `DbContext` ou conexões `Npgsql`.

## Observações

- A fixture sobe `postgres:16-alpine`.
- As migrations do `AppDbContext` são aplicadas automaticamente no `InitializeAsync`.
- O reset preserva o schema `public` e ignora `__EFMigrationsHistory`.
