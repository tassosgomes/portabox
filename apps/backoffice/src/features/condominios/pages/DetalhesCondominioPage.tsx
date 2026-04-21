import { useEffect, useState, useCallback } from 'react'
import { Link, useParams, useLocation } from 'react-router-dom'
import { Card } from '@portabox/ui'
import { getCondominioDetails, downloadOptInDocument, ApiHttpError } from '../api'
import { StatusBadge } from '../components/StatusBadge'
import { AuditLogList } from '../components/AuditLogList'
import { ActivateTenantAction } from '../components/ActivateTenantAction'
import { ResendMagicLinkAction } from '../components/ResendMagicLinkAction'
import { UploadOptInDocument } from '../components/UploadOptInDocument'
import type { CondominioDetails, OptInDocumentItem } from '../types'
import styles from './DetalhesCondominioPage.module.css'

const DOC_KIND_LABELS: Record<number, string> = {
  1: 'Ata de assembleia',
  2: 'Termo de opt-in',
  99: 'Outro documento',
}

function formatCnpjMasked(cnpj: string): string {
  const d = cnpj.replace(/\D/g, '')
  if (d.length === 14) return d.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5')
  return cnpj
}

function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  // Date-only strings (YYYY-MM-DD) would be parsed as UTC midnight by new Date(),
  // shifting the display by one day in negative-offset timezones.
  if (/^\d{4}-\d{2}-\d{2}$/.test(iso)) {
    const [y, m, d] = iso.split('-').map(Number)
    return new Date(y, m - 1, d).toLocaleDateString('pt-BR')
  }
  return new Date(iso).toLocaleDateString('pt-BR')
}

function formatFileSize(bytes: number): string {
  if (bytes >= 1_000_000) return `${(bytes / 1_000_000).toFixed(1)} MB`
  if (bytes >= 1_000) return `${Math.round(bytes / 1_000)} KB`
  return `${bytes} B`
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <Card className={styles.section}>
      <h3 className={styles.sectionTitle}>{title}</h3>
      {children}
    </Card>
  )
}

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className={styles.field}>
      <dt className={styles.fieldLabel}>{label}</dt>
      <dd className={styles.fieldValue}>{value ?? '—'}</dd>
    </div>
  )
}

export function DetalhesCondominioPage() {
  const { id } = useParams<{ id: string }>()
  const location = useLocation()
  const [details, setDetails] = useState<CondominioDetails | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(
    (location.state as { successMessage?: string } | null)?.successMessage ?? null,
  )

  const load = useCallback(() => {
    if (!id) return
    getCondominioDetails(id)
      .then((data) => {
        setDetails(data)
        setError(null)
        setLoading(false)
      })
      .catch((err) => {
        setError(
          err instanceof ApiHttpError && err.status === 404
            ? 'Condomínio não encontrado.'
            : 'Erro ao carregar dados. Tente novamente.',
        )
        setLoading(false)
      })
  }, [id])

  useEffect(() => { load() }, [load])

  async function handleDownload(doc: OptInDocumentItem) {
    if (!id) return
    try {
      const { url } = await downloadOptInDocument(id, doc.id)
      window.open(url, '_blank', 'noopener,noreferrer')
    } catch {
      // silent — download failure shows nothing critical
    }
  }

  if (loading) {
    return (
      <div className={styles.page}>
        <p role="status" className={styles.stateMsg}>Carregando...</p>
      </div>
    )
  }

  if (error || !details) {
    return (
      <div className={styles.page}>
        <Link to="/condominios" className={styles.back}>&larr; Voltar</Link>
        <p role="alert" className={styles.errorMsg}>{error ?? 'Dados indisponíveis.'}</p>
      </div>
    )
  }

  const canActivate = details.status === 1
  const sindico = details.sindico
  const canResend = sindico != null && !details.sindicoSenhaDefinida

  return (
    <div className={styles.page}>
      <Link to="/condominios" className={styles.back}>&larr; Condomínios</Link>

      {successMsg && (
        <p role="status" className={styles.successToast}>
          {successMsg}
        </p>
      )}

      {/* Header */}
      <div className={styles.pageHeader}>
        <div>
          <h2 className={styles.name}>{details.nomeFantasia}</h2>
          <p className={styles.cnpj}>{formatCnpjMasked(details.cnpjMasked)}</p>
        </div>
        <div className={styles.headerMeta}>
          <Link to={`/tenants/${details.id}/estrutura`} className={styles.structureLink}>
            Ver estrutura
          </Link>
          <StatusBadge status={details.status} />
          <span className={styles.metaItem}>Criado em {formatDate(details.createdAt)}</span>
          {details.activatedAt && (
            <span className={styles.metaItem}>Ativado em {formatDate(details.activatedAt)}</span>
          )}
        </div>
      </div>

      {/* Actions */}
      {(canActivate || canResend) && (
        <div className={styles.actions}>
          {canActivate && (
            <ActivateTenantAction
              condominioId={details.id}
              onActivated={() => {
                setSuccessMsg('Operação ativada')
                load()
              }}
            />
          )}
          {canResend && sindico && (
            <ResendMagicLinkAction
              condominioId={details.id}
              sindicoUserId={sindico.userId}
            />
          )}
        </div>
      )}

      {/* Dados do condomínio */}
      <Section title="Dados do condomínio">
        <dl className={styles.fieldGrid}>
          <Field label="Logradouro" value={details.enderecoLogradouro} />
          <Field label="Número" value={details.enderecoNumero} />
          <Field label="Complemento" value={details.enderecoComplemento} />
          <Field label="Bairro" value={details.enderecoBairro} />
          <Field label="Cidade" value={details.enderecoCidade} />
          <Field label="UF" value={details.enderecoUf} />
          <Field label="CEP" value={details.enderecoCep} />
          <Field label="Administradora" value={details.administradoraNome} />
        </dl>
      </Section>

      {/* Consentimento LGPD */}
      {details.optIn && (
        <Section title="Consentimento LGPD">
          <dl className={styles.fieldGrid}>
            <Field label="Data da assembleia" value={formatDate(details.optIn.dataAssembleia)} />
            <Field label="Quórum" value={details.optIn.quorumDescricao} />
            <Field label="Signatário" value={details.optIn.signatarioNome} />
            <Field label="CPF do signatário" value={<span className={styles.mono}>{details.optIn.signatarioCpfMasked}</span>} />
            <Field label="Data do termo" value={formatDate(details.optIn.dataTermo)} />
          </dl>

          <h4 className={styles.subTitle}>Documentos</h4>
          {details.documentos.length === 0 ? (
            <p className={styles.subtle}>Nenhum documento anexado.</p>
          ) : (
            <ul className={styles.docList}>
              {details.documentos.map((doc) => (
                <li key={doc.id} className={styles.docItem}>
                  <span className={styles.docName}>
                    {DOC_KIND_LABELS[doc.kind] ?? 'Documento'}
                    {doc.originalFileName ? ` — ${doc.originalFileName}` : ''}
                  </span>
                  <span className={styles.docSize}>{formatFileSize(doc.sizeBytes)}</span>
                  <button
                    type="button"
                    className={styles.downloadBtn}
                    onClick={() => handleDownload(doc)}
                  >
                    Download
                  </button>
                </li>
              ))}
            </ul>
          )}

          <div className={styles.uploadSection}>
            <h4 className={styles.subTitle}>Adicionar documento</h4>
            <UploadOptInDocument condominioId={details.id} onUploaded={load} />
          </div>
        </Section>
      )}

      {/* Síndico */}
      {sindico && (
        <Section title="Síndico">
          <dl className={styles.fieldGrid}>
            <Field label="Nome" value={sindico.nomeCompleto} />
            <Field label="E-mail" value={sindico.email} />
            <Field label="Celular" value={<span className={styles.mono}>{sindico.celularMasked}</span>} />
            <Field label="Senha definida" value={details.sindicoSenhaDefinida ? 'Sim' : 'Não'} />
          </dl>
        </Section>
      )}

      {/* Histórico de auditoria */}
      <Section title="Histórico de auditoria">
        <AuditLogList entries={details.auditLog} />
      </Section>
    </div>
  )
}
