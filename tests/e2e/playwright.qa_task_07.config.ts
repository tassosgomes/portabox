import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./specs",
  testMatch: "qa_task_07_navegacao_arvore.spec.ts",
  fullyParallel: false,
  forbidOnly: false,
  retries: 0,
  workers: 1,
  timeout: 60_000,

  reporter: [
    ["list"],
  ],

  use: {
    baseURL: "http://localhost:5174",
    video: "on",
    screenshot: "on",
    trace: "on",
  },

  outputDir: "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_07_navegacao_arvore/videos",

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
