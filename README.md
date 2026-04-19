# PortaBox

Scaffold inicial do projeto F01 com solution .NET 8, monorepo frontend em `pnpm` e ambiente local via Docker Compose.

## Estrutura

- `src/`: API, contratos, domínio, infraestrutura e módulo `Gestao`
- `tests/`: testes unitários do módulo e testes de integração da API
- `apps/`: futuras SPAs `backoffice` e `sindico`
- `packages/`: bibliotecas compartilhadas do frontend
- `docker-compose.dev.yml`: PostgreSQL 16, MinIO e MailHog para desenvolvimento

## Pré-requisitos

- `.NET SDK 8` ou superior com suporte a target `net8.0`
- `Node.js 18+`
- `pnpm 10+`
- `Docker` com Compose

## Subir o ambiente

```bash
pnpm install
docker compose -f docker-compose.dev.yml up -d
dotnet build PortaBox.sln
```

Serviços expostos localmente:

- PostgreSQL: `localhost:5432`
- MinIO API: `http://localhost:9000`
- MinIO Console: `http://localhost:9001`
- MailHog SMTP: `localhost:1025`
- MailHog UI: `http://localhost:8025`

## Credenciais de desenvolvimento

- PostgreSQL: `portabox` / `portabox` / database `portabox`
- MinIO: usuário `admin`
- MinIO: senha `adminadmin`

`admin/admin` não é aceito pelas versões atuais do MinIO porque a senha mínima exigida é maior que 5 caracteres. O scaffold usa a combinação mínima funcional mais próxima para manter o ambiente executável.

## Smoke test

```bash
./scripts/smoke.sh
```

O script valida:

- `pnpm install`
- `dotnet build PortaBox.sln`
- `docker compose -f docker-compose.dev.yml config`
- subida do compose e conectividade com PostgreSQL, MinIO e MailHog
