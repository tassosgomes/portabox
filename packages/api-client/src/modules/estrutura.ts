import { apiFetch } from '../http'
import type { Estrutura } from '../types'

export function getEstrutura(condominioId: string, includeInactive: boolean): Promise<Estrutura> {
  const searchParams = new URLSearchParams({ includeInactive: String(includeInactive) })
  return apiFetch<Estrutura>(`/condominios/${condominioId}/estrutura?${searchParams.toString()}`)
}

export function getEstruturaAdmin(condominioId: string, includeInactive: boolean): Promise<Estrutura> {
  const searchParams = new URLSearchParams({ includeInactive: String(includeInactive) })
  return apiFetch<Estrutura>(`/admin/condominios/${condominioId}/estrutura?${searchParams.toString()}`)
}
