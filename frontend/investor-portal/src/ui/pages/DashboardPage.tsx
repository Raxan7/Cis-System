import { useQuery } from '@tanstack/react-query';
import { 
  Bell, 
  FileClock, 
  Landmark, 
  Wallet, 
  TrendingUp,
  Banknote,
} from 'lucide-react';
import { 
  getPortalActivity, 
  getPortalHoldings, 
  getPortalNotices,
  getPortalFundNav,
  getPortalPortfolio,
  getPortalKycProfile,
  type PortalPortfolioPositionDto,
  type PortalPortfolioSummaryDto,
  type PortalKycProfileDto,
} from '../../lib/portal-api';
import { formatNumber, formatDate, titleCase } from '../../lib/format';
import { loadTrackedRequests } from '../../lib/request-tracker';
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
    queryFn: () => getPortalFundNav({ pageNumber: 1, pageSize: 5 }),
    refetchInterval: 60000, // Refresh every minute
  });

  // NEW: Portfolio Query
  const portfolioQuery = useQuery({
    queryKey: ['portal', 'portfolio', 'dashboard'],
    queryFn: getPortalPortfolio,
  });

  // NEW: KYC Query
  const kycQuery = useQuery({
    queryKey: ['portal', 'kyc', 'dashboard'],
    queryFn: getPortalKycProfile,
  });

  const trackedRequests = loadTrackedRequests();
  const holdings = holdingsQuery.data?.data ?? [];
  const navData = navQuery.data?.data ?? [];
  const portfolio: PortalPortfolioSummaryDto | null = portfolioQuery.data ?? null;
  const kycData: PortalKycProfileDto | null = kycQuery.data ?? null;

  // Calculate totals
  const totalMarketValue = holdings.reduce((sum, holding) => sum + Number(holding.marketValue ?? 0), 0);
  const totalRedeemableValue = holdings.reduce((sum, holding) => sum + Number(holding.redeemableAmount ?? 0), 0);
  const totalUnits = holdings.reduce((sum, holding) => sum + Number(holding.units ?? 0), 0);
  
  // Get latest NAV (use publishedNav)
  const latestNAV = navData.length > 0 ? navData[0].publishedNav : 0;

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
          {navData.map((nav) => (
            <article className="mini-card" key={nav.schemeId}>
              <div className="mini-card__row">
                <strong>{nav.schemeName}</strong>
                <StatusBadge value={nav.schemeStatus} />
              </div>
              <p>Unit price: {formatNumber(nav.publishedUnitPrice)}</p>
              <p>NAV: {formatNumber(nav.publishedNav)}</p>
              <small>{formatDate(nav.publishedAtUtc)}</small>
            </article>
          ))}
        </div>
      </section>

      {/* Portfolio Holdings */}
      <section className="panel">
        <div className="panel__header">
          <h2>Current portfolio</h2>
          <p>Units, redeemable balance, and estimated capital gain across your active positions.</p>
        </div>
        <div className="mini-grid">
          {(portfolio?.positions ?? []).map((holding: PortalPortfolioPositionDto) => (
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

      {/* KYC Status Section */}
      <section className="panel">
        <div className="panel__header">
          <h2>KYC Status</h2>
          <p>Your Know Your Customer information</p>
        </div>
        <div className="two-column">
          <div>
            <p><strong>NIDA Number:</strong> {kycData?.identityNumber ?? 'Not provided'}</p>
            <p><strong>Phone:</strong> {kycData?.phoneNumber ?? 'Not provided'}</p>
            <p><strong>Address:</strong> {kycData?.addressLine1 ?? 'Not provided'}</p>
          </div>
          <div>
            <p><strong>Bank:</strong> {kycData?.bankAccounts?.[0]?.bankName ?? 'Not provided'}</p>
            <p><strong>Next of Kin:</strong> {kycData?.nextOfKinName ?? 'Not provided'}</p>
            <p><strong>KYC Status:</strong> <StatusBadge value={kycData?.investorStatus ?? 'Incomplete'} /></p>
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
          {trackedRequests.length === 0 ? <p>No submitted portal requests yet.</p> : trackedRequests.slice(0, 5).map((request: any) => (
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
          {(activityQuery.data?.data ?? []).map((entry: any) => (
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
