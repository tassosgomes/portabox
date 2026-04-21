import { test } from "@playwright/test";
import * as fs from "fs";

const SINDICO_APP_URL = "http://localhost:5174";
const BACKEND_URL = "http://localhost:5272";

function readCookieValue(path: string): string {
  const content = fs.readFileSync(path, "utf-8");
  const lines = content.split("\n").filter((l) => l.includes("portabox.auth"));
  const parts = lines[0].split("\t");
  return parts[parts.length - 1].trim();
}

test("debug fetch errors", async ({ page }) => {
  const cookiePath = "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_02_cadastro_unidade/cookies_sindico_a.txt";
  const cookieValue = readCookieValue(cookiePath);

  const consoleLogs: string[] = [];
  const pageErrors: string[] = [];
  page.on("console", (msg) => {
    const text = `[${msg.type()}] ${msg.text()}`;
    consoleLogs.push(text);
  });
  page.on("pageerror", (err) => pageErrors.push(`[PAGEERROR] ${err.message}`));

  await page.route("**/api/**", async (route) => {
    const request = route.request();
    const url = request.url();
    let backendUrl = url.startsWith(`${SINDICO_APP_URL}/api`)
      ? url.replace(`${SINDICO_APP_URL}/api`, `${BACKEND_URL}/api`)
      : url;
    try {
      const response = await route.fetch({
        url: backendUrl,
        method: request.method(),
        headers: request.headers(),
        postData: request.postData() ?? undefined,
      });
      const body = await response.body();
      await route.fulfill({
        status: response.status(),
        headers: response.headers(),
        body: body,
      });
    } catch {
      await route.abort();
    }
  });

  await page.context().addCookies([{
    name: "portabox.auth",
    value: cookieValue,
    domain: "localhost",
    path: "/",
    httpOnly: true,
    secure: false,
    sameSite: "Lax",
  }]);

  await page.goto(`${SINDICO_APP_URL}/estrutura`);
  await page.waitForLoadState("networkidle");
  await page.waitForTimeout(5000);

  console.log("\n=== CONSOLE LOGS ===");
  consoleLogs.forEach(l => console.log(l));
  console.log("\n=== PAGE ERRORS ===");
  pageErrors.forEach(l => console.log(l));
  
  const text = await page.evaluate(() => document.body.innerText);
  console.log("\nPage text:", text.substring(0, 300));
});
