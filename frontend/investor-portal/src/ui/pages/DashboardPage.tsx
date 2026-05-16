import { useQuery } from '@tanstack/react-query';
import { Bell, FileClock, Landmark, Wallet } from 'lucide-react';
import { getPortalActivity, getPortalHoldings, getPortalNotices } from '../../lib/portal-api';
import { loadTrackedRequests } from '../../lib/request-tracker';
import { formatDate, formatNumber, shortId, titleCase } from '../../lib/format';
import { PageIntro } from '../components/PageIntro';
import { StatCard } from '../components/StatCard';
import { StatusBadge } from '../components/StatusBadge';

export function DashboardPage() {
  const holdingsQuery = useQuery({
    queryKey: ['portal', 'holdings', 'dashboard'],
    queryFn: () => getPortalHoldings({ pageNumber: 1, pageSize: 6 }),
  });
  const noticesQuery = useQuery({
    queryKey: ['portal', 'notices', 'dashboard'],
    queryFn: () => getPortalNotices({ pageNumber: 1, pageSize: 4 }),
  });
  const activityQuery = useQuery({
    queryKey: ['portal', 'activity', 'dashboard'],
    queryFn: () => getPortalActivity({ pageNumber: 1, pageSize: 5 }),
  });

  const trackedRequests = loadTrackedRequests();
  const holdings = holdingsQuery.data?.data ?? [];
  const totalMarketValue = holdings.reduce((sum, holding) => sum + Number(holding.marketValue ?? 0), 0);
  const totalRedeemableValue = holdings.reduce((sum, holding) => sum + Number(holding.redeemableAmount ?? 0), 0);

  return (
    <div className="stack">
      <PageIntro
        eyebrow="My dashboard"
        title="Investor overview"
        description="A quick view of your positions, recent digital requests, and portal activity."
      />
      <div className="stats-grid">
        <StatCard caption="Latest published valuation by class" icon={Wallet} title="Portfolio value" value={formatNumber(totalMarketValue)} />
        <StatCard caption="Based on redeemable units and latest price" icon={Landmark} title="Redeemable amount" value={formatNumber(totalRedeemableValue)} />
        <StatCard caption="Tracked from this portal profile" icon={FileClock} title="Open requests" value={String(trackedRequests.filter((request) => request.status.toLowerCase().includes('pending')).length)} />
        <StatCard caption="Unseen or recent updates" icon={Bell} title="Notices" value={String(noticesQuery.data?.data.length ?? 0)} />
      </div>
      <section className="panel">
        <div className="panel__header">
          <h2>Current holdings</h2>
          <p>Available units, liens, and latest value snapshots across your active positions.</p>
        </div>
        <div className="mini-grid">
          {holdings.map((holding) => (
            <article className="mini-card" key={`${holding.schemeId}:${holding.schemeClassId}`}>
              <div className="mini-card__row">
                <strong>{shortId(holding.schemeClassId)}</strong>
                <StatusBadge value={holding.marketValue ? 'Valued' : 'Pending price'} />
              </div>
              <p>Available units: {formatNumber(holding.units, holding.unitPrecision)}</p>
              <p>Liened units: {formatNumber(holding.lienedUnits, holding.unitPrecision)}</p>
              <p>Redeemable amount: {formatNumber(holding.redeemableAmount)}</p>
            </article>
          ))}
        </div>
      </section>
      <div className="two-column">
        <section className="panel">
          <div className="panel__header">
            <h2>Pending requests</h2>
            <p>Digital requests stay in pending approval until the internal workflow clears them.</p>
          </div>
          <div className="stack stack--compact">
            {trackedRequests.length === 0 ? <p>No submitted portal requests yet.</p> : trackedRequests.slice(0, 5).map((request) => (
              <article className="timeline-entry" key={request.id}>
                <div className="timeline-entry__header">
                  <strong>{titleCase(request.requestType)}</strong>
                  <StatusBadge value={request.status} />
                </div>
                <small>{formatDate(request.submittedAtUtc)}</small>
              </article>
            ))}
          </div>
        </section>
        <section className="panel">
          <div className="panel__header">
            <h2>Recent activity</h2>
            <p>Portal views, downloads, and submissions recorded on your session.</p>
          </div>
          <div className="stack stack--compact">
            {(activityQuery.data?.data ?? []).map((entry) => (
              <article className="timeline-entry" key={entry.id}>
                <div className="timeline-entry__header">
                  <strong>{titleCase(entry.activityType)}</strong>
                  <small>{formatDate(entry.occurredAtUtc)}</small>
                </div>
                <p>{entry.summary}</p>
              </article>
            ))}
          </div>
        </section>
      </div>
    </div>
  );
}
