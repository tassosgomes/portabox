import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Button } from '../button/Button'
import { Tree } from './Tree'
import type { TreeItem } from './types'

const onUnitClick = vi.fn()

const ITEMS: TreeItem[] = [
  {
    id: 'bloco-a',
    label: 'Bloco A',
    badge: <span>2</span>,
    children: [
      { id: 'apto-101', label: 'Apartamento 101', onClick: onUnitClick },
      { id: 'apto-102', label: 'Apartamento 102', state: 'inactive' },
    ],
  },
  { id: 'bloco-b', label: 'Bloco B' },
]

describe('Tree', () => {
  it('renders items and respects visible order', () => {
    render(<Tree items={ITEMS} defaultExpandedIds={['bloco-a']} />)

    expect(screen.getAllByRole('treeitem').map((item) => item.textContent)).toEqual([
      'Bloco A2',
      'Apartamento 101',
      'Apartamento 102',
      'Bloco B',
    ])
  })

  it('toggles expand and collapse in uncontrolled mode when a parent node is clicked', async () => {
    const user = userEvent.setup()
    render(<Tree items={ITEMS} />)

    expect(screen.queryByText('Apartamento 101')).not.toBeInTheDocument()

    await user.click(screen.getByRole('treeitem', { name: 'Bloco A 2' }))
    expect(screen.getByText('Apartamento 101')).toBeInTheDocument()

    await user.click(screen.getByRole('treeitem', { name: 'Bloco A 2' }))
    expect(screen.queryByText('Apartamento 101')).not.toBeInTheDocument()
  })

  it('supports controlled expansion and emits onExpandChange', async () => {
    const user = userEvent.setup()
    const handleExpandChange = vi.fn()
    const { rerender } = render(
      <Tree items={ITEMS} expandedIds={[]} onExpandChange={handleExpandChange} />,
    )

    await user.click(screen.getByRole('treeitem', { name: 'Bloco A 2' }))

    expect(handleExpandChange).toHaveBeenCalledWith(['bloco-a'])
    expect(screen.queryByText('Apartamento 101')).not.toBeInTheDocument()

    rerender(<Tree items={ITEMS} expandedIds={['bloco-a']} onExpandChange={handleExpandChange} />)
    expect(screen.getByText('Apartamento 101')).toBeInTheDocument()
  })

  it('supports keyboard navigation with arrows and Enter', async () => {
    const user = userEvent.setup()
    render(<Tree items={ITEMS} />)

    await user.tab()
    expect(screen.getByRole('treeitem', { name: 'Bloco A 2' })).toHaveFocus()

    await user.keyboard('{ArrowRight}')
    expect(screen.getByText('Apartamento 101')).toBeInTheDocument()

    await user.keyboard('{ArrowDown}')
    expect(screen.getByRole('treeitem', { name: 'Apartamento 101' })).toHaveFocus()

    await user.keyboard('{Enter}')
    expect(onUnitClick).toHaveBeenCalledOnce()

    await user.keyboard('{ArrowLeft}')
    expect(screen.getByRole('treeitem', { name: 'Bloco A 2' })).toHaveFocus()

    await user.keyboard('{ArrowLeft}')
    expect(screen.queryByText('Apartamento 101')).not.toBeInTheDocument()
  })

  it('sets aria-expanded and aria-level according to hierarchy depth', () => {
    render(<Tree items={ITEMS} defaultExpandedIds={['bloco-a']} />)

    expect(screen.getByRole('treeitem', { name: 'Bloco A 2' })).toHaveAttribute(
      'aria-expanded',
      'true',
    )
    expect(screen.getByRole('treeitem', { name: 'Bloco A 2' })).toHaveAttribute('aria-level', '1')
    expect(screen.getByRole('treeitem', { name: 'Apartamento 101' })).toHaveAttribute(
      'aria-level',
      '2',
    )
  })

  it('renders inactive items with muted styling and a distinct state icon', () => {
    render(<Tree items={ITEMS} defaultExpandedIds={['bloco-a']} />)

    const inactiveItem = screen.getByRole('treeitem', { name: 'Apartamento 102' })

    expect(inactiveItem.className).toMatch(/inactive/)
    expect(screen.getByTestId('tree-node-state-icon-apto-102')).toBeInTheDocument()
  })

  it('allows custom TreeNode rendering through renderNode', async () => {
    const user = userEvent.setup()
    render(
      <Tree
        items={ITEMS}
        renderNode={({ item, tabIndex, onFocus, onKeyDown, treeItemRef, onSelect }) => (
          <div
            ref={treeItemRef}
            role="treeitem"
            aria-level={1}
            tabIndex={tabIndex}
            onFocus={onFocus}
            onKeyDown={onKeyDown}
            data-testid={`custom-node-${item.id}`}
            onClick={onSelect}
          >
            {item.label}
          </div>
        )}
      />,
    )

    await user.click(screen.getByTestId('custom-node-bloco-a'))
    expect(screen.getByText('Apartamento 101')).toBeInTheDocument()
  })

  it('matches snapshots for base and inactive states', () => {
    const basicRender = render(<Tree items={ITEMS} />)
    expect(basicRender.asFragment()).toMatchSnapshot('tree-basic')

    basicRender.unmount()

    const inactiveRender = render(<Tree items={ITEMS} defaultExpandedIds={['bloco-a']} />)
    expect(inactiveRender.asFragment()).toMatchSnapshot('tree-with-inactive')
  })

  it('renders consumer actions without breaking row interaction', async () => {
    const user = userEvent.setup()
    const actionSpy = vi.fn()

    render(
      <Tree
        items={[
          {
            id: 'bloco-com-acoes',
            label: 'Bloco com ações',
            actions: (
              <Button size="sm" onClick={actionSpy}>
                Editar
              </Button>
            ),
            children: [{ id: 'filho', label: 'Filho' }],
          },
        ]}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'Editar' }))
    expect(actionSpy).toHaveBeenCalledOnce()
    expect(screen.queryByText('Filho')).not.toBeInTheDocument()
  })
})
