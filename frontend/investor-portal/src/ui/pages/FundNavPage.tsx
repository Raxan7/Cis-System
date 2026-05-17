import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { formatDate, formatDateOnly, formatNumber } from '../../lib/format';
import { getPortalFundNav } from '../../lib/portal-api';
import { PageIntro } from '../components/PageIntro';
import { PaginatedTable } from '../components/PaginatedTable';
import { StatusBadge } from '../components/StatusBadge';

export function FundNavPage() {
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const query = useQuery({
    queryKey: ['portal', 'fund-nav', pageNumber, search],
    queryFn: () => getPortalFundNav({
      pageNumber,
      pageSize: 12,
      search: search || undefined,
      sortBy: 'PublishedAtUtc',
      sortDirection: 'desc',
    }),
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Fund NAV"
        title="Published NAV across all funds"
        description="Review the latest published NAV and unit price for each scheme class available in the system."
      />
      <PaginatedTable
        columns={[
          { key: 'scheme', header: 'Scheme', render: (row) => <div><strong>{row.schemeName}</strong><div>{row.schemeCode}</div></div> },
          { key: 'class', header: 'Class', render: (row) => <div><strong>{row.schemeClassName}</strong><div>{row.schemeClassCode}</div></div> },
          { key: 'currency', header: 'Currency', render: (row) => row.currency },
          { key: 'unitPrice', header: 'Unit price', render: (row) => formatNumber(row.publishedUnitPrice) },
          { key: 'nav', header: 'Published NAV', render: (row) => formatNumber(row.publishedNav) },
          { key: 'valuationDate', header: 'Valuation date', render: (row) => formatDateOnly(row.valuationDate) },
          { key: 'publishedAtUtc', header: 'Published', render: (row) => formatDate(row.publishedAtUtc) },
          { key: 'frequency', header: 'Frequency', render: (row) => `${row.valuationFrequency} / ${row.dealingFrequency}` },
          { key: 'status', header: 'Scheme status', render: (row) => <StatusBadge value={row.schemeStatus} /> },
        ]}
        description="Only published NAV versions are displayed here."
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
        title="Fund NAV"
      />
    </div>
  );
}
