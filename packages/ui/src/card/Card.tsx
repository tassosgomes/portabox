import { type HTMLAttributes } from 'react'
import styles from './Card.module.css'

export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  padding?: 'sm' | 'md' | 'lg'
}

export function Card({ padding = 'md', className = '', children, ...rest }: CardProps) {
  return (
    <div
      className={[styles.card, styles[`pad-${padding}`], className].filter(Boolean).join(' ')}
      {...rest}
    >
      {children}
    </div>
  )
}
