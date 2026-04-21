import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../src/http', () => ({
  apiFetch: vi.fn(),
}))

import { apiFetch } from '../src/http'
import {
  criarBloco,
  inativarBloco,
  reativarBloco,
  renomearBloco,
} from '../src/modules/blocos'
import { getEstrutura, getEstruturaAdmin } from '../src/modules/estrutura'
import { criarUnidade, inativarUnidade, reativarUnidade } from '../src/modules/unidades'

describe('typed modules', () => {
  beforeEach(() => {
    vi.mocked(apiFetch).mockReset()
  })

  it('getEstrutura calls apiFetch with the expected relative path', async () => {
    vi.mocked(apiFetch).mockResolvedValue({} as never)

    await getEstrutura('cond-1', true)

    expect(apiFetch).toHaveBeenCalledWith('/condominios/cond-1/estrutura?includeInactive=true')
  })

  it('criarBloco posts json body in camelCase', async () => {
    vi.mocked(apiFetch).mockResolvedValue({} as never)

    await criarBloco({ condominioId: 'cond-1', nome: 'Bloco A' })

    expect(apiFetch).toHaveBeenCalledWith('/condominios/cond-1/blocos', {
      method: 'POST',
      body: JSON.stringify({ nome: 'Bloco A' }),
    })
  })

  it('covers remaining bloco endpoints', async () => {
    vi.mocked(apiFetch).mockResolvedValue({} as never)

    await renomearBloco({ condominioId: 'cond-1', blocoId: 'bloco-1', nome: 'Torre Alfa' })
    await inativarBloco({ condominioId: 'cond-1', blocoId: 'bloco-1' })
    await reativarBloco({ condominioId: 'cond-1', blocoId: 'bloco-1' })

    expect(apiFetch).toHaveBeenNthCalledWith(1, '/condominios/cond-1/blocos/bloco-1', {
      method: 'PATCH',
      body: JSON.stringify({ nome: 'Torre Alfa' }),
    })
    expect(apiFetch).toHaveBeenNthCalledWith(2, '/condominios/cond-1/blocos/bloco-1:inativar', {
      method: 'POST',
    })
    expect(apiFetch).toHaveBeenNthCalledWith(3, '/condominios/cond-1/blocos/bloco-1:reativar', {
      method: 'POST',
    })
  })

  it('covers admin estrutura and unidade endpoints', async () => {
    vi.mocked(apiFetch).mockResolvedValue({} as never)

    await getEstruturaAdmin('cond-1', false)
    await criarUnidade({ condominioId: 'cond-1', blocoId: 'bloco-1', andar: 2, numero: '201A' })
    await inativarUnidade({ condominioId: 'cond-1', blocoId: 'bloco-1', unidadeId: 'un-1' })
    await reativarUnidade({ condominioId: 'cond-1', blocoId: 'bloco-1', unidadeId: 'un-1' })

    expect(apiFetch).toHaveBeenNthCalledWith(1, '/admin/condominios/cond-1/estrutura?includeInactive=false')
    expect(apiFetch).toHaveBeenNthCalledWith(2, '/condominios/cond-1/blocos/bloco-1/unidades', {
      method: 'POST',
      body: JSON.stringify({ andar: 2, numero: '201A' }),
    })
    expect(apiFetch).toHaveBeenNthCalledWith(
      3,
      '/condominios/cond-1/blocos/bloco-1/unidades/un-1:inativar',
      { method: 'POST' },
    )
    expect(apiFetch).toHaveBeenNthCalledWith(
      4,
      '/condominios/cond-1/blocos/bloco-1/unidades/un-1:reativar',
      { method: 'POST' },
    )
  })
})
