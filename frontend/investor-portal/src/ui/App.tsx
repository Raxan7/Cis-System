import type { ReactElement } from 'react';
import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { portalPermissions } from '../lib/portal-api';
import { hasPermission } from '../lib/permissions';
import { useAuth } from './auth';
import { AppLayout } from './layout';
import { ActivityLogPage } from './pages/ActivityLogPage';
import { DashboardPage } from './pages/DashboardPage';
import { DocumentUploadPage } from './pages/DocumentUploadPage';
import { FundNavPage } from './pages/FundNavPage';
import { HoldingsPage } from './pages/HoldingsPage';
import { KycProfilePage } from './pages/KycProfilePage';
import { LoginPage } from './pages/LoginPage';
import { NoticesPage } from './pages/NoticesPage';
import { PortfolioPage } from './pages/PortfolioPage';
import { ProfileUpdateRequestPage } from './pages/ProfileUpdateRequestPage';
import { RedemptionRequestPage } from './pages/RedemptionRequestPage';
import { RequestStatusPage } from './pages/RequestStatusPage';
import { StatementsPage } from './pages/StatementsPage';
import { SubscriptionRequestPage } from './pages/SubscriptionRequestPage';
import { SwitchRequestPage } from './pages/SwitchRequestPage';
import { TaxCertificatesPage } from './pages/TaxCertificatesPage';
import { TransferRequestPage } from './pages/TransferRequestPage';
import { TransactionsPage } from './pages/TransactionsPage';

// NEW PAGES FOR INVESTOR PORTAL
import { NAVPage } from './pages/NAVPage';
import { PortfolioPage } from './pages/PortfolioPage';
import { BuyDepositPage } from './pages/BuyDepositPage';
import { TransferPage } from './pages/TransferPage';
import { WithdrawalPage } from './pages/WithdrawalPage';
import { UnitsPage } from './pages/UnitsPage';
import { CapitalGainPage } from './pages/CapitalGainPage';
import { KYCPage } from './pages/KYCPage';

function ProtectedLayout() {
  const { session } = useAuth();
  const location = useLocation();

  if (!session) {
    return <Navigate replace state={{ from: location.pathname }} to="/login" />;
  }

  return <AppLayout />;
}

function PermissionRoute({
  permission,
  element,
}: {
  permission: string;
  element: ReactElement;
}) {
  const { session } = useAuth();
  if (!session || !hasPermission(session.user.permissions, permission)) {
    return <Navigate replace to="/" />;
  }

  return element;
}

export function App() {
  const { session } = useAuth();

  return (
    <Routes>
      <Route element={session ? <Navigate replace to="/" /> : <LoginPage />} path="/login" />
      <Route element={<ProtectedLayout />}>
        {/* Existing Routes */}
        <Route element={<DashboardPage />} path="/" />
        <Route element={<PermissionRoute element={<FundNavPage />} permission={portalPermissions.read} />} path="/fund-nav" />
        <Route element={<PermissionRoute element={<PortfolioPage />} permission={portalPermissions.read} />} path="/portfolio" />
        <Route element={<PermissionRoute element={<HoldingsPage />} permission={portalPermissions.read} />} path="/holdings" />
        <Route element={<PermissionRoute element={<TransactionsPage />} permission={portalPermissions.read} />} path="/transactions" />
        <Route element={<PermissionRoute element={<StatementsPage />} permission={portalPermissions.read} />} path="/statements" />
        <Route element={<PermissionRoute element={<TaxCertificatesPage />} permission={portalPermissions.read} />} path="/tax-certificates" />
        <Route element={<PermissionRoute element={<NoticesPage />} permission={portalPermissions.read} />} path="/notices" />
        <Route element={<PermissionRoute element={<KycProfilePage />} permission={portalPermissions.read} />} path="/kyc" />
        <Route element={<PermissionRoute element={<RequestStatusPage />} permission={portalPermissions.read} />} path="/requests" />
        <Route element={<PermissionRoute element={<SubscriptionRequestPage />} permission={portalPermissions.requestsCreate} />} path="/requests/subscription" />
        <Route element={<PermissionRoute element={<RedemptionRequestPage />} permission={portalPermissions.requestsCreate} />} path="/requests/redemption" />
        <Route element={<PermissionRoute element={<SwitchRequestPage />} permission={portalPermissions.requestsCreate} />} path="/requests/switch" />
        <Route element={<PermissionRoute element={<TransferRequestPage />} permission={portalPermissions.requestsCreate} />} path="/requests/transfer" />
        <Route element={<PermissionRoute element={<ProfileUpdateRequestPage />} permission={portalPermissions.requestsCreate} />} path="/requests/profile-update" />
        <Route element={<PermissionRoute element={<DocumentUploadPage />} permission={portalPermissions.documentsUpload} />} path="/documents" />
        <Route element={<PermissionRoute element={<ActivityLogPage />} permission={portalPermissions.read} />} path="/activity" />

        {/* NEW ROUTES FOR INVESTOR PORTAL */}
        <Route element={<PermissionRoute element={<NAVPage />} permission={portalPermissions.read} />} path="/nav" />
        <Route element={<PermissionRoute element={<PortfolioPage />} permission={portalPermissions.read} />} path="/portfolio" />
        <Route element={<PermissionRoute element={<BuyDepositPage />} permission={portalPermissions.requestsCreate} />} path="/buy-deposit" />
        <Route element={<PermissionRoute element={<TransferPage />} permission={portalPermissions.requestsCreate} />} path="/transfer" />
        <Route element={<PermissionRoute element={<WithdrawalPage />} permission={portalPermissions.requestsCreate} />} path="/withdrawal" />
        <Route element={<PermissionRoute element={<UnitsPage />} permission={portalPermissions.read} />} path="/units" />
        <Route element={<PermissionRoute element={<CapitalGainPage />} permission={portalPermissions.read} />} path="/capital-gain" />
        <Route element={<PermissionRoute element={<KYCPage />} permission={portalPermissions.read} />} path="/kyc" />
      </Route>
      <Route element={<Navigate replace to={session ? '/' : '/login'} />} path="*" />
    </Routes>
  );
}
