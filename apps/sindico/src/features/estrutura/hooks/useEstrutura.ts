import { useEffect, useRef } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getEstrutura, queryKeys } from '@portabox/api-client'

export function useEstrutura(condominioId: string, includeInactive: boolean) {
  const lastIncludeInactive = useRef(includeInactive)
  const query = useQuery({
    queryKey: queryKeys.estrutura(condominioId),
    queryFn: () => getEstrutura(condominioId, includeInactive),
    enabled: Boolean(condominioId),
  })

  useEffect(() => {
    if (!condominioId) {
      return
    }

    if (lastIncludeInactive.current === includeInactive) {
      return
    }

    lastIncludeInactive.current = includeInactive
    void query.refetch()
  }, [condominioId, includeInactive, query])

  return query
}
