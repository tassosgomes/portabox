import { test, expect } from "@playwright/test";
import * as fs from "fs";

const SINDICO_APP_URL = "http://localhost:5174";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_01_cadastro_bloco";

const QA_SINDICO_A_EMAIL = "qa-sindico-a-1776724904@portabox.test";
const QA_SINDICO_A_PASSWORD = "QaTestPass123!";

test("debug login network calls", async ({ page }) => {
  const networkLog: string[] = [];

  page.on("request", (req) => {
    networkLog.push(`REQUEST: ${req.method()} ${req.url()}`);
  });
  page.on("response", (res) => {
    networkLog.push(`RESPONSE: ${res.status()} ${res.url()}`);
  });

  await page.goto(`${SINDICO_APP_URL}/login`);
  await page.waitForLoadState("networkidle");

  const emailInput = page
    .locator('input[type="email"]')
    .or(page.locator('input[name="email"]'))
    .first();
  const passwordInput = page
    .locator('input[type="password"]')
    .or(page.locator('input[name="password"]'))
    .first();

  await emailInput.fill(QA_SINDICO_A_EMAIL);
  await passwordInput.fill(QA_SINDICO_A_PASSWORD);
  await page.locator('button[type="submit"]').click();

  await page.waitForTimeout(5000);

  await page.screenshot({
    path: `${EVIDENCE_DIR}/screenshots/debug_login_result.png`,
    fullPage: true,
  });

  fs.appendFileSync(
    `${EVIDENCE_DIR}/requests.log`,
    "\n--- DEBUG LOGIN NETWORK ---\n" + networkLog.join("\n") + "\n"
  );

  console.log("Network log:", networkLog.join("\n"));
});
