import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright config for generating help-doc screenshots (docs/help/images/**).
 *
 * Separate from playwright.config.ts on purpose — see ADR-0007
 * (docs/adr/0007-in-repo-user-help-documentation.md): screenshot generation is a
 * documentation task, not a correctness check, so it must never run as part of the
 * normal `npm run test:e2e` / CI suite. Run it explicitly via `npm run docs:screenshots`.
 *
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests/e2e/docs-screenshots',
  fullyParallel: false,
  retries: 0,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL || 'https://localhost:5200',
    ignoreHTTPSErrors: true,
  },

  projects: [
    {
      name: 'chrome',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: [
    {
      command: 'node --import tsx/esm tests/e2e/mock-api/mock-api-server-runner.ts',
      url: 'http://localhost:5151/health',
      reuseExistingServer: !process.env.CI,
      timeout: 30 * 1000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      command:
        'NebaApi__BaseUrl=http://localhost:5151 AzureMaps__SubscriptionKey=mock-key-for-e2e dotnet run --project src/Neba.Website.Server --urls https://localhost:5200',
      url: 'https://localhost:5200',
      reuseExistingServer: !process.env.CI,
      timeout: 120 * 1000,
      stdout: 'pipe',
      stderr: 'pipe',
      ignoreHTTPSErrors: true,
    },
  ],
});
