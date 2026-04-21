import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { execFile } from 'node:child_process'
import { promisify } from 'node:util'

import { describe, expect, it } from 'vitest'

const execFileAsync = promisify(execFile)

describe('generate:types', () => {
  it('is deterministic when re-run', async () => {
    const packageDir = resolve(import.meta.dirname, '..')
    const generatedPath = resolve(packageDir, 'src/generated.ts')
    const before = readFileSync(generatedPath, 'utf8')

    await execFileAsync('pnpm', ['generate:types'], { cwd: packageDir })

    const after = readFileSync(generatedPath, 'utf8')
    expect(after).toBe(before)
  }, 120_000)
})
