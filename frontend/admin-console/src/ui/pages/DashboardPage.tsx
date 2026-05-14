import { useQueries } from '@tanstack/react-query';
import {
  AlertCircle,
  BarChart3,
  BookOpenText,
  FileCheck2,
  Landmark,
  ShieldAlert,
  Users,
} from 'lucide-react';
import { getList, getRecord } from '../../lib/http';
import { permissions, hasAnyPermission } from '../../lib/permissions';
import { useAuth } from '../auth';
import { ErrorCallout } from '../components/ErrorCallout';
import { StatCard } from '../components/StatCard';

export function DashboardPage() {
  const { session } = useAuth();
  const userPermissions = session?.user.permissions ?? [];

  const results = useQueries({
    queries: [
      {
        queryKey: ['dashboard', 'users'],
        enabled: hasAnyPermission(userPermissions, [permissions.identityUsersRead]),
        queryFn: () => getList('/api/users'),
      },
      {
        queryKey: ['dashboard', 'workflows'],
        enabled: hasAnyPermission(userPermissions, [permissions.workflowRead]),
        queryFn: () => getList('/api/workflows/pending'),
      },
      {
        queryKey: ['dashboard', 'schemes'],
        enabled: hasAnyPermission(userPermissions, [permissions.schemesRead]),
        queryFn: () => getList('/api/schemes'),
      },
      {
        queryKey: ['dashboard', 'investors'],
        enabled: hasAnyPermission(userPermissions, [permissions.investorsRead]),
        queryFn: () => getList('/api/investors'),
      },
      {
        queryKey: ['dashboard', 'aml'],
        enabled: hasAnyPermission(userPermissions, [permissions.investorsAmlRead]),
        queryFn: () => getList('/api/aml/exceptions'),
      },
      {
        queryKey: ['dashboard', 'breaches'],
        enabled: hasAnyPermission(userPermissions, [permissions.complianceRead]),
        queryFn: () => getList('/api/compliance/breaches'),
      },
      {
        queryKey: ['dashboard', 'reports'],
        enabled: hasAnyPermission(userPermissions, [permissions.reportsRead]),
        queryFn: () => getList('/api/reports/runs'),
      },
      {
        queryKey: ['dashboard', 'health'],
        enabled: hasAnyPermission(userPermissions, [permissions.operationsHealthRead]),
        queryFn: () => getRecord('/api/operations/health/deep'),
      },
    ],
  });

  const firstError = results.find((result) => result.error)?.error;
  const health = results[7]?.data as Record<string, unknown> | undefined;

  return (
    <div className="stack">
      <section className="page-header">
        <div>
          <span className="eyebrow">Operational overview</span>
          <h2>Dashboard</h2>
          <p>High-signal monitoring across onboarding, workflow queues, compliance exceptions, and production readiness.</p>
        </div>
      </section>
      <ErrorCallout error={firstError} />
      <div className="stats-grid">
        <StatCard title="Users" value={String((results[0]?.data as unknown[] | undefined)?.length ?? 0)} caption="Active identities visible to your role." icon={Users} />
        <StatCard title="Pending approvals" value={String((results[1]?.data as unknown[] | undefined)?.length ?? 0)} caption="Workflow items awaiting controlled action." icon={FileCheck2} />
        <StatCard title="Schemes" value={String((results[2]?.data as unknown[] | undefined)?.length ?? 0)} caption="Configured products and classes." icon={Landmark} />
        <StatCard title="Investors" value={String((results[3]?.data as unknown[] | undefined)?.length ?? 0)} caption="Onboarding and maintenance population." icon={Users} />
        <StatCard title="AML exceptions" value={String((results[4]?.data as unknown[] | undefined)?.length ?? 0)} caption="Unresolved AML, PEP, or sanctions hits." icon={ShieldAlert} />
        <StatCard title="Compliance breaches" value={String((results[5]?.data as unknown[] | undefined)?.length ?? 0)} caption="Open limit and liquidity exceptions." icon={AlertCircle} />
        <StatCard title="Report runs" value={String((results[6]?.data as unknown[] | undefined)?.length ?? 0)} caption="Published and in-flight reporting activity." icon={BookOpenText} />
        <StatCard title="Deep health" value={String(health?.status ?? 'Unknown')} caption="Database, storage, and background jobs." icon={BarChart3} />
      </div>
      {health ? (
        <section className="panel">
          <header className="panel__header">
            <div>
              <h3>Operations health</h3>
              <p>Backend dependency readiness as reported by the secured health endpoint.</p>
            </div>
          </header>
          <div className="health-grid">
            {Array.isArray(health.entries)
              ? health.entries.map((entry) => {
                  const record = entry as Record<string, unknown>;
                  return (
                    <div className="health-card" key={String(record.name)}>
                      <strong>{String(record.name)}</strong>
                      <p>{String(record.status)}</p>
                      <small>{String(record.description ?? '')}</small>
                    </div>
                  );
                })
              : null}
          </div>
        </section>
      ) : null}
    </div>
  );
}
