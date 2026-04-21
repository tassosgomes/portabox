/**
 * QA Task 05 — Re-execucao 2: TC-03 a TC-08
 *
 * ESTADO REAL DO BANCO (confirmado via API em 2026-04-20T17:00Z):
 *   - "Residencial Rerun QA"           status=2 (Ativo)    ID: 4a3d87ea-f62f-4d9c-80de-a34237d0dae3
 *     NOTA: O briefing informava status=1 (PreAtivo), mas este tenant foi ativado
 *     em 2026-04-20T16:55:45 (durante a sessao de testes anterior a esta).
 *   - "Residencial Teste QA API Check" status=1 (PreAtivo)  ID: 1792efd1-3e2b-4156-8e55-7b7c43576fe0
 *   - "Residencial Teste QA"           status=2 (Ativo)    ID: f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5
 *
 * TC-03: Filtro Pre-ativo — apenas "Residencial Teste QA API Check" aparece
 * TC-04: Filtro Ativo — "Residencial Rerun QA" E "Residencial Teste QA" aparecem
 * TC-05: Busca "Rerun" — apenas "Residencial Rerun QA" aparece
 * TC-06: Busca "444" (CNPJ 11.444.777/0001-61) — apenas "Residencial Rerun QA" aparece
 * TC-07: Clique em "Residencial Rerun QA" -> URL /condominios/4a3d87ea-f62f-4d9c-80de-a34237d0dae3
 * TC-08: Paginacao ausente (totalCount=3, PAGE_SIZE=20)
 */

import { test, expect, type BrowserContext, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = "http://localhost:5173";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_05_lista_tenants";
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const LOG_FILE = path.join(EVIDENCE_DIR, "requests.log");

// Estado real confirmado via API
const TENANT_ATIVO_NAME = "Residencial Teste QA";
const TENANT_ATIVO_ID = "f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5";
const TENANT_RERUN_NAME = "Residencial Rerun QA";
const TENANT_RERUN_ID = "4a3d87ea-f62f-4d9c-80de-a34237d0dae3";
const TENANT_APICHECK_NAME = "Residencial Teste QA API Check";

function log(msg: string) {
  const line = `[${new Date().toISOString()}] ${msg}\n`;
  fs.appendFileSync(LOG_FILE, line);
}

function sc(name: string) {
  return path.join(SCREENSHOTS_DIR, `rerun2_${name}.png`);
}

test.describe.serial("QA Task 05 Rerun2 — TC-03 a TC-08", () => {
  let authPage: Page;
  let authContext: BrowserContext;

  test.beforeAll(async ({ browser }) => {
    log("========================================");
    log("RERUN2 SETUP: Login para suite TC-03 a TC-08");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("ESTADO REAL DO BANCO (confirmado via API):");
    log("  Residencial Rerun QA: status=2 (Ativo) — briefing dizia PreAtivo, mas foi ativado em sessao anterior");
    log("  Residencial Teste QA API Check: status=1 (PreAtivo)");
    log("  Residencial Teste QA: status=2 (Ativo)");
    log("========================================");

    authContext = await browser.newContext();
    authPage = await authContext.newPage();

    authPage.on("console", (msg) => {
      if (msg.type() === "error") log(`[BROWSER_CONSOLE_ERROR] ${msg.text()}`);
    });
    authPage.on("pageerror", (err) => log(`[PAGE_ERROR] ${err.message}`));

    await authPage.goto(`${BASE_URL}/login`);
    await authPage.screenshot({ path: sc("setup_login"), fullPage: true });

    await authPage.locator("#email").first().fill("operator@portabox.dev");
    await authPage.locator("#password").first().fill("PortaBox123!");

    await authPage.screenshot({ path: sc("setup_login_preenchido"), fullPage: true });
    await authPage.click('button[type="submit"]');
    await authPage.waitForURL(/condominios/, { timeout: 20000 });

    await authPage.screenshot({ path: sc("setup_login_sucesso"), fullPage: true });
    log("RERUN2 SETUP: Login realizado, sessao ativa");
  });

  test.afterAll(async () => {
    await authContext.close();
    log("RERUN2 SETUP: Context encerrado");
  });

  // ---------------------------------------------------------------------------
  // TC-03 (re-execucao corrigida)
  // Estado real: status=1 tem apenas "Residencial Teste QA API Check"
  // Filtro Pre-ativo: apenas API Check aparece; Rerun QA (agora Ativo) NAO aparece;
  //                  Teste QA (Ativo) NAO aparece.
  // ---------------------------------------------------------------------------
  test("TC-03: Filtro Pre-ativo — apenas tenants PreAtivos aparecem; Ativos ausentes", async () => {
    log("========================================");
    log("RERUN2 TC-03: Filtro Pre-ativo (assertion corrigida para estado real do banco)");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("Expected: apenas 'Residencial Teste QA API Check' visivel (unico status=1)");
    log("Expected: 'Residencial Rerun QA' (agora status=2) NAO aparece");
    log("Expected: 'Residencial Teste QA' (status=2) NAO aparece");
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table tbody tr", { timeout: 15000 });
    await authPage.screenshot({ path: sc("tc03_antes_filtro"), fullPage: true });

    const allBtns = await authPage.locator("[role='group'] button").allTextContents();
    log(`TC-03: Botoes de filtro encontrados: ${JSON.stringify(allBtns)}`);

    const preAtivoBtn = authPage.locator("[role='group'] button").filter({ hasText: /pré-ativo/i });
    const btnCount = await preAtivoBtn.count();
    log(`TC-03: Botoes 'Pré-ativo' encontrados: ${btnCount}`);

    if (btnCount === 0) {
      await authPage.screenshot({ path: sc("tc03_fail_btn_ausente"), fullPage: true });
      log(`TC-03 FAIL — Expected: botao 'Pré-ativo' presente. Actual: botoes=${JSON.stringify(allBtns)}`);
      throw new Error(`Botao 'Pré-ativo' nao encontrado. Botoes: ${JSON.stringify(allBtns)}`);
    }

    await preAtivoBtn.click();
    log("TC-03: Clicou no filtro 'Pré-ativo'");
    await authPage.waitForTimeout(1500);
    await authPage.screenshot({ path: sc("tc03_resultado_filtro"), fullPage: true });

    const rows = await authPage.locator("table tbody tr").allTextContents();
    log(`TC-03: Linhas na tabela apos filtro Pre-ativo: ${JSON.stringify(rows)}`);

    // Assertion 1: "Residencial Teste QA API Check" (status=1=PreAtivo) DEVE aparecer
    const apiCheckRows = await authPage.locator("table tbody tr").filter({ hasText: TENANT_APICHECK_NAME }).count();
    log(`TC-03: "${TENANT_APICHECK_NAME}" count: ${apiCheckRows}`);
    if (apiCheckRows === 0) {
      await authPage.screenshot({ path: sc("tc03_fail_apicheck_ausente"), fullPage: true });
      log(`TC-03 FAIL — Expected: "${TENANT_APICHECK_NAME}" (status=1) visivel com filtro Pre-ativo`);
      log(`TC-03 FAIL — Actual: apiCheckRows=0`);
    }
    expect(apiCheckRows, `"${TENANT_APICHECK_NAME}" deve aparecer com filtro Pré-ativo`).toBeGreaterThanOrEqual(1);

    // Assertion 2: "Residencial Teste QA" (status=2=Ativo) NAO deve aparecer (texto exato)
    const ativoExactRows = await authPage
      .getByRole("row")
      .filter({ has: authPage.getByText(TENANT_ATIVO_NAME, { exact: true }) })
      .count();
    log(`TC-03: "${TENANT_ATIVO_NAME}" (exact) count: ${ativoExactRows}`);
    if (ativoExactRows > 0) {
      await authPage.screenshot({ path: sc("tc03_fail_ativo_apareceu"), fullPage: true });
      log(`TC-03 FAIL — Expected: "${TENANT_ATIVO_NAME}" (status=2) NAO aparece com filtro Pre-ativo`);
      log(`TC-03 FAIL — Actual: ativoExactRows=${ativoExactRows}`);
    }
    expect(ativoExactRows, `"${TENANT_ATIVO_NAME}" (status=2=Ativo) NAO deve aparecer com filtro Pré-ativo`).toBe(0);

    // Assertion 3: "Residencial Rerun QA" (agora status=2=Ativo) NAO deve aparecer
    const rerunRows = await authPage.locator("table tbody tr").filter({ hasText: TENANT_RERUN_NAME }).count();
    log(`TC-03: "${TENANT_RERUN_NAME}" (agora Ativo) count: ${rerunRows}`);
    if (rerunRows > 0) {
      await authPage.screenshot({ path: sc("tc03_fail_rerun_apareceu"), fullPage: true });
      log(`TC-03 FAIL — Expected: "${TENANT_RERUN_NAME}" (agora status=2=Ativo) NAO aparece com filtro Pre-ativo`);
      log(`TC-03 FAIL — Actual: rerunRows=${rerunRows}`);
    }
    expect(rerunRows, `"${TENANT_RERUN_NAME}" (agora status=2=Ativo) NAO deve aparecer com filtro Pré-ativo`).toBe(0);

    await authPage.screenshot({ path: sc("tc03_pass"), fullPage: true });
    log("TC-03: PASS");
  });

  // ---------------------------------------------------------------------------
  // TC-04: Filtro Ativo
  // Estado real: status=2 tem "Residencial Rerun QA" E "Residencial Teste QA"
  // ---------------------------------------------------------------------------
  test("TC-04: Filtro Ativo — ambos tenants Ativos aparecem; PreAtivo ausente", async () => {
    log("========================================");
    log("RERUN2 TC-04: Filtro Ativo (estado real: 2 tenants Ativos)");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("Expected: 'Residencial Rerun QA' (status=2) aparece");
    log("Expected: 'Residencial Teste QA' (status=2) aparece");
    log("Expected: 'Residencial Teste QA API Check' (status=1) NAO aparece");
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table tbody tr", { timeout: 15000 });
    await authPage.screenshot({ path: sc("tc04_antes_filtro"), fullPage: true });

    const allBtns = await authPage.locator("[role='group'] button").allTextContents();
    log(`TC-04: Botoes de filtro encontrados: ${JSON.stringify(allBtns)}`);

    // Clica em "Ativo" — usa getByText exact para nao clicar em "Pré-ativo"
    const ativoBtn = authPage.locator("[role='group'] button").getByText("Ativo", { exact: true });
    const btnCount = await ativoBtn.count();
    log(`TC-04: Botoes 'Ativo' (exact) encontrados: ${btnCount}`);

    if (btnCount === 0) {
      await authPage.screenshot({ path: sc("tc04_fail_btn_ausente"), fullPage: true });
      log(`TC-04 FAIL — Expected: botao 'Ativo' presente. Actual: botoes=${JSON.stringify(allBtns)}`);
      throw new Error(`Botao 'Ativo' nao encontrado. Botoes: ${JSON.stringify(allBtns)}`);
    }

    await ativoBtn.click();
    log("TC-04: Clicou no filtro 'Ativo'");
    await authPage.waitForTimeout(1500);
    await authPage.screenshot({ path: sc("tc04_resultado_filtro"), fullPage: true });

    const rows = await authPage.locator("table tbody tr").allTextContents();
    log(`TC-04: Linhas na tabela apos filtro Ativo: ${JSON.stringify(rows)}`);

    // Assertion 1: "Residencial Rerun QA" (status=2=Ativo) DEVE aparecer
    const rerunRows = await authPage.locator("table tbody tr").filter({ hasText: TENANT_RERUN_NAME }).count();
    log(`TC-04: "${TENANT_RERUN_NAME}" count: ${rerunRows}`);
    if (rerunRows === 0) {
      await authPage.screenshot({ path: sc("tc04_fail_rerun_ausente"), fullPage: true });
      log(`TC-04 FAIL — Expected: "${TENANT_RERUN_NAME}" (status=2=Ativo) visivel com filtro Ativo`);
      log(`TC-04 FAIL — Actual: rerunRows=0`);
    }
    expect(rerunRows, `"${TENANT_RERUN_NAME}" deve aparecer com filtro Ativo`).toBeGreaterThanOrEqual(1);

    // Assertion 2: "Residencial Teste QA" (status=2=Ativo) DEVE aparecer (texto exato)
    const ativoRows = await authPage
      .getByRole("row")
      .filter({ has: authPage.getByText(TENANT_ATIVO_NAME, { exact: true }) })
      .count();
    log(`TC-04: "${TENANT_ATIVO_NAME}" (exact) count: ${ativoRows}`);
    if (ativoRows === 0) {
      await authPage.screenshot({ path: sc("tc04_fail_ativo_ausente"), fullPage: true });
      log(`TC-04 FAIL — Expected: "${TENANT_ATIVO_NAME}" (status=2=Ativo) visivel com filtro Ativo`);
      log(`TC-04 FAIL — Actual: ativoRows=0`);
    }
    expect(ativoRows, `"${TENANT_ATIVO_NAME}" deve aparecer com filtro Ativo`).toBeGreaterThanOrEqual(1);

    // Assertion 3: "Residencial Teste QA API Check" (status=1=PreAtivo) NAO deve aparecer
    const apiCheckRows = await authPage.locator("table tbody tr").filter({ hasText: TENANT_APICHECK_NAME }).count();
    log(`TC-04: "${TENANT_APICHECK_NAME}" count: ${apiCheckRows}`);
    if (apiCheckRows > 0) {
      await authPage.screenshot({ path: sc("tc04_fail_apicheck_apareceu"), fullPage: true });
      log(`TC-04 FAIL — Expected: "${TENANT_APICHECK_NAME}" (status=1) NAO aparece com filtro Ativo`);
      log(`TC-04 FAIL — Actual: apiCheckRows=${apiCheckRows}`);
    }
    expect(apiCheckRows, `"${TENANT_APICHECK_NAME}" (status=1=PreAtivo) NAO deve aparecer com filtro Ativo`).toBe(0);

    await authPage.screenshot({ path: sc("tc04_pass"), fullPage: true });
    log("TC-04: PASS");
  });

  // ---------------------------------------------------------------------------
  // TC-05: Busca por nome "Rerun"
  // ---------------------------------------------------------------------------
  test("TC-05: Busca por nome 'Rerun' retorna apenas Residencial Rerun QA", async () => {
    log("========================================");
    log("RERUN2 TC-05: Busca por nome 'Rerun'");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("Expected: 'Residencial Rerun QA' aparece; outros ausentes");
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table tbody tr", { timeout: 15000 });

    const searchInput = authPage
      .locator("input[aria-label='Buscar condomínio'], input[placeholder*='uscar'], input[type='search']")
      .first();
    const searchVisible = await searchInput.isVisible();
    log(`TC-05: Campo de busca visivel: ${searchVisible}`);

    if (!searchVisible) {
      await authPage.screenshot({ path: sc("tc05_fail_campo_ausente"), fullPage: true });
      log(`TC-05 FAIL — Expected: campo de busca visivel. Actual: nao encontrado`);
      throw new Error("Campo de busca nao encontrado na pagina");
    }

    await searchInput.clear();
    await searchInput.fill("Rerun");
    log("TC-05: Digitou 'Rerun' no campo de busca");

    await authPage.waitForTimeout(1500);
    await authPage.screenshot({ path: sc("tc05_resultado_busca"), fullPage: true });

    const rows = await authPage.locator("table tbody tr").allTextContents();
    log(`TC-05: Linhas na tabela apos busca 'Rerun': ${JSON.stringify(rows)}`);

    // Assertion 1: "Residencial Rerun QA" DEVE aparecer
    const rerunRows = await authPage.locator("table tbody tr").filter({ hasText: TENANT_RERUN_NAME }).count();
    log(`TC-05: "${TENANT_RERUN_NAME}" count: ${rerunRows}`);
    if (rerunRows === 0) {
      await authPage.screenshot({ path: sc("tc05_fail_rerun_ausente"), fullPage: true });
      log(`TC-05 FAIL — Expected: "${TENANT_RERUN_NAME}" no resultado da busca por 'Rerun'`);
      log(`TC-05 FAIL — Actual: rerunRows=0. Linhas: ${JSON.stringify(rows)}`);
    }
    expect(rerunRows, `"${TENANT_RERUN_NAME}" deve aparecer na busca por 'Rerun'`).toBeGreaterThanOrEqual(1);

    // Assertion 2: "Residencial Teste QA" NAO deve aparecer (exact match)
    const ativoRows = await authPage
      .getByRole("row")
      .filter({ has: authPage.getByText(TENANT_ATIVO_NAME, { exact: true }) })
      .count();
    log(`TC-05: "${TENANT_ATIVO_NAME}" (exact) count: ${ativoRows}`);
    if (ativoRows > 0) {
      await authPage.screenshot({ path: sc("tc05_fail_ativo_apareceu"), fullPage: true });
      log(`TC-05 FAIL — Expected: "${TENANT_ATIVO_NAME}" NAO aparece na busca por 'Rerun'`);
      log(`TC-05 FAIL — Actual: ativoRows=${ativoRows}`);
    }
    expect(ativoRows, `"${TENANT_ATIVO_NAME}" NAO deve aparecer na busca por 'Rerun'`).toBe(0);

    await authPage.screenshot({ path: sc("tc05_pass"), fullPage: true });
    log("TC-05: PASS");
  });

  // ---------------------------------------------------------------------------
  // TC-06: Busca por CNPJ parcial "444"
  // CNPJ de "Residencial Rerun QA": 11.444.777/0001-61
  // ---------------------------------------------------------------------------
  test("TC-06: Busca por CNPJ '444' retorna apenas Residencial Rerun QA", async () => {
    log("========================================");
    log("RERUN2 TC-06: Busca por CNPJ parcial '444'");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("Expected: '${TENANT_RERUN_NAME}' (CNPJ 11.444.777/0001-61) aparece; outros ausentes");
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table tbody tr", { timeout: 15000 });

    const searchInput = authPage
      .locator("input[aria-label='Buscar condomínio'], input[placeholder*='uscar'], input[type='search']")
      .first();
    const searchVisible = await searchInput.isVisible();
    log(`TC-06: Campo de busca visivel: ${searchVisible}`);

    if (!searchVisible) {
      await authPage.screenshot({ path: sc("tc06_fail_campo_ausente"), fullPage: true });
      log(`TC-06 FAIL — Expected: campo de busca visivel. Actual: nao encontrado`);
      throw new Error("Campo de busca nao encontrado na pagina");
    }

    await searchInput.clear();
    await searchInput.fill("444");
    log("TC-06: Digitou '444' no campo de busca");

    await authPage.waitForTimeout(1500);
    await authPage.screenshot({ path: sc("tc06_resultado_busca"), fullPage: true });

    const rows = await authPage.locator("table tbody tr").allTextContents();
    log(`TC-06: Linhas na tabela apos busca '444': ${JSON.stringify(rows)}`);

    // Assertion 1: "Residencial Rerun QA" DEVE aparecer
    const rerunRows = await authPage.locator("table tbody tr").filter({ hasText: TENANT_RERUN_NAME }).count();
    log(`TC-06: "${TENANT_RERUN_NAME}" count: ${rerunRows}`);
    if (rerunRows === 0) {
      await authPage.screenshot({ path: sc("tc06_fail_rerun_ausente"), fullPage: true });
      log(`TC-06 FAIL — Expected: "${TENANT_RERUN_NAME}" no resultado da busca por CNPJ '444'`);
      log(`TC-06 FAIL — Actual: rerunRows=0. Linhas: ${JSON.stringify(rows)}`);
    }
    expect(rerunRows, `"${TENANT_RERUN_NAME}" deve aparecer na busca por CNPJ '444'`).toBeGreaterThanOrEqual(1);

    // Assertion 2: "Residencial Teste QA" NAO deve aparecer (CNPJ diferente)
    const ativoRows = await authPage
      .getByRole("row")
      .filter({ has: authPage.getByText(TENANT_ATIVO_NAME, { exact: true }) })
      .count();
    log(`TC-06: "${TENANT_ATIVO_NAME}" (exact) count: ${ativoRows}`);
    if (ativoRows > 0) {
      await authPage.screenshot({ path: sc("tc06_fail_ativo_apareceu"), fullPage: true });
      log(`TC-06 FAIL — Expected: "${TENANT_ATIVO_NAME}" NAO aparece na busca por '444'`);
      log(`TC-06 FAIL — Actual: ativoRows=${ativoRows}`);
    }
    expect(ativoRows, `"${TENANT_ATIVO_NAME}" NAO deve aparecer na busca por CNPJ '444'`).toBe(0);

    // Assertion 3: "Residencial Teste QA API Check" NAO deve aparecer (CNPJ diferente)
    const apiCheckRows = await authPage.locator("table tbody tr").filter({ hasText: TENANT_APICHECK_NAME }).count();
    log(`TC-06: "${TENANT_APICHECK_NAME}" count: ${apiCheckRows}`);
    if (apiCheckRows > 0) {
      await authPage.screenshot({ path: sc("tc06_fail_apicheck_apareceu"), fullPage: true });
      log(`TC-06 FAIL — Expected: "${TENANT_APICHECK_NAME}" NAO aparece na busca por '444'`);
      log(`TC-06 FAIL — Actual: apiCheckRows=${apiCheckRows}`);
    }
    expect(apiCheckRows, `"${TENANT_APICHECK_NAME}" NAO deve aparecer na busca por CNPJ '444'`).toBe(0);

    await authPage.screenshot({ path: sc("tc06_pass"), fullPage: true });
    log("TC-06: PASS");
  });

  // ---------------------------------------------------------------------------
  // TC-07: Clique no nome "Residencial Rerun QA" redireciona para /condominios/{ID}
  // ---------------------------------------------------------------------------
  test("TC-07: Clique em Residencial Rerun QA redireciona para URL correta", async () => {
    log("========================================");
    log("RERUN2 TC-07: Redirect ao clicar no tenant");
    log(`Timestamp: ${new Date().toISOString()}`);
    log(`Expected: URL muda para /condominios/${TENANT_RERUN_ID}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table tbody tr", { timeout: 15000 });
    await authPage.screenshot({ path: sc("tc07_lista_completa"), fullPage: true });

    const rerunRow = authPage.locator("table tbody tr").filter({ hasText: TENANT_RERUN_NAME });
    const rerunRowCount = await rerunRow.count();
    log(`TC-07: Linha "${TENANT_RERUN_NAME}" encontrada: ${rerunRowCount}`);

    if (rerunRowCount === 0) {
      await authPage.screenshot({ path: sc("tc07_fail_linha_ausente"), fullPage: true });
      log(`TC-07 FAIL — Expected: linha "${TENANT_RERUN_NAME}" visivel na lista`);
      log(`TC-07 FAIL — Actual: linha nao encontrada`);
      throw new Error(`Linha "${TENANT_RERUN_NAME}" nao encontrada na tabela`);
    }

    const nameLink = rerunRow.locator("a").first();
    const nameCell = rerunRow.locator("td").first();

    const linkCount = await nameLink.count();
    const linkHref = linkCount > 0 ? await nameLink.getAttribute("href") : null;
    log(`TC-07: href do link na linha: ${linkHref}`);

    if (linkCount > 0) {
      await nameLink.click();
    } else {
      log("TC-07: Nenhum <a> na linha — clicando na primeira celula (td)");
      await nameCell.click();
    }

    await authPage.waitForTimeout(1500);
    const currentUrl = authPage.url();
    log(`TC-07: URL apos clique: ${currentUrl}`);

    await authPage.screenshot({ path: sc("tc07_apos_clique"), fullPage: true });

    const hasCorrectId = currentUrl.includes(TENANT_RERUN_ID);
    const hasUndefined = currentUrl.includes("/undefined");

    if (hasUndefined) {
      log(`TC-07 FAIL — Expected: URL contem /${TENANT_RERUN_ID}`);
      log(`TC-07 FAIL — Actual: URL contem '/undefined': ${currentUrl}`);
      await authPage.screenshot({ path: sc("tc07_fail_undefined"), fullPage: true });
    }
    if (!hasCorrectId) {
      log(`TC-07 FAIL — Expected: URL contem ID ${TENANT_RERUN_ID}`);
      log(`TC-07 FAIL — Actual: URL atual: ${currentUrl}`);
      await authPage.screenshot({ path: sc("tc07_fail_id_errado"), fullPage: true });
    }

    expect(currentUrl, "URL nao deve conter '/undefined'").not.toContain("/undefined");
    expect(currentUrl, `URL deve conter o ID do tenant ${TENANT_RERUN_ID}`).toContain(TENANT_RERUN_ID);

    await authPage.screenshot({ path: sc("tc07_pass"), fullPage: true });
    log("TC-07: PASS");
  });

  // ---------------------------------------------------------------------------
  // TC-08: Paginacao ausente com totalCount=3 < PAGE_SIZE=20
  // ---------------------------------------------------------------------------
  test("TC-08: Paginacao nao exibida com totalCount=3 < PAGE_SIZE=20", async () => {
    log("========================================");
    log("RERUN2 TC-08: Paginacao ausente");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("Expected: controles de paginacao ausentes (3 tenants < PAGE_SIZE=20)");
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table, [class*='emptyState']", { timeout: 15000 });
    await authPage.screenshot({ path: sc("tc08_lista"), fullPage: true });

    const totalRows = await authPage.locator("table tbody tr").count();
    log(`TC-08: Total de linhas na tabela: ${totalRows}`);

    // Verificar controles de paginacao
    const paginationSelectors = [
      "[role='navigation'][aria-label*='aginaç']",
      "[class*='pagination']",
      "[class*='Pagination']",
      "nav[aria-label*='page']",
    ];

    let paginationFound = false;
    let paginationSelector = "";
    for (const sel of paginationSelectors) {
      const count = await authPage.locator(sel).count();
      if (count > 0) {
        paginationFound = true;
        paginationSelector = sel;
        log(`TC-08: Controle de paginacao encontrado via selector "${sel}" — count=${count}`);
        break;
      }
    }

    // Verificar tambem por texto de botoes de paginacao
    const nextBtnCount = await authPage.locator("button").filter({ hasText: /^Próxima$/ }).count();
    const prevBtnCount = await authPage.locator("button").filter({ hasText: /^Anterior$/ }).count();
    log(`TC-08: Botao 'Proxima' encontrado: ${nextBtnCount}`);
    log(`TC-08: Botao 'Anterior' encontrado: ${prevBtnCount}`);

    if (nextBtnCount > 0 || prevBtnCount > 0) {
      paginationFound = true;
      paginationSelector = "button Próxima/Anterior";
      log(`TC-08: Botoes de paginacao encontrados: Proxima=${nextBtnCount}, Anterior=${prevBtnCount}`);
    }

    log(`TC-08: paginationFound: ${paginationFound}`);

    if (paginationFound) {
      await authPage.screenshot({ path: sc("tc08_fail_paginacao_presente"), fullPage: true });
      log(`TC-08 FAIL — Expected: controles de paginacao AUSENTES (totalCount=3 < PAGE_SIZE=20)`);
      log(`TC-08 FAIL — Actual: paginacao encontrada via "${paginationSelector}"`);
    }

    expect(
      paginationFound,
      `Controles de paginacao devem estar ausentes quando totalCount=3 < PAGE_SIZE=20. Encontrado via: "${paginationSelector}"`
    ).toBe(false);

    expect(totalRows, "Total de linhas deve ser <= 20 (PAGE_SIZE)").toBeLessThanOrEqual(20);
    expect(totalRows, "Total de linhas deve ser 3 (quantidade real de tenants no banco)").toBe(3);

    await authPage.screenshot({ path: sc("tc08_pass"), fullPage: true });
    log("TC-08: PASS");
  });
});
