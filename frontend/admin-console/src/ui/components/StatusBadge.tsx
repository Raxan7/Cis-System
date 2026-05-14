import clsx from 'clsx';

const palette: Record<string, string> = {
  approved: 'badge badge--green',
  active: 'badge badge--green',
  completed: 'badge badge--green',
  healthy: 'badge badge--green',
  published: 'badge badge--green',
  pending: 'badge badge--amber',
  pendingreview: 'badge badge--amber',
  pendingapproval: 'badge badge--amber',
  draft: 'badge badge--slate',
  submitted: 'badge badge--blue',
  checked: 'badge badge--blue',
  open: 'badge badge--amber',
  running: 'badge badge--blue',
  locked: 'badge badge--red',
  rejected: 'badge badge--red',
  failed: 'badge badge--red',
  suspended: 'badge badge--red',
  closed: 'badge badge--slate',
  exception: 'badge badge--red',
};

export function StatusBadge({ value }: { value: unknown }) {
  const text = String(value ?? 'Unknown');
  const key = text.replace(/\s+/g, '').toLowerCase();

  return <span className={clsx('badge', palette[key] ?? 'badge badge--slate')}>{text}</span>;
}
