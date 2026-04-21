import { createElement } from 'react'
import type { BlocoNode, Estrutura, UnidadeLeaf } from '@portabox/api-client'
import { Badge, type TreeItem } from '@portabox/ui'
import { BlocoActionsMenu } from '../components/BlocoActionsMenu'
import { UnidadeActionsMenu } from '../components/UnidadeActionsMenu'

interface TreeItemOptions {
  onRenameBloco?: (bloco: BlocoNode) => void
  onInativarBloco?: (bloco: BlocoNode) => void
  onReativarBloco?: (bloco: BlocoNode) => void
  onSelectBloco?: (bloco: BlocoNode) => void
  onInativarUnidade?: (bloco: BlocoNode, unidade: UnidadeLeaf) => void
  onReativarUnidade?: (bloco: BlocoNode, unidade: UnidadeLeaf) => void
  selectedBlocoId?: string | null
}

function formatActiveUnitsLabel(total: number) {
  return total === 1 ? '1 unidade ativa' : `${total} unidades ativas`
}

function countActiveUnits(andares: Estrutura['blocos'][number]['andares']) {
  return andares.reduce(
    (total, andar) => total + andar.unidades.filter((unidade) => unidade.ativo).length,
    0,
  )
}

export function toTreeItems(estrutura: Estrutura, options: TreeItemOptions = {}): TreeItem[] {
  return [
    {
      id: `condominio-${estrutura.condominioId}`,
      label: estrutura.nomeFantasia,
      children: estrutura.blocos.map((bloco) => ({
        id: `bloco-${bloco.id}`,
        label: `${bloco.nome} · ${formatActiveUnitsLabel(countActiveUnits(bloco.andares))}`,
        state: bloco.ativo ? 'default' : 'inactive',
        badge: options.selectedBlocoId === bloco.id
          ? createElement(Badge, { status: 'info' }, 'Selecionado')
          : undefined,
        actions: createElement(BlocoActionsMenu, {
          blocoNome: bloco.nome,
          ativo: bloco.ativo,
          onRename: () => options.onRenameBloco?.(bloco),
          onInativar: () => options.onInativarBloco?.(bloco),
          onReativar: () => options.onReativarBloco?.(bloco),
        }),
        onClick: () => options.onSelectBloco?.(bloco),
        children: bloco.andares.map((andar) => ({
          id: `bloco-${bloco.id}-andar-${andar.andar}`,
          label: `Andar ${andar.andar}`,
          badge: `${andar.unidades.length} ${andar.unidades.length === 1 ? 'unidade' : 'unidades'}`,
          children: andar.unidades.map((unidade) => ({
            id: `unidade-${unidade.id}`,
            label: `Unidade ${unidade.numero}`,
            badge: createElement(Badge, { status: unidade.ativo ? 'ativo' : 'inativo' }),
            state: unidade.ativo ? 'default' : 'inactive',
            actions: createElement(UnidadeActionsMenu, {
              unidadeNumero: unidade.numero,
              ativo: unidade.ativo,
              onInativar: () => options.onInativarUnidade?.(bloco, unidade),
              onReativar: () => options.onReativarUnidade?.(bloco, unidade),
            }),
          })),
        })),
      })),
    },
  ]
}
