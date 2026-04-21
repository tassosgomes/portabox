import { useEffect } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Button, Input, Modal } from '@portabox/ui'
import styles from './BlocoForm.module.css'
import { blocoSchema, type BlocoFormValues } from './blocoFormSchema'

interface BlocoFormProps {
  open: boolean
  mode: 'create' | 'rename'
  initialNome?: string
  isSubmitting?: boolean
  apiErrorMessage?: string | null
  onClose: () => void
  onSubmit: (values: BlocoFormValues) => Promise<void> | void
}

export function BlocoForm({
  open,
  mode,
  initialNome = '',
  isSubmitting = false,
  apiErrorMessage = null,
  onClose,
  onSubmit,
}: BlocoFormProps) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<BlocoFormValues>({
    resolver: zodResolver(blocoSchema),
    defaultValues: { nome: initialNome },
  })

  useEffect(() => {
    if (open) {
      reset({ nome: initialNome })
    }
  }, [initialNome, open, reset])

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={mode === 'create' ? 'Novo bloco' : 'Renomear bloco'}
      size="sm"
    >
      <form className={styles.form} onSubmit={handleSubmit(async (values) => onSubmit(values))}>
        <Input
          label="Nome do bloco"
          placeholder={mode === 'create' ? 'Ex.: Torre Alfa' : 'Ex.: Bloco A'}
          error={errors.nome?.message}
          autoFocus
          {...register('nome')}
        />

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
            {mode === 'create' ? 'Criar bloco' : 'Salvar nome'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
