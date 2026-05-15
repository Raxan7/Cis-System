import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    investor_search: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '30s', target: 5 },
        { duration: '60s', target: 15 },
        { duration: '30s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1500'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const loginEmail = __ENV.LOGIN_EMAIL;
const loginPassword = __ENV.LOGIN_PASSWORD;

export function setup() {
  if (!loginEmail || !loginPassword) {
    throw new Error('LOGIN_EMAIL and LOGIN_PASSWORD environment variables are required.');
  }

  const response = http.post(
    `${baseUrl}/api/auth/login`,
    JSON.stringify({
      email: loginEmail,
      password: loginPassword,
    }),
    {
      headers: {
        'Content-Type': 'application/json',
      },
    },
  );

  check(response, {
    'login returned 200': (r) => r.status === 200,
    'login returned token': (r) => !!r.json('data.accessToken'),
  });

  return {
    token: response.json('data.accessToken'),
  };
}

export default function (data) {
  const pageNumber = ((__ITER % 10) + 1);
  const response = http.get(
    `${baseUrl}/api/investors?pageNumber=${pageNumber}&pageSize=25&sortBy=DisplayName&search=Investor`,
    {
      headers: {
        Authorization: `Bearer ${data.token}`,
      },
    },
  );

  check(response, {
    'investor search returned 200': (r) => r.status === 200,
    'pagination metadata present': (r) => !!r.json('meta.pagination.totalCount'),
    'page size respected': (r) => (r.json('data') || []).length <= 25,
  });

  sleep(1);
}
