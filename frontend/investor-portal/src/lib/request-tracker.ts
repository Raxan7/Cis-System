import type { DigitalServiceRequestDto } from './portal-api';

const REQUESTS_KEY = 'cis.portal.submittedRequests';

function getStorage() {
  return typeof window === 'undefined' ? null : window.sessionStorage;
}

export function loadTrackedRequests() {
  const storage = getStorage();
  if (!storage) {
    return [] as DigitalServiceRequestDto[];
  }

  const raw = storage.getItem(REQUESTS_KEY);
  if (!raw) {
    return [] as DigitalServiceRequestDto[];
  }

  try {
    return JSON.parse(raw) as DigitalServiceRequestDto[];
  } catch {
    storage.removeItem(REQUESTS_KEY);
    return [] as DigitalServiceRequestDto[];
  }
}

export function rememberTrackedRequest(request: DigitalServiceRequestDto) {
  const storage = getStorage();
  if (!storage) {
    return;
  }

  const requests = loadTrackedRequests()
    .filter((candidate) => candidate.id !== request.id)
    .concat(request)
    .sort((left, right) => Date.parse(right.submittedAtUtc) - Date.parse(left.submittedAtUtc))
    .slice(0, 50);

  storage.setItem(REQUESTS_KEY, JSON.stringify(requests));
}
