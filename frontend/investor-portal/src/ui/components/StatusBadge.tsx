import clsx from 'clsx';

const palette: Record<string, string> = {
  approved: 'badge badge--green',
  active: 'badge badge--green',
  completed: 'badge badge--green',
  published: 'badge badge--green',
  workflowpending: 'badge badge--amber',
  pending: 'badge badge--amber',
  pendingreview: 'badge badge--amber',
  pendingapproval: 'badge badge--amber',
  submitted: 'badge badge--blue',
  open: 'badge badge--blue',
  uploaded: 'badge badge--blue',
  rejected: 'badge badge--red',
  failed: 'badge badge--red',
  suspended: 'badge badge--red',
  expired: 'badge badge--red',
  closed: 'badge badge--slate',
  draft: 'badge badge--slate',
};

export function StatusBadge({ value }: { value: unknown }) {
  const text = String(value ?? 'Unknown');
  const key = text.replace(/\s+/g, '').toLowerCase();

  return <span className={clsx('badge', palette[key] ?? 'badge badge--slate')}>{text}</span>;
}
