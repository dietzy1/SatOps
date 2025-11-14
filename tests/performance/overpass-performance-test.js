import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

// --- Configuration ---
// PASTE VALID JWT HERE
const AUTH_TOKEN = 'TOKEN_HERE_YES_VERY_NICE_TOKEN_HERE_YES_VERY_NICE_TOKEN_HERE_YES_VERY_NICE_TOKEN_HERE_YES_VERY_NICE_TOKEN_HERE_YES_VERY_NICE_'; 

const BASE_URL = 'http://localhost:5111';
const SATELLITE_ID = 2;
const GROUND_STATION_ID = 1;

// --- Custom Metrics ---
// Trend metric to collect all response times
const overpassCalcDuration = new Trend('overpass_calculation_duration');
const errorRate = new Rate('errors');

// --- Test Options ---
export const options = {
  thresholds: {
    // The 95th percentile of response times must be less than 2000ms.
    'overpass_calculation_duration': ['p(95) < 2000'],
    
    'errors': ['rate<0.01'], // less than 1% errors
  },

  // This simulates a gradual ramp-up, a sustained load, and a ramp-down.
  stages: [
    { duration: '10s', target: 5 }, // Ramp up to 5 virtual users over 10 seconds
    { duration: '30s', target: 5 }, // Stay at 5 virtual users for 30 seconds
    { duration: '5s', target: 0 },  // Ramp down to 0 users
  ],
};

// --- Test Logic ---
// This is the main function that each virtual user will execute repeatedly.
export default function () {
  const url = `${BASE_URL}/api/v1/overpasses/satellite/${SATELLITE_ID}/groundstation/${GROUND_STATION_ID}`;
  
  const params = {
    headers: {
      'Authorization': `Bearer ${AUTH_TOKEN}`,
      'Content-Type': 'application/json',
    },
  };

  // The controller defaults to a 7-day window
  const res = http.get(url, params);

  // 1. Check if the request was successful (HTTP 200)
  const success = check(res, {
    'status is 200': (r) => r.status === 200,
  });

  // 2. Record the response time for this request into Trend metric.
  overpassCalcDuration.add(res.timings.duration);

  // 3. Record whether an error occurred.
  if (!success) {
    errorRate.add(1);
  }

  // Simulate user behavior.
  sleep(1);
}