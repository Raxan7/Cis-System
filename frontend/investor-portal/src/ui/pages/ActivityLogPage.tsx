import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { formatDate, titleCase } from '../../lib/format';
import { getPortalActivity } from '../../lib/portal-api';
import { PageIntro } from '../components/PageIntro';
import { PaginatedTable } from '../components/PaginatedTable';

export function ActivityLogPage() {
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const query = useQuery({
    queryKey: ['portal', 'activity', pageNumber, search],
    queryFn: () => getPortalActivity({
      pageNumber,
      pageSize: 20,
      search: search || undefined,
      sortBy: 'OccurredAtUtc',
      sortDirection: 'desc',
    }),
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Activity"
        title="Portal activity log"
        description="A self-service view of the activity that the backend records for your portal session and investor profile."
      />
      <PaginatedTable
        columns={[
          { key: 'occurredAtUtc', header: 'Occurred at', render: (row) => formatDate(row.occurredAtUtc) },
          { key: 'activityType', header: 'Activity', render: (row) => titleCase(row.activityType) },
          { key: 'summary', header: 'Summary', render: (row) => row.summary },
          { key: 'entityType', header: 'Entity type', render: (row) => row.entityType ?? '-' },
          { key: 'entityId', header: 'Entity id', render: (row) => row.entityId ?? '-' },
        ]}
        error={query.error}
        loading={query.isLoading}
        onNextPage={() => setPageNumber((value) => value + 1)}
        onPreviousPage={() => setPageNumber((value) => Math.max(1, value - 1))}
        onSearch={(value) => {
          setSearch(value);
          setPageNumber(1);
        }}
        pagination={query.data?.pagination ?? null}
        rows={query.data?.data ?? []}
        search={search}
        title="Activity log"
      />
    </div>
  );
}
