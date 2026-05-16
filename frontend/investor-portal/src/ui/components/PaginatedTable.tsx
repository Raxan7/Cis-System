import type { ReactNode } from 'react';
import { Search } from 'lucide-react';
import { ErrorCallout } from './ErrorCallout';

type Column<T> = {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
};

type PaginatedTableProps<T> = {
  title: string;
  description?: string;
  rows: T[];
  columns: Column<T>[];
  loading?: boolean;
  error?: unknown;
  search?: string;
  onSearch?: (value: string) => void;
  pagination?: {
    pageNumber: number;
    totalPages: number;
    totalCount: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
  } | null;
  onPreviousPage?: () => void;
  onNextPage?: () => void;
  actions?: (row: T) => ReactNode;
};

export function PaginatedTable<T>({
  title,
  description,
  rows,
  columns,
  loading,
  error,
  search,
  onSearch,
  pagination,
  onPreviousPage,
  onNextPage,
  actions,
}: PaginatedTableProps<T>) {
  return (
    <section className="panel">
      <header className="panel__header panel__header--split">
        <div>
          <h2>{title}</h2>
          {description ? <p>{description}</p> : null}
        </div>
        {onSearch ? (
          <label className="search-box">
            <Search size={14} />
            <input
              placeholder="Filter"
              value={search ?? ''}
              onChange={(event) => onSearch(event.target.value)}
            />
          </label>
        ) : null}
      </header>
      <ErrorCallout error={error} />
      <div className="table-wrap">
        <table className="data-table">
          <thead>
            <tr>
              {columns.map((column) => (
                <th key={column.key}>{column.header}</th>
              ))}
              {actions ? <th>Actions</th> : null}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td className="table-empty" colSpan={columns.length + (actions ? 1 : 0)}>
                  Loading...
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td className="table-empty" colSpan={columns.length + (actions ? 1 : 0)}>
                  No records found.
                </td>
              </tr>
            ) : rows.map((row, index) => (
              <tr key={index}>
                {columns.map((column) => (
                  <td key={column.key}>{column.render(row)}</td>
                ))}
                {actions ? <td><div className="inline-actions">{actions(row)}</div></td> : null}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {pagination ? (
        <footer className="table-footer">
          <span>{pagination.totalCount} record{pagination.totalCount === 1 ? '' : 's'}</span>
          <div className="pagination">
            <button className="button button--ghost" disabled={!pagination.hasPreviousPage} onClick={onPreviousPage} type="button">
              Previous
            </button>
            <span>Page {pagination.pageNumber} of {Math.max(pagination.totalPages, 1)}</span>
            <button className="button button--ghost" disabled={!pagination.hasNextPage} onClick={onNextPage} type="button">
              Next
            </button>
          </div>
        </footer>
      ) : null}
    </section>
  );
}
