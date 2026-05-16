import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { downloadTextFile } from '../../lib/download';
import { formatDateOnly } from '../../lib/format';
import { getPortalTaxCertificates } from '../../lib/portal-api';
import { PageIntro } from '../components/PageIntro';
import { PaginatedTable } from '../components/PaginatedTable';

export function TaxCertificatesPage() {
  const [pageNumber, setPageNumber] = useState(1);
  const query = useQuery({
    queryKey: ['portal', 'tax-certificates', pageNumber],
    queryFn: () => getPortalTaxCertificates({ pageNumber, pageSize: 10, sortBy: 'CertificateDate', sortDirection: 'desc' }),
  });

  async function handleDownload(certificateId: string) {
    const response = await getPortalTaxCertificates({ pageNumber: 1, pageSize: 200, sortBy: 'CertificateDate', sortDirection: 'desc' });
    const certificate = response.data.find((candidate) => candidate.id === certificateId);
    if (!certificate) {
      return;
    }

    downloadTextFile(
      `${certificate.certificateReference}.txt`,
      [
        `Certificate reference: ${certificate.certificateReference}`,
        `Certificate date: ${certificate.certificateDate}`,
        `Tax number: ${certificate.taxNumber}`,
        `Tax residence: ${certificate.countryOfTaxResidence}`,
      ].join('\n'),
    );
  }

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Tax documents"
        title="Tax certificate downloads"
        description="Download investor tax references while keeping access events logged through the backend portal service."
      />
      <PaginatedTable
        actions={(row) => (
          <button className="button button--primary" onClick={() => void handleDownload(row.id)} type="button">
            Download
          </button>
        )}
        columns={[
          { key: 'reference', header: 'Reference', render: (row) => row.certificateReference },
          { key: 'certificateDate', header: 'Certificate date', render: (row) => formatDateOnly(row.certificateDate) },
          { key: 'taxNumber', header: 'Tax number', render: (row) => row.taxNumber },
          { key: 'country', header: 'Tax residence', render: (row) => row.countryOfTaxResidence },
        ]}
        error={query.error}
        loading={query.isLoading}
        onNextPage={() => setPageNumber((value) => value + 1)}
        onPreviousPage={() => setPageNumber((value) => Math.max(1, value - 1))}
        pagination={query.data?.pagination ?? null}
        rows={query.data?.data ?? []}
        title="Tax certificates"
      />
    </div>
  );
}
