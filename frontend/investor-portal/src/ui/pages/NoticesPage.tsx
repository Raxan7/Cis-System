import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { formatDate, formatDateOnly } from '../../lib/format';
import { getPortalNotices } from '../../lib/portal-api';
import { PageIntro } from '../components/PageIntro';
import { PaginatedTable } from '../components/PaginatedTable';

export function NoticesPage() {
  const [pageNumber, setPageNumber] = useState(1);
  const query = useQuery({
    queryKey: ['portal', 'notices', pageNumber],
    queryFn: () => getPortalNotices({ pageNumber, pageSize: 10, sortBy: 'PublishedAtUtc', sortDirection: 'desc' }),
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Notices"
        title="Investor notices"
        description="Stay current on fund notices, service messages, and investor-facing updates published into your portal feed."
      />
      <PaginatedTable
        columns={[
          { key: 'title', header: 'Title', render: (row) => <strong>{row.title}</strong> },
          { key: 'publishedDate', header: 'Published date', render: (row) => formatDateOnly(row.publishedDate) },
          { key: 'publishedAtUtc', header: 'Published at', render: (row) => formatDate(row.publishedAtUtc) },
          { key: 'body', header: 'Notice', render: (row) => <span>{row.body}</span> },
        ]}
        error={query.error}
        loading={query.isLoading}
        onNextPage={() => setPageNumber((value) => value + 1)}
        onPreviousPage={() => setPageNumber((value) => Math.max(1, value - 1))}
        pagination={query.data?.pagination ?? null}
        rows={query.data?.data ?? []}
        title="Notices"
      />
    </div>
  );
}
