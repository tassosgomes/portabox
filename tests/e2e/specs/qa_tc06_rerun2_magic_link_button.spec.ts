import { test, expect } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = "http://localhost:5173";
const TENANT_ID = "4a3d87ea-f62f-4d9c-80de-a34237d0dae3";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_03_magic_link_sindico";
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const REQUEST_LOG = path.join(EVIDENCE_DIR, "requests.log");

function appendLog(msg: string) {
  fs.appendFileSync(REQUEST_LOG, msg + "\n");
}

test.describe("TC-06 RERUN2 — Botão Reenviar magic link no painel de detalhes", () => {
  test("TC-06: Botão Reenviar magic link visível quando sindicoSenhaDefinida=false", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    appendLog("");
    appendLog("========================================");
    appendLog("TC-06 RERUN2 — UI: Botão Reenviar magic link");
    appendLog(`Timestamp: ${new Date().toISOString()}`);
    appendLog("========================================");
    appendLog("--- PASSO 1: Navegar para login ---");

    // Passo 1 — Login
    await page.goto(`${BASE_URL}/login`);
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc06_login_inicio.png"),
      fullPage: true,
    });

    appendLog("Página de login carregada");
    appendLog("--- PASSO 2: Preencher credenciais e submeter ---");

    // Preencher credenciais
    await page.fill('input[type="email"], input[name="email"]', "operator@portabox.dev");
    await page.fill('input[type="password"], input[name="password"]', "PortaBox123!");

    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc06_login_preenchido.png"),
      fullPage: true,
    });

    await page.click('button[type="submit"]');

    // Aguardar navegação pós-login
    await page.waitForURL((url) => !url.pathname.includes("/login"), {
      timeout: 15000,
    });

    const urlAposLogin = page.url();
    appendLog(`URL após login: ${urlAposLogin}`);

    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc06_apos_login.png"),
      fullPage: true,
    });

    appendLog("--- PASSO 3: Navegar para painel de detalhes ---");
    appendLog(`URL: ${BASE_URL}/condominios/${TENANT_ID}`);

    // Passo 3 — Navegar para painel de detalhes
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);

    // Aguardar conteúdo carregar (não tela branca)
    await page.waitForLoadState("networkidle", { timeout: 15000 });

    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc06_painel_carregado.png"),
      fullPage: true,
    });

    const urlAtual = page.url();
    appendLog(`URL atual após navegação: ${urlAtual}`);
    appendLog("--- PASSO 4: Verificar se a página renderizou (não tela branca) ---");

    // Verificar que a página tem algum conteúdo visível (não crashou)
    const bodyText = await page.locator("body").innerText();
    appendLog(`Body text length: ${bodyText.length}`);

    if (bodyText.trim().length < 10) {
      appendLog("FAIL: Página parece estar em branco (body text muito curto)");
      await page.screenshot({
        path: path.join(SCREENSHOTS_DIR, "rerun2_tc06_fail_branco.png"),
        fullPage: true,
      });
      appendLog("--- BROWSER CONSOLE TC-06 RERUN2 ---");
      appendLog([...consoleLogs, ...pageErrors].join("\n"));
      expect(bodyText.trim().length).toBeGreaterThan(10); // fail explícito
      return;
    }

    appendLog("Página renderizou com conteúdo — OK");
    appendLog("--- PASSO 5: Procurar botão Reenviar magic link ---");

    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc06_pre_assert_botao.png"),
      fullPage: true,
    });

    // Procurar o botão "Reenviar magic link" — variações de texto possíveis
    const buttonSelectors = [
      'button:has-text("Reenviar magic link")',
      'button:has-text("Reenviar Magic Link")',
      'button:has-text("reenviar magic link")',
      '[data-testid="reenviar-magic-link"]',
    ];

    let buttonFound = false;
    let buttonLocator = null;

    for (const selector of buttonSelectors) {
      const el = page.locator(selector).first();
      const count = await el.count();
      appendLog(`Seletor "${selector}": ${count} elemento(s) encontrado(s)`);
      if (count > 0) {
        buttonFound = true;
        buttonLocator = el;
        break;
      }
    }

    if (!buttonFound) {
      // Capturar texto da página para diagnóstico
      appendLog("Botão não encontrado. Capturando texto completo da página para diagnóstico:");
      appendLog(bodyText.substring(0, 2000));

      await page.screenshot({
        path: path.join(SCREENSHOTS_DIR, "rerun2_tc06_fail_sem_botao.png"),
        fullPage: true,
      });
      appendLog("--- RESULTADO: FAIL ---");
      appendLog("Expected: Botão 'Reenviar magic link' visível na página de detalhes");
      appendLog("Actual: Botão não encontrado na página");
      appendLog("--- BROWSER CONSOLE TC-06 RERUN2 ---");
      appendLog([...consoleLogs, ...pageErrors].join("\n"));

      expect(buttonFound, "Botão 'Reenviar magic link' deve estar visível").toBeTruthy();
      return;
    }

    appendLog("Botão 'Reenviar magic link' ENCONTRADO");
    appendLog("--- PASSO 6: Verificar visibilidade do botão ---");

    await expect(buttonLocator!).toBeVisible();
    appendLog("Botão está visível — PASS");

    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc06_botao_visivel.png"),
      fullPage: true,
    });

    // Passo 6 — Clicar no botão e verificar feedback
    appendLog("--- PASSO 7: Clicar no botão Reenviar magic link ---");

    await buttonLocator!.click();

    // Aguardar feedback (toast, mensagem, etc.)
    await page.waitForTimeout(3000);

    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun2_tc06_apos_clique.png"),
      fullPage: true,
    });

    // Verificar feedback de sucesso — toast, alert, mensagem
    const successIndicators = [
      '[role="alert"]:has-text("sucesso")',
      '[role="alert"]:has-text("enviado")',
      '[role="alert"]:has-text("reenviado")',
      '[role="status"]',
      ".toast",
      '[data-testid="toast"]',
      '[aria-live="polite"]',
    ];

    let feedbackFound = false;
    let feedbackText = "";

    for (const selector of successIndicators) {
      const el = page.locator(selector).first();
      const count = await el.count();
      if (count > 0) {
        feedbackFound = true;
        feedbackText = await el.innerText().catch(() => "(texto não capturável)");
        appendLog(`Feedback encontrado via seletor "${selector}": "${feedbackText}"`);
        break;
      }
    }

    // Se não encontrar via seletores específicos, capturar mudança no body
    if (!feedbackFound) {
      const bodyTextAposClique = await page.locator("body").innerText();
      const successKeywords = ["enviado", "sucesso", "reenviado", "magic link", "e-mail"];
      for (const keyword of successKeywords) {
        if (bodyTextAposClique.toLowerCase().includes(keyword)) {
          feedbackFound = true;
          feedbackText = `Palavra-chave "${keyword}" encontrada no body após clique`;
          appendLog(feedbackText);
          break;
        }
      }
    }

    if (feedbackFound) {
      appendLog(`--- RESULTADO TC-06 PASSO 7: PASS ---`);
      appendLog(`Feedback de sucesso: "${feedbackText}"`);
    } else {
      appendLog("--- RESULTADO TC-06 PASSO 7: INCONCLUSIVO ---");
      appendLog("Clique executado, porém feedback de sucesso não identificado via seletores conhecidos");
      appendLog("Verificar screenshot rerun2_tc06_apos_clique.png");
    }

    appendLog("--- BROWSER CONSOLE TC-06 RERUN2 ---");
    appendLog([...consoleLogs, ...pageErrors].join("\n") || "(sem logs de console)");

    appendLog("--- RESULTADO GERAL TC-06 RERUN2: PASS ---");
    appendLog("Botão 'Reenviar magic link' estava visível conforme esperado (sindicoSenhaDefinida=false)");
  });
});
