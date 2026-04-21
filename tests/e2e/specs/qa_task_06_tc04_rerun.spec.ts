import { test, expect } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = process.env["PLAYWRIGHT_APP_URL"] ?? "http://localhost:5173";
const API_URL = process.env["PLAYWRIGHT_API_URL"] ?? "http://localhost:5272";
const TENANT_ID = "4a3d87ea-f62f-4d9c-80de-a34237d0dae3";
const SCREENSHOTS_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_06_log_auditoria/screenshots";
const EVIDENCE_LOG =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_06_log_auditoria/requests.log";

function appendLog(msg: string) {
  fs.appendFileSync(EVIDENCE_LOG, msg + "\n");
}

test.describe("TC-04 RERUN: UI — Log de auditoria no painel de detalhes (tenant ativado)", () => {
  test("TC-04: Navegar para detalhes do tenant ativado e verificar seção de audit log", async ({
    page,
    context,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    appendLog("========================================");
    appendLog("TC-04 RERUN: UI — Log de auditoria no painel de detalhes");
    appendLog(`Timestamp: ${new Date().toISOString()}`);
    appendLog(`Tenant ID: ${TENANT_ID}`);
    appendLog("========================================");

    // Step 1: Login via API to get cookie
    const loginResp = await context.request.post(
      `${API_URL}/api/v1/auth/login`,
      {
        data: { email: "operator@portabox.dev", password: "PortaBox123!" },
      }
    );
    appendLog(`Login status: ${loginResp.status()}`);

    if (loginResp.status() !== 200) {
      appendLog("FAIL: Login falhou. Abortando.");
      throw new Error(`Login failed with status ${loginResp.status()}`);
    }

    // Step 2: Navigate to the tenant detail page
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);

    // Screenshot inicial (antes do carregamento)
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun_tc04_inicio.png"),
      fullPage: true,
    });
    appendLog("Screenshot capturado: rerun_tc04_inicio.png");

    // Wait for page to fully load — either loading spinner disappears or content appears
    await page.waitForTimeout(4000);

    // Screenshot pós-carregamento
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun_tc04_pos_carga.png"),
      fullPage: true,
    });
    appendLog("Screenshot capturado: rerun_tc04_pos_carga.png");

    // Step 3: Verificar que a página não está em branco
    const bodyText = await page.locator("body").innerText().catch(() => "");
    const bodyHTML = await page.locator("body").innerHTML().catch(() => "");
    appendLog(`Body text length: ${bodyText.length}`);
    appendLog(`Body HTML length: ${bodyHTML.length}`);
    appendLog(`URL atual: ${page.url()}`);

    const isBlank =
      bodyText.trim().length < 10 ||
      bodyHTML.trim() === '<div id="root"></div>';
    appendLog(`Tela em branco detectada: ${isBlank}`);

    // Step 4: Verificar ausência de erros JavaScript
    if (pageErrors.length > 0) {
      appendLog("--- PAGE ERRORS DETECTADOS ---");
      pageErrors.forEach((e) => appendLog(e));
    } else {
      appendLog("Page errors: NENHUM");
    }

    // Step 5: Localizar seção "Histórico de auditoria"
    // O componente renderiza <h3>Histórico de auditoria</h3> e <ol aria-label="Histórico de auditoria">
    const auditHeading = page.locator("h3", { hasText: "Histórico de auditoria" });
    const auditHeadingVisible = await auditHeading.isVisible().catch(() => false);
    appendLog(`Heading "Histórico de auditoria" visível: ${auditHeadingVisible}`);

    const auditList = page.locator('[aria-label="Histórico de auditoria"]');
    const auditListVisible = await auditList.isVisible().catch(() => false);
    appendLog(`Lista de auditoria (aria-label) visível: ${auditListVisible}`);

    // Step 6: Contar registros de auditoria
    let auditItemCount = 0;
    if (auditListVisible) {
      const items = auditList.locator("li");
      auditItemCount = await items.count();
      appendLog(`Registros de auditoria encontrados: ${auditItemCount}`);

      // Capturar texto de cada item
      for (let i = 0; i < auditItemCount; i++) {
        const itemText = await items.nth(i).innerText().catch(() => "");
        appendLog(`  Item ${i + 1}: ${itemText.replace(/\n/g, " | ")}`);
      }
    }

    // Step 7: Verificar que há pelo menos criação (Criado) e ativação (Ativado)
    const pageContent = await page.content();
    const hasCriado = pageContent.includes("Criado");
    const hasAtivado = pageContent.includes("Ativado");
    appendLog(`Texto "Criado" presente na página: ${hasCriado}`);
    appendLog(`Texto "Ativado" presente na página: ${hasAtivado}`);

    // Screenshot com seção de auditoria visível (se encontrada)
    if (auditHeadingVisible || auditListVisible) {
      await page.screenshot({
        path: path.join(SCREENSHOTS_DIR, "rerun_tc04_audit_section.png"),
        fullPage: true,
      });
      appendLog("Screenshot da seção de audit: rerun_tc04_audit_section.png");
    }

    // Step 8: Log do console do browser
    appendLog("\n--- BROWSER CONSOLE TC-04 RERUN ---");
    if (consoleLogs.length > 0) {
      consoleLogs.forEach((l) => appendLog(l));
    } else {
      appendLog("(sem mensagens de console)");
    }
    if (pageErrors.length > 0) {
      appendLog("--- PAGE ERRORS ---");
      pageErrors.forEach((e) => appendLog(e));
    }

    // Step 9: Determinar resultado e registrar
    appendLog("\n--- RESULTADO TC-04 RERUN ---");

    if (isBlank || pageErrors.length > 0) {
      appendLog("STATUS: FAIL");
      appendLog("Motivo: Tela em branco ou erros JavaScript detectados");
    } else if (!auditHeadingVisible && !auditListVisible) {
      appendLog("STATUS: FAIL");
      appendLog("Motivo: Página carregou mas seção 'Histórico de auditoria' não encontrada");
    } else if (auditItemCount < 2) {
      appendLog("STATUS: FAIL");
      appendLog(`Motivo: Seção visível mas apenas ${auditItemCount} registro(s) encontrado(s) — esperado >= 2`);
    } else {
      appendLog("STATUS: PASS");
      appendLog(`Seção visível, ${auditItemCount} registro(s) exibido(s)`);
      appendLog(`Evento "Criado": ${hasCriado ? "PRESENTE" : "AUSENTE"}`);
      appendLog(`Evento "Ativado": ${hasAtivado ? "PRESENTE" : "AUSENTE"}`);
    }

    // Screenshot final
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, "rerun_tc04_final.png"),
      fullPage: true,
    });
    appendLog("Screenshot final: rerun_tc04_final.png");

    // Assertions formais
    expect(loginResp.status(), "Login deve retornar 200").toBe(200);
    expect(isBlank, "Página não deve estar em branco").toBe(false);
    expect(pageErrors.length, `Não deve haver erros JavaScript: ${pageErrors.join("; ")}`).toBe(0);
    expect(auditHeadingVisible, "Heading 'Histórico de auditoria' deve estar visível").toBe(true);
    expect(auditListVisible, "Lista de auditoria deve estar visível").toBe(true);
    expect(auditItemCount, "Deve haver ao menos 2 registros de auditoria").toBeGreaterThanOrEqual(2);
  });
});
