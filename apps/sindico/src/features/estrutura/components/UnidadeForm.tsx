import { useEffect, useRef, useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Button, Input, Modal } from '@portabox/ui'
import styles from './UnidadeForm.module.css'
import { unidadeSchema, type UnidadeFormValues } from './unidadeFormSchema'

interface UnidadeFormProps {
  open: boolean
  blocoNome: string
  keepOpenOnSuccess?: boolean
  isSubmitting?: boolean
  apiErrorMessage?: string | null
  onClose: () => void
  onSubmit: (values: UnidadeFormValues) => Promise<'keep-open' | 'close' | void> | 'keep-open' | 'close' | void
}

function normalizeValues(values: UnidadeFormValues): UnidadeFormValues {
  return {
    andar: values.andar,
    numero: values.numero.trim().toUpperCase(),
  }
}

export function UnidadeForm({
  open,
  blocoNome,
  keepOpenOnSuccess = false,
  isSubmitting = false,
  apiErrorMessage = null,
  onClose,
  onSubmit,
}: UnidadeFormProps) {
  const andarInputRef = useRef<HTMLInputElement | null>(null)
  const [shouldFocusAndar, setShouldFocusAndar] = useState(false)

  useEffect(() => {
    if (shouldFocusAndar && andarInputRef.current) {
      andarInputRef.current.focus()
      setShouldFocusAndar(false)
    }
  }, [shouldFocusAndar])

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<UnidadeFormValues>({
    resolver: zodResolver(unidadeSchema),
    defaultValues: { andar: 0, numero: '' },
  })

  const numeroField = register('numero')
  const andarFieldWithParser = register('andar', {
    setValueAs: (value) => {
      if (value === '' || value === null || value === undefined) {
        return Number.NaN
      }

      return Number(value)
    },
  })

  useEffect(() => {
    if (open) {
      reset({ andar: 0, numero: '' })
    }
  }, [open, reset])

  async function handleValidSubmit(values: UnidadeFormValues) {
    const normalized = normalizeValues(values)
    setValue('numero', normalized.numero, { shouldValidate: true })
    const result = await onSubmit(normalized)

    if (result === 'keep-open') {
      if (keepOpenOnSuccess) {
        reset({ andar: 0, numero: '' })
        setShouldFocusAndar(true)
      }
      return
    }

    if (keepOpenOnSuccess && result !== 'close') {
      reset({ andar: 0, numero: '' })
      setShouldFocusAndar(true)
      return
    }

    onClose()
  }

  return (
    <Modal open={open} onClose={onClose} title="Adicionar unidade" size="sm">
      <form className={styles.form} onSubmit={handleSubmit(handleValidSubmit)}>
        <p className={styles.batchHint}>
          Cadastro no bloco <strong>{blocoNome}</strong>.
          {keepOpenOnSuccess ? ' O modal permanece aberto para agilizar a próxima unidade.' : ''}
        </p>

        <div className={styles.fields}>
          <Input
            label="Andar"
            type="number"
            error={errors.andar?.message}
            autoFocus
            {...andarFieldWithParser}
            ref={(element) => {
              andarFieldWithParser.ref(element)
              andarInputRef.current = element
            }}
          />

          <Input
            label="Número"
            placeholder="Ex.: 101A"
            error={errors.numero?.message}
            {...numeroField}
            onChange={(event) => {
              numeroField.onChange(event)
              const normalized = event.target.value.toUpperCase()
              if (normalized !== event.target.value) {
                setValue('numero', normalized, { shouldDirty: true, shouldValidate: true })
              }
            }}
          />
        </div>

        {apiErrorMessage ? (
          <p className={styles.apiError} role="alert">
            {apiErrorMessage}
          </p>
        ) : null}

        <div className={styles.actions}>
          <Button type="button" variant="ghost" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {keepOpenOnSuccess ? 'Salvar e continuar' : 'Adicionar unidade'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
