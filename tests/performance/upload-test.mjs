import fs from 'node:fs/promises';
import crypto from 'node:crypto';


const API_URL = 'http://localhost:5111';

const APPLICATION_ID = '05ea40f1-e0c9-49ac-a92f-fc975a86dfc8';
const API_KEY = '-X1GAmZfq8SBzqmVlDHjQbJ1WpyZ8dz9jWaS6U5yhEs=';

const SATELLITE_ID = 1;
const GROUND_STATION_ID = 1;


const FILE_NAME = 'dummy-50mb.bin';
const FILE_SIZE_BYTES = 50 * 1024 * 1024; // 50 MiB

const TEST_DURATION_MS = 30 * 1000; // 30 seconds
const UPLOAD_THRESHOLD_MS = 3000; // 3 seconds - performance requirement

/**
 * Creates a large dummy file for testing.
 */
async function createTestFile() {
  console.log(`Checking for ${FILE_NAME}...`);
  try {
    const stats = await fs.stat(FILE_NAME);
    if (stats.size === FILE_SIZE_BYTES) {
      console.log('✓ Test file already exists and is the correct size.');
      return;
    }
  } catch (error) {
  }

  console.log(`Creating a new ${FILE_SIZE_BYTES / (1024 * 1024)}MB test file...`);
  const buffer = crypto.randomBytes(FILE_SIZE_BYTES);
  await fs.writeFile(FILE_NAME, buffer);
  console.log('✓ Test file created successfully.');
}

/**
 * Authenticates with the API to get a Ground Station JWT.
 */
async function getToken() {
  console.log('Step 1: Authenticating to get Ground Station token...');
  try {
    const response = await fetch(`${API_URL}/api/v1/ground-station-link/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        applicationId: APPLICATION_ID,
        apiKey: API_KEY,
      }),
    });

    if (!response.ok) {
      const errorBody = await response.json();
      throw new Error(`Failed to get token. Status: ${response.status}. Body: ${JSON.stringify(errorBody)}`);
    }

    const data = await response.json();
    console.log('✓ Successfully obtained token.');
    return data.accessToken;
  } catch (error) {
    if (error.cause?.code === 'ECONNREFUSED') {
        console.error('✗ FAILED to get token. Error: Connection refused. Is the SatOps backend running?');
    } else {
        console.error(`✗ FAILED to get token. Error: ${error.message}`);
    }
    process.exit(1);
  }
}

/**
 * Uploads the test file to the API and returns the duration in ms.
 */
async function uploadFile(token, fileContent, uploadNumber) {
  const fileBlob = new Blob([fileContent]);

  const formData = new FormData();
  formData.append('SatelliteId', SATELLITE_ID);
  formData.append('GroundStationId', GROUND_STATION_ID);
  formData.append('CaptureTime', new Date().toISOString());
  formData.append('ImageFile', fileBlob, FILE_NAME);

  const startTime = performance.now();

  const response = await fetch(`${API_URL}/api/v1/ground-station-link/images`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
    },
    body: formData,
  });

  const endTime = performance.now();
  const durationMs = endTime - startTime;

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`Upload #${uploadNumber} failed. Status: ${response.status}. Body: ${errorText}`);
  }

  return durationMs;
}

/**
 * Runs multiple uploads over a specified duration and reports statistics.
 */
async function runUploadTest(token) {
  console.log(`\nStep 2: Running upload test for ${TEST_DURATION_MS / 1000} seconds...`);
  console.log(`Performance requirement: Each upload should complete within ${UPLOAD_THRESHOLD_MS / 1000}s\n`);

  // Pre-load file content once
  const fileContent = await fs.readFile(FILE_NAME);

  const durations = [];
  const startTime = performance.now();
  let uploadCount = 0;
  let successCount = 0;
  let failCount = 0;

  while (performance.now() - startTime < TEST_DURATION_MS) {
    uploadCount++;
    const elapsedSec = ((performance.now() - startTime) / 1000).toFixed(1);
    console.log(`[${elapsedSec}s] Upload #${uploadCount} starting...`);

    try {
      const durationMs = await uploadFile(token, fileContent, uploadCount);
      durations.push(durationMs);

      const durationSec = (durationMs / 1000).toFixed(2);
      const passed = durationMs < UPLOAD_THRESHOLD_MS;

      if (passed) {
        successCount++;
        console.log(`  ✓ Upload #${uploadCount} completed in ${durationSec}s`);
      } else {
        failCount++;
        console.log(`  ⚠ Upload #${uploadCount} completed in ${durationSec}s (exceeded ${UPLOAD_THRESHOLD_MS / 1000}s threshold)`);
      }
    } catch (error) {
      failCount++;
      console.error(`  ✗ Upload #${uploadCount} failed: ${error.message}`);
    }
  }

  // Calculate and display statistics
  console.log('\n' + '='.repeat(60));
  console.log('UPLOAD TEST RESULTS');
  console.log('='.repeat(60));

  if (durations.length === 0) {
    console.log('No successful uploads completed.');
    process.exit(1);
  }

  const totalDuration = durations.reduce((sum, d) => sum + d, 0);
  const avgDuration = totalDuration / durations.length;
  const minDuration = Math.min(...durations);
  const maxDuration = Math.max(...durations);

  // Calculate percentiles
  const sortedDurations = [...durations].sort((a, b) => a - b);
  const p50 = sortedDurations[Math.floor(sortedDurations.length * 0.5)];
  const p95 = sortedDurations[Math.floor(sortedDurations.length * 0.95)];
  const p99 = sortedDurations[Math.floor(sortedDurations.length * 0.99)];

  const passedThreshold = durations.filter(d => d < UPLOAD_THRESHOLD_MS).length;

  console.log(`\nTest Duration:     ${TEST_DURATION_MS / 1000}s`);
  console.log(`Total Uploads:     ${uploadCount}`);
  console.log(`Successful:        ${durations.length}`);
  console.log(`Failed:            ${uploadCount - durations.length}`);
  console.log(`\nUpload Duration Statistics:`);
  console.log(`  Average:         ${(avgDuration / 1000).toFixed(2)}s`);
  console.log(`  Min:             ${(minDuration / 1000).toFixed(2)}s`);
  console.log(`  Max:             ${(maxDuration / 1000).toFixed(2)}s`);
  console.log(`  P50 (median):    ${(p50 / 1000).toFixed(2)}s`);
  console.log(`  P95:             ${(p95 / 1000).toFixed(2)}s`);
  console.log(`  P99:             ${(p99 / 1000).toFixed(2)}s`);
  console.log(`\nPerformance Threshold (${UPLOAD_THRESHOLD_MS / 1000}s):`);
  console.log(`  Passed:          ${passedThreshold}/${durations.length} (${((passedThreshold / durations.length) * 100).toFixed(1)}%)`);

  console.log('\n' + '='.repeat(60));

  if (avgDuration < UPLOAD_THRESHOLD_MS) {
    console.log(`✓ PASSED: Average upload time (${(avgDuration / 1000).toFixed(2)}s) is within ${UPLOAD_THRESHOLD_MS / 1000}s threshold`);
  } else {
    console.log(`✗ FAILED: Average upload time (${(avgDuration / 1000).toFixed(2)}s) exceeds ${UPLOAD_THRESHOLD_MS / 1000}s threshold`);
    process.exit(1);
  }
}

/**
 * Main execution function.
 */
async function main() {
  await createTestFile();
  const token = await getToken();
  await runUploadTest(token);
}

main();