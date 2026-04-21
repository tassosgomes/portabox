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

test("debug tree rendering", async ({ page }) => {
  const cookiePath = "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_02_cadastro_unidade/cookies_sindico_a.txt";
  const cookieValue = readCookieValue(cookiePath);

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
      await route.fulfill({ response });
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
  await page.waitForTimeout(3000);

  // Get ALL text content from the page
  const allText = await page.evaluate(() => document.body.innerText);
  console.log("PAGE TEXT:", allText.substring(0, 2000));
  
  // Check for specific text patterns
  const hasBloco = await page.locator('text=Bloco').first().isVisible().catch(() => false);
  const hasQA01 = await page.locator('text=QA-01').first().isVisible().catch(() => false);
  const hasBlocoQA01 = await page.locator('text=Bloco QA-01').first().isVisible().catch(() => false);
  
  console.log("Has 'Bloco':", hasBloco);
  console.log("Has 'QA-01':", hasQA01);  
  console.log("Has 'Bloco QA-01':", hasBlocoQA01);

  await page.screenshot({ path: "/tmp/debug_tree.png", fullPage: true });
});
