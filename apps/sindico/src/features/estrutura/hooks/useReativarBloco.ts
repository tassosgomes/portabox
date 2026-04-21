import { useMutation, useQueryClient } from '@tanstack/react-query'
import { queryKeys, reativarBloco, type Bloco, type Estrutura } from '@portabox/api-client'
import { upsertBlocoInEstrutura } from './cache'

interface ReativarBlocoInput {
  blocoId: string
}

export function useReativarBloco(condominioId: string) {
  const queryClient = useQueryClient()

  return useMutation<Bloco, Error, ReativarBlocoInput>({
    mutationFn: ({ blocoId }) => reativarBloco({ condominioId, blocoId }),
    onSuccess: (bloco) => {
      queryClient.setQueryData<Estrutura | undefined>(
        queryKeys.estrutura(condominioId),
        (current) => upsertBlocoInEstrutura(current, bloco),
      )

      void queryClient.invalidateQueries({ queryKey: queryKeys.estrutura(condominioId) })
    },
  })
}
