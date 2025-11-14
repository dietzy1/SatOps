# Performance Tests

This directory contains performance and load tests for the SatOps API, written for the k6 load testing tool.

## Prerequisites

1.  [Install k6](https://k6.io/docs/getting-started/installation/).
2.  The SatOps backend application must be running locally (`http://localhost:5111`).
3.  You must have a valid JWT for an authenticated user.

## Running Tests

1.  Open `overpass-performance-test.js` and paste your valid bearer token into the `AUTH_TOKEN` constant.
2.  From the root of the repository, run the test using the following command:

    ```bash
    k6 run tests/performance/overpass-performance-test.js
    ```

Same approach with genera-api-performance-test.js
