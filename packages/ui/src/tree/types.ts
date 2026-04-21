import type { HTMLAttributes, KeyboardEvent, ReactNode } from 'react'

export type TreeItemState = 'default' | 'inactive'

export interface TreeItem {
  id: string
  label: ReactNode
  children?: TreeItem[]
  badge?: ReactNode
  state?: TreeItemState
  actions?: ReactNode
  onClick?: () => void
}

export interface TreeNodeProps extends HTMLAttributes<HTMLDivElement> {
  item: TreeItem
  level: number
  expanded: boolean
  hasChildren: boolean
  focused?: boolean
  onToggle?: () => void
}

export interface TreeNodeRenderProps extends TreeNodeProps {
  onKeyDown: (event: KeyboardEvent<HTMLDivElement>) => void
  onFocus: () => void
  onSelect: () => void
  tabIndex: 0 | -1
  treeItemRef?: (element: HTMLDivElement | null) => void
}

export type TreeRenderNode = (props: TreeNodeRenderProps) => ReactNode

export interface TreeProps extends HTMLAttributes<HTMLDivElement> {
  items: TreeItem[]
  expandedIds?: string[]
  defaultExpandedIds?: string[]
  onExpandChange?: (expandedIds: string[]) => void
  renderNode?: TreeRenderNode
  ariaLabel?: string
}
