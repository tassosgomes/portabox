import { defineConfig, devices } from "@playwright/test";
export default defineConfig({
  testDir: "./specs",
  testMatch: "test_cookie_debug.spec.ts",
  workers: 1,
  timeout: 30_000,
  use: { baseURL: "http://localhost:5174" },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
