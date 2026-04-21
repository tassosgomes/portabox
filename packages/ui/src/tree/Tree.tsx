import {
  type KeyboardEvent,
  type ReactNode,
  useMemo,
  useState,
} from 'react'
import { TreeNode } from './TreeNode'
import type { TreeItem, TreeNodeRenderProps, TreeProps } from './types'
import styles from './Tree.module.css'

interface VisibleTreeItem {
  item: TreeItem
  level: number
  parentId?: string
  hasChildren: boolean
  expanded: boolean
}

function flattenItems(
  items: TreeItem[],
  expandedLookup: Set<string>,
  level = 1,
  parentId?: string,
): VisibleTreeItem[] {
  const flattened: VisibleTreeItem[] = []

  for (const item of items) {
    const hasChildren = !!item.children?.length
    const expanded = hasChildren && expandedLookup.has(item.id)

    flattened.push({ item, level, parentId, hasChildren, expanded })

    if (hasChildren && expanded) {
      flattened.push(...flattenItems(item.children ?? [], expandedLookup, level + 1, item.id))
    }
  }

  return flattened
}

export function Tree({
  items,
  expandedIds,
  defaultExpandedIds,
  onExpandChange,
  renderNode,
  ariaLabel = 'Estrutura hierárquica',
  className = '',
  ...rest
}: TreeProps) {
  const isControlled = expandedIds !== undefined
  const [internalExpandedIds, setInternalExpandedIds] = useState(defaultExpandedIds ?? [])
  const resolvedExpandedIds = isControlled ? expandedIds : internalExpandedIds
  const expandedLookup = useMemo(() => new Set(resolvedExpandedIds), [resolvedExpandedIds])
  const visibleItems = useMemo(
    () => flattenItems(items, expandedLookup),
    [items, expandedLookup],
  )
  const [focusedId, setFocusedId] = useState<string | null>(visibleItems[0]?.item.id ?? null)
  const activeFocusedId =
    focusedId && visibleItems.some((entry) => entry.item.id === focusedId)
      ? focusedId
      : (visibleItems[0]?.item.id ?? null)

  function updateExpandedIds(nextExpandedIds: string[]) {
    if (!isControlled) {
      setInternalExpandedIds(nextExpandedIds)
    }

    onExpandChange?.(nextExpandedIds)
  }

  function toggleNode(itemId: string) {
    const isExpanded = expandedLookup.has(itemId)
    const nextExpandedIds = isExpanded
      ? resolvedExpandedIds.filter((id) => id !== itemId)
      : [...resolvedExpandedIds, itemId]

    updateExpandedIds(nextExpandedIds)
  }

  function focusItem(itemId: string | undefined) {
    if (!itemId) {
      return
    }

    setFocusedId(itemId)

    const element = document.getElementById(`tree-item-${itemId}`)
    if (element) {
      element.focus()
    }
  }

  function handleSelect(entry: VisibleTreeItem) {
    if (entry.hasChildren) {
      toggleNode(entry.item.id)
    }

    entry.item.onClick?.()
  }

  function handleKeyDown(entry: VisibleTreeItem, event: KeyboardEvent<HTMLDivElement>) {
    const currentIndex = visibleItems.findIndex((visibleItem) => visibleItem.item.id === entry.item.id)

    switch (event.key) {
      case 'ArrowDown': {
        event.preventDefault()
        focusItem(visibleItems[currentIndex + 1]?.item.id)
        break
      }
      case 'ArrowUp': {
        event.preventDefault()
        focusItem(visibleItems[currentIndex - 1]?.item.id)
        break
      }
      case 'ArrowRight': {
        event.preventDefault()

        if (entry.hasChildren && !entry.expanded) {
          toggleNode(entry.item.id)
          return
        }

        if (entry.hasChildren && entry.expanded) {
          const firstChild = visibleItems.find((visibleItem) => visibleItem.parentId === entry.item.id)
          focusItem(firstChild?.item.id)
        }
        break
      }
      case 'ArrowLeft': {
        event.preventDefault()

        if (entry.hasChildren && entry.expanded) {
          toggleNode(entry.item.id)
          return
        }

        focusItem(entry.parentId)
        break
      }
      case 'Enter': {
        event.preventDefault()
        entry.item.onClick?.()
        break
      }
      default:
        break
    }
  }

  function renderTree(itemsToRender: TreeItem[], level = 1, parentId?: string): ReactNode {
    return itemsToRender.map((item) => {
      const hasChildren = !!item.children?.length
      const expanded = hasChildren && expandedLookup.has(item.id)
      const entry: VisibleTreeItem = { item, level, parentId, hasChildren, expanded }
      const nodeProps: TreeNodeRenderProps = {
        item,
        id: `tree-item-${item.id}`,
        level,
        expanded,
        hasChildren,
        focused: activeFocusedId === item.id,
        tabIndex: activeFocusedId === item.id ? 0 : -1,
        onToggle: () => toggleNode(item.id),
        onFocus: () => setFocusedId(item.id),
        onSelect: () => handleSelect(entry),
        onKeyDown: (event) => handleKeyDown(entry, event),
        role: 'treeitem',
        'aria-expanded': hasChildren ? expanded : undefined,
        'aria-level': level,
        'aria-disabled': item.state === 'inactive' ? true : undefined,
        onClick: () => handleSelect(entry),
      }

      return (
        <div key={item.id} role="none">
          {renderNode ? (
            renderNode(nodeProps)
          ) : (
            <TreeNode
              id={nodeProps.id}
              item={item}
              level={level}
              expanded={expanded}
              hasChildren={hasChildren}
              focused={nodeProps.focused}
              role="treeitem"
              tabIndex={nodeProps.tabIndex}
              aria-expanded={nodeProps['aria-expanded']}
              aria-level={nodeProps['aria-level']}
              aria-disabled={nodeProps['aria-disabled']}
              onFocus={nodeProps.onFocus}
              onClick={nodeProps.onClick}
              onKeyDown={nodeProps.onKeyDown}
            />
          )}
          {hasChildren && expanded ? (
            <div role="group" className={styles.group}>
              {renderTree(item.children ?? [], level + 1, item.id)}
            </div>
          ) : null}
        </div>
      )
    })
  }

  return (
    <div
      role="tree"
      aria-label={ariaLabel}
      className={[styles.tree, className].filter(Boolean).join(' ')}
      {...rest}
    >
      {renderTree(items)}
    </div>
  )
}
