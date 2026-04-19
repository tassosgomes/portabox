import { defineConfig, devices } from "@playwright/test";

const apiUrl = process.env["PLAYWRIGHT_API_URL"] ?? "http://localhost:5000";
const appUrl = process.env["PLAYWRIGHT_APP_URL"] ?? "http://localhost:4173";

export default defineConfig({
  testDir: "./specs",
  fullyParallel: false,
  forbidOnly: !!process.env["CI"],
  retries: process.env["CI"] ? 1 : 0,
  workers: 1,
  timeout: 30_000,

  reporter: [
    ["list"],
    ["html", { outputFolder: "playwright-report", open: "never" }],
  ],

  use: {
    baseURL: appUrl,
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],

  // When API_URL/APP_URL are localhost, start the servers automatically.
  webServer: apiUrl.includes("localhost")
    ? [
        {
          command:
            "dotnet run --project ../../src/PortaBox.Api/PortaBox.Api.csproj --no-build",
          url: `${apiUrl}/health/live`,
          reuseExistingServer: !process.env["CI"],
          timeout: 60_000,
          env: {
            ASPNETCORE_URLS: apiUrl,
            ASPNETCORE_ENVIRONMENT: "Development",
          },
        },
        {
          command: "pnpm --filter @portabox/backoffice preview --port 4173",
          url: appUrl,
          reuseExistingServer: !process.env["CI"],
          timeout: 30_000,
        },
      ]
    : undefined,
});
