import { createElement } from 'react'
import type { Estrutura } from '@portabox/api-client'
import { Badge, type TreeItem } from '@portabox/ui'

function formatActiveUnitsLabel(total: number) {
  return total === 1 ? '1 unidade ativa' : `${total} unidades ativas`
}

function countActiveUnits(andares: Estrutura['blocos'][number]['andares']) {
  return andares.reduce(
    (total, andar) => total + andar.unidades.filter((unidade) => unidade.ativo).length,
    0,
  )
}

// Intentionally kept local to backoffice to avoid coupling the operator app to sindico action menus.
export function toReadOnlyTreeItems(estrutura: Estrutura): TreeItem[] {
  return [
    {
      id: `condominio-${estrutura.condominioId}`,
      label: estrutura.nomeFantasia,
      children: estrutura.blocos.map((bloco) => ({
        id: `bloco-${bloco.id}`,
        label: `${bloco.nome} · ${formatActiveUnitsLabel(countActiveUnits(bloco.andares))}`,
        state: bloco.ativo ? 'default' : 'inactive',
        badge: createElement(Badge, { status: bloco.ativo ? 'ativo' : 'inativo' }),
        children: bloco.andares.map((andar) => ({
          id: `bloco-${bloco.id}-andar-${andar.andar}`,
          label: `Andar ${andar.andar}`,
          badge: `${andar.unidades.length} ${andar.unidades.length === 1 ? 'unidade' : 'unidades'}`,
          children: andar.unidades.map((unidade) => ({
            id: `unidade-${unidade.id}`,
            label: `Unidade ${unidade.numero}`,
            badge: createElement(Badge, { status: unidade.ativo ? 'ativo' : 'inativo' }),
            state: unidade.ativo ? 'default' : 'inactive',
          })),
        })),
      })),
    },
  ]
}
