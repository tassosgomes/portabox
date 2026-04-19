export function formatCnpj(value: string): string {
  const d = value.replace(/\D/g, '').slice(0, 14)
  if (d.length <= 2) return d
  if (d.length <= 5) return `${d.slice(0, 2)}.${d.slice(2)}`
  if (d.length <= 8) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5)}`
  if (d.length <= 12) return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8)}`
  return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`
}

export function validateCnpj(cnpj: string): boolean {
  const d = cnpj.replace(/\D/g, '')
  if (d.length !== 14 || /^(\d)\1{13}$/.test(d)) return false
  const calc = (nums: string, weights: number[]): number => {
    const s = [...nums].reduce((a, c, i) => a + +c * weights[i], 0)
    const r = s % 11
    return r < 2 ? 0 : 11 - r
  }
  return (
    calc(d.slice(0, 12), [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) === +d[12] &&
    calc(d.slice(0, 13), [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) === +d[13]
  )
}

export function formatCpf(value: string): string {
  const d = value.replace(/\D/g, '').slice(0, 11)
  if (d.length <= 3) return d
  if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`
  if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`
}

export function validateCpf(cpf: string): boolean {
  const d = cpf.replace(/\D/g, '')
  if (d.length !== 11 || /^(\d)\1{10}$/.test(d)) return false
  const calc = (nums: string, weight: number): number => {
    const s = [...nums].reduce((a, c, i) => a + +c * (weight - i), 0)
    const r = s % 11
    return r < 2 ? 0 : 11 - r
  }
  return calc(d.slice(0, 9), 10) === +d[9] && calc(d.slice(0, 10), 11) === +d[10]
}

export function formatCep(value: string): string {
  const d = value.replace(/\D/g, '').slice(0, 8)
  return d.length > 5 ? `${d.slice(0, 5)}-${d.slice(5)}` : d
}

export function validateEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(email.trim())
}

export function validateE164(phone: string): boolean {
  return /^\+[1-9]\d{7,14}$/.test(phone.trim())
}

export function isDateNotFuture(dateStr: string): boolean {
  if (!dateStr) return false
  const [y, m, day] = dateStr.split('-').map(Number)
  const date = new Date(y, m - 1, day)
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  return date <= today
}
