declare global {
  interface Window {
    __ENV__?: Record<string, string | undefined>
  }
}

type ImportMetaWithEnv = ImportMeta & {
  env?: Record<string, string | undefined>
}

export function getRuntimeEnv(key: string): string | undefined {
  if (typeof window !== 'undefined') {
    const fromWindow = window.__ENV__?.[key]
    if (fromWindow) {
      return fromWindow
    }
  }

  const fromImportMeta = (import.meta as ImportMetaWithEnv).env?.[key]
  if (fromImportMeta) {
    return fromImportMeta
  }

  if (typeof process !== 'undefined' && process.env?.[key]) {
    return process.env[key]
  }

  return undefined
}
