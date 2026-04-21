import { describe, expect, it } from 'vitest'

import { ApiError } from '../src/errors'

describe('ApiError', () => {
  it('serializes field errors from validation problem details', () => {
    const error = new ApiError({
      type: 'https://portabox.app/problems/validation-error',
      title: 'Falha de validação',
      status: 400,
      detail: 'Um ou mais campos estão inválidos',
      errors: {
        nome: ['O nome é obrigatório'],
      },
    })

    expect(error.fieldErrors).toEqual({ nome: ['O nome é obrigatório'] })
    expect(error.message).toBe('Um ou mais campos estão inválidos')
  })
})
