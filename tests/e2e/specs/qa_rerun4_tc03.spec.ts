import { test, expect } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = "http://localhost:5174";
const TOKEN = "uZWYNMGiYckEXNQeOIdHmGTkDhPOYuMzlYsq3BG4nq4";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_03_magic_link_sindico";
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const REQUEST_LOG = path.join(EVIDENCE_DIR, "requests.log");

function appendLog(content: string) {
  fs.appendFileSync(REQUEST_LOG, content + "\n");
}

test.describe("TC-03 RERUN4 — Página de definição de senha acessível via magic link", () => {
  test("TC-03: Navegar para /password-setup?token=<token> e verificar formulário", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    const url = `${BASE_URL}/password-setup?token=${TOKEN}`;

    appendLog("========================================");
    appendLog("TC-03 RERUN4: Página de definição de senha via magic link");
    appendLog(`Timestamp: ${new Date().toISOString()}`);
    appendLog("========================================");
    appendLog(`--- NAVEGAÇÃO ---`);
    appendLog(`URL: ${url}`);
    appendLog(`Token: [REDACTED — comprimento ${TOKEN.length}]`);
    appendLog(`Token fonte: Mailpit — e-mail mais recente 2026-04-20T17:05:56Z`);
    appendLog(`Banco: consumed_at=NULL, invalidated_at=NULL — token válido`);

    // Navegar para a URL do magic link
    await page.goto(url, { waitUntil: "domcontentloaded" });

    // Screenshot imediato após navegação
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_inicio.png"),
      fullPage: true,
    });
    appendLog(`Screenshot: rerun4_tc03_inicio.png`);

    // Aguardar estabilização (networkidle ou 3s)
    await page.waitForTimeout(3000);

    // Screenshot após espera
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_apos_wait.png"),
      fullPage: true,
    });
    appendLog(`Screenshot: rerun4_tc03_apos_wait.png`);

    // Capturar estado atual
    const currentUrl = page.url();
    const pageTitle = await page.title();
    const bodyText = await page.locator("body").innerText().catch(() => "");
    const allInputs = await page.locator("input").count();
    const passwordInputs = await page
      .locator('input[type="password"]')
      .count();

    appendLog(`--- ESTADO DA PÁGINA ---`);
    appendLog(`URL final: ${currentUrl}`);
    appendLog(`Título: ${pageTitle}`);
    appendLog(`Body text length: ${bodyText.length}`);
    appendLog(`Total inputs: ${allInputs}`);
    appendLog(`Inputs type=password: ${passwordInputs}`);

    // Screenshot pré-assertion
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_pre_assert.png"),
      fullPage: true,
    });
    appendLog(`Screenshot: rerun4_tc03_pre_assert.png`);

    // --- ASSERTION 1: Não redirecionou para /login ---
    const redirectedToLogin =
      currentUrl.includes("/login") || currentUrl.includes("/signin");
    appendLog(`--- ASSERTION 1: URL não é /login ---`);
    appendLog(`  URL atual: ${currentUrl}`);
    appendLog(`  Redirecionou para /login: ${redirectedToLogin}`);

    if (redirectedToLogin) {
      appendLog(`  RESULTADO: FAIL — redirecionamento para /login detectado`);
      await page.screenshot({
        path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_fail_redirect.png"),
        fullPage: true,
      });
      appendLog(`--- BROWSER CONSOLE TC-03 RERUN4 ---`);
      appendLog([...consoleLogs, ...pageErrors].join("\n"));
      throw new Error(
        `FAIL — Assertion 1: URL redirecionou para /login. URL atual: ${currentUrl}`
      );
    }
    appendLog(`  RESULTADO: PASS — URL mantida em /password-setup`);

    // --- ASSERTION 2: Formulário visível (ao menos 1 input[type=password]) ---
    appendLog(`--- ASSERTION 2: Formulário com input[type=password] visível ---`);
    appendLog(`  Inputs password encontrados: ${passwordInputs}`);

    if (passwordInputs === 0) {
      appendLog(`  RESULTADO: FAIL — nenhum input[type=password] encontrado`);
      appendLog(`  Body text (primeiros 500 chars): ${bodyText.substring(0, 500)}`);
      await page.screenshot({
        path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_fail_no_form.png"),
        fullPage: true,
      });
      appendLog(`--- BROWSER CONSOLE TC-03 RERUN4 ---`);
      appendLog([...consoleLogs, ...pageErrors].join("\n"));
      throw new Error(
        `FAIL — Assertion 2: Nenhum input[type=password] encontrado. Total inputs: ${allInputs}. Body vazio: ${bodyText.length === 0}`
      );
    }
    appendLog(`  RESULTADO: PASS — ${passwordInputs} input(s) password encontrado(s)`);

    // Screenshot final (PASS)
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_pass.png"),
      fullPage: true,
    });
    appendLog(`Screenshot: rerun4_tc03_pass.png`);

    appendLog(`--- RESULTADO GERAL: PASS ---`);
    appendLog(`  URL: ${currentUrl} (não redirecionou para /login)`);
    appendLog(`  Formulário: ${passwordInputs} input(s) password visíveis`);

    // Log do console ao final
    appendLog(`--- BROWSER CONSOLE TC-03 RERUN4 ---`);
    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog([...consoleLogs, ...pageErrors].join("\n"));
    } else {
      appendLog("(sem logs)");
    }

    // Assertions formais Playwright
    expect(redirectedToLogin).toBe(false);
    expect(passwordInputs).toBeGreaterThan(0);
  });
});
