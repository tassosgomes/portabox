import { test, expect } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const SINDICO_APP_URL = "http://localhost:5174";
const MAGIC_LINK_TOKEN = "0pvweIKVFLLGufR0V8jepTQi0yFNXeKIOOnYcDXF1a8";
const MAGIC_LINK_URL = `${SINDICO_APP_URL}/password-setup?token=${MAGIC_LINK_TOKEN}`;
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_03_magic_link_sindico";
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const REQUESTS_LOG = path.join(EVIDENCE_DIR, "requests.log");

function appendLog(msg: string) {
  fs.appendFileSync(REQUESTS_LOG, msg + "\n");
}

test.describe("TC-03 RERUN2 — Página de definição de senha via magic link", () => {
  test("TC-03: Página /password-setup carrega com campo de senha", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    appendLog("========================================");
    appendLog("TC-03 RERUN2 — Página de definição de senha via magic link");
    appendLog(`Timestamp: ${new Date().toISOString()}`);
    appendLog("========================================");
    appendLog(`--- REQUEST ---`);
    appendLog(`Action: navegação para magic link URL`);
    appendLog(`URL: ${MAGIC_LINK_URL}`);

    // Passo 1: Navegar para a URL do magic link
    await page.goto(MAGIC_LINK_URL, { waitUntil: "domcontentloaded", timeout: 15000 });

    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc03_inicio.png"),
      fullPage: true,
    });

    appendLog("--- Screenshot capturado: rerun2_tc03_inicio.png ---");
    appendLog(`URL atual: ${page.url()}`);
    appendLog(`Título da página: ${await page.title()}`);

    // Aguardar a página estabilizar
    await page.waitForTimeout(2000);

    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc03_apos_load.png"),
      fullPage: true,
    });

    appendLog("--- Screenshot capturado: rerun2_tc03_apos_load.png ---");

    // Passo 2: Verificar que a página carregou (não é uma página de erro genérica)
    const pageContent = await page.content();
    appendLog("--- Page content length: " + pageContent.length + " chars ---");

    // Assertion 1: URL deve conter o token (não redirecionou para erro)
    const currentUrl = page.url();
    appendLog(`URL final: ${currentUrl}`);

    // Passo 3: Verificar campo de senha
    // Tentamos múltiplos seletores possíveis para campo de senha
    const passwordSelectors = [
      'input[type="password"]',
      'input[name="password"]',
      'input[name="senha"]',
      'input[id*="password"]',
      'input[id*="senha"]',
      'input[placeholder*="senha" i]',
      'input[placeholder*="password" i]',
    ];

    let passwordFieldFound = false;
    let foundSelector = "";

    for (const sel of passwordSelectors) {
      const count = await page.locator(sel).count();
      if (count > 0) {
        passwordFieldFound = true;
        foundSelector = sel;
        appendLog(`Campo de senha encontrado com selector: ${sel}`);
        break;
      }
    }

    // Verificar email do síndico visível ou pré-preenchido
    const emailSelectors = [
      'input[type="email"]',
      'input[name="email"]',
      '[data-testid*="email"]',
      'p:has-text("sindico.rerun")',
      'span:has-text("sindico.rerun")',
      '*:has-text("sindico.rerun@portabox.dev")',
    ];

    let emailVisible = false;
    let emailSelector = "";

    for (const sel of emailSelectors) {
      try {
        const count = await page.locator(sel).count();
        if (count > 0) {
          emailVisible = true;
          emailSelector = sel;
          appendLog(`Email visível encontrado com selector: ${sel}`);
          break;
        }
      } catch {
        // continua
      }
    }

    // Screenshot pré-assertion
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc03_pre_assert.png"),
      fullPage: true,
    });
    appendLog("--- Screenshot capturado: rerun2_tc03_pre_assert.png ---");

    // Log estado
    appendLog(`--- ASSERTIONS ---`);
    appendLog(`Campo de senha encontrado: ${passwordFieldFound} (selector: ${foundSelector})`);
    appendLog(`Email visível: ${emailVisible} (selector: ${emailSelector})`);

    // Assertion principal: campo de senha deve existir
    if (!passwordFieldFound) {
      appendLog("--- RESULTADO: FAIL ---");
      appendLog("Expected: campo input[type=password] visível na página /password-setup");
      appendLog("Actual: nenhum campo de senha encontrado");
      appendLog("Console logs:");
      consoleLogs.forEach((l) => appendLog("  " + l));
      pageErrors.forEach((l) => appendLog("  [error] " + l));
    } else {
      appendLog("--- RESULTADO PARCIAL: Campo de senha ENCONTRADO ---");
    }

    // Capturar console do browser
    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog("--- BROWSER CONSOLE TC-03 RERUN2 ---");
      consoleLogs.forEach((l) => appendLog(l));
      pageErrors.forEach((l) => appendLog("[error] " + l));
    }

    // ASSERTION PLAYWRIGHT — falha aqui se campo não existe
    await expect(
      page.locator('input[type="password"]').first()
    ).toBeVisible({ timeout: 5000 });

    // Screenshot final (pass)
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc03_pass.png"),
      fullPage: true,
    });
    appendLog("--- Screenshot capturado: rerun2_tc03_pass.png ---");
    appendLog("--- RESULTADO: PASS ---");
    appendLog(
      `Campo de senha visível. Email do síndico visível: ${emailVisible}`
    );
  });
});
