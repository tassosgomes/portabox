import { useMutation, useQueryClient } from '@tanstack/react-query'
import { inativarUnidade, queryKeys, type Estrutura, type Unidade } from '@portabox/api-client'
import { upsertUnidadeInEstrutura } from './cache'

interface InativarUnidadeInput {
  blocoId: string
  unidadeId: string
}

export function useInativarUnidade(condominioId: string) {
  const queryClient = useQueryClient()

  return useMutation<Unidade, Error, InativarUnidadeInput>({
    mutationFn: ({ blocoId, unidadeId }) => inativarUnidade({ condominioId, blocoId, unidadeId }),
    onSuccess: (unidade, { blocoId }) => {
      queryClient.setQueryData<Estrutura | undefined>(
        queryKeys.estrutura(condominioId),
        (current) => upsertUnidadeInEstrutura(current, blocoId, unidade),
      )

      void queryClient.invalidateQueries({ queryKey: queryKeys.estrutura(condominioId) })
    },
  })
}
