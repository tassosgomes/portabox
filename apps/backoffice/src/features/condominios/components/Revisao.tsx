import { Button } from '@portabox/ui'
import { formatCnpj, formatCpf } from '../validation'
import type { DadosCondominio, DadosOptIn, DadosSindico } from '../types'
import styles from './Revisao.module.css'

interface Props {
  dados: DadosCondominio
  optIn: DadosOptIn
  sindico: DadosSindico
  submitting: boolean
  error: string | null
  onSubmit: () => void
  onBack: () => void
}

function fmtDate(d: string): string {
  if (!d) return '—'
  const [y, m, day] = d.split('-').map(Number)
  return new Date(y, m - 1, day).toLocaleDateString('pt-BR')
}

function Row({ label, value }: { label: string; value: string }) {
  if (!value) return null
  return (
    <>
      <dt className={styles.dt}>{label}</dt>
      <dd className={styles.dd}>{value}</dd>
    </>
  )
}

export function Revisao({ dados, optIn, sindico, submitting, error, onSubmit, onBack }: Props) {
  const addressParts = [
    dados.logradouro,
    dados.numero,
    dados.complemento,
  ]
    .filter(Boolean)
    .join(', ')

  return (
    <div className={styles.revisao}>
      <section className={styles.section}>
        <h3 className={styles.sectionTitle}>Dados do condomínio</h3>
        <dl className={styles.dl}>
          <Row label="Nome fantasia" value={dados.nomeFantasia} />
          <Row label="CNPJ" value={formatCnpj(dados.cnpj)} />
          <Row label="Endereço" value={addressParts} />
          <Row label="Bairro" value={dados.bairro} />
          <Row
            label="Cidade / UF"
            value={[dados.cidade, dados.uf].filter(Boolean).join(' / ')}
          />
          <Row label="CEP" value={dados.cep} />
          <Row label="Administradora" value={dados.administradoraNome} />
        </dl>
      </section>

      <section className={styles.section}>
        <h3 className={styles.sectionTitle}>Consentimento LGPD</h3>
        <dl className={styles.dl}>
          <Row label="Data da assembleia" value={fmtDate(optIn.dataAssembleia)} />
          <Row label="Quórum" value={optIn.quorumDescricao} />
          <Row label="Signatário" value={optIn.signatarioNome} />
          <Row label="CPF do signatário" value={formatCpf(optIn.signatarioCpf)} />
          <Row label="Data do termo" value={fmtDate(optIn.dataTermo)} />
        </dl>
      </section>

      <section className={styles.section}>
        <h3 className={styles.sectionTitle}>Síndico responsável</h3>
        <dl className={styles.dl}>
          <Row label="Nome" value={sindico.nome} />
          <Row label="E-mail" value={sindico.email} />
          <Row label="Celular" value={sindico.celularE164} />
        </dl>
      </section>

      {error && (
        <div className={styles.errorBanner} role="alert">
          {error}
        </div>
      )}

      <div className={styles.actions}>
        <Button variant="ghost" onClick={onBack} disabled={submitting}>
          Voltar
        </Button>
        <Button variant="primary" onClick={onSubmit} loading={submitting}>
          Criar condomínio
        </Button>
      </div>
    </div>
  )
}
