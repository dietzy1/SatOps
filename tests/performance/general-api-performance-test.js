import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

// --- Configuration ---
// PASTE VALID JWT HERE
const AUTH_TOKEN = 'eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IlljcXZJZm9rQ2ZlZWMzVFpxTEdQOSJ9.eyJpc3MiOiJodHRwczovL2Rldi03Nmd2djhpMHp1N2NxYzNkLnVzLmF1dGgwLmNvbS8iLCJzdWIiOiJnb29nbGUtb2F1dGgyfDExMTY3MDc4ODY1NjA5MjY1MzYzNSIsImF1ZCI6WyJodHRwOi8vbG9jYWxob3N0OjUxMTEiLCJodHRwczovL2Rldi03Nmd2djhpMHp1N2NxYzNkLnVzLmF1dGgwLmNvbS91c2VyaW5mbyJdLCJpYXQiOjE3NjUzMTU3NDMsImV4cCI6MTc2NTQwMjE0Mywic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSBlbWFpbCIsImF6cCI6ImRzNXB5ZE5OcjFrQlJaM3N5eHJLS1FIdkRqN09GYkJRIn0.flBJT2RLhsQAB27-K0Fooa2pI3WXgSrqb3IbN_Nz_ymyoWOKavGHJqFxpLeT6g3ezZVmaZBtgoNedrMQIu27v8Mbq7rYv54g0D4zkxjmW7sa8Jk9Rh2EgkG6LS_EhXmqRIisfxCmosbgF7-e6JR6GJa96I26dH5KXd68983-bB10tE8reEk6SVAQRNs45O-ncE3zzmM496u9SsHMxf8A-8paJhC7ItrFnIOKUXPqFVABQIzs42zrCkARS3MTZ43l05DGenaM_1wy77Qaf5uUkWnJq9l3EUEAwiVedpqTK9S43hgpuzPOeF7-x5kzOSf2f9mIAtmdzRtJ-fumf_zHwg'; 

const BASE_URL = 'http://localhost:5111';


const responseDuration = new Trend('response_duration');
const errorRate = new Rate('errors');

export const options = {
  thresholds: {

    'response_duration': ['p(95) < 2000'],
    
    'response_duration{group:::list_satellites}': ['p(95) < 1000'],
    'response_duration{group:::list_ground_stations}': ['p(95) < 1000'],
    'response_duration{group:::list_flight_plans}': ['p(95) < 1500'],

    'errors': ['rate<0.01'],
  },
  stages: [
    { duration: '15s', target: 20 }, // Ramp up to 20 concurrent users (as per the requirement)
    { duration: '45s', target: 20 }, // Maintain the load for 45 seconds
    { duration: '10s', target: 0 }, // Ramp down
  ],
};

export default function () {
  const params = {
    headers: {
      'Authorization': `Bearer ${AUTH_TOKEN}`,
      'Content-Type': 'application/json',
    },
  };

  group('list_satellites', function () {
    const res = http.get(`${BASE_URL}/api/v1/satellites`, params);
    const success = check(res, { 'status is 200': (r) => r.status === 200 });
    responseDuration.add(res.timings.duration);
    errorRate.add(!success);
  });

  sleep(1);

  group('list_ground_stations', function () {
    const res = http.get(`${BASE_URL}/api/v1/ground-stations`, params);
    const success = check(res, { 'status is 200': (r) => r.status === 200 });
    responseDuration.add(res.timings.duration);
    errorRate.add(!success);
  });
  
  sleep(1);

  group('list_flight_plans', function () {
    const res = http.get(`${BASE_URL}/api/v1/flight-plans`, params);
    const success = check(res, { 'status is 200': (r) => r.status === 200 });
    responseDuration.add(res.timings.duration);
    errorRate.add(!success);

    if (success && res.json().length > 0) {
      const flightPlanId = res.json()[0].id;
      
      group('get_flight_plan_details', function () {
        const detailRes = http.get(`${BASE_URL}/api/v1/flight-plans/${flightPlanId}`, params);
        const detailSuccess = check(detailRes, { 'details status is 200': (r) => r.status === 200 });
        responseDuration.add(detailRes.timings.duration);
        errorRate.add(!detailSuccess);
      });
    }
  });

  sleep(2);
}