# @portabox/api-client

Cliente HTTP tipado compartilhado para os apps frontend da PortaBox.

## Query keys

Use sempre os helpers exportados por `queryKeys` para manter a convenção hierárquica definida no ADR-010.

```ts
queryKeys.estrutura(condominioId)
queryKeys.estruturaAdmin(condominioId)
```

## Tipos gerados

Os tipos deste pacote são derivados de `.compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml`.

Para regenerar:

```bash
pnpm --filter @portabox/api-client generate:types
```
