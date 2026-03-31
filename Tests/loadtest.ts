import http from 'k6/http';
import { check, sleep } from 'k6';
import type { Options } from 'k6/options';

export const options: Options = {
    stages: [
        { duration: '10s', target: 50 }, // Ramp up to 50 users
        { duration: '30s', target: 50 }, // Stay at 50 users
        { duration: '10s', target: 0 },  // Ramp down to 0 users
    ],
};

export default function () {
    const res = http.get('http://host.docker.internal:5000/health');
    check(res, {
        'status is 200': (r: any) => r.status === 200,
        'response time < 200ms': (r: any) => r.timings.duration < 200,
    });
    sleep(1);
}
