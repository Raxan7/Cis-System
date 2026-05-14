import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { AppLayout } from './layout';
import { useAuth } from './auth';
import { DashboardPage } from './pages/DashboardPage';
import { LoginPage } from './pages/LoginPage';
import { ModuleWorkspacePage } from './pages/ModuleWorkspacePage';
import { moduleRegistry } from './module-registry';
import { hasAnyPermission } from '../lib/permissions';

function ProtectedLayout() {
  const { session } = useAuth();
  const location = useLocation();

  if (!session) {
    return <Navigate replace state={{ from: location.pathname }} to="/login" />;
  }

  return <AppLayout />;
}

export function App() {
  const { session } = useAuth();

  return (
    <Routes>
      <Route element={session ? <Navigate replace to="/" /> : <LoginPage />} path="/login" />
      <Route element={<ProtectedLayout />}>
        <Route element={<DashboardPage />} path="/" />
        {moduleRegistry
          .filter((module) => module.route !== '/')
          .map((module) => (
            <Route
              element={
                session && hasAnyPermission(session.user.permissions, module.navPermission ? [module.navPermission] : [])
                  ? <ModuleWorkspacePage module={module} />
                  : <Navigate replace to="/" />
              }
              key={module.route}
              path={module.route}
            />
          ))}
      </Route>
      <Route element={<Navigate replace to={session ? '/' : '/login'} />} path="*" />
    </Routes>
  );
}
