import { Navigate, Route, Routes } from 'react-router-dom'
import { LoginPage } from '@/features/auth/pages/LoginPage'
import { SetupPasswordPage } from '@/features/auth/pages/SetupPasswordPage'
import { HomePage } from '@/features/home/HomePage'
import { EstruturaRoute } from '@/routes/estrutura'
import { RequireSindico } from '@/shared/auth/RequireSindico'
import { PublicLayout } from '@/shared/layouts/PublicLayout'
import { PrivateLayout } from '@/shared/layouts/PrivateLayout'

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<PublicLayout />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/password-setup" element={<SetupPasswordPage />} />
        <Route path="/setup-password" element={<SetupPasswordPage />} />
      </Route>
      <Route
        path="/"
        element={
          <RequireSindico>
            <PrivateLayout />
          </RequireSindico>
        }
      >
        <Route index element={<HomePage />} />
        <Route path="estrutura" element={<EstruturaRoute />} />
        <Route path="estrutura/:condominioId" element={<EstruturaRoute />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
