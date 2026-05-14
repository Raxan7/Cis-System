import { useQuery } from '@tanstack/react-query';
import { X } from 'lucide-react';
import { getList } from '../../lib/http';
import { ErrorCallout } from './ErrorCallout';
import { StatusBadge } from './StatusBadge';

type AuditDrawerProps = {
  entityType?: string;
  entityId?: string;
  onClose: () => void;
};

export function AuditDrawer({ entityType, entityId, onClose }: AuditDrawerProps) {
  const enabled = Boolean(entityType && entityId);
  const query = useQuery({
    enabled,
    queryKey: ['audit-entity', entityType, entityId],
    queryFn: () => getList(`/api/audit-logs/entity/${entityType}/${entityId}`),
  });

  if (!enabled) {
    return null;
  }

  return (
    <aside className="drawer">
      <header className="drawer__header">
        <div>
          <h3>Audit trail</h3>
          <p>
            {entityType} / {entityId}
          </p>
        </div>
        <button className="button button--ghost" onClick={onClose} type="button">
          <X size={16} />
        </button>
      </header>
      <ErrorCallout error={query.error} />
      <div className="drawer__content">
        {query.isLoading ? <p>Loading audit events...</p> : null}
        {(query.data ?? []).map((entry) => (
          <article className="timeline-entry" key={String(entry.id)}>
            <div className="timeline-entry__header">
              <strong>{String(entry.action ?? entry.eventType ?? 'Audit event')}</strong>
              <StatusBadge value={entry.eventType ?? entry.action} />
            </div>
            <p>{String(entry.reason ?? entry.comment ?? entry.summary ?? 'No commentary supplied.')}</p>
            <small>{String(entry.timestampUtc ?? entry.occurredAtUtc ?? '')}</small>
          </article>
        ))}
      </div>
    </aside>
  );
}
