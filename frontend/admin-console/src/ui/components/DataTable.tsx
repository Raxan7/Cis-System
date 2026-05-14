import { useMemo, useState } from 'react';
import { Search } from 'lucide-react';
import { formatValue, toTitleCase } from '../../lib/format';
import type { ColumnDescriptor, RowAction } from '../../lib/module-types';
import { StatusBadge } from './StatusBadge';

type DataTableProps = {
  title: string;
  description?: string;
  rows: Record<string, unknown>[];
  columns: ColumnDescriptor[];
  loading?: boolean;
  rowActions?: RowAction[];
  onAudit?: (row: Record<string, unknown>) => void;
  onRowAction?: (action: RowAction, row: Record<string, unknown>) => Promise<void>;
  userPermissions: string[];
};

export function DataTable({
  title,
  description,
  rows,
  columns,
  loading,
  rowActions = [],
  onAudit,
  onRowAction,
  userPermissions,
}: DataTableProps) {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const pageSize = 8;
  const actionColumnCount = rowActions.length > 0 || onAudit ? 1 : 0;

  const filteredRows = useMemo(
    () => rows.filter((row) => JSON.stringify(row).toLowerCase().includes(search.toLowerCase())),
    [rows, search],
  );
  const pageCount = Math.max(1, Math.ceil(filteredRows.length / pageSize));
  const pagedRows = filteredRows.slice((page - 1) * pageSize, page * pageSize);

  return (
    <section className="panel">
      <header className="panel__header panel__header--split">
        <div>
          <h3>{title}</h3>
          {description ? <p>{description}</p> : null}
        </div>
        <label className="search-box">
          <Search size={14} />
          <input
            placeholder="Filter rows"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />
        </label>
      </header>
      <div className="table-wrap">
        <table className="data-table">
          <thead>
            <tr>
              {columns.map((column) => (
                <th key={column.key}>{column.label}</th>
              ))}
              {actionColumnCount > 0 ? <th>Actions</th> : null}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={columns.length + actionColumnCount} className="table-empty">
                  Loading...
                </td>
              </tr>
            ) : pagedRows.length === 0 ? (
              <tr>
                <td colSpan={columns.length + actionColumnCount} className="table-empty">
                  No records match the current filter.
                </td>
              </tr>
            ) : (
              pagedRows.map((row, index) => (
                <tr key={String(row.id ?? row.code ?? row.name ?? index)}>
                  {columns.map((column) => {
                    const value = row[column.key];
                    return (
                      <td key={column.key}>
                        {column.kind === 'status' ? <StatusBadge value={value} /> : formatValue(value, column.kind)}
                      </td>
                    );
                  })}
                  {actionColumnCount > 0 ? (
                    <td>
                      <div className="inline-actions">
                        {rowActions
                          .filter(
                            (action) =>
                              !action.permission ||
                              userPermissions.includes(action.permission) ||
                              userPermissions.includes('*'),
                          )
                          .map((action) => (
                            <button
                              className="button button--ghost"
                              key={action.label}
                              onClick={async () => {
                                if (onRowAction) {
                                  await onRowAction(action, row);
                                  return;
                                }

                                const note =
                                  window.prompt(`${action.label}: add an optional note or reason`, '') ?? '';
                                await action.run(row, note);
                              }}
                              type="button"
                            >
                              {action.label}
                            </button>
                          ))}
                        {onAudit ? (
                          <button className="button button--ghost" onClick={() => onAudit(row)} type="button">
                            Audit
                          </button>
                        ) : null}
                      </div>
                    </td>
                  ) : null}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      <footer className="table-footer">
        <span>
          {filteredRows.length} row{filteredRows.length === 1 ? '' : 's'}
        </span>
        <div className="pagination">
          <button
            className="button button--ghost"
            disabled={page <= 1}
            onClick={() => setPage((value) => Math.max(1, value - 1))}
            type="button"
          >
            Previous
          </button>
          <span>{toTitleCase(`page ${page} of ${pageCount}`)}</span>
          <button
            className="button button--ghost"
            disabled={page >= pageCount}
            onClick={() => setPage((value) => Math.min(pageCount, value + 1))}
            type="button"
          >
            Next
          </button>
        </div>
      </footer>
    </section>
  );
}
