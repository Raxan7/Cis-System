import { AlertTriangle } from 'lucide-react';
import { ApiError, problemDetailsMessage } from '../../lib/problem-details';

export function ErrorCallout({ error }: { error: unknown }) {
  if (!error) {
    return null;
  }

  const apiError = error instanceof ApiError ? error : null;
  const message = apiError ? problemDetailsMessage(apiError.problem) : error instanceof Error ? error.message : 'An unexpected error occurred.';

  return (
    <div className="callout callout--error" role="alert">
      <AlertTriangle size={16} />
      <div>
        <strong>{apiError?.problem?.title || 'Request failed'}</strong>
        <div>{message}</div>
        {apiError?.problem?.traceId ? <small>Trace ID: {apiError.problem.traceId}</small> : null}
      </div>
    </div>
  );
}
