import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { listTenantOptions } from '../api'
import styles from './TenantSelector.module.css'

interface TenantSelectorProps {
  condominioId: string
}

export function TenantSelector({ condominioId }: TenantSelectorProps) {
  const navigate = useNavigate()
  const { data, isPending, error } = useQuery({
    queryKey: ['tenant-options'],
    queryFn: listTenantOptions,
  })

  return (
    <div className={styles.field}>
      <label className={styles.label} htmlFor="tenant-selector">
        Condomínio
      </label>
      <select
        id="tenant-selector"
        className={styles.select}
        value={condominioId}
        disabled={isPending || Boolean(error) || !data?.length}
        onChange={(event) => {
          navigate(`/tenants/${event.target.value}/estrutura`)
        }}
      >
        {isPending ? <option value={condominioId}>Carregando condomínios...</option> : null}
        {error ? <option value={condominioId}>Não foi possível carregar os condomínios</option> : null}
        {data?.map((tenant) => (
          <option key={tenant.id} value={tenant.id}>
            {tenant.nomeFantasia}
          </option>
        ))}
      </select>
    </div>
  )
}
