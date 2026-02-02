import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E Test Configuration
 *
 * Prerequisites:
 * - Local: Start Aspire manually before running tests (dotnet run --project src/Neba.AppHost)
 * - CI: Server is started in a separate workflow step
 *
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests/e2e',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use */
  reporter: [['html', { open: 'never' }]],
  /* Shared settings for all the projects below */
  use: {
    /* Base URL to use in actions like `await page.goto('/')` */
    baseURL: process.env.PLAYWRIGHT_BASE_URL || 'https://localhost:5200',

    /* Ignore HTTPS errors for local development with self-signed certs */
    ignoreHTTPSErrors: true,

    /* Collect trace when retrying the failed test */
    trace: 'on-first-retry',
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chrome',
      use: { ...devices['Desktop Chrome'] },
    },
    {
      name: 'mobile-chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'mobile-safari',
      use: { ...devices['iPhone 12'] },
    },
  ],

  /* Start mock API server and website before running tests */
  webServer: [
    {
      command: 'npx tsx --esm tests/e2e/mock-api/mock-api-server-runner.ts',
      url: 'http://localhost:5151/health',
      reuseExistingServer: !process.env.CI,
      timeout: 30 * 1000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      command:
        'NebaApi__BaseUrl=http://localhost:5151 dotnet run --project src/Neba.Website.Server --urls https://localhost:5200',
      url: 'https://localhost:5200',
      reuseExistingServer: !process.env.CI,
      timeout: 120 * 1000,
      stdout: 'pipe',
      stderr: 'pipe',
      ignoreHTTPSErrors: true,
    },
  ],
});
