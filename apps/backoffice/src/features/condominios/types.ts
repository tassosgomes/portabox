export interface DadosCondominio {
  nomeFantasia: string
  cnpj: string
  logradouro: string
  numero: string
  complemento: string
  bairro: string
  cidade: string
  uf: string
  cep: string
  administradoraNome: string
}

export interface DadosOptIn {
  dataAssembleia: string
  quorumDescricao: string
  signatarioNome: string
  signatarioCpf: string
  dataTermo: string
}

export interface DadosSindico {
  nome: string
  email: string
  celularE164: string
}

export interface WizardData {
  dados: DadosCondominio
  optIn: DadosOptIn
  sindico: DadosSindico
}

export interface CreateCondominioResponse {
  id: string
}

export interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  extensions?: Record<string, unknown>
}

// --- List & Detail types ---

export type CondominioStatus = 1 | 2 // 1=PreAtivo, 2=Ativo

export interface CondominioListItem {
  id: string
  nomeFantasia: string
  cnpj: string
  status: CondominioStatus
  createdAt: string
  activatedAt: string | null
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface OptInRecord {
  dataAssembleia: string
  quorumDescricao: string
  signatarioNome: string
  signatarioCpf: string
  dataTermo: string
}

export interface OptInDocumentItem {
  id: string
  kind: number // 1=Ata, 2=Termo, 99=Outro
  contentType: string
  sizeBytes: number
  uploadedAt: string
  originalFileName: string | null
}

export interface SindicoInfo {
  id: string
  userId: string
  nomeCompleto: string
  email: string
  celularE164: string
  passwordDefined: boolean
}

export interface AuditEntry {
  id: number
  eventKind: number // 1=Created, 2=Activated, 3=MagicLinkResent, 4=Other
  performedByEmail: string
  occurredAt: string
  note: string | null
}

export interface CondominioDetails {
  id: string
  nomeFantasia: string
  cnpj: string
  status: CondominioStatus
  createdAt: string
  activatedAt: string | null
  enderecoLogradouro: string | null
  enderecoNumero: string | null
  enderecoComplemento: string | null
  enderecoBairro: string | null
  enderecoCidade: string | null
  enderecoUf: string | null
  enderecoCep: string | null
  administradoraNome: string | null
  optIn: OptInRecord | null
  optInDocuments: OptInDocumentItem[]
  sindico: SindicoInfo | null
  auditLog: AuditEntry[]
}

export interface DownloadUrlResponse {
  url: string
  expiresAt: string
}

export interface ListCondominiosParams {
  page?: number
  pageSize?: number
  status?: number // 0=Todos, 1=PreAtivo, 2=Ativo
  q?: string
}
