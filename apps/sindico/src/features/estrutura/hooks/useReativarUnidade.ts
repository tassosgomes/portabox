import { useMutation, useQueryClient } from '@tanstack/react-query'
import { queryKeys, reativarUnidade, type Estrutura, type Unidade } from '@portabox/api-client'
import { upsertUnidadeInEstrutura } from './cache'

interface ReativarUnidadeInput {
  blocoId: string
  unidadeId: string
}

export function useReativarUnidade(condominioId: string) {
  const queryClient = useQueryClient()

  return useMutation<Unidade, Error, ReativarUnidadeInput>({
    mutationFn: ({ blocoId, unidadeId }) => reativarUnidade({ condominioId, blocoId, unidadeId }),
    onSuccess: (unidade, { blocoId }) => {
      queryClient.setQueryData<Estrutura | undefined>(
        queryKeys.estrutura(condominioId),
        (current) => upsertUnidadeInEstrutura(current, blocoId, unidade),
      )

      void queryClient.invalidateQueries({ queryKey: queryKeys.estrutura(condominioId) })
    },
  })
}
