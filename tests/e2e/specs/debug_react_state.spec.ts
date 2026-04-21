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

test("debug react query state", async ({ page }) => {
  const cookiePath = "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_02_cadastro_unidade/cookies_sindico_a.txt";
  const cookieValue = readCookieValue(cookiePath);

  const responses: Record<string, any> = {};

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
      if (url.includes("estrutura") || url.includes("auth/me")) {
        try { responses[url] = JSON.parse(body.toString()); } catch { responses[url] = body.toString(); }
      }
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

  // Check console errors
  const consoleLogs: string[] = [];
  page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
  page.on("pageerror", (err) => consoleLogs.push(`[PAGEERROR] ${err.message}`));

  console.log("\n=== CAPTURED RESPONSES ===");
  Object.entries(responses).forEach(([url, data]) => {
    console.log(`URL: ${url}`);
    console.log(`Data: ${JSON.stringify(data).substring(0, 300)}`);
  });

  // Wait extra time
  await page.waitForTimeout(3000);
  
  const hasBloco = await page.locator('text=Bloco QA-01').isVisible().catch(() => false);
  const hasCard = await page.locator('[class*="treeCard"], [class*="card"]').isVisible().catch(() => false);
  const allText = await page.evaluate(() => document.body.innerText);
  console.log("\nHas Bloco QA-01:", hasBloco);
  console.log("Has card:", hasCard);
  console.log("Full page text:", allText.substring(0, 800));
});
