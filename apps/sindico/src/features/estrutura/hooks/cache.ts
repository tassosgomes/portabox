import type { Bloco, Estrutura, Unidade } from '@portabox/api-client'

type EstruturaBloco = Estrutura['blocos'][number]
type EstruturaAndar = EstruturaBloco['andares'][number]
type EstruturaUnidade = EstruturaAndar['unidades'][number]

function compareByName(left: EstruturaBloco, right: EstruturaBloco) {
  return left.nome.localeCompare(right.nome, 'pt-BR', { sensitivity: 'base' })
}

function compareByAndar(left: EstruturaAndar, right: EstruturaAndar) {
  return left.andar - right.andar
}

function parseNumero(numero: string) {
  const match = /^(\d+)([A-Z]?)$/i.exec(numero.trim())

  return {
    parteNumerica: match ? Number(match[1]) : Number.NaN,
    sufixo: match?.[2]?.toUpperCase() ?? '',
  }
}

function compareUnidade(left: EstruturaUnidade, right: EstruturaUnidade) {
  const leftNumero = parseNumero(left.numero)
  const rightNumero = parseNumero(right.numero)

  if (Number.isNaN(leftNumero.parteNumerica) || Number.isNaN(rightNumero.parteNumerica)) {
    return left.numero.localeCompare(right.numero, 'pt-BR', { sensitivity: 'base' })
  }

  if (leftNumero.parteNumerica !== rightNumero.parteNumerica) {
    return leftNumero.parteNumerica - rightNumero.parteNumerica
  }

  return leftNumero.sufixo.localeCompare(rightNumero.sufixo, 'pt-BR', { sensitivity: 'base' })
}

function toEstruturaBloco(
  bloco: Bloco,
  andares: EstruturaBloco['andares'] = [],
): EstruturaBloco {
  return {
    id: bloco.id,
    nome: bloco.nome,
    ativo: bloco.ativo,
    andares,
  }
}

function toEstruturaUnidade(unidade: Unidade): EstruturaUnidade {
  return {
    id: unidade.id,
    numero: unidade.numero,
    ativo: unidade.ativo,
  }
}

function upsertAndares(
  andares: EstruturaAndar[],
  unidade: Unidade,
  previousId?: string,
) {
  const nextLeaf = toEstruturaUnidade(unidade)
  const targetAndar = unidade.andar
  const nextAndares = andares
    .map((andar) => {
      const unidadeAtual = andar.unidades.find((item) => item.id === previousId || item.id === unidade.id)

      if (andar.andar !== targetAndar && !unidadeAtual) {
        return andar
      }

      const unidades = andar.unidades
        .filter((item) => item.id !== previousId && item.id !== unidade.id)
        .concat(andar.andar === targetAndar ? nextLeaf : [])
        .sort(compareUnidade)

      return {
        ...andar,
        unidades,
      }
    })
    .filter((andar) => andar.unidades.length > 0)

  if (!nextAndares.some((andar) => andar.andar === targetAndar)) {
    nextAndares.push({
      andar: targetAndar,
      unidades: [nextLeaf],
    })
  }

  return nextAndares.sort(compareByAndar)
}

export function insertBlocoIntoEstrutura(estrutura: Estrutura | undefined, bloco: Bloco) {
  if (!estrutura) {
    return estrutura
  }

  const blocos = [...estrutura.blocos, toEstruturaBloco(bloco)].sort(compareByName)

  return {
    ...estrutura,
    blocos,
  }
}

export function insertUnidadeIntoEstrutura(
  estrutura: Estrutura | undefined,
  blocoId: string,
  unidade: Unidade,
) {
  if (!estrutura) {
    return estrutura
  }

  return {
    ...estrutura,
    blocos: estrutura.blocos.map((bloco) => {
      if (bloco.id !== blocoId) {
        return bloco
      }

      return {
        ...bloco,
        andares: upsertAndares(bloco.andares, unidade),
      }
    }),
  }
}

export function upsertBlocoInEstrutura(
  estrutura: Estrutura | undefined,
  bloco: Bloco,
  previousId?: string,
) {
  if (!estrutura) {
    return estrutura
  }

  const existing = estrutura.blocos.find((item) => item.id === previousId || item.id === bloco.id)
  const nextBloco = toEstruturaBloco(bloco, existing?.andares ?? [])
  const blocos = estrutura.blocos
    .filter((item) => item.id !== previousId && item.id !== bloco.id)
    .concat(nextBloco)
    .sort(compareByName)

  return {
    ...estrutura,
    blocos,
  }
}

export function upsertUnidadeInEstrutura(
  estrutura: Estrutura | undefined,
  blocoId: string,
  unidade: Unidade,
  previousId?: string,
) {
  if (!estrutura) {
    return estrutura
  }

  return {
    ...estrutura,
    blocos: estrutura.blocos.map((bloco) => {
      if (bloco.id !== blocoId) {
        return bloco
      }

      return {
        ...bloco,
        andares: upsertAndares(bloco.andares, unidade, previousId),
      }
    }),
  }
}
