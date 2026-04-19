import { describe, it, expect, vi, beforeAll, afterAll, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { UploadOptInDocument } from './UploadOptInDocument'
import { validateFile } from '../uploadUtils'

const BASE = 'http://localhost/api'
const server = setupServer()

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterAll(() => server.close())
afterEach(() => { server.resetHandlers(); vi.clearAllMocks() })

function makeFile(name: string, type: string, sizeBytes: number): File {
  const file = new File([''], name, { type })
  Object.defineProperty(file, 'size', { value: sizeBytes, configurable: true })
  return file
}

function renderUpload(onUploaded = vi.fn()) {
  return render(<UploadOptInDocument condominioId="condo-1" onUploaded={onUploaded} />)
}

describe('UploadOptInDocument', () => {
  it('rejeita arquivo maior que 10 MB client-side sem chamar API', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch')
    const user = userEvent.setup()
    renderUpload()

    const bigFile = makeFile('big.pdf', 'application/pdf', 12 * 1024 * 1024)
    const input = screen.getByLabelText(/selecionar arquivo/i)
    await user.upload(input, bigFile)

    expect(screen.getByRole('alert')).toHaveTextContent(/arquivo muito grande/i)
    expect(fetchSpy).not.toHaveBeenCalled()
  })

  it('rejeita tipo de arquivo inválido client-side (validação unitária)', () => {
    const result = validateFile({ size: 1024, type: 'application/x-msdownload' })
    expect(result).toMatch(/tipo de arquivo não permitido/i)
  })

  it('aceita PDF na validação unitária', () => {
    expect(validateFile({ size: 1024, type: 'application/pdf' })).toBeNull()
  })

  it('aceita imagem jpeg na validação unitária', () => {
    expect(validateFile({ size: 1024, type: 'image/jpeg' })).toBeNull()
  })

  it('aceita PDF 500 KB e envia para API', async () => {
    const onUploaded = vi.fn()
    server.use(
      http.post(`${BASE}/v1/admin/condominios/condo-1/opt-in-documents`, () =>
        HttpResponse.json({
          id: 'doc-1',
          kind: 1,
          contentType: 'application/pdf',
          sizeBytes: 500_000,
          uploadedAt: '2026-01-01T00:00:00Z',
          originalFileName: 'ata.pdf',
        }),
      ),
    )
    const user = userEvent.setup()
    render(<UploadOptInDocument condominioId="condo-1" onUploaded={onUploaded} />)

    const validFile = makeFile('ata.pdf', 'application/pdf', 500_000)
    const input = screen.getByLabelText(/selecionar arquivo/i)
    await user.upload(input, validFile)

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /enviar documento/i }))

    await waitFor(() => {
      expect(onUploaded).toHaveBeenCalledTimes(1)
    })
  })

  it('mostra erro quando API falha', async () => {
    server.use(
      http.post(`${BASE}/v1/admin/condominios/condo-1/opt-in-documents`, () =>
        HttpResponse.json({ title: 'Error' }, { status: 500 }),
      ),
    )
    const user = userEvent.setup()
    renderUpload()

    const validFile = makeFile('ata.pdf', 'application/pdf', 100_000)
    const input = screen.getByLabelText(/selecionar arquivo/i)
    await user.upload(input, validFile)
    await user.click(screen.getByRole('button', { name: /enviar documento/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
    })
  })
})
