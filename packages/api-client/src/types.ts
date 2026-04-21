import type { components } from './generated'

export type Estrutura = components['schemas']['Estrutura']
export type BlocoNode = components['schemas']['BlocoNode']
export type AndarNode = components['schemas']['AndarNode']
export type UnidadeLeaf = components['schemas']['UnidadeLeaf']
export type Bloco = components['schemas']['Bloco']
export type Unidade = components['schemas']['Unidade']
export type CreateBlocoRequest = components['schemas']['CreateBlocoRequest']
export type RenameBlocoRequest = components['schemas']['RenameBlocoRequest']
export type CreateUnidadeRequest = components['schemas']['CreateUnidadeRequest']
export type ProblemDetails = components['schemas']['ProblemDetails']
export type ValidationProblemDetails = components['schemas']['ValidationProblemDetails']

export type { components, operations, paths } from './generated'
