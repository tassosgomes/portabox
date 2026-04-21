import type { ProblemDetails, ValidationProblemDetails } from './types'

export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail?: string | null
  readonly type: string
  readonly instance?: string | null
  readonly traceId?: string | null
  readonly fieldErrors?: Record<string, string[]>

  constructor(problem: ProblemDetails | ValidationProblemDetails) {
    super(problem.detail ?? problem.title)
    this.name = 'ApiError'
    this.status = problem.status
    this.title = problem.title
    this.detail = problem.detail
    this.type = problem.type
    this.instance = problem.instance
    this.traceId = problem.traceId
    this.fieldErrors = 'errors' in problem ? problem.errors : undefined
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    const fallback = {
      type: 'about:blank',
      title: response.statusText || 'Erro na API',
      status: response.status,
      detail: undefined,
      instance: undefined,
      traceId: undefined,
    } satisfies ProblemDetails

    const contentType = response.headers.get('content-type') ?? ''
    if (!contentType.includes('application/json') && !contentType.includes('application/problem+json')) {
      return new ApiError(fallback)
    }

    const problem = (await response.json()) as ProblemDetails | ValidationProblemDetails
    return new ApiError({ ...fallback, ...problem, status: problem.status ?? response.status })
  }
}
