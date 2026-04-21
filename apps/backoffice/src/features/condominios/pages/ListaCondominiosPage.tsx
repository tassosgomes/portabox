import { useEffect, useState, useRef } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Button, Building2 } from '@portabox/ui'
import { listCondominios, ApiHttpError } from '../api'
import { StatusBadge } from '../components/StatusBadge'
import type { CondominioListItem, PagedResult } from '../types'
import styles from './ListaCondominiosPage.module.css'

const PAGE_SIZE = 20

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR')
}

const STATUS_FILTER_OPTIONS = [
  { value: 0, label: 'Todos' },
  { value: 1, label: 'Pré-ativo' },
  { value: 2, label: 'Ativo' },
]

export function ListaCondominiosPage() {
  const location = useLocation()
  const successMsg = (location.state as { successMessage?: string } | null)?.successMessage

  const [result, setResult] = useState<PagedResult<CondominioListItem> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<number>(0)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)

  const searchTimeout = useRef<ReturnType<typeof setTimeout> | null>(null)
  const [debouncedSearch, setDebouncedSearch] = useState('')

  useEffect(() => {
    if (searchTimeout.current) clearTimeout(searchTimeout.current)
    searchTimeout.current = setTimeout(() => {
      setDebouncedSearch(search)
      setPage(1)
    }, 300)
    return () => {
      if (searchTimeout.current) clearTimeout(searchTimeout.current)
    }
  }, [search])

  useEffect(() => {
    let cancelled = false

    listCondominios({ page, pageSize: PAGE_SIZE, status: statusFilter, q: debouncedSearch || undefined })
      .then((data) => {
        if (!cancelled) {
          setResult(data)
          setError(null)
          setLoading(false)
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(
            err instanceof ApiHttpError
              ? 'Erro ao carregar condomínios. Tente novamente.'
              : 'Erro inesperado.',
          )
          setLoading(false)
        }
      })

    return () => { cancelled = true }
  }, [page, statusFilter, debouncedSearch])

  const totalPages = result ? Math.ceil(result.totalCount / PAGE_SIZE) : 0

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h2 className={styles.title}>
          <Building2 size={20} aria-hidden="true" />
          Condomínios
        </h2>
        <Link to="/condominios/novo" className={styles.newBtn}>
          Novo condomínio
        </Link>
      </div>

      {successMsg && (
        <p role="status" className={styles.successToast}>
          {successMsg}
        </p>
      )}

      <div className={styles.filters}>
        <div className={styles.statusTabs} role="group" aria-label="Filtrar por status">
          {STATUS_FILTER_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              type="button"
              className={[styles.tab, statusFilter === opt.value ? styles.tabActive : '']
                .filter(Boolean)
                .join(' ')}
              onClick={() => { setStatusFilter(opt.value); setPage(1) }}
              aria-pressed={statusFilter === opt.value}
            >
              {opt.label}
            </button>
          ))}
        </div>
        <input
          type="search"
          className={styles.searchInput}
          placeholder="Buscar por nome ou CNPJ..."
          aria-label="Buscar condomínio"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {loading && (
        <p aria-live="polite" className={styles.stateMsg}>
          Carregando...
        </p>
      )}

      {!loading && error && (
        <p role="alert" className={styles.errorMsg}>
          {error}
        </p>
      )}

      {!loading && !error && result && result.items.length === 0 && (
        <div className={styles.emptyState}>
          <Building2 size={40} aria-hidden="true" className={styles.emptyIcon} />
          <p>Nenhum condomínio cadastrado</p>
          <Link to="/condominios/novo">Cadastrar novo condomínio</Link>
        </div>
      )}

      {!loading && !error && result && result.items.length > 0 && (
        <>
          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th scope="col">Nome</th>
                  <th scope="col">CNPJ</th>
                  <th scope="col">Status</th>
                  <th scope="col">Criado em</th>
                  <th scope="col">Ativado em</th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <Link to={`/condominios/${item.id}`} className={styles.nameLink}>
                        {item.nomeFantasia}
                      </Link>
                    </td>
                    <td className={styles.mono}>{item.cnpjMasked}</td>
                    <td>
                      <StatusBadge status={item.status} />
                    </td>
                    <td>{formatDate(item.createdAt)}</td>
                    <td>{item.activatedAt ? formatDate(item.activatedAt) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div className={styles.pagination} role="navigation" aria-label="Paginação">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage((p) => p - 1)}
                disabled={page <= 1}
              >
                Anterior
              </Button>
              <span className={styles.pageInfo}>
                Página {page} de {totalPages}
              </span>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage((p) => p + 1)}
                disabled={page >= totalPages}
              >
                Próxima
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
