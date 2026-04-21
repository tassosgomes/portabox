/**
 * QA re-run 3 — CT-02 somente
 * Tenant: Residencial Rerun QA (4a3d87ea-f62f-4d9c-80de-a34237d0dae3)
 * data_assembleia = 2026-03-15  →  expected display: 15/03/2026
 * Verifica que a correção do formatDate() eliminou o off-by-one em UTC-3.
 */

import { test, expect } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = "http://localhost:5173";
const TENANT_ID = "4a3d87ea-f62f-4d9c-80de-a34237d0dae3";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_04_painel_detalhes_golive";
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const LOG_FILE = path.join(EVIDENCE_DIR, "requests.log");

function log(msg: string) {
  const line = `[${new Date().toISOString()}] ${msg}\n`;
  fs.appendFileSync(LOG_FILE, line);
}

function screenshot(name: string) {
  return path.join(SCREENSHOTS_DIR, `${name}.png`);
}

async function login(page: import("@playwright/test").Page) {
  await page.goto(`${BASE_URL}/login`);
  // Aguarda o campo de email estar visível (evita depender de networkidle com Vite HMR)
  await page.waitForSelector('#email', { state: 'visible', timeout: 20000 });
  await page.fill('#email', "operator@portabox.dev");
  await page.fill('#password', "PortaBox123!");
  await page.click('button[type="submit"]');
  await page.waitForURL(/condominios/, { timeout: 15000 });
}

test.describe("CT-02 rerun3 — Data assembleia off-by-one fix", () => {
  test("CT-02: Data da assembleia deve exibir 15/03/2026", async ({ page }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    page.on("pageerror", (err) => pageErrors.push(err.message));

    log("========================================");
    log("CT-02 (rerun3): Data assembleia off-by-one — iniciando");
    log(`Timestamp: ${new Date().toISOString()}`);
    log(`Tenant: ${TENANT_ID}`);
    log("Expected: 15/03/2026, NOT 14/03/2026");
    log("========================================");

    // Step 1: Login
    await login(page);
    log("CT-02 (rerun3): Login concluido");

    // Step 2: Navegar para página de detalhes
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    // Aguardar o conteúdo principal (nome do condomínio) estar visível
    await page.waitForSelector('body', { state: 'visible' });
    // Dar tempo suficiente para a API retornar os dados do opt-in
    await page.waitForTimeout(3000);
    await page.screenshot({ path: screenshot("rerun3_ct02_inicio"), fullPage: true });

    const pageText = await page.textContent("body") ?? "";
    log(`CT-02 (rerun3): Página carregada. URL: ${page.url()}`);
    log(`CT-02 (rerun3): Body (500 chars): ${pageText.slice(0, 500)}`);

    // Step 3: Verificar que a seção LGPD está presente
    const hasLGPDSection =
      pageText.toLowerCase().includes("lgpd") ||
      pageText.toLowerCase().includes("consentimento") ||
      pageText.toLowerCase().includes("assembleia");

    log(`CT-02 (rerun3): Seção LGPD/assembleia presente: ${hasLGPDSection}`);

    if (!hasLGPDSection) {
      await page.screenshot({ path: screenshot("rerun3_ct02_fail_sem_secao"), fullPage: true });
      log(`CT-02 (rerun3): FAIL — Seção LGPD/assembleia não encontrada`);
      log(`CT-02 (rerun3): Body completo (2000 chars): ${pageText.slice(0, 2000)}`);
      if (consoleLogs.length > 0 || pageErrors.length > 0) {
        log("--- BROWSER CONSOLE ---");
        [...consoleLogs, ...pageErrors].forEach((l) => log(l));
      }
      expect(hasLGPDSection, "Seção LGPD/Consentimento deve estar presente na página").toBe(true);
      return;
    }

    // Step 4: Verificar a data CORRETA (15/03/2026)
    const hasDateCorrect = pageText.includes("15/03/2026");

    // Step 5: Verificar ausência da data ERRADA (14/03/2026) — bug anterior
    const hasDateWrong = pageText.includes("14/03/2026");

    log(`CT-02 (rerun3): Data correta '15/03/2026' presente: ${hasDateCorrect}`);
    log(`CT-02 (rerun3): Data errada '14/03/2026' presente (deve ser false): ${hasDateWrong}`);

    // Log detalhado do trecho da página com a data
    const idxAssembleia = pageText.toLowerCase().indexOf("assembleia");
    if (idxAssembleia !== -1) {
      const snippet = pageText.slice(Math.max(0, idxAssembleia - 20), idxAssembleia + 150);
      log(`CT-02 (rerun3): Trecho com 'assembleia': "${snippet}"`);
    }

    // Screenshot pré-assertion
    await page.screenshot({ path: screenshot("rerun3_ct02_pre_assert"), fullPage: true });

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      log("--- BROWSER CONSOLE CT-02 rerun3 ---");
      [...consoleLogs, ...pageErrors].forEach((l) => log(l));
    }

    if (hasDateCorrect && !hasDateWrong) {
      await page.screenshot({ path: screenshot("rerun3_ct02_pass"), fullPage: true });
      log("CT-02 (rerun3): PASS — '15/03/2026' exibido, '14/03/2026' ausente");
    } else if (hasDateWrong) {
      await page.screenshot({ path: screenshot("rerun3_ct02_fail"), fullPage: true });
      log("CT-02 (rerun3): FAIL — '14/03/2026' ainda sendo exibido (bug não corrigido)");
      log(`CT-02 (rerun3): Expected: '15/03/2026' | Actual: '14/03/2026'`);
    } else {
      await page.screenshot({ path: screenshot("rerun3_ct02_fail"), fullPage: true });
      log(`CT-02 (rerun3): FAIL — '15/03/2026' não encontrado e '14/03/2026' também ausente`);
      log(`CT-02 (rerun3): Body (2000 chars): ${pageText.slice(0, 2000)}`);
    }

    // Assertion principal: data correta deve estar presente
    expect(
      hasDateCorrect,
      `Data da assembleia deve exibir '15/03/2026'. Encontrado '14/03/2026': ${hasDateWrong}. Body: ${pageText.slice(0, 500)}`
    ).toBe(true);

    // Assertion negativa: data errada NÃO deve estar presente
    expect(
      hasDateWrong,
      `Data errada '14/03/2026' NÃO deve aparecer (off-by-one bug deve estar corrigido)`
    ).toBe(false);
  });
});
