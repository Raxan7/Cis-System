import { useQuery } from '@tanstack/react-query';
import {
  Bell,
  CircleUserRound,
  FileChartColumn,
  FileText,
  History,
  LayoutDashboard,
  LogOut,
  Receipt,
  RefreshCcw,
  ShieldCheck,
  Upload,
  Wallet,
} from 'lucide-react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { getPortalProfile, portalPermissions } from '../lib/portal-api';
import { hasPermission } from '../lib/permissions';
import { useAuth } from './auth';
import { ErrorCallout } from './components/ErrorCallout';
import { StatusBadge } from './components/StatusBadge';

const navigation = [
  { title: 'Dashboard', route: '/', icon: LayoutDashboard, permission: portalPermissions.read },
  { title: 'Holdings', route: '/holdings', icon: Wallet, permission: portalPermissions.read },
  { title: 'Transactions', route: '/transactions', icon: History, permission: portalPermissions.read },
  { title: 'Statements', route: '/statements', icon: FileText, permission: portalPermissions.read },
  { title: 'Tax certificates', route: '/tax-certificates', icon: Receipt, permission: portalPermissions.read },
  { title: 'Notices', route: '/notices', icon: Bell, permission: portalPermissions.read },
  { title: 'Request status', route: '/requests', icon: RefreshCcw, permission: portalPermissions.read },
  { title: 'Subscription', route: '/requests/subscription', icon: FileChartColumn, permission: portalPermissions.requestsCreate },
  { title: 'Redemption', route: '/requests/redemption', icon: FileChartColumn, permission: portalPermissions.requestsCreate },
  { title: 'Switch', route: '/requests/switch', icon: FileChartColumn, permission: portalPermissions.requestsCreate },
  { title: 'Profile update', route: '/requests/profile-update', icon: CircleUserRound, permission: portalPermissions.requestsCreate },
  { title: 'Documents', route: '/documents', icon: Upload, permission: portalPermissions.documentsUpload },
  { title: 'Activity', route: '/activity', icon: ShieldCheck, permission: portalPermissions.read },
];

export function AppLayout() {
  const { session, logout } = useAuth();
  const location = useLocation();
  const profileQuery = useQuery({
    queryKey: ['portal', 'me'],
    queryFn: getPortalProfile,
  });

  const visibleNavigation = navigation.filter((item) => hasPermission(session?.user.permissions ?? [], item.permission));

  return (
    <div className="portal-shell">
      <aside className="portal-sidebar">
        <div className="portal-brand">
          <div className="portal-brand__logo">VF</div>
          <div>
            <strong>Victory CIS</strong>
            <p>Investor Portal</p>
          </div>
        </div>
        <nav className="portal-nav">
          {visibleNavigation.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink className={({ isActive }) => `portal-nav__link${isActive ? ' active' : ''}`} key={item.route} to={item.route}>
                <Icon size={16} />
                <span>{item.title}</span>
              </NavLink>
            );
          })}
        </nav>
      </aside>
      <div className="portal-main">
        <header className="portal-topbar">
          <div>
            <h2>{visibleNavigation.find((item) => item.route === location.pathname)?.title ?? 'Investor Portal'}</h2>
            <p>{session?.user.displayName} · {session?.user.email}</p>
          </div>
          <button className="button button--ghost" onClick={() => void logout()} type="button">
            <LogOut size={16} />
            Sign out
          </button>
        </header>
        <main className="portal-content">
          {profileQuery.isLoading ? (
            <section className="panel"><p>Loading investor profile...</p></section>
          ) : null}
          {profileQuery.error ? (
            <section className="panel stack">
              <div className="panel__header">
                <h2>Portal access needs attention</h2>
                <p>This session is authenticated, but the investor profile still needs a valid portal state and MFA policy compliance.</p>
              </div>
              <ErrorCallout error={profileQuery.error} />
              <div className="inline-actions">
                <button className="button button--primary" onClick={() => void logout()} type="button">
                  Return to sign in
                </button>
              </div>
            </section>
          ) : null}
          {profileQuery.data ? (
            <>
              <section className="profile-ribbon">
                <div>
                  <span className="eyebrow">Investor number</span>
                  <strong>{profileQuery.data.investorNumber}</strong>
                </div>
                <div>
                  <span className="eyebrow">Status</span>
                  <StatusBadge value={profileQuery.data.investorStatus} />
                </div>
                <div>
                  <span className="eyebrow">MFA</span>
                  <StatusBadge value={profileQuery.data.mfaSatisfied ? 'Active' : 'Required'} />
                </div>
              </section>
              <Outlet />
            </>
          ) : null}
        </main>
      </div>
    </div>
  );
}
