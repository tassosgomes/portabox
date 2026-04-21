import { Navigate, Route, Routes } from 'react-router-dom'
import { LoginPage } from '@/features/auth/pages/LoginPage'
import { RequireOperator } from '@/shared/auth/RequireOperator'
import { AppLayout } from '@/shared/layouts/AppLayout'
import { NovoCondominioPage } from '@/features/condominios/pages/NovoCondominioPage'
import { ListaCondominiosPage } from '@/features/condominios/pages/ListaCondominiosPage'
import { DetalhesCondominioPage } from '@/features/condominios/pages/DetalhesCondominioPage'
import { EstruturaReadOnlyPage } from '@/features/tenants/estrutura/EstruturaReadOnlyPage'
import { AccessDeniedPage } from '@/features/errors/AccessDeniedPage'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <RequireOperator>
            <AppLayout />
          </RequireOperator>
        }
      >
        <Route index element={<Navigate to="/condominios" replace />} />
        <Route path="condominios" element={<ListaCondominiosPage />} />
        <Route path="condominios/novo" element={<NovoCondominioPage />} />
        <Route path="condominios/:id" element={<DetalhesCondominioPage />} />
        <Route path="tenants/:condominioId/estrutura" element={<EstruturaReadOnlyPage />} />
        <Route path="erro/acesso-negado" element={<AccessDeniedPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
