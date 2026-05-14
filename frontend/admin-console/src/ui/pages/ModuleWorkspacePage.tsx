import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toRequestPayload } from '../../lib/forms';
import { hasAnyPermission } from '../../lib/permissions';
import type { ModuleDefinition, RowAction, SectionMutation, SectionQuery } from '../../lib/module-types';
import { useAuth } from '../auth';
import { AuditDrawer } from '../components/AuditDrawer';
import { DataTable } from '../components/DataTable';
import { ErrorCallout } from '../components/ErrorCallout';
import { FormSection } from '../components/FormSection';

function QuerySection({ definition }: { definition: SectionQuery }) {
  const { session } = useAuth();
  const queryClient = useQueryClient();
  const [queryValues, setQueryValues] = useState<Record<string, string>>(definition.defaultQuery ?? {});
  const [auditTarget, setAuditTarget] = useState<{ entityType?: string; entityId?: string }>({});
  const [actionError, setActionError] = useState<unknown>(null);
  const canView = hasAnyPermission(
    session?.user.permissions ?? [],
    definition.requiredPermission ? [definition.requiredPermission] : [],
  );

  const query = useQuery({
    queryKey: ['module', definition.key, queryValues],
    queryFn: () => definition.fetch(queryValues),
    enabled: !definition.queryFields,
  });

  const rows = useMemo(() => {
    const value = query.data;
    if (!value) {
      return [];
    }

    return Array.isArray(value) ? value : [value];
  }, [query.data]);

  if (!canView) {
    return null;
  }

  return (
    <div className="stack">
      {definition.queryFields ? (
        <section className="panel">
          <header className="panel__header">
            <div>
              <h3>{definition.title}</h3>
              {definition.description ? <p>{definition.description}</p> : null}
            </div>
          </header>
          <form
            className="form-grid"
            onSubmit={async (event) => {
              event.preventDefault();
              await query.refetch();
            }}
          >
            {definition.queryFields.map((field) => (
              <label className="field" key={field.name}>
                <span>{field.label}</span>
                <input
                  placeholder={field.placeholder}
                  type={
                    field.type === 'date' || field.type === 'time' || field.type === 'datetime-local'
                      ? field.type
                      : 'text'
                  }
                  value={queryValues[field.name] ?? ''}
                  onChange={(event) =>
                    setQueryValues((current) => ({ ...current, [field.name]: event.target.value }))
                  }
                />
              </label>
            ))}
            <div className="form-actions">
              <button className="button button--primary" type="submit">
                Load
              </button>
            </div>
          </form>
        </section>
      ) : null}
      <ErrorCallout error={actionError ?? query.error} />
      {definition.type === 'table' ? (
        <DataTable
          columns={definition.columns ?? []}
          description={definition.description}
          loading={query.isLoading}
          onAudit={(row) => {
            const entityType =
              typeof definition.entityType === 'function' ? definition.entityType(row) : definition.entityType;
            const entityId = definition.entityId?.(row);
            setAuditTarget({ entityType, entityId });
          }}
          onRowAction={async (action: RowAction, row) => {
            try {
              const note = window.prompt(`${action.label}: add an optional note or reason`, '') ?? '';
              await action.run(row, note);
              setActionError(null);
              await queryClient.invalidateQueries({ queryKey: ['module', definition.key] });
              await query.refetch();
            } catch (error) {
              setActionError(error);
            }
          }}
          rowActions={definition.rowActions}
          rows={rows}
          title={definition.title}
          userPermissions={session?.user.permissions ?? []}
        />
      ) : (
        <section className="panel">
          <header className="panel__header">
            <div>
              <h3>{definition.title}</h3>
              {definition.description ? <p>{definition.description}</p> : null}
            </div>
          </header>
          <pre className="record-preview">{JSON.stringify(query.data ?? {}, null, 2)}</pre>
        </section>
      )}
      <AuditDrawer
        entityId={auditTarget.entityId}
        entityType={auditTarget.entityType}
        onClose={() => setAuditTarget({})}
      />
    </div>
  );
}

function MutationSection({ definition }: { definition: SectionMutation }) {
  const { session } = useAuth();
  const queryClient = useQueryClient();
  const [mutationError, setMutationError] = useState<unknown>(null);
  const canRun = hasAnyPermission(
    session?.user.permissions ?? [],
    definition.permission ? [definition.permission] : [],
  );

  const mutation = useMutation({
    mutationFn: definition.submit,
    onSuccess: async () => {
      setMutationError(null);
      await queryClient.invalidateQueries({ queryKey: ['module'] });
    },
    onError: (error) => setMutationError(error),
  });

  if (!canRun) {
    return null;
  }

  return (
    <FormSection
      description={definition.description}
      error={mutationError ?? mutation.error}
      fields={definition.fields}
      isSubmitting={mutation.isPending}
      onSubmit={async (values) => {
        await mutation.mutateAsync(toRequestPayload(values, definition.fields));
      }}
      schema={definition.schema}
      submitLabel={definition.submitLabel}
      title={definition.title}
    />
  );
}

export function ModuleWorkspacePage({ module }: { module: ModuleDefinition }) {
  return (
    <div className="stack">
      <section className="page-header">
        <div>
          <span className="eyebrow">{module.featureLabel ?? 'Administration module'}</span>
          <h2>{module.title}</h2>
          <p>{module.description}</p>
        </div>
      </section>
      {module.mutations?.map((mutationDefinition) => (
        <MutationSection definition={mutationDefinition} key={mutationDefinition.key} />
      ))}
      {module.queries?.map((queryDefinition) => (
        <QuerySection definition={queryDefinition} key={queryDefinition.key} />
      ))}
    </div>
  );
}
