import { test, expect } from "@playwright/test";
import * as fs from "fs";

const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_03_magic_link_sindico";
const SCREENSHOTS_DIR = `${EVIDENCE_DIR}/screenshots`;
const REQUEST_LOG = `${EVIDENCE_DIR}/requests.log`;
const TOKEN = "gcEBtNw2q44PdiYPsdldzpPAXLgWVzOvuMGD43x1qW8";
const BASE_URL = "http://localhost:5174";

function log(msg: string) {
  fs.appendFileSync(REQUEST_LOG, msg + "\n");
}

test.describe("TC-03 RERUN5 — Página de definição de senha via magic link", () => {
  test("TC-03: Formulário de definição de senha visível em /password-setup?token=...", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    page.on("console", (msg) => {
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
    });
    page.on("pageerror", (err) => {
      pageErrors.push(err.message);
    });

    log("========================================");
    log("TC-03 RERUN5 — Página de definição de senha");
    log(`Timestamp: ${new Date().toISOString()}`);
    log(`URL: ${BASE_URL}/password-setup?token=${TOKEN}`);
    log("========================================");

    // Navigate to magic link URL
    await page.goto(`${BASE_URL}/password-setup?token=${TOKEN}`);
    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/rerun5_tc03_inicio.png`,
      fullPage: true,
    });
    log("--- Screenshot: rerun5_tc03_inicio.png (imediato após navegação)");

    // Wait for page to stabilize
    await page.waitForTimeout(3000);
    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/rerun5_tc03_apos_wait.png`,
      fullPage: true,
    });
    log("--- Screenshot: rerun5_tc03_apos_wait.png (após 3s de espera)");

    // Capture page state
    const finalUrl = page.url();
    const bodyText = await page.evaluate(() => document.body.innerText);
    const bodyLength = bodyText.length;
    const pageTitle = await page.title();
    const passwordInputCount = await page
      .locator('input[type="password"]')
      .count();
    const totalInputCount = await page.locator("input").count();

    log(`--- URL final: ${finalUrl}`);
    log(`--- Título da página: ${pageTitle}`);
    log(`--- body text length: ${bodyLength}`);
    log(`--- Campos input[type="password"]: ${passwordInputCount}`);
    log(`--- Total de inputs: ${totalInputCount}`);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/rerun5_tc03_pre_assert.png`,
      fullPage: true,
    });
    log("--- Screenshot: rerun5_tc03_pre_assert.png (antes das assertions)");

    // ASSERTION 1: URL deve permanecer em /password-setup (não redirecionar para /login)
    const redirectedToLogin = finalUrl.includes("/login");
    if (redirectedToLogin) {
      log("--- RESULTADO: FAIL ---");
      log("Expected: URL em /password-setup");
      log(`Actual: URL redirecionou para /login — ${finalUrl}`);
      await page.screenshot({
        path: `${SCREENSHOTS_DIR}/rerun5_tc03_fail_redirect.png`,
        fullPage: true,
      });
      log("--- BROWSER CONSOLE TC-03 RERUN5 ---");
      consoleLogs.forEach((l) => log(l));
      pageErrors.forEach((l) => log(`[pageerror] ${l}`));
      expect(redirectedToLogin, "URL não deve redirecionar para /login").toBe(
        false
      );
      return;
    }
    log("--- Assertion 1 PASS: URL não redirecionou para /login");

    // ASSERTION 2: Formulário visível — ao menos 1 campo input[type="password"]
    if (passwordInputCount === 0) {
      log("--- RESULTADO: FAIL ---");
      log(
        "Expected: ao menos 1 campo input[type=\"password\"] visível no formulário"
      );
      log(`Actual: ${passwordInputCount} campos encontrados`);
      log(`Body text (primeiros 500 chars): ${bodyText.substring(0, 500)}`);
      await page.screenshot({
        path: `${SCREENSHOTS_DIR}/rerun5_tc03_fail_no_form.png`,
        fullPage: true,
      });
      log("--- BROWSER CONSOLE TC-03 RERUN5 ---");
      consoleLogs.forEach((l) => log(l));
      pageErrors.forEach((l) => log(`[pageerror] ${l}`));
      expect(
        passwordInputCount,
        'Deve haver ao menos 1 campo input[type="password"] visível'
      ).toBeGreaterThan(0);
      return;
    }

    log(`--- Assertion 2 PASS: ${passwordInputCount} campo(s) de senha encontrado(s)`);

    // Additional check: button "Definir senha"
    const submitButtonCount = await page
      .locator('button:has-text("Definir senha")')
      .count();
    log(`--- Botão "Definir senha" encontrado: ${submitButtonCount}`);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/rerun5_tc03_pass.png`,
      fullPage: true,
    });

    log("--- RESULTADO: PASS ---");
    log(`Expected: formulário visível com input[type="password"]`);
    log(
      `Actual: ${passwordInputCount} campo(s) de senha, botão "Definir senha": ${submitButtonCount}`
    );
    log("--- BROWSER CONSOLE TC-03 RERUN5 ---");
    consoleLogs.forEach((l) => log(l));
    pageErrors.forEach((l) => log(`[pageerror] ${l}`));

    expect(passwordInputCount).toBeGreaterThan(0);
  });
});
