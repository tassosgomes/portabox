export const MAX_UPLOAD_SIZE_BYTES = 10 * 1024 * 1024 // 10 MB

export function validateFile(f: { size: number; type: string }): string | null {
  if (f.size > MAX_UPLOAD_SIZE_BYTES) return 'Arquivo muito grande. O limite é 10 MB.'
  if (f.type !== 'application/pdf' && !f.type.startsWith('image/')) {
    return 'Tipo de arquivo não permitido. Use PDF ou imagem.'
  }
  return null
}
