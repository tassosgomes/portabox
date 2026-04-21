import { once } from 'node:events'
import { readFileSync } from 'node:fs'
import { createServer } from 'node:http'
import { resolve } from 'node:path'
import { spawn } from 'node:child_process'

import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest'

import { ApiError } from '../src/errors'
import { apiFetch, configure } from '../src/http'
import { criarBloco } from '../src/modules/blocos'

const contractPath = resolve(import.meta.dirname, '../../../.compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml')
const originalContract = readFileSync(contractPath, 'utf8')

async function getFreePort(): Promise<number> {
  const server = createServer()
  server.listen(0, '127.0.0.1')
  await once(server, 'listening')
  const address = server.address()
  if (!address || typeof address === 'string') {
    server.close()
    throw new Error('Unable to allocate port')
  }

  const { port } = address
  server.close()
  await once(server, 'close')
  return port
}

describe('api client integration with Prism', () => {
  let prismProcess: ReturnType<typeof spawn>
  let port: number

  beforeAll(async () => {
    port = await getFreePort()
    prismProcess = spawn(
      'pnpm',
      ['exec', 'prism', 'mock', contractPath, '--port', String(port), '--host', '127.0.0.1'],
      {
        cwd: resolve(import.meta.dirname, '..'),
        stdio: ['ignore', 'pipe', 'pipe'],
      },
    )

    let started = false
    let output = ''
    prismProcess.stdout?.on('data', (chunk) => {
      output += chunk.toString()
    })
    prismProcess.stderr?.on('data', (chunk) => {
      output += chunk.toString()
    })

    for (let attempt = 0; attempt < 60; attempt += 1) {
      if (prismProcess.exitCode !== null) {
        throw new Error(`Prism exited early with code ${prismProcess.exitCode}. Output: ${output}`)
      }

      try {
        const response = await fetch(`http://127.0.0.1:${port}/api/v1/condominios/550e8400-e29b-41d4-a716-446655440000/estrutura`)
        if (response.status > 0) {
          started = true
          break
        }
      } catch {
        // Keep polling until Prism is ready.
      }

      await new Promise((resolveDelay) => setTimeout(resolveDelay, 500))
    }

    if (!started) {
      throw new Error(`Prism startup timeout. Output: ${output}`)
    }
  }, 40_000)

  afterEach(() => {
    configure({ baseUrl: `http://127.0.0.1:${port}`, getAuthToken: () => 'mock-token' })
  })

  afterAll(async () => {
    if (prismProcess.exitCode === null && !prismProcess.killed) {
      prismProcess.kill('SIGTERM')
      await once(prismProcess, 'exit')
    }
  })

  it('returns a Bloco shape for 201 Created contract mock', async () => {
    configure({ baseUrl: `http://127.0.0.1:${port}`, getAuthToken: () => 'mock-token' })

    const bloco = await criarBloco({
      condominioId: '550e8400-e29b-41d4-a716-446655440000',
      nome: 'Bloco A',
    })

    expect(bloco).toEqual(
      expect.objectContaining({
        id: expect.any(String),
        condominioId: expect.any(String),
        nome: expect.any(String),
        ativo: expect.any(Boolean),
      }),
    )
  }, 40_000)

  it('throws ApiError for conflict payload shape', async () => {
    configure({ baseUrl: `http://127.0.0.1:${port}`, getAuthToken: () => 'mock-token' })

    await expect(
      apiFetch('/condominios/550e8400-e29b-41d4-a716-446655440000/blocos', {
        method: 'POST',
        headers: {
          Prefer: 'code=409',
        },
        body: JSON.stringify({ nome: 'Bloco A' }),
      }),
    ).rejects.toMatchObject<ApiError>({
      status: 409,
      type: expect.stringContaining('problems'),
    })
  })

  it('makes contract drift visible in generated types', () => {
    expect(originalContract).toContain('Bloco:')
    expect(readFileSync(resolve(import.meta.dirname, '../src/types.ts'), 'utf8')).toContain(
      "export type Bloco = components['schemas']['Bloco']",
    )
  })
})
