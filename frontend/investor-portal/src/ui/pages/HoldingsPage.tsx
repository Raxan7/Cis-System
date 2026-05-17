import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { formatDateOnly, formatNumber } from '../../lib/format';
import { getPortalHoldings } from '../../lib/portal-api';
import { PageIntro } from '../components/PageIntro';
import { PaginatedTable } from '../components/PaginatedTable';

export function HoldingsPage() {
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const query = useQuery({
    queryKey: ['portal', 'holdings', pageNumber, search],
    queryFn: () => getPortalHoldings({
      pageNumber,
      pageSize: 10,
      search: search || undefined,
      sortBy: 'LastMovementDate',
      sortDirection: 'desc',
    }),
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="My holdings"
        title="Holdings and valuation"
        description="See available units, liens, redeemable balance, and the latest NAV-linked market values for your investor profile."
      />
      <PaginatedTable
        columns={[
          { key: 'scheme', header: 'Scheme', render: (row) => <div><strong>{row.schemeName}</strong><div>{row.schemeCode}</div></div> },
          { key: 'class', header: 'Class', render: (row) => <div><strong>{row.schemeClassName}</strong><div>{row.schemeClassCode}</div></div> },
          { key: 'currency', header: 'Currency', render: (row) => row.currency },
          { key: 'units', header: 'Available units', render: (row) => formatNumber(row.units, row.unitPrecision) },
          { key: 'lienedUnits', header: 'Liened units', render: (row) => formatNumber(row.lienedUnits, row.unitPrecision) },
          { key: 'redeemableUnits', header: 'Redeemable units', render: (row) => formatNumber(row.redeemableUnits, row.unitPrecision) },
          { key: 'unitPrice', header: 'Latest unit price', render: (row) => formatNumber(row.unitPrice) },
          { key: 'marketValue', header: 'Market / NAV value', render: (row) => formatNumber(row.marketValue) },
          { key: 'redeemableAmount', header: 'Redeemable amount', render: (row) => formatNumber(row.redeemableAmount) },
          { key: 'latestValuationDate', header: 'Valuation date', render: (row) => formatDateOnly(row.latestValuationDate) },
          { key: 'lastMovementDate', header: 'Last movement', render: (row) => formatDateOnly(row.lastMovementDate) },
        ]}
        description="Values are based on the latest published NAV available for each scheme class."
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
        title="Holdings"
      />
    </div>
  );
}
