import { apiFetch } from '../http'
import type { CreateUnidadeRequest, Unidade } from '../types'

export function criarUnidade(input: {
  condominioId: string
  blocoId: string
} & CreateUnidadeRequest): Promise<Unidade> {
  const { condominioId, blocoId, ...body } = input
  return apiFetch<Unidade>(`/condominios/${condominioId}/blocos/${blocoId}/unidades`, {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export function inativarUnidade(input: {
  condominioId: string
  blocoId: string
  unidadeId: string
}): Promise<Unidade> {
  return apiFetch<Unidade>(
    `/condominios/${input.condominioId}/blocos/${input.blocoId}/unidades/${input.unidadeId}:inativar`,
    {
      method: 'POST',
    },
  )
}

export function reativarUnidade(input: {
  condominioId: string
  blocoId: string
  unidadeId: string
}): Promise<Unidade> {
  return apiFetch<Unidade>(
    `/condominios/${input.condominioId}/blocos/${input.blocoId}/unidades/${input.unidadeId}:reativar`,
    {
      method: 'POST',
    },
  )
}
