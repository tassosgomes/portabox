import { useState } from 'react'
import { Button, Input } from '@portabox/ui'
import { formatCpf, validateCpf, isDateNotFuture } from '../validation'
import type { DadosOptIn } from '../types'
import styles from './Step.module.css'

interface Props {
  initialData: DadosOptIn
  onNext: (data: DadosOptIn) => void
  onBack: () => void
}

type Errors = Partial<Record<keyof DadosOptIn, string>>

export function StepOptIn({ initialData, onNext, onBack }: Props) {
  const [form, setForm] = useState<DadosOptIn>(initialData)
  const [errors, setErrors] = useState<Errors>({})

  function set(field: keyof DadosOptIn, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }))
    if (errors[field]) setErrors((prev) => ({ ...prev, [field]: undefined }))
  }

  function validate(): Errors {
    const errs: Errors = {}
    if (!form.dataAssembleia) {
      errs.dataAssembleia = 'Data da assembleia é obrigatória'
    } else if (!isDateNotFuture(form.dataAssembleia)) {
      errs.dataAssembleia = 'Data não pode ser futura'
    }
    if (!form.quorumDescricao.trim()) errs.quorumDescricao = 'Quórum é obrigatório'
    if (!form.signatarioNome.trim()) errs.signatarioNome = 'Nome do signatário é obrigatório'
    if (!validateCpf(form.signatarioCpf)) errs.signatarioCpf = 'CPF inválido'
    if (!form.dataTermo) {
      errs.dataTermo = 'Data do termo é obrigatória'
    } else if (!isDateNotFuture(form.dataTermo)) {
      errs.dataTermo = 'Data não pode ser futura'
    }
    return errs
  }

  function handleNext() {
    const errs = validate()
    if (Object.keys(errs).length > 0) {
      setErrors(errs)
      return
    }
    onNext(form)
  }

  const today = new Date().toISOString().slice(0, 10)

  return (
    <div className={styles.step}>
      <p className={styles.hint}>
        Registre os dados do consentimento coletivo obtido em assembleia de condôminos.
      </p>
      <Input
        label="Data da assembleia *"
        type="date"
        value={form.dataAssembleia}
        onChange={(e) => set('dataAssembleia', e.target.value)}
        error={errors.dataAssembleia}
        max={today}
      />
      <Input
        label="Descrição do quórum *"
        value={form.quorumDescricao}
        onChange={(e) => set('quorumDescricao', e.target.value)}
        error={errors.quorumDescricao}
        placeholder="Ex.: Aprovado por 2/3 dos condôminos presentes"
      />
      <Input
        label="Nome do signatário *"
        value={form.signatarioNome}
        onChange={(e) => set('signatarioNome', e.target.value)}
        error={errors.signatarioNome}
        placeholder="Nome completo de quem assinou o termo"
      />
      <Input
        label="CPF do signatário *"
        value={formatCpf(form.signatarioCpf)}
        onChange={(e) => set('signatarioCpf', e.target.value)}
        error={errors.signatarioCpf}
        placeholder="000.000.000-00"
        inputMode="numeric"
        maxLength={14}
      />
      <Input
        label="Data do termo *"
        type="date"
        value={form.dataTermo}
        onChange={(e) => set('dataTermo', e.target.value)}
        error={errors.dataTermo}
        max={today}
      />
      <div className={styles.actions}>
        <Button variant="ghost" onClick={onBack}>
          Voltar
        </Button>
        <Button variant="primary" onClick={handleNext}>
          Avançar
        </Button>
      </div>
    </div>
  )
}
