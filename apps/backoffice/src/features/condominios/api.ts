import { apiClient, ApiHttpError } from '@/shared/api/client'
import type {
  WizardData,
  CreateCondominioResponse,
  CondominioListItem,
  CondominioDetails,
  OptInDocumentItem,
  DownloadUrlResponse,
  PagedResult,
  ListCondominiosParams,
} from './types'

export { ApiHttpError }

export function buildPayload(data: WizardData) {
  return {
    nomeFantasia: data.dados.nomeFantasia.trim(),
    cnpj: data.dados.cnpj.replace(/\D/g, ''),
    enderecoLogradouro: data.dados.logradouro.trim() || undefined,
    enderecoNumero: data.dados.numero.trim() || undefined,
    enderecoComplemento: data.dados.complemento.trim() || undefined,
    enderecoBairro: data.dados.bairro.trim() || undefined,
    enderecoCidade: data.dados.cidade.trim() || undefined,
    enderecoUf: data.dados.uf.toUpperCase().trim() || undefined,
    enderecoCep: data.dados.cep.replace(/\D/g, '') || undefined,
    administradoraNome: data.dados.administradoraNome.trim() || undefined,
    optIn: {
      dataAssembleia: data.optIn.dataAssembleia,
      quorumDescricao: data.optIn.quorumDescricao.trim(),
      signatarioNome: data.optIn.signatarioNome.trim(),
      signatarioCpf: data.optIn.signatarioCpf.replace(/\D/g, ''),
      dataTermo: data.optIn.dataTermo,
    },
    sindico: {
      nome: data.sindico.nome.trim(),
      email: data.sindico.email.trim(),
      celularE164: data.sindico.celularE164.trim(),
    },
  }
}

export async function createCondominio(data: WizardData): Promise<CreateCondominioResponse> {
  return apiClient.post<CreateCondominioResponse>('/v1/admin/condominios', buildPayload(data))
}

export async function listCondominios(
  params: ListCondominiosParams = {},
): Promise<PagedResult<CondominioListItem>> {
  const { page = 1, pageSize = 20, status, q } = params
  const qs = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (status !== undefined && status !== 0) qs.set('status', String(status))
  if (q) qs.set('q', q)
  return apiClient.get<PagedResult<CondominioListItem>>(`/v1/admin/condominios?${qs}`)
}

export async function getCondominioDetails(id: string): Promise<CondominioDetails> {
  return apiClient.get<CondominioDetails>(`/v1/admin/condominios/${id}`)
}

export async function activateCondominio(
  id: string,
  note?: string,
): Promise<CondominioDetails> {
  return apiClient.post<CondominioDetails>(`/v1/admin/condominios/${id}:activate`, { note })
}

export async function resendMagicLink(condominioId: string, userId: string): Promise<void> {
  return apiClient.post<void>(
    `/v1/admin/condominios/${condominioId}/sindicos/${userId}:resend-magic-link`,
  )
}

export async function uploadOptInDocument(
  condominioId: string,
  file: File,
  kind: number,
): Promise<OptInDocumentItem> {
  const form = new FormData()
  form.append('file', file)
  form.append('kind', String(kind))
  return apiClient.postFormData<OptInDocumentItem>(
    `/v1/admin/condominios/${condominioId}/opt-in-documents`,
    form,
  )
}

export async function downloadOptInDocument(
  condominioId: string,
  docId: string,
): Promise<DownloadUrlResponse> {
  return apiClient.get<DownloadUrlResponse>(
    `/v1/admin/condominios/${condominioId}/opt-in-documents/${docId}:download`,
  )
}
