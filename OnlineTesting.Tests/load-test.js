
import http from 'k6/http';
import { sleep, check, group } from 'k6';

// Адреса сервисов
const BASE_URL = __ENV.BASE_URL || 'http://host.docker.internal:5006';
const WEB_URL = __ENV.WEB_URL || 'http://host.docker.internal:5000';

// JWT для проверки защищённых API
const TOKEN = __ENV.TOKEN || '';

// Адрес получения курсов
const COURSES_PATH = __ENV.COURSES_PATH || '/api/courses';

// Настройки нагрузки
export const options = {
    scenarios: {
        load_test: {
            executor: 'ramping-vus',

            // Постепенно увеличиваем количество пользователей
            startVUs: 1,

            stages: [
                { duration: '20s', target: 5 },
                { duration: '30s', target: 10 },
                { duration: '30s', target: 20 },
                { duration: '20s', target: 20 },
                { duration: '20s', target: 0 },
            ],

            gracefulRampDown: '10s',
        },
    },

    // Критерии успешного прохождения теста
    thresholds: {
        http_req_failed: ['rate<0.01'],
        http_req_duration: ['p(95)<500'],
        checks: ['rate>0.95'],
    },
};

// Общие настройки HTTP-запросов
const params = {
    headers: {
        'Content-Type': 'application/json',
    },
    tags: {
        project: 'OnlineTesting',
    },
    timeout: '10s',
};

// Основной сценарий
export default function () {

    // 1. Проверяем доступность API Gateway
    group('API Gateway', function () {

        const health = http.get(
            `${BASE_URL}/health`,
            params
        );

        check(health, {
            'gateway health returns 200': (r) => r.status === 200,
            'gateway responds under 200ms': (r) => r.timings.duration < 200,
        });
    });

    // 2. Проверяем доступность WebClient
    group('WebClient', function () {

        const page = http.get(
            `${WEB_URL}/`,
            params
        );

        check(page, {
            'web client returns 200': (r) => r.status === 200,

            'web client returns HTML': (r) =>
                (r.headers['Content-Type'] || '').includes('text/html'),

            'web client responds under 500ms': (r) =>
                r.timings.duration < 500,
        });
    });

    // 3. Проверяем получение курсов через API
    // Этот endpoint требует авторизации.
    // Если JWT не передан, проверка пропускается.
    if (TOKEN) {

        group('Course Service', function () {

            const courseParams = {
                headers: {
                    'Authorization': `Bearer ${TOKEN}`,
                    'Content-Type': 'application/json',
                },
                tags: {
                    endpoint: 'courses',
                },
            };

            const courses = http.get(
                `${BASE_URL}${COURSES_PATH}`,
                courseParams
            );

            check(courses, {
                'courses endpoint returns 200': (r) =>
                    r.status === 200,

                'courses responds under 500ms': (r) =>
                    r.timings.duration < 500,

                'courses returns JSON': (r) =>
                    (r.headers['Content-Type'] || '').includes('application/json'),
            });
        });
    }

    // 4. Пауза между итерациями пользователя
    sleep(1);
}
