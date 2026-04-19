import { describe, it, expect } from 'vitest'
import {
  formatCnpj,
  validateCnpj,
  formatCpf,
  validateCpf,
  formatCep,
  validateEmail,
  validateE164,
  isDateNotFuture,
} from './validation'

describe('formatCnpj', () => {
  it('returns raw digits when fewer than 3', () => {
    expect(formatCnpj('11')).toBe('11')
  })
  it('formats 14 digits correctly', () => {
    expect(formatCnpj('11222333000181')).toBe('11.222.333/0001-81')
  })
  it('strips non-digits before formatting', () => {
    expect(formatCnpj('11.222.333/0001-81')).toBe('11.222.333/0001-81')
  })
  it('truncates beyond 14 digits', () => {
    expect(formatCnpj('112223330001819999')).toBe('11.222.333/0001-81')
  })
})

describe('validateCnpj', () => {
  it('accepts a valid CNPJ', () => {
    expect(validateCnpj('11.222.333/0001-81')).toBe(true)
  })
  it('accepts digits-only input', () => {
    expect(validateCnpj('11222333000181')).toBe(true)
  })
  it('rejects wrong check digits', () => {
    expect(validateCnpj('11.222.333/0001-00')).toBe(false)
  })
  it('rejects all-same digits', () => {
    expect(validateCnpj('11111111111111')).toBe(false)
  })
  it('rejects fewer than 14 digits', () => {
    expect(validateCnpj('1234567')).toBe(false)
  })
  it('rejects empty string', () => {
    expect(validateCnpj('')).toBe(false)
  })
})

describe('formatCpf', () => {
  it('returns raw digits when fewer than 4', () => {
    expect(formatCpf('529')).toBe('529')
  })
  it('formats 11 digits correctly', () => {
    expect(formatCpf('52998224725')).toBe('529.982.247-25')
  })
  it('strips non-digits before formatting', () => {
    expect(formatCpf('529.982.247-25')).toBe('529.982.247-25')
  })
})

describe('validateCpf', () => {
  it('accepts a valid CPF', () => {
    expect(validateCpf('529.982.247-25')).toBe(true)
  })
  it('accepts digits-only input', () => {
    expect(validateCpf('52998224725')).toBe(true)
  })
  it('rejects wrong check digits', () => {
    expect(validateCpf('529.982.247-00')).toBe(false)
  })
  it('rejects all-same digits', () => {
    expect(validateCpf('11111111111')).toBe(false)
  })
  it('rejects empty string', () => {
    expect(validateCpf('')).toBe(false)
  })
})

describe('formatCep', () => {
  it('formats 8 digits with hyphen', () => {
    expect(formatCep('01310100')).toBe('01310-100')
  })
  it('returns partial digits without hyphen', () => {
    expect(formatCep('0131')).toBe('0131')
  })
})

describe('validateEmail', () => {
  it('accepts valid email', () => {
    expect(validateEmail('sindico@example.com.br')).toBe(true)
  })
  it('rejects missing @', () => {
    expect(validateEmail('sindicoexample.com')).toBe(false)
  })
  it('rejects missing TLD', () => {
    expect(validateEmail('sindico@example')).toBe(false)
  })
  it('rejects empty string', () => {
    expect(validateEmail('')).toBe(false)
  })
})

describe('validateE164', () => {
  it('accepts valid Brazilian mobile E.164', () => {
    expect(validateE164('+5511999999999')).toBe(true)
  })
  it('rejects without leading +', () => {
    expect(validateE164('5511999999999')).toBe(false)
  })
  it('rejects too short', () => {
    expect(validateE164('+551199')).toBe(false)
  })
  it('rejects non-digit after country code', () => {
    expect(validateE164('+55abc')).toBe(false)
  })
  it('rejects empty string', () => {
    expect(validateE164('')).toBe(false)
  })
})

describe('isDateNotFuture', () => {
  it('returns true for today', () => {
    const now = new Date()
    const today = [
      now.getFullYear(),
      String(now.getMonth() + 1).padStart(2, '0'),
      String(now.getDate()).padStart(2, '0'),
    ].join('-')
    expect(isDateNotFuture(today)).toBe(true)
  })
  it('returns true for past date', () => {
    expect(isDateNotFuture('2020-01-01')).toBe(true)
  })
  it('returns false for future date', () => {
    const future = new Date(Date.now() + 86_400_000).toISOString().slice(0, 10)
    expect(isDateNotFuture(future)).toBe(false)
  })
  it('returns false for empty string', () => {
    expect(isDateNotFuture('')).toBe(false)
  })
})
