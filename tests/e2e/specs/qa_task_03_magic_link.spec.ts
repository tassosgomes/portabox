import { test, expect } from "@playwright/test";
import fs from "fs";
import path from "path";

const BASE_URL = "http://localhost:5173";
const SINDICO_APP_URL = "http://localhost:5174";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_03_magic_link_sindico";
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const TENANT_ID = "f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5";
const MAGIC_TOKEN = "GSmVY1TW_q6ThuPXyFsuGHJYRPSNiEYt5OvnMjS4-HA";
const MAGIC_LINK = `${SINDICO_APP_URL}/password-setup?token=${MAGIC_TOKEN}`;

function appendLog(content: string) {
  fs.appendFileSync(path.join(EVIDENCE_DIR, "requests.log"), content + "\n");
}

test.describe("qa_task_03 — Magic Link do Síndico (UI Tests)", () => {
  test("TC-03: Página de definição de senha acessível via magic link", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    appendLog(
      "========================================\n" +
        "TC-03: Página de definição de senha via magic link\n" +
        `Timestamp: ${new Date().toISOString()}\n` +
        "========================================\n" +
        `Navigating to: ${MAGIC_LINK}\n`
    );

    let navigationError: string | null = null;
    try {
      await page.goto(MAGIC_LINK, { timeout: 15000 });
    } catch (e: unknown) {
      navigationError = e instanceof Error ? e.message : String(e);
    }

    await page
      .screenshot({
        path: path.join(SCREENSHOTS_DIR, "tc03_inicio.png"),
        fullPage: true,
      })
      .catch(() => {});

    if (navigationError) {
      appendLog(
        `--- RESULTADO: FAIL ---\n` +
          `Expected: page loads at ${MAGIC_LINK}\n` +
          `Actual: navigation error — ${navigationError}\n`
      );
      appendLog(
        `--- BROWSER CONSOLE TC-03 ---\n` +
          [...consoleLogs, ...pageErrors].join("\n") +
          "\n"
      );
      // Fail the test with proper assertion
      expect(navigationError, "TC-03: Page should load at magic link URL").toBe(
        null
      );
      return;
    }

    const pageTitle = await page.title();
    const pageUrl = page.url();
    appendLog(`Current URL: ${pageUrl}\nPage title: ${pageTitle}\n`);

    // Take pre-assertion screenshot
    await page
      .screenshot({
        path: path.join(SCREENSHOTS_DIR, "tc03_pre_assert.png"),
        fullPage: true,
      })
      .catch(() => {});

    // Assert: page should have a password input or relevant form
    const passwordInput = page.locator(
      'input[type="password"], input[name*="senha"], input[name*="password"]'
    );
    const emailDisplay = page.locator(
      'input[type="email"], [data-testid*="email"], span:has-text("sindico.qa")'
    );

    const hasPasswordInput = await passwordInput.count();
    appendLog(
      `Password input found: ${hasPasswordInput > 0 ? "YES" : "NO"}\n`
    );

    if (hasPasswordInput === 0) {
      await page
        .screenshot({
          path: path.join(SCREENSHOTS_DIR, "tc03_fail.png"),
          fullPage: true,
        })
        .catch(() => {});
      appendLog(
        `--- RESULTADO: FAIL ---\n` +
          `Expected: page with password input field at ${MAGIC_LINK}\n` +
          `Actual: no password input found on page. URL: ${pageUrl}\n`
      );
      appendLog(
        `--- BROWSER CONSOLE TC-03 ---\n` +
          [...consoleLogs, ...pageErrors].join("\n") +
          "\n"
      );
      expect(hasPasswordInput, "TC-03: Password input should be visible").toBeGreaterThan(0);
    } else {
      await page
        .screenshot({
          path: path.join(SCREENSHOTS_DIR, "tc03_pass.png"),
          fullPage: true,
        })
        .catch(() => {});
      appendLog(
        `--- RESULTADO: PASS ---\n` +
          `Page loaded, password input visible\n` +
          `Email display found: ${(await emailDisplay.count()) > 0 ? "YES" : "NO"}\n`
      );
    }

    appendLog(
      `--- BROWSER CONSOLE TC-03 ---\n` +
        [...consoleLogs, ...pageErrors].join("\n") +
        "\n"
    );
  });

  test("TC-06: Botão Reenviar Magic Link no painel de detalhes do tenant", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    appendLog(
      "========================================\n" +
        "TC-06: Botão Reenviar Magic Link no painel de detalhes\n" +
        `Timestamp: ${new Date().toISOString()}\n` +
        "========================================\n"
    );

    // Login as operator first
    await page.goto(`${BASE_URL}/login`);
    await page
      .screenshot({
        path: path.join(SCREENSHOTS_DIR, "tc06_login_inicio.png"),
        fullPage: true,
      })
      .catch(() => {});

    // Fill login form
    const emailInput = page.locator(
      'input[type="email"], input[name="email"], input[id="email"]'
    );
    const passwordInput = page.locator(
      'input[type="password"], input[name="password"], input[id="password"]'
    );
    const submitBtn = page.locator(
      'button[type="submit"], button:has-text("Entrar"), button:has-text("Login")'
    );

    await emailInput.fill("operator@portabox.dev");
    await passwordInput.fill("PortaBox123!");
    await page
      .screenshot({
        path: path.join(SCREENSHOTS_DIR, "tc06_login_pre_submit.png"),
        fullPage: true,
      })
      .catch(() => {});
    await submitBtn.click();
    await page.waitForTimeout(2000);

    await page
      .screenshot({
        path: path.join(SCREENSHOTS_DIR, "tc06_apos_login.png"),
        fullPage: true,
      })
      .catch(() => {});
    appendLog(`After login URL: ${page.url()}\n`);

    // Navigate to tenant detail page
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForTimeout(2000);
    await page
      .screenshot({
        path: path.join(SCREENSHOTS_DIR, "tc06_painel_detalhes.png"),
        fullPage: true,
      })
      .catch(() => {});

    appendLog(
      `Tenant detail page URL: ${page.url()}\n` +
        `Page title: ${await page.title()}\n`
    );

    // Look for "Reenviar" or "Enviar" button related to magic link
    const resendButton = page.locator(
      'button:has-text("Reenviar"), button:has-text("reenviar"), button:has-text("Enviar link"), button:has-text("enviar link"), button:has-text("Magic Link"), [data-testid*="resend"], [data-testid*="magic"]'
    );
    const resendCount = await resendButton.count();

    appendLog(`Resend magic link button found: ${resendCount > 0 ? "YES (" + resendCount + " elements)" : "NO"}\n`);

    // Take pre-assertion screenshot
    await page
      .screenshot({
        path: path.join(SCREENSHOTS_DIR, "tc06_pre_assert.png"),
        fullPage: true,
      })
      .catch(() => {});

    if (resendCount === 0) {
      // Check page content for any mention of magic link / reenvio
      const pageContent = await page.content();
      const hasMagicMention =
        pageContent.toLowerCase().includes("magic") ||
        pageContent.toLowerCase().includes("reenviar") ||
        pageContent.toLowerCase().includes("enviar link");
      appendLog(
        `Page mentions magic link / reenviar: ${hasMagicMention ? "YES" : "NO"}\n` +
          `--- RESULTADO: FAIL ---\n` +
          `Expected: Button to resend magic link visible on tenant detail page\n` +
          `Actual: No resend button found. Page URL: ${page.url()}\n`
      );
      appendLog(
        `--- BROWSER CONSOLE TC-06 ---\n` +
          [...consoleLogs, ...pageErrors].join("\n") +
          "\n"
      );
      expect(resendCount, "TC-06: Resend magic link button should be visible").toBeGreaterThan(0);
    } else {
      const isVisible = await resendButton.first().isVisible();
      const isEnabled = await resendButton.first().isEnabled();
      await page
        .screenshot({
          path: path.join(SCREENSHOTS_DIR, "tc06_pass.png"),
          fullPage: true,
        })
        .catch(() => {});
      appendLog(
        `Button visible: ${isVisible}, enabled: ${isEnabled}\n` +
          `--- RESULTADO: PASS ---\n` +
          `Resend magic link button found and visible on tenant detail page\n`
      );
      appendLog(
        `--- BROWSER CONSOLE TC-06 ---\n` +
          [...consoleLogs, ...pageErrors].join("\n") +
          "\n"
      );
    }
  });
});
