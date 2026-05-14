export function formatValue(
  value: unknown,
  kind: 'text' | 'status' | 'date' | 'datetime' | 'money' | 'json' | 'array' = 'text',
) {
  if (value === null || value === undefined || value === '') {
    return 'N/A';
  }

  if (kind === 'date') {
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(String(value)));
  }

  if (kind === 'datetime') {
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(String(value)));
  }

  if (kind === 'money') {
    const amount = Number(value);
    return Number.isFinite(amount)
      ? amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
      : String(value);
  }

  if (kind === 'array' && Array.isArray(value)) {
    return value.join(', ');
  }

  if (kind === 'json') {
    return JSON.stringify(value, null, 2);
  }

  if (typeof value === 'object') {
    return JSON.stringify(value);
  }

  return String(value);
}

export function toTitleCase(value: string) {
  return value
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/[_-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/\b\w/g, (match) => match.toUpperCase());
}
