import { describe, it, expect } from 'vitest'
import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join, extname } from 'node:path'

function collectFiles(dir: string, exts: string[]): string[] {
  const results: string[] = []
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry)
    const stat = statSync(full)
    if (stat.isDirectory() && entry !== 'node_modules' && !entry.startsWith('.')) {
      results.push(...collectFiles(full, exts))
    } else if (exts.includes(extname(entry))) {
      results.push(full)
    }
  }
  return results
}

// import.meta.dirname is the absolute path to the directory of this file (Node >=21)
const SRC_DIR = import.meta.dirname

describe('No hardcoded color values in component source', () => {
  it('TSX files do not contain hardcoded hex colors outside of test files', () => {
    const tsxFiles = collectFiles(SRC_DIR, ['.tsx'])
    const violations: string[] = []

    for (const file of tsxFiles) {
      if (file.includes('.test.')) continue
      const content = readFileSync(file, 'utf-8')
      const lines = content.split('\n')
      lines.forEach((line, i) => {
        if (line.trim().startsWith('//') || line.trim().startsWith('*')) return
        const varPattern = /var\(--[^)]+\)/g
        const stripped = line.replace(varPattern, '')
        const hexPattern = /#[0-9a-fA-F]{3,8}\b/g
        if (hexPattern.test(stripped)) {
          violations.push(`${file}:${i + 1}: ${line.trim()}`)
        }
      })
    }

    expect(violations).toEqual([])
  })

  it('CSS module files do not contain hardcoded hex colors (badge status colors exempted)', () => {
    const cssFiles = collectFiles(SRC_DIR, ['.css'])
    const violations: string[] = []

    for (const file of cssFiles) {
      if (file.includes('Badge.module')) continue
      const content = readFileSync(file, 'utf-8')
      const lines = content.split('\n')
      lines.forEach((line, i) => {
        if (line.trim().startsWith('/*') || line.trim().startsWith('*')) return
        const hexPattern = /#[0-9a-fA-F]{3,8}[^-\w]/g
        if (hexPattern.test(line)) {
          violations.push(`${file}:${i + 1}: ${line.trim()}`)
        }
      })
    }

    // Allow rgba() color values used in shadow tokens
    expect(violations.filter((v) => !v.includes('rgba('))).toEqual([])
  })
})
