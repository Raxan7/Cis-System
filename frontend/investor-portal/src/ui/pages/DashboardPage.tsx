import { useQuery } from '@tanstack/react-query';
import { Bell, FileClock, Landmark, Wallet } from 'lucide-react';
import { getPortalActivity, getPortalFundNav, getPortalNotices, getPortalPortfolio } from '../../lib/portal-api';
import { formatDate, formatNumber, titleCase } from '../../lib/format';
import { loadTrackedRequests } from '../../lib/request-tracker';
import { PageIntro } from '../components/PageIntro';
import { StatCard } from '../components/StatCard';
import { StatusBadge } from '../components/StatusBadge';

export function DashboardPage() {
  const portfolioQuery = useQuery({
    queryKey: ['portal', 'portfolio', 'dashboard'],
    queryFn: getPortalPortfolio,
  });
  const fundNavQuery = useQuery({
    queryKey: ['portal', 'fund-nav', 'dashboard'],
    queryFn: () => getPortalFundNav({ pageNumber: 1, pageSize: 4, sortBy: 'PublishedAtUtc', sortDirection: 'desc' }),
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
  const portfolio = portfolioQuery.data;

  return (
    <div className="stack">
      <PageIntro
        eyebrow="My dashboard"
        title="Investor overview"
        description="A quick view of published fund prices, your portfolio position, pending requests, and recent activity."
      />
      <div className="stats-grid">
        <StatCard caption="Latest published valuation by class" icon={Wallet} title="Portfolio value" value={formatNumber(portfolio?.totalMarketValue)} />
        <StatCard caption="Based on redeemable units and latest price" icon={Landmark} title="Redeemable amount" value={formatNumber(portfolio?.totalRedeemableAmount)} />
        <StatCard caption="Tracked from this portal profile" icon={FileClock} title="Open requests" value={String(trackedRequests.filter((request) => request.status.toLowerCase().includes('pending')).length)} />
        <StatCard caption="Unseen or recent updates" icon={Bell} title="Notices" value={String(noticesQuery.data?.data.length ?? 0)} />
      </div>
      <section className="panel">
        <div className="panel__header">
          <h2>Current portfolio</h2>
          <p>Units, redeemable balance, and estimated capital gain across your active positions.</p>
        </div>
        <div className="mini-grid">
          {(portfolio?.positions ?? []).map((holding) => (
            <article className="mini-card" key={`${holding.schemeId}:${holding.schemeClassId}`}>
              <div className="mini-card__row">
                <strong>{holding.schemeClassName}</strong>
                <StatusBadge value={holding.marketValue ? 'Valued' : 'Pending price'} />
              </div>
              <p>{holding.schemeName}</p>
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
            <h2>Latest fund NAVs</h2>
            <p>Published fund pricing snapshots available for subscriptions, switches, and valuation checks.</p>
          </div>
          <div className="stack stack--compact">
            {(fundNavQuery.data?.data ?? []).map((fund) => (
              <article className="timeline-entry" key={`${fund.schemeId}:${fund.schemeClassId}`}>
                <div className="timeline-entry__header">
                  <strong>{fund.schemeName} / {fund.schemeClassName}</strong>
                  <small>{formatDate(fund.publishedAtUtc)}</small>
                </div>
                <p>Unit price {formatNumber(fund.publishedUnitPrice)} - NAV {formatNumber(fund.publishedNav)}</p>
              </article>
            ))}
          </div>
        </section>
      </div>
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
  );
}
