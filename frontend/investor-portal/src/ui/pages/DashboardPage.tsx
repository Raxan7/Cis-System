import { useQuery } from '@tanstack/react-query';
import { 
  Bell, 
  FileClock, 
  Landmark, 
  Wallet, 
  TrendingUp, 
  ArrowUpRight, 
  ArrowDownRight, 
  RefreshCw,
  Banknote,
  Users
} from 'lucide-react';
import { 
  getPortalActivity, 
  getPortalHoldings, 
  getPortalNotices,
  getPortalNAV,
  getPortalPortfolio,
  getPortalKYC
} from '../../lib/portal-api';
import { loadTrackedRequests } from '../../lib/request-tracker';
import { formatDate, formatNumber, shortId, titleCase } from '../../lib/format';
import { PageIntro } from '../components/PageIntro';
import { StatCard } from '../components/StatCard';
import { StatusBadge } from '../components/StatusBadge';

export function DashboardPage() {
  // Existing queries
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

  // NEW: NAV Query
  const navQuery = useQuery({
    queryKey: ['portal', 'nav', 'dashboard'],
    queryFn: () => getPortalNAV({ pageNumber: 1, pageSize: 5 }),
    refetchInterval: 60000, // Refresh every minute
  });

  // NEW: Portfolio Query
  const portfolioQuery = useQuery({
    queryKey: ['portal', 'portfolio', 'dashboard'],
    queryFn: () => getPortalPortfolio({ pageNumber: 1, pageSize: 5 }),
  });

  // NEW: KYC Query
  const kycQuery = useQuery({
    queryKey: ['portal', 'kyc', 'dashboard'],
    queryFn: getPortalKYC,
  });

  const trackedRequests = loadTrackedRequests();
  const holdings = holdingsQuery.data?.data ?? [];
  const navData = navQuery.data?.data ?? [];
  const portfolioData = portfolioQuery.data?.data ?? [];
  const kycData = kycQuery.data ?? {};

  // Calculate totals
  const totalMarketValue = holdings.reduce((sum, holding) => sum + Number(holding.marketValue ?? 0), 0);
  const totalRedeemableValue = holdings.reduce((sum, holding) => sum + Number(holding.redeemableAmount ?? 0), 0);
  const totalUnits = holdings.reduce((sum, holding) => sum + Number(holding.units ?? 0), 0);
  
  // Get latest NAV
  const latestNAV = navData.length > 0 ? navData[0].nav : 0;
  const navChange = navData.length > 0 ? navData[0].change : 0;

  return (
    <div className="stack">
      <PageIntro
        eyebrow="My dashboard"
        title="Investor overview"
        description="A quick view of your positions, NAV, portfolio, and KYC status."
      />

      {/* Stats Row */}
      <div className="stats-grid">
        <StatCard 
          caption="Latest published valuation by class" 
          icon={Wallet} 
          title="Portfolio value" 
          value={formatNumber(totalMarketValue)} 
        />
        <StatCard 
          caption="Based on redeemable units and latest price" 
          icon={Landmark} 
          title="Redeemable amount" 
          value={formatNumber(totalRedeemableValue)} 
        />
        <StatCard 
          caption="Total units across all funds" 
          icon={Banknote} 
          title="Total Units" 
          value={formatNumber(totalUnits, 2)} 
        />
        <StatCard 
          caption="Latest NAV per unit" 
          icon={TrendingUp} 
          title="Current NAV" 
          value={formatNumber(latestNAV)} 
        />
        <StatCard 
          caption="Tracked from this portal profile" 
          icon={FileClock} 
          title="Open requests" 
          value={String(trackedRequests.filter((request) => request.status.toLowerCase().includes('pending')).length)} 
        />
        <StatCard 
          caption="Unseen or recent updates" 
          icon={Bell} 
          title="Notices" 
          value={String(noticesQuery.data?.data.length ?? 0)} 
        />
      </div>

      {/* NAV Information Section */}
      <section className="panel">
        <div className="panel__header">
          <h2>NAV Information</h2>
          <p>Net Asset Value for all funds</p>
        </div>
        <div className="mini-grid">
          {navData.map((nav: any) => (
            <article className="mini-card" key={nav.fundId}>
              <div className="mini-card__row">
                <strong>{nav.fundName}</strong>
                <StatusBadge value={nav.change >= 0 ? 'Up' : 'Down'} />
              </div>
              <p>NAV: TZS {formatNumber(nav.nav)}</p>
              <p>Change: <span style={{ color: nav.change >= 0 ? '#156646' : '#9e2c2c' }}>
                {nav.change >= 0 ? '+' : ''}{nav.change}%
              </span></p>
              <small>{formatDate(nav.date)}</small>
            </article>
          ))}
        </div>
      </section>

      {/* Portfolio Holdings */}
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

      {/* KYC Status Section */}
      <section className="panel">
        <div className="panel__header">
          <h2>KYC Status</h2>
          <p>Your Know Your Customer information</p>
        </div>
        <div className="two-column">
          <div>
            <p><strong>NIDA Number:</strong> {kycData.nidaNumber || 'Not provided'}</p>
            <p><strong>Phone:</strong> {kycData.phoneNumber || 'Not provided'}</p>
            <p><strong>Address:</strong> {kycData.address || 'Not provided'}</p>
          </div>
          <div>
            <p><strong>Bank:</strong> {kycData.bankName || 'Not provided'}</p>
            <p><strong>Next of Kin:</strong> {kycData.nextOfKinName || 'Not provided'}</p>
            <p><strong>KYC Status:</strong> <StatusBadge value={kycData.status || 'Incomplete'} /></p>
          </div>
        </div>
        <div style={{ marginTop: '1rem' }}>
          <a href="/kyc" className="button button--primary">Update KYC</a>
        </div>
      </section>

      {/* Pending Requests */}
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

      {/* Recent Activity */}
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
