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

test("debug tree2", async ({ page }) => {
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

  const allText = await page.evaluate(() => document.body.innerText);
  console.log("=FULL PAGE TEXT=");
  console.log(allText);
  console.log("=END=");
});
