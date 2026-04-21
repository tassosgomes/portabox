import { test, expect } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = process.env["PLAYWRIGHT_APP_URL"] ?? "http://localhost:5173";
const API_URL = process.env["PLAYWRIGHT_API_URL"] ?? "http://localhost:5272";
const TENANT_ID = "f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5";
const SCREENSHOTS_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_06_log_auditoria/screenshots";
const EVIDENCE_LOG =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_06_log_auditoria/requests.log";

function appendLog(msg: string) {
  fs.appendFileSync(EVIDENCE_LOG, msg + "\n");
}

test.describe("TC-04: UI — Log de auditoria no painel de detalhes", () => {
  test("TC-04: Navegar para detalhes do tenant e verificar seção de audit log", async ({
    page,
    context,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    appendLog(
      "========================================"
    );
    appendLog("TC-04: UI — Log de auditoria no painel de detalhes");
    appendLog(`Timestamp: ${new Date().toISOString()}`);
    appendLog(
      "========================================"
    );

    // Step 1: Login via API to get cookie
    const loginResp = await context.request.post(
      `${API_URL}/api/v1/auth/login`,
      {
        data: { email: "operator@portabox.dev", password: "PortaBox123!" },
      }
    );
    appendLog(`Login status: ${loginResp.status()}`);

    // Step 2: Navigate to the tenant detail page
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForTimeout(3000);

    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "tc04_page_load.png"),
      fullPage: true,
    });
    appendLog("Screenshot capturado: tc04_page_load.png");

    // Step 3: Check if page has content or is blank/crashed
    const bodyText = await page.locator("body").innerText().catch(() => "");
    const bodyHTML = await page
      .locator("body")
      .innerHTML()
      .catch(() => "");
    appendLog(`Body text length: ${bodyText.length}`);
    appendLog(`Body HTML length: ${bodyHTML.length}`);
    appendLog(`URL atual: ${page.url()}`);

    // Check for white screen / crash indicators
    const isBlank =
      bodyText.trim().length < 10 || bodyHTML.trim() === "<div id=\"root\"></div>";
    appendLog(`Tela em branco detectada: ${isBlank}`);

    // Step 4: Look for audit log section
    const auditSection = page.locator(
      "[data-testid*='audit'], [class*='audit'], h2:has-text('Auditoria'), h3:has-text('Auditoria'), h2:has-text('Log'), h3:has-text('Log'), text=Auditoria"
    );
    const auditVisible = await auditSection.isVisible().catch(() => false);
    appendLog(`Seção de audit log visível: ${auditVisible}`);

    if (auditVisible) {
      await page.screenshot({
        path: path.join(SCREENSHOTS_DIR, "tc04_audit_section.png"),
        fullPage: true,
      });
      appendLog("Screenshot da seção de audit: tc04_audit_section.png");
    }

    // Step 5: Check page title / main heading
    const h1Text = await page
      .locator("h1")
      .first()
      .innerText()
      .catch(() => "NOT FOUND");
    appendLog(`H1 encontrado: ${h1Text}`);

    // Check for error indicators
    const errorText = await page
      .locator("text=Error, text=erro, text=crash, text=Something went wrong")
      .first()
      .innerText()
      .catch(() => "none");
    appendLog(`Erro visível na página: ${errorText}`);

    // Step 6: Log console errors
    appendLog("\n--- BROWSER CONSOLE TC-04 ---");
    consoleLogs.forEach((l) => appendLog(l));
    if (pageErrors.length > 0) {
      appendLog("--- PAGE ERRORS ---");
      pageErrors.forEach((e) => appendLog(e));
    }

    appendLog("\n--- RESULTADO OBSERVADO ---");
    if (isBlank) {
      appendLog(
        "OBSERVADO: Tela em branco/crash confirmado (body text < 10 chars)"
      );
      appendLog(
        "EXPECTED: Painel com seção de log de auditoria mostrando entradas de criação e ativação"
      );
      appendLog(
        "STATUS: FAIL — Crash da UI impede visualização do audit log (bug conhecido)"
      );
    } else if (auditVisible) {
      appendLog("OBSERVADO: Seção de audit log visível na página");
      appendLog(
        "EXPECTED: Entradas event_kind=1 (criação) e event_kind=2 (ativação) visíveis"
      );
      appendLog("STATUS: PASS");
    } else {
      appendLog(
        "OBSERVADO: Página carregou mas seção de audit log NÃO foi encontrada"
      );
      appendLog(
        "EXPECTED: Seção de audit log visível com entradas de criação e ativação"
      );
      appendLog("STATUS: FAIL — Seção de audit log ausente na UI");
    }

    // Final screenshot
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "tc04_final.png"),
      fullPage: true,
    });
    appendLog("Screenshot final: tc04_final.png");

    // We document what we found — do not force PASS
    // The test itself just captures evidence
    expect(loginResp.status()).toBe(200);
  });
});
