import { NavLink, Outlet } from 'react-router-dom';
import { LogOut } from 'lucide-react';
import { useAuth } from './auth';
import { moduleRegistry } from './module-registry';
import { hasAnyPermission } from '../lib/permissions';

export function AppLayout() {
  const { session, logout } = useAuth();
  const visibleModules = moduleRegistry.filter((module) =>
    hasAnyPermission(session?.user.permissions ?? [], module.navPermission ? [module.navPermission] : []),
  );

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="sidebar__brand">
          <div className="sidebar__logo">VF</div>
          <div>
            <strong>Victory CIS</strong>
            <p>Admin Console</p>
          </div>
        </div>
        <nav className="sidebar__nav">
          {visibleModules.map((module) => {
            const Icon = module.icon;
            return (
              <NavLink className="sidebar__link" key={module.route} to={module.route}>
                <Icon size={16} />
                <span>{module.title}</span>
              </NavLink>
            );
          })}
        </nav>
      </aside>
      <div className="content">
        <header className="topbar">
          <div>
            <h1>Collective Investment Scheme Management</h1>
            <p>
              {session?.user.displayName} - {session?.user.roles.join(', ')}
            </p>
          </div>
          <button className="button button--ghost" onClick={() => void logout()} type="button">
            <LogOut size={16} />
            Sign out
          </button>
        </header>
        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
