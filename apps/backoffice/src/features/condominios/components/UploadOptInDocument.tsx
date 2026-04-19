import { useRef, useState } from 'react'
import { Button } from '@portabox/ui'
import { UploadCloud } from '@portabox/ui'
import { uploadOptInDocument, ApiHttpError } from '../api'
import { validateFile } from '../uploadUtils'
import styles from './UploadOptInDocument.module.css'

const KIND_OPTIONS = [
  { value: 1, label: 'Ata de assembleia' },
  { value: 2, label: 'Termo de opt-in' },
  { value: 99, label: 'Outro documento' },
]

interface Props {
  condominioId: string
  onUploaded: () => void
}

export function UploadOptInDocument({ condominioId, onUploaded }: Props) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [kind, setKind] = useState<number>(1)
  const [error, setError] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const [dragging, setDragging] = useState(false)


  function handleFileChange(f: File) {
    const err = validateFile(f)
    if (err) {
      setError(err)
      setFile(null)
    } else {
      setError(null)
      setFile(f)
    }
  }

  function handleInputChange(e: React.ChangeEvent<HTMLInputElement>) {
    const f = e.target.files?.[0]
    if (f) handleFileChange(f)
  }

  function handleDrop(e: React.DragEvent<HTMLDivElement>) {
    e.preventDefault()
    setDragging(false)
    const f = e.dataTransfer.files?.[0]
    if (f) handleFileChange(f)
  }

  async function handleUpload() {
    if (!file) return
    setUploading(true)
    setError(null)
    try {
      await uploadOptInDocument(condominioId, file, kind)
      setFile(null)
      if (inputRef.current) inputRef.current.value = ''
      onUploaded()
    } catch (err) {
      if (err instanceof ApiHttpError) {
        setError('Erro ao enviar documento. Tente novamente.')
      } else {
        setError('Erro inesperado. Tente novamente.')
      }
    } finally {
      setUploading(false)
    }
  }

  return (
    <div className={styles.container}>
      <div
        className={[styles.dropzone, dragging ? styles.dragover : ''].filter(Boolean).join(' ')}
        onDragOver={(e) => { e.preventDefault(); setDragging(true) }}
        onDragLeave={() => setDragging(false)}
        onDrop={handleDrop}
        onClick={() => inputRef.current?.click()}
        role="button"
        tabIndex={0}
        aria-label="Área de upload de documento"
        onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') inputRef.current?.click() }}
      >
        <UploadCloud size={32} aria-hidden="true" className={styles.icon} />
        {file ? (
          <span className={styles.fileName}>{file.name}</span>
        ) : (
          <span className={styles.hint}>
            Arraste um arquivo ou clique para selecionar (PDF ou imagem, máx. 10 MB)
          </span>
        )}
      </div>

      <input
        ref={inputRef}
        type="file"
        accept=".pdf,image/*"
        className={styles.hiddenInput}
        aria-label="Selecionar arquivo"
        onChange={handleInputChange}
      />

      <div className={styles.row}>
        <label className={styles.label} htmlFor="doc-kind">
          Tipo de documento
        </label>
        <select
          id="doc-kind"
          className={styles.select}
          value={kind}
          onChange={(e) => setKind(Number(e.target.value))}
        >
          {KIND_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      {error && (
        <p role="alert" className={styles.error}>
          {error}
        </p>
      )}

      {uploading && (
        <progress aria-label="Enviando documento..." className={styles.progress} />
      )}

      <Button
        variant="secondary"
        onClick={handleUpload}
        disabled={!file || uploading}
        loading={uploading}
      >
        Enviar documento
      </Button>
    </div>
  )
}
