import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    vus: 10, 
    duration: '10s', 
};

export default function () {
    const url = 'http://host.docker.internal:5004/health';

    const res = http.get(url);

    check(res, {
        'status is 200': (r) => r.status === 200,
        'response time < 200ms': (r) => r.timings.duration < 200,
    });

    sleep(1);
}