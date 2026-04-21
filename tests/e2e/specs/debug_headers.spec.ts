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

test("debug headers", async ({ page }) => {
  const cookiePath = "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_02_cadastro_unidade/cookies_sindico_a.txt";
  const cookieValue = readCookieValue(cookiePath);

  const apiResponses: Array<{url: string, status: number, contentType: string, bodyLen: number}> = [];

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
      const ct = response.headers()['content-type'] ?? '';
      apiResponses.push({ url, status: response.status(), contentType: ct, bodyLen: body.length });
      // Use the body to fulfill since we already consumed it
      await route.fulfill({
        status: response.status(),
        headers: response.headers(),
        body: body,
      });
    } catch (err: any) {
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
  await page.waitForTimeout(3000);

  console.log("\n=== API CALLS ===");
  apiResponses.forEach(r => console.log(`${r.status} ${r.url} [${r.contentType}] body=${r.bodyLen}bytes`));

  const allText = await page.evaluate(() => document.body.innerText);
  console.log("\nHas Bloco QA-01:", allText.includes("Bloco QA-01"));
  console.log("Page text:", allText.substring(0, 500));
});
