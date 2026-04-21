import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { StepIndicator } from '@portabox/ui'
import { StepDadosCondominio } from '../components/StepDadosCondominio'
import { StepOptIn } from '../components/StepOptIn'
import { StepSindico } from '../components/StepSindico'
import { Revisao } from '../components/Revisao'
import { createCondominio, ApiHttpError } from '../api'
import type { WizardData, DadosCondominio, DadosOptIn, DadosSindico, ProblemDetails } from '../types'
import styles from './NovoCondominioPage.module.css'

const WIZARD_STEPS = [
  { label: 'Dados do condomínio' },
  { label: 'Consentimento LGPD' },
  { label: 'Síndico responsável' },
]

const defaultDados: DadosCondominio = {
  nomeFantasia: '',
  cnpj: '',
  logradouro: '',
  numero: '',
  complemento: '',
  bairro: '',
  cidade: '',
  uf: '',
  cep: '',
  administradoraNome: '',
}

const defaultOptIn: DadosOptIn = {
  dataAssembleia: '',
  quorumDescricao: '',
  signatarioNome: '',
  signatarioCpf: '',
  dataTermo: '',
}

const defaultSindico: DadosSindico = {
  nome: '',
  email: '',
  celularE164: '',
}

type WizardStep = 1 | 2 | 3 | 4

const SUCCESS_MESSAGE =
  'Condomínio criado em estado pré-ativo. Enviamos o link de definição de senha para o síndico.'

export function NovoCondominioPage() {
  const navigate = useNavigate()
  const [step, setStep] = useState<WizardStep>(1)
  const [dados, setDados] = useState<DadosCondominio>(defaultDados)
  const [optIn, setOptIn] = useState<DadosOptIn>(defaultOptIn)
  const [sindico, setSindico] = useState<DadosSindico>(defaultSindico)
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  async function handleSubmit() {
    const data: WizardData = { dados, optIn, sindico }
    setSubmitting(true)
    setSubmitError(null)
    try {
      const result = await createCondominio(data)
      navigate(`/condominios/${result.condominioId}`, {
        state: { successMessage: SUCCESS_MESSAGE },
      })
    } catch (err) {
      if (err instanceof ApiHttpError) {
        if (err.status === 409) {
          const body = err.body as ProblemDetails | null
          const ext = body?.extensions
          const nome = ext?.nomeExistente as string | undefined
          const criadoEm = ext?.criadoEm as string | undefined
          const dateFormatted = criadoEm
            ? new Date(criadoEm).toLocaleDateString('pt-BR')
            : ''
          setSubmitError(
            nome
              ? `Este CNPJ já está cadastrado como "${nome}"${dateFormatted ? `, criado em ${dateFormatted}` : ''}.`
              : 'Este CNPJ já está cadastrado.',
          )
        } else {
          setSubmitError('Erro ao criar condomínio. Tente novamente.')
        }
      } else {
        setSubmitError('Erro inesperado. Tente novamente.')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Novo condomínio</h1>
      <StepIndicator
        steps={WIZARD_STEPS}
        currentStep={step}
        className={styles.stepIndicator}
      />
      <div className={styles.card}>
        {step === 1 && (
          <StepDadosCondominio
            initialData={dados}
            onNext={(data) => {
              setDados(data)
              setStep(2)
            }}
          />
        )}
        {step === 2 && (
          <StepOptIn
            initialData={optIn}
            onNext={(data) => {
              setOptIn(data)
              setStep(3)
            }}
            onBack={() => setStep(1)}
          />
        )}
        {step === 3 && (
          <StepSindico
            initialData={sindico}
            onNext={(data) => {
              setSindico(data)
              setStep(4)
            }}
            onBack={() => setStep(2)}
          />
        )}
        {step === 4 && (
          <Revisao
            dados={dados}
            optIn={optIn}
            sindico={sindico}
            submitting={submitting}
            error={submitError}
            onSubmit={handleSubmit}
            onBack={() => setStep(3)}
          />
        )}
      </div>
    </div>
  )
}
