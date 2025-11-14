import fs from 'node:fs/promises';
import crypto from 'node:crypto';


const API_URL = 'http://localhost:5111';

const APPLICATION_ID = '7db1e716-8c60-4ac8-bfcf-b7aa565907c1';
const API_KEY = 'aY1SZM6LYZyzagn__dKIq5A-gLvI1oG58YqG89zewtI=';

const SATELLITE_ID = 1;
const GROUND_STATION_ID = 1;


const FILE_NAME = 'dummy-50mb.bin';
const FILE_SIZE_BYTES = 50 * 1024 * 1024; // 50 MiB

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
 * Uploads the test file to the API.
 */
async function uploadFile(token) {
  console.log(`Step 2: Uploading ${FILE_NAME}... (this may take a moment)`);
  try {
    const fileContent = await fs.readFile(FILE_NAME);
    const fileBlob = new Blob([fileContent]);

    const formData = new FormData();
    formData.append('SatelliteId', SATELLITE_ID);
    formData.append('GroundStationId', GROUND_STATION_ID);
    formData.append('CaptureTime', new Date().toISOString());
    formData.append('ImageFile', fileBlob, FILE_NAME);

    const response = await fetch(`${API_URL}/api/v1/ground-station-link/images`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
      body: formData,
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Upload failed. Status: ${response.status}. Body: ${errorText}`);
    }

    const successText = await response.text();
    console.log('✓ Upload successful! The API responded with:');
    console.log(successText);
  } catch (error) {
    console.error(`✗ UPLOAD FAILED. Error: ${error.message}`);
    process.exit(1);
  }
}

/**
 * Main execution function.
 */
async function main() {
  await createTestFile();
  const token = await getToken();
  await uploadFile(token);
}

main();