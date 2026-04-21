import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./specs",
  testMatch: "**/qa_rerun5_tc03.spec.ts",
  fullyParallel: false,
  retries: 0,
  workers: 1,
  timeout: 30_000,

  reporter: [["list"]],

  use: {
    baseURL: "http://localhost:5174",
    video: "on",
    screenshot: "on",
    trace: "on",
  },

  outputDir:
    "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_03_magic_link_sindico/videos/rerun5/",

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
