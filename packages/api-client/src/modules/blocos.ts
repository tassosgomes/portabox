import { apiFetch } from '../http'
import type { Bloco, CreateBlocoRequest, RenameBlocoRequest } from '../types'

export function criarBloco(input: { condominioId: string } & CreateBlocoRequest): Promise<Bloco> {
  const { condominioId, ...body } = input
  return apiFetch<Bloco>(`/condominios/${condominioId}/blocos`, {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export function renomearBloco(input: {
  condominioId: string
  blocoId: string
} & RenameBlocoRequest): Promise<Bloco> {
  const { condominioId, blocoId, ...body } = input
  return apiFetch<Bloco>(`/condominios/${condominioId}/blocos/${blocoId}`, {
    method: 'PATCH',
    body: JSON.stringify(body),
  })
}

export function inativarBloco(input: { condominioId: string; blocoId: string }): Promise<Bloco> {
  return apiFetch<Bloco>(`/condominios/${input.condominioId}/blocos/${input.blocoId}:inativar`, {
    method: 'POST',
  })
}

export function reativarBloco(input: { condominioId: string; blocoId: string }): Promise<Bloco> {
  return apiFetch<Bloco>(`/condominios/${input.condominioId}/blocos/${input.blocoId}:reativar`, {
    method: 'POST',
  })
}
