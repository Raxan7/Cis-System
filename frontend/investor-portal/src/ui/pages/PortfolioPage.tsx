import { useQuery } from '@tanstack/react-query';
import { Landmark, TrendingUp, Wallet } from 'lucide-react';
import { formatDateOnly, formatNumber } from '../../lib/format';
import { getPortalPortfolio } from '../../lib/portal-api';
import { PageIntro } from '../components/PageIntro';
import { ErrorCallout } from '../components/ErrorCallout';
import { StatCard } from '../components/StatCard';
import { StatusBadge } from '../components/StatusBadge';

export function PortfolioPage() {
  const query = useQuery({
    queryKey: ['portal', 'portfolio'],
    queryFn: getPortalPortfolio,
  });

  const portfolio = query.data;

  return (
    <div className="stack">
      <PageIntro
        eyebrow="Portfolio"
        title="Investor portfolio and capital gain"
        description="See your units, redeemable balance, latest market value, net contribution position, and estimated capital gain."
      />
      <ErrorCallout error={query.error} />
      {portfolio ? (
        <>
          <div className="stats-grid">
            <StatCard caption="Latest published values across your positions" icon={Wallet} title="Market value" value={formatNumber(portfolio.totalMarketValue)} />
            <StatCard caption="Current redeemable balance" icon={Landmark} title="Redeemable amount" value={formatNumber(portfolio.totalRedeemableAmount)} />
            <StatCard caption="Subscriptions less completed redemptions" icon={TrendingUp} title="Estimated capital gain" value={formatNumber(portfolio.estimatedCapitalGain)} />
          </div>
          <section className="panel">
            <div className="panel__header panel__header--split">
              <div>
                <h2>Portfolio summary</h2>
                <p>Units and capital are shown for the investor profile linked to this portal account.</p>
              </div>
              <StatusBadge value={portfolio.investorStatus} />
            </div>
            <div className="mini-grid">
              <article className="mini-card">
                <strong>Total units</strong>
                <p>{formatNumber(portfolio.totalUnits, 4)}</p>
              </article>
              <article className="mini-card">
                <strong>Liened units</strong>
                <p>{formatNumber(portfolio.totalLienedUnits, 4)}</p>
              </article>
              <article className="mini-card">
                <strong>Redeemable units</strong>
                <p>{formatNumber(portfolio.totalRedeemableUnits, 4)}</p>
              </article>
              <article className="mini-card">
                <strong>Net contribution</strong>
                <p>{formatNumber(portfolio.totalNetContribution)}</p>
              </article>
              <article className="mini-card">
                <strong>Latest valuation date</strong>
                <p>{formatDateOnly(portfolio.latestValuationDate)}</p>
              </article>
            </div>
          </section>
          <section className="panel">
            <div className="panel__header">
              <h2>Positions</h2>
              <p>Each position uses the latest published NAV available for its scheme class.</p>
            </div>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Scheme</th>
                    <th>Class</th>
                    <th>Units</th>
                    <th>Redeemable</th>
                    <th>Unit price</th>
                    <th>Market value</th>
                    <th>Valuation date</th>
                  </tr>
                </thead>
                <tbody>
                  {portfolio.positions.map((position) => (
                    <tr key={`${position.schemeId}:${position.schemeClassId}`}>
                      <td><strong>{position.schemeName}</strong><div>{position.schemeCode}</div></td>
                      <td><strong>{position.schemeClassName}</strong><div>{position.schemeClassCode}</div></td>
                      <td>{formatNumber(position.units, position.unitPrecision)}</td>
                      <td>{formatNumber(position.redeemableAmount)}</td>
                      <td>{formatNumber(position.unitPrice)}</td>
                      <td>{formatNumber(position.marketValue)}</td>
                      <td>{formatDateOnly(position.latestValuationDate)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      ) : null}
    </div>
  );
}
