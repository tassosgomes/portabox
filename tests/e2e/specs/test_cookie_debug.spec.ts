import { test, expect } from "@playwright/test";
import * as fs from "fs";

const COOKIE_A = fs.readFileSync(
  "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_07_navegacao_arvore/cookies_sindico_a.txt",
  "utf8"
).split("\n").find(l => l.includes("portabox.auth"))?.split("\t").pop()?.trim() ?? "";

test("check cookie injection", async ({ browser }) => {
  const context = await browser.newContext();
  await context.addCookies([{
    name: "portabox.auth",
    value: COOKIE_A,
    domain: "localhost",
    path: "/",
    httpOnly: true,
    secure: false,
    sameSite: "Lax",
  }]);
  const page = await context.newPage();
  console.log("Cookie length:", COOKIE_A.length);
  console.log("Cookie first 30:", COOKIE_A.substring(0, 30));
  
  // Check cookies are set
  const cookies = await context.cookies("http://localhost:5174");
  console.log("Cookies set:", cookies.length);
  
  await page.goto("http://localhost:5174/estrutura");
  await page.waitForTimeout(5000);
  console.log("Final URL:", page.url());
  console.log("Page title:", await page.title());
  expect(page.url()).not.toContain("login");
});
