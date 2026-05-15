import { clearSession, loadSession, persistSession, type StoredSession } from './auth-storage';
import { ApiError, isProblemDetails, problemDetailsMessage, type ProblemDetails } from './problem-details';

type ApiEnvelope<T> = {
  data: T;
  meta: {
    correlationId?: string | null;
    timestampUtc: string;
  };
};

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT';
  body?: unknown;
  headers?: Record<string, string>;
  query?: Record<string, string | number | boolean | null | undefined>;
  raw?: boolean;
};

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL
  ?? (import.meta.env.DEV
    ? 'http://localhost:8080'
    : 'https://cis-api-n4ryz2zhlq-uc.a.run.app');
let inMemorySession: StoredSession | null = loadSession();

function serializeQuery(query?: RequestOptions['query']) {
  const search = new URLSearchParams();
  Object.entries(query ?? {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value));
    }
  });
  const queryString = search.toString();
  return queryString ? `?${queryString}` : '';
}

function buildUrl(path: string, query?: RequestOptions['query']) {
  const normalized = path.startsWith('http') ? path : `${API_BASE_URL}${path}`;
  return `${normalized}${serializeQuery(query)}`;
}

async function parseJson(response: Response) {
  const text = await response.text();
  if (!text) {
    return undefined;
  }

  try {
    return JSON.parse(text) as unknown;
  } catch {
    return text;
  }
}

async function refreshSession() {
  if (!inMemorySession?.refreshToken) {
    clearSession();
    inMemorySession = null;
    return null;
  }

  const response = await fetch(buildUrl('/api/auth/refresh'), {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ refreshToken: inMemorySession.refreshToken }),
  });

  if (!response.ok) {
    clearSession();
    inMemorySession = null;
    return null;
  }

  const payload = (await parseJson(response)) as ApiEnvelope<{
    accessToken: string;
    accessTokenExpiresAtUtc: string;
    refreshToken: string;
    refreshTokenExpiresAtUtc: string;
    user: StoredSession['user'];
  }>;

  const nextSession: StoredSession = {
    accessToken: payload.data.accessToken,
    accessTokenExpiresAtUtc: payload.data.accessTokenExpiresAtUtc,
    refreshToken: payload.data.refreshToken,
    refreshTokenExpiresAtUtc: payload.data.refreshTokenExpiresAtUtc,
    user: payload.data.user,
  };

  inMemorySession = nextSession;
  persistSession(nextSession);
  return nextSession;
}

export function getSession() {
  return inMemorySession;
}

export function setSession(session: StoredSession | null) {
  inMemorySession = session;
  if (session) {
    persistSession(session);
  } else {
    clearSession();
  }
}

export async function request<T>(path: string, options: RequestOptions = {}, allowRefresh = true): Promise<T> {
  const session = getSession();
  const headers = new Headers(options.headers);
  if (!headers.has('Content-Type') && options.body !== undefined) {
    headers.set('Content-Type', 'application/json');
  }

  if (session?.accessToken) {
    headers.set('Authorization', `Bearer ${session.accessToken}`);
  }

  const response = await fetch(buildUrl(path, options.query), {
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (response.status === 401 && allowRefresh && session?.refreshToken) {
    const refreshed = await refreshSession();
    if (refreshed) {
      return request<T>(path, options, false);
    }
  }

  const parsed = await parseJson(response);

  if (!response.ok) {
    const problem = isProblemDetails(parsed) ? parsed : undefined;
    throw new ApiError(problemDetailsMessage(problem), response.status, problem);
  }

  if (options.raw) {
    return parsed as T;
  }

  return (parsed as ApiEnvelope<T>).data;
}

export async function login(email: string, password: string, mfaCode?: string | null) {
  const response = await request<{
    accessToken: string;
    accessTokenExpiresAtUtc: string;
    refreshToken: string;
    refreshTokenExpiresAtUtc: string;
    user: StoredSession['user'];
  }>('/api/auth/login', {
    method: 'POST',
    body: { email, password, mfaCode: mfaCode || null },
  }, false);

  const session: StoredSession = {
    accessToken: response.accessToken,
    accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc,
    refreshToken: response.refreshToken,
    refreshTokenExpiresAtUtc: response.refreshTokenExpiresAtUtc,
    user: response.user,
  };

  setSession(session);
  return session;
}

export async function logout() {
  const session = getSession();
  try {
    if (session) {
      await request('/api/auth/logout', {
        method: 'POST',
        body: { refreshToken: session.refreshToken },
      }, false);
    }
  } finally {
    setSession(null);
  }
}

export async function getList(path: string, query?: RequestOptions['query']) {
  return request<Record<string, unknown>[]>(path, { query });
}

export async function getRecord(path: string, query?: RequestOptions['query']) {
  return request<Record<string, unknown>>(path, { query });
}

export async function postRecord(path: string, body?: unknown, query?: RequestOptions['query'], headers?: Record<string, string>) {
  return request<Record<string, unknown>>(path, { method: 'POST', body, query, headers });
}

export async function putRecord(path: string, body?: unknown, query?: RequestOptions['query']) {
  return request<Record<string, unknown>>(path, { method: 'PUT', body, query });
}

export type ApiEntity = Record<string, unknown>;
export type ApiProblem = ProblemDetails;
