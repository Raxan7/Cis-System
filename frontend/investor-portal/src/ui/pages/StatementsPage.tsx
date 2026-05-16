import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { downloadTextFile } from '../../lib/download';
import { formatDateOnly, formatNumber } from '../../lib/format';
import { getPortalStatements } from '../../lib/portal-api';
import { PageIntro } from '../components/PageIntro';
import { PaginatedTable } from '../components/PaginatedTable';

export function StatementsPage() {
  const [pageNumber, setPageNumber] = useState(1);
  const query = useQuery({
    queryKey: ['portal', 'statements', pageNumber],
    queryFn: () => getPortalStatements({ pageNumber, pageSize: 10, sortBy: 'StatementDate', sortDirection: 'desc' }),
  });

  async function handleDownload(statementId: string) {
    const response = await getPortalStatements({ pageNumber: 1, pageSize: 200, sortBy: 'StatementDate', sortDirection: 'desc' });
    const statement = response.data.find((candidate) => candidate.id === statementId);
    if (!statement) {
      return;
    }

    downloadTextFile(
      `${statement.statementReference}.txt`,
      [
        `Statement reference: ${statement.statementReference}`,
        `Statement date: ${statement.statementDate}`,
        `Holding count: ${statement.holdingCount}`,
        `Total units: ${statement.totalUnits}`,
      ].join('\n'),
    );
  }

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Statements"
        title="Account statement downloads"
        description="Every download request calls the portal backend so the document access event is logged before the file is generated locally."
      />
      <PaginatedTable
        actions={(row) => (
          <button className="button button--primary" onClick={() => void handleDownload(row.id)} type="button">
            Download
          </button>
        )}
        columns={[
          { key: 'reference', header: 'Reference', render: (row) => row.statementReference },
          { key: 'statementDate', header: 'Statement date', render: (row) => formatDateOnly(row.statementDate) },
          { key: 'holdingCount', header: 'Holdings', render: (row) => String(row.holdingCount) },
          { key: 'totalUnits', header: 'Total units', render: (row) => formatNumber(row.totalUnits, 4) },
        ]}
        error={query.error}
        loading={query.isLoading}
        onNextPage={() => setPageNumber((value) => value + 1)}
        onPreviousPage={() => setPageNumber((value) => Math.max(1, value - 1))}
        pagination={query.data?.pagination ?? null}
        rows={query.data?.data ?? []}
        title="Statements"
      />
    </div>
  );
}
