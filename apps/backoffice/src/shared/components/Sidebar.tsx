import { NavLink } from 'react-router-dom'
import { Building2, FileBarChart, Package, ShieldCheck, Users } from 'lucide-react'
import styles from './Sidebar.module.css'

type ComingSoonItem = {
  label: string
  icon: typeof Package
}

const COMING_SOON: ComingSoonItem[] = [
  { label: 'Encomendas', icon: Package },
  { label: 'Moradores', icon: Users },
  { label: 'Relatórios', icon: FileBarChart },
  { label: 'Auditoria', icon: ShieldCheck },
]

export function Sidebar() {
  return (
    <aside className={styles.sidebar} aria-label="Navegação principal">
      <NavLink
        to="/condominios"
        className={({ isActive }) =>
          [styles.navItem, isActive ? styles.active : ''].filter(Boolean).join(' ')
        }
      >
        <Building2 size={16} strokeWidth={1.75} aria-hidden="true" />
        Condomínios
      </NavLink>

      <div className={styles.sectionLbl}>Em breve</div>

      {COMING_SOON.map(({ label, icon: Icon }) => (
        <span
          key={label}
          className={`${styles.navItem} ${styles.disabled}`}
          aria-disabled="true"
        >
          <Icon size={16} strokeWidth={1.75} aria-hidden="true" />
          {label}
          <span className={styles.badgeSoon}>em breve</span>
        </span>
      ))}

      <div className={styles.sideFoot}>
        <b>PortaBox Backoffice</b>
        v0.1.0 · build #184
        <br />
        Homolog · pt-BR
      </div>
    </aside>
  )
}
