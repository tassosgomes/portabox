import { useEffect, useMemo, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { ApiError, type BlocoNode, type UnidadeLeaf } from '@portabox/api-client'
import { Button, Card, ConfirmModal, Tree } from '@portabox/ui'
import { useAuth } from '@/features/auth/hooks/useAuth'
import { BlocoForm } from './components/BlocoForm'
import type { BlocoFormValues } from './components/blocoFormSchema'
import { EmptyState } from './components/EmptyState'
import { UnidadeForm } from './components/UnidadeForm'
import type { UnidadeFormValues } from './components/unidadeFormSchema'
import { useCriarBloco } from './hooks/useCriarBloco'
import { useCriarUnidade } from './hooks/useCriarUnidade'
import { useInativarBloco } from './hooks/useInativarBloco'
import { useInativarUnidade } from './hooks/useInativarUnidade'
import { useReativarBloco } from './hooks/useReativarBloco'
import { useReativarUnidade } from './hooks/useReativarUnidade'
import { useRenomearBloco } from './hooks/useRenomearBloco'
import { useEstrutura } from './hooks/useEstrutura'
import { toTreeItems } from './mappers/toTreeItems'
import styles from './EstruturaPage.module.css'

function buildLoginRedirect(pathname: string, search: string) {
  return `/login?redirectTo=${encodeURIComponent(pathname + search)}`
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
          Não localizamos a estrutura deste condomínio. Confira se você está no condomínio certo ou volte para a página inicial.
        </p>
        <Link className={styles.backLink} to="/">
          Voltar ao início
        </Link>
      </div>
    </Card>
  )
}

interface FeedbackState {
  message: string
  detail?: string | null
}

interface ConfirmActionState {
  bloco: BlocoNode
  mode: 'inativar' | 'reativar'
}

interface UnidadeModalState {
  bloco: BlocoNode
  mode: 'single' | 'batch'
}

interface ConfirmUnidadeActionState {
  bloco: BlocoNode
  unidade: UnidadeLeaf
  mode: 'inativar' | 'reativar'
}

function normalizeNome(value: string) {
  return value.trim().toLocaleLowerCase('pt-BR')
}

function getBlocoErrorFeedback(error: unknown, action: 'create' | 'rename' | 'inativar' | 'reativar') {
  if (!(error instanceof ApiError)) {
    return {
      message: 'Nao foi possivel concluir a operacao com o bloco agora. Tente novamente.',
      detail: null,
      shouldSuggestReactivation: false,
    }
  }

  const detail = error.detail ?? null

  if (action === 'create' && error.status === 409) {
    return {
      message: detail ?? 'Ja existe um bloco com esse nome. Se ele estiver inativo, reative-o em vez de criar outro.',
      detail,
      shouldSuggestReactivation: true,
    }
  }

  if (action === 'reativar' && error.status === 409) {
    return {
      message: detail ?? 'Ja existe outro bloco ativo com esse nome. Inative o duplicado antes de reativar este.',
      detail,
      shouldSuggestReactivation: false,
    }
  }

  if (error.status === 422) {
    return {
      message: detail ?? 'O status deste bloco mudou enquanto voce editava. Atualize a estrutura e tente novamente.',
      detail,
      shouldSuggestReactivation: false,
    }
  }

  return {
    message: detail ?? 'Nao foi possivel concluir a operacao com o bloco agora. Tente novamente.',
    detail,
    shouldSuggestReactivation: false,
  }
}

function getUnidadeErrorFeedback(error: unknown, action: 'create' | 'inativar' | 'reativar') {
  if (!(error instanceof ApiError)) {
    return {
      message: 'Nao foi possivel concluir a operacao com a unidade agora. Tente novamente.',
      detail: null,
    }
  }

  const detail = error.detail ?? null

  if (action === 'create' && error.status === 409) {
    return {
      message: detail ?? 'Ja existe outra unidade ativa com este bloco, andar e numero.',
      detail,
    }
  }

  if (action === 'create' && error.status === 422) {
    return {
      message: detail ?? 'O bloco selecionado esta inativo. Reative o bloco antes de cadastrar novas unidades.',
      detail,
    }
  }

  if (action === 'reativar' && error.status === 409) {
    return {
      message: detail ?? 'Ja existe outra unidade ativa com a mesma tripla. Inative a duplicada antes de reativar esta.',
      detail,
    }
  }

  if (error.status === 422) {
    return {
      message: detail ?? 'O status desta unidade mudou enquanto voce editava. Atualize a estrutura e tente novamente.',
      detail,
    }
  }

  return {
    message: detail ?? 'Nao foi possivel concluir a operacao com a unidade agora. Tente novamente.',
    detail,
  }
}

export function EstruturaPage() {
  const { user } = useAuth()
  const { condominioId: condominioIdFromPath } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  const [includeInactive, setIncludeInactive] = useState(false)
  const [feedback, setFeedback] = useState<FeedbackState | null>(null)
  const [createModalOpen, setCreateModalOpen] = useState(false)
  const [renameTarget, setRenameTarget] = useState<BlocoNode | null>(null)
  const [confirmAction, setConfirmAction] = useState<ConfirmActionState | null>(null)
  const [explicitlySelectedBlocoId, setSelectedBlocoId] = useState<string | null>(null)
  const [unidadeModal, setUnidadeModal] = useState<UnidadeModalState | null>(null)
  const [confirmUnidadeAction, setConfirmUnidadeAction] = useState<ConfirmUnidadeActionState | null>(null)
  const [pendingReactivationName, setPendingReactivationName] = useState<string | null>(null)
  const condominioId = condominioIdFromPath ?? user?.tenantId ?? ''
  const { data, error, isPending, isFetching, refetch } = useEstrutura(
    condominioId,
    includeInactive,
  )
  const criarBlocoMutation = useCriarBloco(condominioId)
  const renomearBlocoMutation = useRenomearBloco(condominioId)
  const inativarBlocoMutation = useInativarBloco(condominioId)
  const reativarBlocoMutation = useReativarBloco(condominioId)
  const criarUnidadeMutation = useCriarUnidade(condominioId)
  const inativarUnidadeMutation = useInativarUnidade(condominioId)
  const reativarUnidadeMutation = useReativarUnidade(condominioId)

  const selectedBlocoId = useMemo(() => {
    if (!data) {
      return null
    }

    if (explicitlySelectedBlocoId && data.blocos.some((bloco) => bloco.id === explicitlySelectedBlocoId)) {
      return explicitlySelectedBlocoId
    }

    return data.blocos[0]?.id ?? null
  }, [data, explicitlySelectedBlocoId])

  const apiError = error instanceof ApiError ? error : null
  const selectedBloco = data?.blocos.find((bloco) => bloco.id === selectedBlocoId) ?? null
  const suggestedReactivation = useMemo(() => {
    if (!pendingReactivationName || !includeInactive || !data) {
      return null
    }

    const bloco = data.blocos.find(
      (item) => !item.ativo && normalizeNome(item.nome) === pendingReactivationName,
    )

    return bloco ? { bloco, mode: 'reativar' as const } : null
  }, [data, includeInactive, pendingReactivationName])
  const activeConfirmAction = confirmAction ?? suggestedReactivation
  const items = useMemo(
    () => (data
      ? toTreeItems(data, {
        selectedBlocoId,
        onSelectBloco: (bloco) => {
          setSelectedBlocoId(bloco.id)
        },
        onRenameBloco: (bloco) => setRenameTarget(bloco),
        onInativarBloco: (bloco) => setConfirmAction({ bloco, mode: 'inativar' }),
        onReativarBloco: (bloco) => setConfirmAction({ bloco, mode: 'reativar' }),
        onInativarUnidade: (bloco, unidade) => setConfirmUnidadeAction({ bloco, unidade, mode: 'inativar' }),
        onReativarUnidade: (bloco, unidade) => setConfirmUnidadeAction({ bloco, unidade, mode: 'reativar' }),
      })
      : []),
    [data, selectedBlocoId],
  )
  const feedbackMessage = apiError && apiError.status !== 404
    ? (apiError.detail ?? 'Nao foi possivel carregar a estrutura agora. Tente novamente.')
    : feedback?.message ?? null
  const formErrorMessage = feedback?.detail ?? null

  useEffect(() => {
    if (apiError?.status === 401) {
      navigate(buildLoginRedirect(location.pathname, location.search), { replace: true })
    }
  }, [apiError, location.pathname, location.search, navigate])

  async function handleCreateSubmit(values: BlocoFormValues) {
    try {
      await criarBlocoMutation.mutateAsync(values)
      setCreateModalOpen(false)
      setFeedback(null)
    } catch (mutationError) {
      const nextFeedback = getBlocoErrorFeedback(mutationError, 'create')
      setFeedback({ message: nextFeedback.message, detail: nextFeedback.detail })

      if (nextFeedback.shouldSuggestReactivation) {
        setPendingReactivationName(normalizeNome(values.nome))
        setIncludeInactive(true)
        await refetch()
      }
    }
  }

  async function handleRenameSubmit(values: BlocoFormValues) {
    if (!renameTarget) {
      return
    }

    try {
      await renomearBlocoMutation.mutateAsync({ blocoId: renameTarget.id, nome: values.nome })
      setRenameTarget(null)
      setFeedback(null)
    } catch (mutationError) {
      const nextFeedback = getBlocoErrorFeedback(mutationError, 'rename')
      setFeedback({ message: nextFeedback.message, detail: nextFeedback.detail })
    }
  }

  async function handleConfirmAction() {
    if (!activeConfirmAction) {
      return
    }

    const action = activeConfirmAction

    try {
      if (action.mode === 'inativar') {
        await inativarBlocoMutation.mutateAsync({ blocoId: action.bloco.id })
      } else {
        await reativarBlocoMutation.mutateAsync({ blocoId: action.bloco.id })
      }

      setConfirmAction(null)
      setPendingReactivationName(null)
      setFeedback(null)
    } catch (mutationError) {
      const nextFeedback = getBlocoErrorFeedback(mutationError, action.mode)
      setFeedback({ message: nextFeedback.message, detail: nextFeedback.detail })
    }
  }

  async function handleCreateUnidadeSubmit(values: UnidadeFormValues) {
    if (!unidadeModal) {
      return 'close' as const
    }

    try {
      await criarUnidadeMutation.mutateAsync({
        blocoId: unidadeModal.bloco.id,
        andar: values.andar,
        numero: values.numero,
      })
      setFeedback(null)
      return unidadeModal.mode === 'batch' ? 'keep-open' : 'close'
    } catch (mutationError) {
      const nextFeedback = getUnidadeErrorFeedback(mutationError, 'create')
      setFeedback({ message: nextFeedback.message, detail: nextFeedback.detail })
      return 'keep-open' as const
    }
  }

  async function handleConfirmUnidadeAction() {
    if (!confirmUnidadeAction) {
      return
    }

    try {
      if (confirmUnidadeAction.mode === 'inativar') {
        await inativarUnidadeMutation.mutateAsync({
          blocoId: confirmUnidadeAction.bloco.id,
          unidadeId: confirmUnidadeAction.unidade.id,
        })
      } else {
        await reativarUnidadeMutation.mutateAsync({
          blocoId: confirmUnidadeAction.bloco.id,
          unidadeId: confirmUnidadeAction.unidade.id,
        })
      }

      setConfirmUnidadeAction(null)
      setFeedback(null)
    } catch (mutationError) {
      const nextFeedback = getUnidadeErrorFeedback(mutationError, confirmUnidadeAction.mode)
      setFeedback({ message: nextFeedback.message, detail: nextFeedback.detail })
    }
  }

  if (!condominioId) {
    return (
      <Card className={styles.feedbackCard} padding="lg">
        <div className={styles.feedbackBody}>
          <h1 className={styles.feedbackTitle}>Condomínio indisponível</h1>
          <p className={styles.feedbackText}>
            Não foi possível identificar o condomínio associado ao seu acesso.
          </p>
        </div>
      </Card>
    )
  }

  if (apiError?.status === 401) {
    return null
  }

  if (apiError?.status === 404) {
    return <NotFoundState />
  }

  const showInitialLoading = isPending && !data
  const showEmptyState = data && data.blocos.length === 0
  const showTree = data && data.blocos.length > 0

  return (
    <section className={styles.page}>
      <header className={styles.header}>
        <div>
          <h1 className={styles.title}>Estrutura do condomínio</h1>
          <p className={styles.subtitle}>
            Consulte blocos, andares e unidades em uma única árvore operacional.
          </p>
        </div>
        <div className={styles.headerActions}>
          <Button type="button" onClick={() => {
            setFeedback(null)
            setCreateModalOpen(true)
          }}>
            Novo bloco
          </Button>
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

      {feedbackMessage ? (
        <div className={styles.toast} role="alert">
          <div className={styles.toastCopy}>
            <span>{feedbackMessage}</span>
            {pendingReactivationName ? (
              <small>Os blocos inativos foram exibidos para facilitar a reativação.</small>
            ) : null}
          </div>
          <div className={styles.toastActions}>
            <Button type="button" size="sm" variant="ghost" onClick={() => setFeedback(null)}>
              Fechar
            </Button>
            <Button type="button" size="sm" variant="ghost" onClick={() => void refetch()}>
              Tentar novamente
            </Button>
          </div>
        </div>
      ) : null}

      {showInitialLoading ? <LoadingState /> : null}

      {showEmptyState ? (
        <EmptyState
          onCreateFirstBlock={() => {
            setFeedback(null)
            setCreateModalOpen(true)
          }}
        />
      ) : null}

      {showTree ? (
        <Card className={styles.treeCard} padding="lg">
          <div className={styles.treeHeader}>
            <div>
              <h2 className={styles.treeTitle}>{data.nomeFantasia}</h2>
              <p className={styles.treeMeta}>
                {data.blocos.length} {data.blocos.length === 1 ? 'bloco cadastrado' : 'blocos cadastrados'}
              </p>
            </div>
            {isFetching && !showInitialLoading ? (
              <span className={styles.fetchingHint} role="status" aria-live="polite">
                Atualizando...
              </span>
            ) : null}
          </div>

          {selectedBloco ? (
            <div className={styles.treeToolbar}>
              <div className={styles.treeToolbarCopy}>
                <strong>Bloco selecionado: {selectedBloco.nome}</strong>
                <p>
                  {selectedBloco.ativo
                    ? 'Use este contexto para cadastrar unidades novas ou continuar o preenchimento em rajada.'
                    : 'Reative este bloco antes de cadastrar novas unidades.'}
                </p>
              </div>
              {selectedBloco.ativo ? (
                <div className={styles.treeToolbarActions}>
                  <Button
                    type="button"
                    variant="ghost"
                    onClick={() => {
                      setFeedback(null)
                      setUnidadeModal({ bloco: selectedBloco, mode: 'single' })
                    }}
                  >
                    Adicionar unidade
                  </Button>
                  <Button
                    type="button"
                    onClick={() => {
                      setFeedback(null)
                      setUnidadeModal({ bloco: selectedBloco, mode: 'batch' })
                    }}
                  >
                    Adicionar próxima unidade
                  </Button>
                </div>
              ) : null}
            </div>
          ) : null}

          <Tree items={items} defaultExpandedIds={items[0] ? [items[0].id] : []} />
        </Card>
      ) : null}

      <BlocoForm
        open={createModalOpen && !suggestedReactivation}
        mode="create"
        isSubmitting={criarBlocoMutation.isPending}
        apiErrorMessage={createModalOpen && !suggestedReactivation ? formErrorMessage : null}
        onClose={() => {
          setCreateModalOpen(false)
          setPendingReactivationName(null)
        }}
        onSubmit={handleCreateSubmit}
      />

      <BlocoForm
        open={Boolean(renameTarget)}
        mode="rename"
        initialNome={renameTarget?.nome}
        isSubmitting={renomearBlocoMutation.isPending}
        apiErrorMessage={renameTarget ? formErrorMessage : null}
        onClose={() => setRenameTarget(null)}
        onSubmit={handleRenameSubmit}
      />

      <UnidadeForm
        open={Boolean(unidadeModal)}
        blocoNome={unidadeModal?.bloco.nome ?? ''}
        keepOpenOnSuccess={unidadeModal?.mode === 'batch'}
        isSubmitting={criarUnidadeMutation.isPending}
        apiErrorMessage={unidadeModal ? formErrorMessage : null}
        onClose={() => setUnidadeModal(null)}
        onSubmit={handleCreateUnidadeSubmit}
      />

      <ConfirmModal
        open={Boolean(activeConfirmAction)}
        title={activeConfirmAction?.mode === 'reativar' ? 'Reativar bloco' : 'Inativar bloco'}
        description={activeConfirmAction?.mode === 'reativar'
          ? `Reativar ${activeConfirmAction.bloco.nome} vai devolve-lo aos novos cadastros. As unidades deste bloco permanecem no status atual.`
          : `Inativar ${activeConfirmAction?.bloco.nome} vai oculta-lo de novos cadastros; unidades ativas permanecem e precisam ser inativadas separadamente.`}
        confirmLabel={activeConfirmAction?.mode === 'reativar' ? 'Reativar bloco' : 'Inativar bloco'}
        cancelLabel="Cancelar"
        danger={activeConfirmAction?.mode !== 'reativar'}
        onCancel={() => {
          setConfirmAction(null)
          setPendingReactivationName(null)
        }}
        onConfirm={() => void handleConfirmAction()}
      />

      <ConfirmModal
        open={Boolean(confirmUnidadeAction)}
        title={confirmUnidadeAction?.mode === 'reativar' ? 'Reativar unidade' : 'Inativar unidade'}
        description={confirmUnidadeAction?.mode === 'reativar'
          ? `Reativar a unidade ${confirmUnidadeAction.unidade.numero} em ${confirmUnidadeAction.bloco.nome} volta a exibi-la no cadastro ativo. Se ja existir outra unidade ativa com a mesma tripla, a reativacao falhara.`
          : `Inativar a unidade ${confirmUnidadeAction?.unidade.numero} em ${confirmUnidadeAction?.bloco.nome} retira-a dos cadastros ativos. Moradores associados permanecem vinculados; inative-os separadamente em F03 se necessario.`}
        confirmLabel={confirmUnidadeAction?.mode === 'reativar' ? 'Reativar unidade' : 'Inativar unidade'}
        cancelLabel="Cancelar"
        danger={confirmUnidadeAction?.mode !== 'reativar'}
        onCancel={() => setConfirmUnidadeAction(null)}
        onConfirm={() => void handleConfirmUnidadeAction()}
      />
    </section>
  )
}
