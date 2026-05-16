export const sessionChangeEventName = 'cis:portal-session-changed';

const ACCESS_TOKEN_KEY = 'cis.portal.accessToken';
const REFRESH_TOKEN_KEY = 'cis.portal.refreshToken';
const USER_KEY = 'cis.portal.user';

export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
  status: string;
  mfaEnabled: boolean;
  roles: string[];
  permissions: string[];
};

export type StoredSession = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
  user: AuthUser;
};

function getStorage() {
  return typeof window === 'undefined' ? null : window.sessionStorage;
}

function notifyChange() {
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new Event(sessionChangeEventName));
  }
}

export function loadSession(): StoredSession | null {
  const storage = getStorage();
  if (!storage) {
    return null;
  }

  const accessToken = storage.getItem(ACCESS_TOKEN_KEY);
  const refreshToken = storage.getItem(REFRESH_TOKEN_KEY);
  const userJson = storage.getItem(USER_KEY);
  const accessTokenExpiresAtUtc = storage.getItem(`${ACCESS_TOKEN_KEY}:expires`);
  const refreshTokenExpiresAtUtc = storage.getItem(`${REFRESH_TOKEN_KEY}:expires`);

  if (!accessToken || !refreshToken || !userJson || !accessTokenExpiresAtUtc || !refreshTokenExpiresAtUtc) {
    return null;
  }

  try {
    const user = JSON.parse(userJson) as AuthUser;
    return {
      accessToken,
      refreshToken,
      accessTokenExpiresAtUtc,
      refreshTokenExpiresAtUtc,
      user,
    };
  } catch {
    clearSession();
    return null;
  }
}

export function persistSession(session: StoredSession) {
  const storage = getStorage();
  if (!storage) {
    return;
  }

  storage.setItem(ACCESS_TOKEN_KEY, session.accessToken);
  storage.setItem(REFRESH_TOKEN_KEY, session.refreshToken);
  storage.setItem(`${ACCESS_TOKEN_KEY}:expires`, session.accessTokenExpiresAtUtc);
  storage.setItem(`${REFRESH_TOKEN_KEY}:expires`, session.refreshTokenExpiresAtUtc);
  storage.setItem(USER_KEY, JSON.stringify(session.user));
  notifyChange();
}

export function clearSession() {
  const storage = getStorage();
  if (!storage) {
    return;
  }

  storage.removeItem(ACCESS_TOKEN_KEY);
  storage.removeItem(REFRESH_TOKEN_KEY);
  storage.removeItem(`${ACCESS_TOKEN_KEY}:expires`);
  storage.removeItem(`${REFRESH_TOKEN_KEY}:expires`);
  storage.removeItem(USER_KEY);
  notifyChange();
}
