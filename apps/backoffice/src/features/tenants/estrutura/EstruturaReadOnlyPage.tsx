import { useEffect, useMemo, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { ApiError, type Estrutura } from '@portabox/api-client'
import { Card, Tree } from '@portabox/ui'
import { TenantSelector } from '../components/TenantSelector'
import { useEstruturaAdmin } from './hooks/useEstruturaAdmin'
import { toReadOnlyTreeItems } from './mappers/toReadOnlyTreeItems'
import styles from './EstruturaReadOnlyPage.module.css'

function buildLoginRedirect(pathname: string, search: string) {
  return `/login?redirectTo=${encodeURIComponent(pathname + search)}`
}

function countActiveBlocks(total: Estrutura['blocos']) {
  return total.filter((bloco) => bloco.ativo).length
}

function countActiveUnits(total: Estrutura['blocos']) {
  return total.reduce(
    (sum, bloco) => sum + bloco.andares.reduce(
      (andarTotal, andar) => andarTotal + andar.unidades.filter((unidade) => unidade.ativo).length,
      0,
    ),
    0,
  )
}

function LoadingState() {
  return (
    <Card className={styles.feedbackCard} padding="lg">
      <div className={styles.loadingState} role="status" aria-live="polite">
        <span className={styles.spinner} aria-hidden="true" />
        <span>Carregando a estrutura do condomínio...</span>
      </div>
    </Card>
  )
}

function NotFoundState() {
  return (
    <Card className={styles.feedbackCard} padding="lg">
      <div className={styles.feedbackBody}>
        <h2 className={styles.feedbackTitle}>Estrutura não encontrada</h2>
        <p className={styles.feedbackText}>
          Não localizamos a estrutura do condomínio selecionado. Revise o tenant escolhido e tente novamente.
        </p>
      </div>
    </Card>
  )
}

export function EstruturaReadOnlyPage() {
  const { condominioId = '' } = useParams<{ condominioId: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const [includeInactive, setIncludeInactive] = useState(false)
  const { data, error, isPending, isFetching } = useEstruturaAdmin(condominioId, includeInactive)

  const apiError = error instanceof ApiError ? error : null
  const items = useMemo(() => (data ? toReadOnlyTreeItems(data) : []), [data])
  const activeBlocks = data ? countActiveBlocks(data.blocos) : 0
  const activeUnits = data ? countActiveUnits(data.blocos) : 0

  useEffect(() => {
    if (apiError?.status === 401) {
      navigate(buildLoginRedirect(location.pathname, location.search), { replace: true })
      return
    }

    if (apiError?.status === 403) {
      navigate('/erro/acesso-negado', { replace: true })
    }
  }, [apiError, location.pathname, location.search, navigate])

  // TODO(task_17): call POST /api/v1/admin/audit-access when the backend endpoint exists in Phase 2.

  if (apiError?.status === 401 || apiError?.status === 403) {
    return null
  }

  if (apiError?.status === 404) {
    return <NotFoundState />
  }

  return (
    <section className={styles.page}>
      <div className={styles.backRow}>
        <Link to={`/condominios/${condominioId}`} className={styles.backLink}>
          &larr; Voltar para o condomínio
        </Link>
      </div>

      <header className={styles.header}>
        <div className={styles.headerCopy}>
          <h1 className={styles.title}>Estrutura do condomínio</h1>
          <p className={styles.subtitle}>
            Visualização read-only para suporte cross-tenant com a mesma árvore operacional do síndico.
          </p>
        </div>

        <div className={styles.headerControls}>
          <TenantSelector condominioId={condominioId} />
          <label className={styles.toggle}>
            <input
              type="checkbox"
              checked={includeInactive}
              onChange={(event) => setIncludeInactive(event.target.checked)}
            />
            <span>Mostrar inativos</span>
          </label>
        </div>
      </header>

      <div className={styles.metrics}>
        <Card className={styles.metricCard} padding="lg">
          <span className={styles.metricLabel}>Blocos ativos</span>
          <strong className={styles.metricValue}>{activeBlocks}</strong>
        </Card>
        <Card className={styles.metricCard} padding="lg">
          <span className={styles.metricLabel}>Unidades ativas</span>
          <strong className={styles.metricValue}>{activeUnits}</strong>
        </Card>
      </div>

      {isPending && !data ? <LoadingState /> : null}

      {apiError && apiError.status !== 404 ? (
        <Card className={styles.feedbackCard} padding="lg">
          <div className={styles.feedbackBody}>
            <h2 className={styles.feedbackTitle}>Não foi possível carregar a estrutura</h2>
            <p className={styles.feedbackText}>
              {apiError.detail ?? 'Tente novamente em alguns instantes.'}
            </p>
          </div>
        </Card>
      ) : null}

      {data ? (
        <Card className={styles.treeCard} padding="lg">
          <div className={styles.treeHeader}>
            <div>
              <h2 className={styles.treeTitle}>{data.nomeFantasia}</h2>
              <p className={styles.treeMeta}>
                {data.blocos.length} {data.blocos.length === 1 ? 'bloco listado' : 'blocos listados'}
              </p>
            </div>

            {isFetching && !isPending ? (
              <span className={styles.fetchingHint} role="status" aria-live="polite">
                Atualizando...
              </span>
            ) : null}
          </div>

          <div className={styles.readOnlyHint}>
            Esta visão é somente leitura. Criação, edição e inativação continuam restritas ao app do síndico.
          </div>

          <Tree items={items} defaultExpandedIds={items[0] ? [items[0].id] : []} />
        </Card>
      ) : null}
    </section>
  )
}
