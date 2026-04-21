import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { TenantSelector } from './TenantSelector'
import { listTenantOptions } from '../api'

vi.mock('../api', () => ({
  listTenantOptions: vi.fn(),
}))

function LocationProbe() {
  const location = useLocation()
  return <span data-testid="location-probe">{location.pathname}</span>
}

describe('TenantSelector', () => {
  beforeEach(() => {
    vi.mocked(listTenantOptions).mockReset()
  })

  it('lists tenants and navigates to the selected tenant structure route', async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    })

    vi.mocked(listTenantOptions).mockResolvedValue([
      { id: 'cond-1', nomeFantasia: 'Residencial Sol', status: 2 },
      { id: 'cond-2', nomeFantasia: 'Residencial Lua', status: 1 },
    ])

    const user = userEvent.setup()

    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/tenants/cond-1/estrutura']}>
          <Routes>
            <Route
              path="/tenants/:condominioId/estrutura"
              element={
                <>
                  <TenantSelector condominioId="cond-1" />
                  <LocationProbe />
                </>
              }
            />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    )

    await waitFor(() => {
      expect(screen.getByRole('option', { name: 'Residencial Sol' })).toBeInTheDocument()
    })

    await user.selectOptions(screen.getByLabelText('Condomínio'), 'cond-2')

    await waitFor(() => {
      expect(screen.getByTestId('location-probe')).toHaveTextContent('/tenants/cond-2/estrutura')
    })
  })
})
