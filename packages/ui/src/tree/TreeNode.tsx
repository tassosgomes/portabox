import { forwardRef, type ReactNode } from 'react'
import { Building2, ChevronDown, ChevronRight, Home, Power } from '../icons'
import type { TreeNodeProps } from './types'
import styles from './TreeNode.module.css'

function renderLeadingIcon(hasChildren: boolean, expanded: boolean): ReactNode {
  if (!hasChildren) {
    return <span className={styles.leading} aria-hidden="true" />
  }

  return expanded ? (
    <ChevronDown className={styles.leading} aria-hidden="true" size={18} strokeWidth={2} />
  ) : (
    <ChevronRight className={styles.leading} aria-hidden="true" size={18} strokeWidth={2} />
  )
}

export const TreeNode = forwardRef<HTMLDivElement, TreeNodeProps>(
  (
    {
      item,
      level,
      expanded,
      hasChildren,
      focused = false,
      className = '',
      onToggle: _onToggle,
      ...rest
    },
    ref,
  ) => {
    const typeIcon = hasChildren ? (
      <Building2 className={styles.typeIcon} aria-hidden="true" size={18} strokeWidth={2} />
    ) : (
      <Home className={styles.typeIcon} aria-hidden="true" size={18} strokeWidth={2} />
    )

    return (
      <div
        ref={ref}
        className={[
          styles.node,
          item.state === 'inactive' ? styles.inactive : '',
          focused ? styles.nodeFocused : '',
          className,
        ]
          .filter(Boolean)
          .join(' ')}
        data-level={level}
        {...rest}
      >
        {renderLeadingIcon(hasChildren, expanded)}
        {typeIcon}
        <div className={styles.content}>
          <span className={styles.label}>{item.label}</span>
          <span className={styles.meta}>
            {item.badge}
            {item.state === 'inactive' ? (
              <Power
                className={styles.stateIcon}
                aria-hidden="true"
                data-testid={`tree-node-state-icon-${item.id}`}
                size={16}
                strokeWidth={2}
              />
            ) : null}
          </span>
        </div>
        {item.actions ? (
          <span className={styles.actions} onClick={(event) => event.stopPropagation()}>
            {item.actions}
          </span>
        ) : null}
      </div>
    )
  },
)

TreeNode.displayName = 'TreeNode'
