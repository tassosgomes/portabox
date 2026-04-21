import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  criarUnidade,
  queryKeys,
  type CreateUnidadeRequest,
  type Estrutura,
  type Unidade,
} from '@portabox/api-client'
import { insertUnidadeIntoEstrutura, upsertUnidadeInEstrutura } from './cache'

interface CreateUnidadeInput extends CreateUnidadeRequest {
  blocoId: string
}

interface CreateUnidadeContext {
  previousData?: Estrutura
  tempId: string
}

function createTemporaryId() {
  return globalThis.crypto?.randomUUID?.() ?? `temp-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export function useCriarUnidade(condominioId: string) {
  const queryClient = useQueryClient()

  return useMutation<Unidade, Error, CreateUnidadeInput, CreateUnidadeContext>({
    mutationFn: ({ blocoId, ...input }) => criarUnidade({ condominioId, blocoId, ...input }),
    onMutate: async ({ blocoId, andar, numero }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.estrutura(condominioId) })

      const previousData = queryClient.getQueryData<Estrutura>(queryKeys.estrutura(condominioId))
      const tempId = createTemporaryId()

      queryClient.setQueryData<Estrutura | undefined>(
        queryKeys.estrutura(condominioId),
        (current) => insertUnidadeIntoEstrutura(current, blocoId, {
          id: tempId,
          blocoId,
          andar,
          numero: numero.trim().toUpperCase(),
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
    onSuccess: (unidade, { blocoId }, context) => {
      queryClient.setQueryData<Estrutura | undefined>(
        queryKeys.estrutura(condominioId),
        (current) => upsertUnidadeInEstrutura(current, blocoId, unidade, context?.tempId),
      )

      void queryClient.invalidateQueries({ queryKey: queryKeys.estrutura(condominioId) })
    },
  })
}
