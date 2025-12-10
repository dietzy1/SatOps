import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

// --- Configuration ---
// PASTE VALID JWT HERE
const AUTH_TOKEN = 'eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IlljcXZJZm9rQ2ZlZWMzVFpxTEdQOSJ9.eyJpc3MiOiJodHRwczovL2Rldi03Nmd2djhpMHp1N2NxYzNkLnVzLmF1dGgwLmNvbS8iLCJzdWIiOiJnb29nbGUtb2F1dGgyfDExMTY3MDc4ODY1NjA5MjY1MzYzNSIsImF1ZCI6WyJodHRwOi8vbG9jYWxob3N0OjUxMTEiLCJodHRwczovL2Rldi03Nmd2djhpMHp1N2NxYzNkLnVzLmF1dGgwLmNvbS91c2VyaW5mbyJdLCJpYXQiOjE3NjUzMTU3NDMsImV4cCI6MTc2NTQwMjE0Mywic2NvcGUiOiJvcGVuaWQgcHJvZmlsZSBlbWFpbCIsImF6cCI6ImRzNXB5ZE5OcjFrQlJaM3N5eHJLS1FIdkRqN09GYkJRIn0.flBJT2RLhsQAB27-K0Fooa2pI3WXgSrqb3IbN_Nz_ymyoWOKavGHJqFxpLeT6g3ezZVmaZBtgoNedrMQIu27v8Mbq7rYv54g0D4zkxjmW7sa8Jk9Rh2EgkG6LS_EhXmqRIisfxCmosbgF7-e6JR6GJa96I26dH5KXd68983-bB10tE8reEk6SVAQRNs45O-ncE3zzmM496u9SsHMxf8A-8paJhC7ItrFnIOKUXPqFVABQIzs42zrCkARS3MTZ43l05DGenaM_1wy77Qaf5uUkWnJq9l3EUEAwiVedpqTK9S43hgpuzPOeF7-x5kzOSf2f9mIAtmdzRtJ-fumf_zHwg'; 

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
  // Calculate 7-day window from current moment
  const startTime = new Date();
  const endTime = new Date(startTime);
  endTime.setDate(endTime.getDate() + 7); // Add 7 days
  
  // Format as ISO 8601 strings
  const startTimeStr = startTime.toISOString();
  const endTimeStr = endTime.toISOString();
  
  // Build URL with query parameters
  const url = `${BASE_URL}/api/v1/overpasses/satellite/${SATELLITE_ID}/groundstation/${GROUND_STATION_ID}?startTime=${encodeURIComponent(startTimeStr)}&endTime=${encodeURIComponent(endTimeStr)}`;
  
  const params = {
    headers: {
      'Authorization': `Bearer ${AUTH_TOKEN}`,
      'Content-Type': 'application/json',
    },
  };

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