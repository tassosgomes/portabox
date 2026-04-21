import { expectTypeOf } from 'vitest'
import { describe, expect, it } from 'vitest'

import { queryKeys } from '../src/queryKeys'

describe('queryKeys', () => {
  it('returns literal tuple for estrutura', () => {
    const key = queryKeys.estrutura('cond-1')

    expect(key).toEqual(['estrutura', 'cond-1'])
    expectTypeOf(key).toEqualTypeOf<readonly ['estrutura', string]>()
  })
})
