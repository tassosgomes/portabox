import { expectTypeOf } from 'vitest'
import { describe, it } from 'vitest'

import type { Bloco, Estrutura } from '../src/types'

describe('exported types', () => {
  it('compile against generated aliases', () => {
    expectTypeOf<Bloco>().toMatchTypeOf<{ id: string; nome: string; ativo: boolean }>()
    expectTypeOf<Estrutura>().toMatchTypeOf<{ condominioId: string; blocos: unknown[] }>()
  })
})
