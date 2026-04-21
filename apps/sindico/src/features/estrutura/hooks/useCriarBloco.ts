import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  criarBloco,
  queryKeys,
  type Bloco,
  type CreateBlocoRequest,
  type Estrutura,
} from '@portabox/api-client'
import { insertBlocoIntoEstrutura, upsertBlocoInEstrutura } from './cache'

interface CreateBlocoContext {
  previousData?: Estrutura
  tempId: string
}

function createTemporaryId() {
  return globalThis.crypto?.randomUUID?.() ?? `temp-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export function useCriarBloco(condominioId: string) {
  const queryClient = useQueryClient()

  return useMutation<Bloco, Error, CreateBlocoRequest, CreateBlocoContext>({
    mutationFn: (input) => criarBloco({ condominioId, ...input }),
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.estrutura(condominioId) })

      const previousData = queryClient.getQueryData<Estrutura>(queryKeys.estrutura(condominioId))
      const tempId = createTemporaryId()

      queryClient.setQueryData<Estrutura | undefined>(
        queryKeys.estrutura(condominioId),
        (current) => insertBlocoIntoEstrutura(current, {
          id: tempId,
          condominioId,
          nome: input.nome.trim(),
          ativo: true,
          inativadoEm: null,
        }),
      )

      return { previousData, tempId }
    },
    onError: (_error, _input, context) => {
      if (context?.previousData) {
        queryClient.setQueryData(queryKeys.estrutura(condominioId), context.previousData)
      }
    },
    onSuccess: (bloco, _input, context) => {
      queryClient.setQueryData<Estrutura | undefined>(
        queryKeys.estrutura(condominioId),
        (current) => upsertBlocoInEstrutura(current, bloco, context?.tempId),
      )

      void queryClient.invalidateQueries({ queryKey: queryKeys.estrutura(condominioId) })
    },
  })
}
