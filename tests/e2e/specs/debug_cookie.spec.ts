import { test, expect } from "@playwright/test";
import * as fs from "fs";

const SINDICO_APP_URL = "http://localhost:5174";
const BACKEND_URL = "http://localhost:5272";

function readCookieValue(path: string): string {
  const content = fs.readFileSync(path, "utf-8");
  const lines = content.split("\n").filter((l) => l.includes("portabox.auth"));
  if (lines.length === 0) throw new Error("portabox.auth cookie not found");
  const parts = lines[0].split("\t");
  return parts[parts.length - 1];
}

test("debug: navigate to backend to set cookie then frontend", async ({ page, context }) => {
  const cookiePath = "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_02_cadastro_unidade/cookies_sindico_a.txt";
  const cookieValue = readCookieValue(cookiePath);
  
  // Add cookie for BACKEND domain (5272)
  await context.addCookies([{
    name: "portabox.auth",
    value: cookieValue,
    domain: "localhost",
    path: "/",
    httpOnly: true,
    secure: false,
    sameSite: "Lax",
  }]);

  // Navigate to frontend
  await page.goto(`${SINDICO_APP_URL}/estrutura`);
  await page.waitForLoadState("networkidle");
  await page.waitForTimeout(3000);
  
  const url = page.url();
  const content = await page.content();
  
  console.log("Current URL:", url);
  console.log("Has login:", content.includes("E-mail ou senha"));
  console.log("Has estrutura:", content.includes("Estrutura do condom"));
  console.log("Has Bloco QA:", content.includes("Bloco QA"));
  
  // Take screenshot
  await page.screenshot({ path: "/tmp/debug_cookie_test.png", fullPage: true });
});
