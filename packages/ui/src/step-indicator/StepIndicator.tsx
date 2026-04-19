import styles from './StepIndicator.module.css'

export interface Step {
  label: string
  description?: string
}

export interface StepIndicatorProps {
  steps: Step[]
  currentStep: number
  className?: string
}

export function StepIndicator({ steps, currentStep, className = '' }: StepIndicatorProps) {
  return (
    <nav
      aria-label="Progresso do formulário"
      className={[styles.nav, className].filter(Boolean).join(' ')}
    >
      <ol className={styles.list}>
        {steps.map((step, index) => {
          const stepNumber = index + 1
          const isCompleted = stepNumber < currentStep
          const isCurrent = stepNumber === currentStep

          return (
            <li
              key={step.label}
              className={[
                styles.item,
                isCompleted ? styles.completed : '',
                isCurrent ? styles.current : '',
              ]
                .filter(Boolean)
                .join(' ')}
              aria-current={isCurrent ? 'step' : undefined}
            >
              <span className={styles.circle} aria-hidden="true">
                {isCompleted ? '✓' : stepNumber}
              </span>
              <span className={styles.labelWrap}>
                <span className={styles.label}>{step.label}</span>
                {step.description ? (
                  <span className={styles.description}>{step.description}</span>
                ) : null}
              </span>
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
