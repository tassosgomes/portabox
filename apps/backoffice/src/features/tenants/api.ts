import { listCondominios } from '@/features/condominios/api'

export interface TenantOption {
  id: string
  nomeFantasia: string
  status: number
}

const TENANTS_PAGE_SIZE = 100

export async function listTenantOptions(): Promise<TenantOption[]> {
  const tenants: TenantOption[] = []
  let page = 1

  for (;;) {
    const response = await listCondominios({ page, pageSize: TENANTS_PAGE_SIZE, status: 0 })

    tenants.push(
      ...response.items.map((tenant) => ({
        id: tenant.id,
        nomeFantasia: tenant.nomeFantasia,
        status: tenant.status,
      })),
    )

    if (tenants.length >= response.totalCount) break
    page += 1
  }

  return tenants.sort((left, right) => left.nomeFantasia.localeCompare(right.nomeFantasia, 'pt-BR'))
}
