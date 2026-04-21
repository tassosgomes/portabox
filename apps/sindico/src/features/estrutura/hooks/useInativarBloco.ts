import { useMutation, useQueryClient } from '@tanstack/react-query'
import { inativarBloco, queryKeys, type Bloco, type Estrutura } from '@portabox/api-client'
import { upsertBlocoInEstrutura } from './cache'

interface InativarBlocoInput {
  blocoId: string
}

export function useInativarBloco(condominioId: string) {
  const queryClient = useQueryClient()

  return useMutation<Bloco, Error, InativarBlocoInput>({
    mutationFn: ({ blocoId }) => inativarBloco({ condominioId, blocoId }),
    onSuccess: (bloco) => {
      queryClient.setQueryData<Estrutura | undefined>(
        queryKeys.estrutura(condominioId),
        (current) => upsertBlocoInEstrutura(current, bloco),
      )

      void queryClient.invalidateQueries({ queryKey: queryKeys.estrutura(condominioId) })
    },
  })
}
