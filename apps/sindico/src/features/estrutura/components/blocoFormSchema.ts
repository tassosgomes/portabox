import { z } from 'zod'

export const blocoSchema = z.object({
  nome: z
    .string()
    .trim()
    .min(1, 'Informe o nome do bloco.')
    .max(50, 'Use no máximo 50 caracteres.'),
})

export type BlocoFormValues = z.infer<typeof blocoSchema>
