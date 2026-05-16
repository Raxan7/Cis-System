import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { formatDateOnly, formatNumber, shortId, titleCase } from '../../lib/format';
import { getPortalTransactions } from '../../lib/portal-api';
import { PageIntro } from '../components/PageIntro';
import { PaginatedTable } from '../components/PaginatedTable';
import { StatusBadge } from '../components/StatusBadge';

export function TransactionsPage() {
  const [search, setSearch] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const query = useQuery({
    queryKey: ['portal', 'transactions', pageNumber, search],
    queryFn: () => getPortalTransactions({
      pageNumber,
      pageSize: 10,
      search: search || undefined,
      sortBy: 'BusinessDate',
      sortDirection: 'desc',
    }),
  });

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Transaction history"
        title="Instruction and unit movement history"
        description="Subscriptions, redemptions, switches, and unit register movements visible on your investor account."
      />
      <PaginatedTable
        columns={[
          { key: 'reference', header: 'Reference', render: (row) => row.reference },
          { key: 'type', header: 'Type', render: (row) => titleCase(row.type) },
          { key: 'source', header: 'Source', render: (row) => row.source },
          { key: 'businessDate', header: 'Business date', render: (row) => formatDateOnly(row.businessDate) },
          { key: 'amount', header: 'Amount', render: (row) => formatNumber(row.amount) },
          { key: 'units', header: 'Units', render: (row) => formatNumber(row.units, 4) },
          { key: 'schemeClass', header: 'Scheme class', render: (row) => shortId(row.schemeClassId) },
          { key: 'status', header: 'Status', render: (row) => <StatusBadge value={row.status ?? 'Posted'} /> },
        ]}
        description="Both dealing instructions and posted unit-register entries are consolidated here."
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
        title="Transactions"
      />
    </div>
  );
}
