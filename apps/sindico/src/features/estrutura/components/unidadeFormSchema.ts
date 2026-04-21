import { z } from 'zod'

export const unidadeSchema = z.object({
  andar: z
    .number()
    .finite('Informe um andar valido.')
    .int('Informe um andar inteiro.')
    .min(0, 'Use andar 0 ou maior.'),
  numero: z
    .string()
    .trim()
    .min(1, 'Informe o numero da unidade.')
    .regex(/^[0-9]{1,4}[A-Za-z]?$/i, 'Use ate 4 digitos e, no maximo, um sufixo. Ex.: 101A.'),
})

export type UnidadeFormValues = z.infer<typeof unidadeSchema>
