import { useState } from 'react'
import { Button, Input } from '@portabox/ui'
import { formatCnpj, validateCnpj, formatCep } from '../validation'
import type { DadosCondominio } from '../types'
import styles from './Step.module.css'

interface Props {
  initialData: DadosCondominio
  onNext: (data: DadosCondominio) => void
}

type Errors = Partial<Record<keyof DadosCondominio, string>>

export function StepDadosCondominio({ initialData, onNext }: Props) {
  const [form, setForm] = useState<DadosCondominio>(initialData)
  const [errors, setErrors] = useState<Errors>({})

  function set(field: keyof DadosCondominio, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }))
    if (errors[field]) setErrors((prev) => ({ ...prev, [field]: undefined }))
  }

  function validate(): Errors {
    const errs: Errors = {}
    if (!form.nomeFantasia.trim()) errs.nomeFantasia = 'Nome fantasia é obrigatório'
    if (!validateCnpj(form.cnpj)) errs.cnpj = 'CNPJ inválido'
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
      <Input
        label="Nome fantasia *"
        value={form.nomeFantasia}
        onChange={(e) => set('nomeFantasia', e.target.value)}
        error={errors.nomeFantasia}
        placeholder="Ex.: Residencial Parque das Flores"
      />
      <Input
        label="CNPJ *"
        value={formatCnpj(form.cnpj)}
        onChange={(e) => set('cnpj', e.target.value)}
        error={errors.cnpj}
        placeholder="00.000.000/0000-00"
        inputMode="numeric"
        maxLength={18}
      />
      <Input
        label="Logradouro"
        value={form.logradouro}
        onChange={(e) => set('logradouro', e.target.value)}
        placeholder="Ex.: Rua das Acácias"
      />
      <div className={styles.row}>
        <Input
          label="Número"
          value={form.numero}
          onChange={(e) => set('numero', e.target.value)}
          placeholder="123"
        />
        <Input
          label="Complemento"
          value={form.complemento}
          onChange={(e) => set('complemento', e.target.value)}
          placeholder="Apto, Bloco..."
        />
      </div>
      <Input
        label="Bairro"
        value={form.bairro}
        onChange={(e) => set('bairro', e.target.value)}
        placeholder="Ex.: Centro"
      />
      <div className={styles.row}>
        <div className={styles.grow}>
          <Input
            label="Cidade"
            value={form.cidade}
            onChange={(e) => set('cidade', e.target.value)}
            placeholder="São Paulo"
          />
        </div>
        <div className={styles.uf}>
          <Input
            label="UF"
            value={form.uf.toUpperCase()}
            onChange={(e) => set('uf', e.target.value.toUpperCase().slice(0, 2))}
            placeholder="SP"
            maxLength={2}
          />
        </div>
        <Input
          label="CEP"
          value={formatCep(form.cep)}
          onChange={(e) => set('cep', e.target.value)}
          placeholder="00000-000"
          inputMode="numeric"
          maxLength={9}
        />
      </div>
      <Input
        label="Administradora"
        value={form.administradoraNome}
        onChange={(e) => set('administradoraNome', e.target.value)}
        hint="Opcional — nome da empresa administradora"
      />
      <div className={styles.actions}>
        <Button variant="primary" onClick={handleNext}>
          Avançar
        </Button>
      </div>
    </div>
  )
}
