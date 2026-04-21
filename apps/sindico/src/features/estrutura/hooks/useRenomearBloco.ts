import { useMutation, useQueryClient } from '@tanstack/react-query'
import { queryKeys, renomearBloco, type Bloco, type Estrutura } from '@portabox/api-client'
import { upsertBlocoInEstrutura } from './cache'

interface RenameBlocoInput {
  blocoId: string
  nome: string
}

export function useRenomearBloco(condominioId: string) {
  const queryClient = useQueryClient()

  return useMutation<Bloco, Error, RenameBlocoInput>({
    mutationFn: ({ blocoId, nome }) => renomearBloco({ condominioId, blocoId, nome }),
    onSuccess: (bloco) => {
      queryClient.setQueryData<Estrutura | undefined>(
        queryKeys.estrutura(condominioId),
        (current) => upsertBlocoInEstrutura(current, bloco),
      )

      void queryClient.invalidateQueries({ queryKey: queryKeys.estrutura(condominioId) })
    },
  })
}
