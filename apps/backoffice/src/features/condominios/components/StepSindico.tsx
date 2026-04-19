import { useState } from 'react'
import { Button, Input } from '@portabox/ui'
import { validateEmail, validateE164 } from '../validation'
import type { DadosSindico } from '../types'
import styles from './Step.module.css'

interface Props {
  initialData: DadosSindico
  onNext: (data: DadosSindico) => void
  onBack: () => void
}

type Errors = Partial<Record<keyof DadosSindico, string>>

export function StepSindico({ initialData, onNext, onBack }: Props) {
  const [form, setForm] = useState<DadosSindico>(initialData)
  const [errors, setErrors] = useState<Errors>({})

  function set(field: keyof DadosSindico, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }))
    if (errors[field]) setErrors((prev) => ({ ...prev, [field]: undefined }))
  }

  function validate(): Errors {
    const errs: Errors = {}
    if (!form.nome.trim()) errs.nome = 'Nome é obrigatório'
    if (!validateEmail(form.email)) errs.email = 'E-mail inválido'
    if (!validateE164(form.celularE164)) errs.celularE164 = 'Celular deve estar no formato E.164 (ex.: +5511999999999)'
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

  return (
    <div className={styles.step}>
      <p className={styles.hint}>
        O síndico receberá um link por e-mail para definir sua senha de acesso.
      </p>
      <Input
        label="Nome completo *"
        value={form.nome}
        onChange={(e) => set('nome', e.target.value)}
        error={errors.nome}
        placeholder="Ex.: Maria Silva"
      />
      <Input
        label="E-mail *"
        type="email"
        value={form.email}
        onChange={(e) => set('email', e.target.value)}
        error={errors.email}
        placeholder="sindico@exemplo.com.br"
      />
      <Input
        label="Celular *"
        value={form.celularE164}
        onChange={(e) => set('celularE164', e.target.value)}
        error={errors.celularE164}
        placeholder="+5511999999999"
        hint="Formato E.164 com código do país"
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
