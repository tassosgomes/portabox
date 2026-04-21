import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./specs",
  testMatch: "qa_task_03_cf03_edicao_nome_bloco_ui.spec.ts",
  fullyParallel: false,
  retries: 0,
  workers: 1,
  timeout: 45_000,

  reporter: [
    ["list"],
  ],

  use: {
    baseURL: "http://localhost:5174",
    trace: "off",
    screenshot: "on",
    video: "on",
  },

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
