# Smoke Manual — F02: Gestão de Blocos e Unidades

**Tempo estimado:** < 15 minutos  
**Pré-requisitos:** Stack local rodando (`docker-compose.dev.yml` + API + apps)  
**Data de referência:** 2026-04-20

---

## Pré-condições

Antes de iniciar, certifique-se de que:

- [ ] `docker-compose.dev.yml` está UP (`docker compose -f docker-compose.dev.yml up -d`)
- [ ] API rodando em `http://localhost:5000` (`curl http://localhost:5000/health/live` → 200)
- [ ] `apps/sindico` rodando em `http://localhost:5173` (`pnpm --filter @portabox/sindico dev`)
- [ ] `apps/backoffice` rodando em `http://localhost:4173` (`pnpm --filter @portabox/backoffice dev`)
- [ ] Usuário operador disponível: `operator@portabox.dev` / `PortaBox123!`

---

## Passo 1 — Operador cria o tenant (F01)

> Assume que F01 está completo. Este passo cria o tenant de teste para o smoke.

1. Acesse `http://localhost:4173` e faça login como operador (`operator@portabox.dev`).
2. Clique em **Novo Condomínio** e preencha o wizard:
   - **Nome Fantasia:** `Residencial Smoke F02`
   - **Cidade / UF:** São Paulo / SP
   - **Data da Assembleia:** data de hoje
   - **Nome do Síndico:** `Síndico Smoke`
   - **E-mail do Síndico:** `sindico-smoke@portabox.test`
   - **Celular (E.164):** `+5511999990001`
3. Confirme e clique em **Criar**.
4. ✅ **Verificar:** tenant aparece na lista com status `Pré-ativo`.
5. Anote o `condominioId` da URL ou do painel de detalhes (será usado nos passos seguintes).

> **Variável:** `$CID=<condominioId anotado>`

---

## Passo 2 — Síndico recebe magic link, define senha e loga

1. No backoffice, abra os detalhes do tenant recém-criado e clique em **Reenviar Magic Link**.
2. Acesse o MailHog em `http://localhost:8025` e abra o e-mail recebido por `sindico-smoke@portabox.test`.
3. Clique no link de ativação. Você será redirecionado para `apps/sindico` na página de definição de senha.
4. Defina a senha: `Sindico@Smoke123` e confirme.
5. ✅ **Verificar:** redirecionado para a tela inicial do síndico, logado como `Síndico Smoke`.

---

## Passo 3 — Síndico acessa `/estrutura` e vê empty state

1. No menu lateral do `apps/sindico`, clique em **Estrutura** (ou acesse `/estrutura`).
2. ✅ **Verificar:** empty state exibido com botão **"Cadastrar primeiro bloco"**. Não há blocos na tela.

---

## Passo 4 — Síndico cria "Bloco A"

1. Clique em **Cadastrar primeiro bloco** (ou **Novo bloco** no cabeçalho).
2. No modal, preencha **Nome:** `Bloco A` e clique em **Criar**.
3. ✅ **Verificar:** "Bloco A" aparece na árvore. Contador no cabeçalho exibe "1 bloco cadastrado".

---

## Passo 5 — Síndico cria 3 unidades em "Bloco A"

> Selecione "Bloco A" na árvore e use **Adicionar unidade** (modo individual).

**Unidade 1:**
- Andar: `1` / Número: `101`
- Clique em **Salvar**.

**Unidade 2:**
- Clique em **Adicionar próxima unidade** (modo batch) ou **Adicionar unidade** novamente.
- Andar: `2` / Número: `201`
- Clique em **Salvar**.

**Unidade 3:**
- Andar: `2` / Número: `201A`
- Clique em **Salvar**.

✅ **Verificar:** árvore exibe "Bloco A" com:
- Andar 1: 101
- Andar 2: 201, 201A

---

## Passo 6 — Síndico renomeia "Bloco A" para "Torre Alfa"

1. Na árvore, clique no menu de ações (⋯) ao lado de **Bloco A**.
2. Selecione **Renomear**.
3. No modal, altere o nome para `Torre Alfa` e confirme.
4. ✅ **Verificar:** árvore exibe "Torre Alfa" (sem "Bloco A"). Contador permanece "1 bloco cadastrado".

---

## Passo 7 — Síndico inativa unidade 201A, reativa e confirma

### 7a — Inativar

1. Na árvore, dentro de "Torre Alfa" / Andar 2, clique no menu de ações (⋯) de **201A**.
2. Selecione **Inativar unidade**.
3. Confirme no modal de confirmação.
4. ✅ **Verificar:** 201A some da árvore (filtro padrão não exibe inativos). Andar 2 exibe apenas "201".

### 7b — Tornar inativos visíveis

1. Marque o toggle **Mostrar inativos** no cabeçalho.
2. ✅ **Verificar:** 201A reaparece na árvore com indicador visual de inativo (ex.: badge ou opacidade reduzida).

### 7c — Reativar

1. Clique no menu de ações (⋯) de **201A** e selecione **Reativar unidade**.
2. Confirme no modal.
3. ✅ **Verificar:** 201A volta ao estado ativo. Andar 2 exibe "201" e "201A".
4. Desmarque o toggle **Mostrar inativos**. ✅ **Verificar:** ambas as unidades continuam visíveis.

---

## Passo 8 — Operador verifica a árvore em modo read-only no backoffice

1. No backoffice (`http://localhost:4173`), acesse a lista de condomínios.
2. Clique em **Detalhes** do tenant `Residencial Smoke F02`.
3. Acesse a aba ou link **Estrutura** (`/tenants/$CID/estrutura`).
4. ✅ **Verificar:**
   - Árvore exibida em modo read-only (sem botões de criar/renomear/inativar).
   - "Torre Alfa" aparece com 3 unidades ativas (101, 201, 201A).
   - Seletor de tenant exibe o nome correto.
   - Contador indica "1 bloco cadastrado" e "3 unidades ativas".

---

## Passo 9 — Inspeção do banco de dados (tenant_audit_entry)

Execute o comando abaixo (requer `psql` ou acesso ao container Postgres):

```bash
docker exec -it portabox-postgres-1 psql -U portabox -d portabox -c \
  "SELECT event_kind, performed_by_user_id, metadata_json->>'nomeBloco' AS bloco, created_at \
   FROM tenant_audit_entry \
   WHERE tenant_id = '$CID' \
   ORDER BY created_at;"
```

✅ **Verificar** que existem exatamente as seguintes entradas (uma por operação):

| event_kind | Operação |
|---|---|
| 5 (BlocoCriado) | Criação de "Bloco A" |
| 9 (UnidadeCriada) | Criação de unidade 101 |
| 9 (UnidadeCriada) | Criação de unidade 201 |
| 9 (UnidadeCriada) | Criação de unidade 201A |
| 6 (BlocoRenomeado) | Rename → "Torre Alfa" |
| 10 (UnidadeInativada) | Inativação de 201A |
| 11 (UnidadeReativada) | Reativação de 201A |

**Total esperado:** 7 entradas para este tenant.

---

## Passo 10 — Verificação de Logs Estruturados

Conforme TechSpec seção **Monitoring**, cada handler loga campos obrigatórios. Durante ou após o smoke, inspecione os logs da API (stdout em dev ou arquivo `logs/portabox-*.log`) e confirme que cada operação emite uma linha com todos os campos abaixo:

| Campo | Exemplo |
|---|---|
| `event` | `"bloco.criado"`, `"bloco.renomeado"`, `"unidade.inativada"`, etc. |
| `tenant_id` | UUID do tenant |
| `condominio_id` | UUID do condomínio |
| `bloco_id` | UUID do bloco (quando aplicável) |
| `unidade_id` | UUID da unidade (quando aplicável) |
| `performed_by_user_id` | UUID do síndico autenticado |
| `outcome` | `"success"`, `"validation_failure"` ou `"conflict"` |
| `duration_ms` | Inteiro positivo |

**Como verificar (logs em JSON via Serilog/OTel):**

```bash
# Filtrar logs das operações de F02 no stdout da API (em dev)
grep -E '"event".*"bloco\.|unidade\.' portabox-dev.log | \
  python3 -c "
import sys, json
for line in sys.stdin:
    try:
        obj = json.loads(line)
        required = ['event','tenant_id','bloco_id','performed_by_user_id','outcome']
        missing = [f for f in required if f not in obj]
        if missing:
            print(f'MISSING FIELDS: {missing} in: {obj.get(\"event\")}')
        else:
            print(f'OK: {obj[\"event\"]} outcome={obj[\"outcome\"]}')
    except json.JSONDecodeError:
        pass
"
```

✅ **Verificar:** nenhuma linha imprime `MISSING FIELDS`. Todos os campos obrigatórios presentes em cada operação.

---

## Critérios de Aprovação (Gate Humano)

| # | Critério | Resultado |
|---|---|---|
| 1 | Tenant criado e síndico logado | ☐ Passou / ☐ Falhou |
| 2 | Empty state visível em /estrutura | ☐ Passou / ☐ Falhou |
| 3 | "Bloco A" criado com sucesso | ☐ Passou / ☐ Falhou |
| 4 | 3 unidades cadastradas corretamente | ☐ Passou / ☐ Falhou |
| 5 | Bloco renomeado para "Torre Alfa" | ☐ Passou / ☐ Falhou |
| 6 | Unidade 201A inativada e reativada | ☐ Passou / ☐ Falhou |
| 7 | Backoffice mostra árvore read-only | ☐ Passou / ☐ Falhou |
| 8 | Banco tem exatamente 7 audit entries | ☐ Passou / ☐ Falhou |
| 9 | Logs estruturados têm todos os campos obrigatórios (Passo 10) | ☐ Passou / ☐ Falhou |

**Execução aprovada quando todos os critérios marcados como "Passou".**

---

## Evidências a Capturar (opcional mas recomendado)

- Screenshot do empty state (Passo 3)
- Screenshot da árvore com 3 unidades (Passo 5)
- Screenshot da árvore renomeada (Passo 6)
- Screenshot do backoffice read-only (Passo 8)
- Output do SQL de auditoria (Passo 9)

---

## Notas de Performance

Após o smoke básico, execute o script de carga para validar performance com 300 unidades:

```bash
./scripts/seed-f02.sh --condominio "$CID" --token "$SINDICO_TOKEN" --unidades 300
```

O endpoint `GET /api/v1/condominios/$CID/estrutura` deve responder com p95 < 500ms. Ver `scripts/seed-f02.sh` para detalhes.

---

## Notas de Acessibilidade

Executar Lighthouse a11y localmente:

```bash
# Sindico app
npx lighthouse http://localhost:5173/estrutura \
  --only-categories=accessibility \
  --output=json \
  --output-path=docs/lighthouse-sindico-estrutura.json

# Backoffice
npx lighthouse "http://localhost:4173/tenants/$CID/estrutura" \
  --only-categories=accessibility \
  --output=json \
  --output-path=docs/lighthouse-backoffice-estrutura.json
```

Meta: score ≥ 95 em ambas as rotas.
