import { test } from "@playwright/test";
import * as fs from "fs";

const SINDICO_APP_URL = "http://localhost:5174";
const BACKEND_URL = "http://localhost:5272";

function readCookieValue(path: string): string {
  const content = fs.readFileSync(path, "utf-8");
  const lines = content.split("\n").filter((l) => l.includes("portabox.auth"));
  if (lines.length === 0) throw new Error("portabox.auth cookie not found");
  const parts = lines[0].split("\t");
  return parts[parts.length - 1].trim();
}

test("debug estrutura loading", async ({ page }) => {
  const cookiePath = "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_02_cadastro_unidade/cookies_sindico_a.txt";
  const cookieValue = readCookieValue(cookiePath);
  
  const intercepted: string[] = [];

  await page.route("**/api/**", async (route) => {
    const request = route.request();
    const url = request.url();
    let backendUrl: string;
    if (url.startsWith(`${SINDICO_APP_URL}/api`)) {
      backendUrl = url.replace(`${SINDICO_APP_URL}/api`, `${BACKEND_URL}/api`);
    } else {
      backendUrl = url;
    }
    intercepted.push(`INTERCEPT: ${url} -> ${backendUrl}`);
    
    try {
      const response = await route.fetch({
        url: backendUrl,
        method: request.method(),
        headers: request.headers(),
        postData: request.postData() ?? undefined,
      });
      intercepted.push(`RESPONSE: ${response.status()} for ${backendUrl}`);
      await route.fulfill({ response });
    } catch (err: any) {
      intercepted.push(`ERROR: ${err.message} for ${backendUrl}`);
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

  console.log("\n=== INTERCEPTED CALLS ===");
  intercepted.forEach(l => console.log(l));

  const url = page.url();
  console.log("\nFinal URL:", url);

  await page.screenshot({ path: "/tmp/debug_estrutura.png", fullPage: true });
  
  const content = await page.content();
  console.log("Has Bloco QA:", content.includes("Bloco QA"));
  console.log("Has estrutura heading:", content.includes("Estrutura do condom"));
});
