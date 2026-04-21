import { test, expect } from "@playwright/test";

const SINDICO_APP_URL = "http://localhost:5174";
const BACKEND_URL = "http://localhost:5272";
const QA_SINDICO_A_EMAIL = "qa-sindico-a-1776724904@portabox.test";
const QA_SINDICO_A_PASSWORD = "QaTestPass123!";

test("debug proxy and cookie", async ({ page }) => {
  const intercepted: string[] = [];
  
  // Log all requests
  page.on("request", (req) => {
    if (req.url().includes("/api/")) {
      intercepted.push(`REQUEST: ${req.method()} ${req.url()}`);
    }
  });
  page.on("response", (resp) => {
    if (resp.url().includes("/api/")) {
      intercepted.push(`RESPONSE: ${resp.status()} ${resp.url()}`);
    }
  });

  // Setup proxy
  await page.route("**/api/**", async (route) => {
    const request = route.request();
    const url = request.url();
    intercepted.push(`ROUTE_INTERCEPTED: ${url}`);
    const backendUrl = url.replace(`${SINDICO_APP_URL}/api`, `${BACKEND_URL}/api`);
    intercepted.push(`ROUTE_FORWARDING_TO: ${backendUrl}`);

    try {
      const response = await route.fetch({
        url: backendUrl,
        method: request.method(),
        headers: request.headers(),
        postData: request.postData() ?? undefined,
      });
      intercepted.push(`ROUTE_RESPONSE: ${response.status()}`);
      await route.fulfill({ response });
    } catch (err: any) {
      intercepted.push(`ROUTE_ERROR: ${err.message}`);
      await route.abort();
    }
  });

  await page.goto(`${SINDICO_APP_URL}/login`);
  await page.waitForLoadState("networkidle");
  await page.getByLabel("E-mail").fill(QA_SINDICO_A_EMAIL);
  await page.getByLabel("Senha").fill(QA_SINDICO_A_PASSWORD);
  await page.locator('button[type="submit"]').click();
  await page.waitForURL((url) => !url.pathname.includes("/login"), { timeout: 15000 });
  
  await page.goto(`${SINDICO_APP_URL}/estrutura`);
  await page.waitForLoadState("networkidle");
  await page.waitForTimeout(3000);

  console.log("=== INTERCEPTED CALLS ===");
  intercepted.forEach(l => console.log(l));
  
  // Log page content
  const content = await page.content();
  console.log("=== BODY EXCERPT ===");
  console.log(content.substring(0, 2000));
});
