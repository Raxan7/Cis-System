import { useQuery } from '@tanstack/react-query';
import { formatDate, titleCase } from '../../lib/format';
import { getPortalActivity } from '../../lib/portal-api';
import { loadTrackedRequests } from '../../lib/request-tracker';
import { PageIntro } from '../components/PageIntro';
import { StatusBadge } from '../components/StatusBadge';

function summarizePayload(payloadJson: string) {
  try {
    const payload = JSON.parse(payloadJson) as Record<string, unknown>;
    return Object.entries(payload)
      .slice(0, 4)
      .map(([key, value]) => `${titleCase(key)}: ${String(value)}`)
      .join(' · ');
  } catch {
    return payloadJson;
  }
}

export function RequestStatusPage() {
  const trackedRequests = loadTrackedRequests();
  const activityQuery = useQuery({
    queryKey: ['portal', 'activity', 'request-status'],
    queryFn: () => getPortalActivity({ pageNumber: 1, pageSize: 50, sortBy: 'OccurredAtUtc', sortDirection: 'desc' }),
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Request tracking"
        title="Digital request status"
        description="Portal submissions enter approval workflow immediately and remain pending until the internal reviewer chain completes."
      />
      <section className="panel">
        <div className="panel__header">
          <h2>Submitted requests</h2>
          <p>Tracked from workflow-backed submission responses stored for this portal session.</p>
        </div>
        <div className="stack stack--compact">
          {trackedRequests.length === 0 ? <p>No digital requests have been submitted from this portal yet.</p> : trackedRequests.map((request) => (
            <article className="timeline-entry" key={request.id}>
              <div className="timeline-entry__header">
                <strong>{titleCase(request.requestType)}</strong>
                <StatusBadge value={request.status} />
              </div>
              <p>{summarizePayload(request.requestPayloadJson)}</p>
              <small>{formatDate(request.submittedAtUtc)}</small>
            </article>
          ))}
        </div>
      </section>
      <section className="panel">
        <div className="panel__header">
          <h2>Related activity</h2>
          <p>Submission and document activity recorded on the backend for your investor profile.</p>
        </div>
        <div className="stack stack--compact">
          {(activityQuery.data?.data ?? [])
            .filter((entry) => entry.entityType === 'DigitalServiceRequest' || entry.activityType.toLowerCase().includes('request') || entry.activityType.toLowerCase().includes('document'))
            .map((entry) => (
              <article className="timeline-entry" key={entry.id}>
                <div className="timeline-entry__header">
                  <strong>{titleCase(entry.activityType)}</strong>
                  <small>{formatDate(entry.occurredAtUtc)}</small>
                </div>
                <p>{entry.summary}</p>
              </article>
            ))}
        </div>
      </section>
    </div>
  );
}
