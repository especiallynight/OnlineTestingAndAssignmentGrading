import http from 'k6/http';
import { sleep, check } from 'k6';

// Адреса можно переопределить:
//   k6 run -e BASE_URL=http://localhost:5006 -e WEB_URL=http://localhost:5000 docs/load-test.js
const BASE_URL = __ENV.BASE_URL || 'http://host.docker.internal:5006';
const WEB_URL = __ENV.WEB_URL || 'http://host.docker.internal:5000';

export const options = {
    scenarios: {
        smoke: {
            executor: 'constant-vus',
            vus: 10,
            duration: '30s',
        },
    },
    thresholds: {
        http_req_failed: ['rate<0.01'],
        http_req_duration: ['p(95)<500'],
    },
};

export default function () {
    const health = http.get(`${BASE_URL}/health`);
    check(health, {
        'gateway health is 200': (r) => r.status === 200,
        'gateway health < 200ms': (r) => r.timings.duration < 200,
    });

    const page = http.get(`${WEB_URL}/`);
    check(page, {
        'web client is 200': (r) => r.status === 200,
        'web client returns html': (r) => (r.headers['Content-Type'] || '').includes('text/html'),
        'web client < 500ms': (r) => r.timings.duration < 500,
    });

    sleep(1);
}
