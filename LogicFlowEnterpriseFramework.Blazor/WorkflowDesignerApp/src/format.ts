export function formatDate(value?: string | null) {
  if (!value) return '-'
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function formatValue(value?: string | null) {
  if (!value) return '-'
  try {
    return String(JSON.parse(value))
  } catch {
    return value
  }
}

export function toUtcIso(value: string) {
  return value ? new Date(value).toISOString() : undefined
}
