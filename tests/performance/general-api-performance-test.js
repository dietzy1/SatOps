import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

// --- Configuration ---
// PASTE VALID JWT HERE
const AUTH_TOKEN = 'MyTokenIsUpHereYouCreep'; 

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