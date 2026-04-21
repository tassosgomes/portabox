import { test, expect, type BrowserContext, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = "http://localhost:5173";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_05_lista_tenants";
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const LOG_FILE = path.join(EVIDENCE_DIR, "requests.log");

const TENANT_ID = "f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5";
const TENANT_NAME = "Residencial Teste QA";
const TENANT_CNPJ_PARTIAL = "11.222";

function log(msg: string) {
  const line = `[${new Date().toISOString()}] ${msg}\n`;
  fs.appendFileSync(LOG_FILE, line);
}

function sc(name: string) {
  return path.join(SCREENSHOTS_DIR, `${name}.png`);
}

test.describe.serial("QA Task 05 — Lista de Tenants (CF5)", () => {
  let authPage: Page;
  let authContext: BrowserContext;

  test.beforeAll(async ({ browser }) => {
    log("========================================");
    log("SETUP: Login unico para toda a suite");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    authContext = await browser.newContext();
    authPage = await authContext.newPage();

    authPage.on("console", (msg) => {
      if (msg.type() === "error") log(`[BROWSER_CONSOLE_ERROR] ${msg.text()}`);
    });
    authPage.on("pageerror", (err) => log(`[PAGE_ERROR] ${err.message}`));

    await authPage.goto(`${BASE_URL}/login`);
    await authPage.screenshot({ path: sc("setup_login"), fullPage: true });

    const emailInput = authPage.locator("#email").first();
    await emailInput.fill("operator@portabox.dev");

    const passwordInput = authPage.locator("#password").first();
    await passwordInput.fill("PortaBox123!");

    await authPage.screenshot({ path: sc("setup_login_preenchido"), fullPage: true });
    await authPage.click('button[type="submit"]');
    await authPage.waitForURL(/condominios/, { timeout: 20000 });

    log("SETUP: Login realizado, sessao ativa");
    await authPage.screenshot({ path: sc("setup_login_sucesso"), fullPage: true });
  });

  test.afterAll(async () => {
    await authContext.close();
    log("SETUP: Context encerrado");
  });

  test("TC-01: Lista exibe o tenant criado (Residencial Teste QA)", async () => {
    log("========================================");
    log("TC-01: Lista exibe o tenant criado");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.screenshot({ path: sc("tc01_inicio"), fullPage: true });

    // Wait for table to load
    await authPage.waitForSelector("table, [class*='emptyState'], [aria-live='polite']", {
      timeout: 15000,
    });

    await authPage.screenshot({ path: sc("tc01_lista_carregada"), fullPage: true });

    const tenantRow = authPage.locator("table tbody tr").filter({ hasText: TENANT_NAME });
    const rowCount = await tenantRow.count();
    log(`TC-01: Tenant '${TENANT_NAME}' encontrado na lista: ${rowCount > 0 ? "SIM" : "NAO"}`);

    if (rowCount === 0) {
      const tableContent = await authPage.locator("table").textContent().catch(() => "TABLE NOT FOUND");
      const emptyState = await authPage.locator("[class*='emptyState']").textContent().catch(() => null);
      log(`TC-01 FAIL — Expected: linha com '${TENANT_NAME}' visivel na tabela`);
      log(`TC-01 FAIL — Actual: rowCount=0. Tabela: ${tableContent}`);
      log(`TC-01 FAIL — Empty state: ${emptyState}`);
      await authPage.screenshot({ path: sc("tc01_fail"), fullPage: true });
    }

    await expect(tenantRow).toHaveCount(1, { timeout: 5000 });

    const rowText = await tenantRow.textContent();
    log(`TC-01: Conteudo da linha: ${rowText}`);

    const hasCnpj = rowText?.includes("11.222.333/0001-81") ?? false;
    log(`TC-01: CNPJ '11.222.333/0001-81' presente na linha: ${hasCnpj}`);
    if (!hasCnpj) {
      log(`TC-01 FAIL — Expected: CNPJ '11.222.333/0001-81' na linha`);
      log(`TC-01 FAIL — Actual: conteudo da linha: '${rowText}'`);
      await authPage.screenshot({ path: sc("tc01_fail_cnpj"), fullPage: true });
    }

    await expect(tenantRow).toContainText("11.222.333/0001-81");

    await authPage.screenshot({ path: sc("tc01_pass"), fullPage: true });
    log("TC-01: PASS");
  });

  test("TC-02: Colunas da tabela estao presentes", async () => {
    log("========================================");
    log("TC-02: Colunas da tabela");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table thead", { timeout: 15000 });
    await authPage.screenshot({ path: sc("tc02_tabela"), fullPage: true });

    const headers = await authPage.locator("table thead th").allTextContents();
    log(`TC-02: Headers encontrados: ${JSON.stringify(headers)}`);

    const requiredColumns = ["Nome", "CNPJ", "Status", "Criado"];
    let allPresent = true;
    for (const col of requiredColumns) {
      const found = headers.some((h) => h.toLowerCase().includes(col.toLowerCase()));
      log(`TC-02: Coluna '${col}': ${found ? "PRESENTE" : "AUSENTE"}`);
      if (!found) {
        allPresent = false;
        log(`TC-02 FAIL — Expected: coluna '${col}' presente`);
        log(`TC-02 FAIL — Actual: headers encontrados: ${JSON.stringify(headers)}`);
        await authPage.screenshot({ path: sc("tc02_fail"), fullPage: true });
      }
      expect(found, `Coluna '${col}' deveria estar presente`).toBe(true);
    }

    if (allPresent) {
      await authPage.screenshot({ path: sc("tc02_pass"), fullPage: true });
      log("TC-02: PASS");
    }
  });

  test("TC-03: Filtro por status pre-ativo exibe Residencial Teste QA", async () => {
    log("========================================");
    log("TC-03: Filtro por status pre-ativo");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("[role='group']", { timeout: 15000 });
    await authPage.screenshot({ path: sc("tc03_antes_filtro"), fullPage: true });

    // List all filter buttons
    const allBtns = await authPage.locator("[role='group'] button").allTextContents();
    log(`TC-03: Botoes de filtro encontrados: ${JSON.stringify(allBtns)}`);

    // Click "Pre-ativo" filter button
    const preAtivoBtn = authPage.locator("[role='group'] button").filter({ hasText: /pré-ativo/i });
    const btnCount = await preAtivoBtn.count();
    log(`TC-03: Botoes 'Pré-ativo' encontrados: ${btnCount}`);

    if (btnCount === 0) {
      log(`TC-03 FAIL — Expected: botao 'Pré-ativo' presente`);
      log(`TC-03 FAIL — Actual: botoes encontrados: ${JSON.stringify(allBtns)}`);
      await authPage.screenshot({ path: sc("tc03_fail_btn"), fullPage: true });
      throw new Error(`Botao 'Pré-ativo' nao encontrado. Botoes: ${JSON.stringify(allBtns)}`);
    }

    await preAtivoBtn.click();
    log("TC-03: Clicou no filtro 'Pre-ativo'");

    await authPage.waitForTimeout(1000);
    await authPage.screenshot({ path: sc("tc03_resultado"), fullPage: true });

    const tenantRow = authPage.locator("table tbody tr").filter({ hasText: TENANT_NAME });
    const rowCount = await tenantRow.count();
    log(`TC-03: '${TENANT_NAME}' aparece com filtro pre-ativo: ${rowCount > 0 ? "SIM" : "NAO"}`);

    if (rowCount === 0) {
      const content = await authPage.locator("table tbody, [class*='emptyState']").textContent().catch(() => "");
      log(`TC-03 FAIL — Expected: '${TENANT_NAME}' aparece com filtro 'Pré-ativo'`);
      log(`TC-03 FAIL — Actual: nenhuma linha. Conteudo: ${content}`);
      await authPage.screenshot({ path: sc("tc03_fail"), fullPage: true });
    }

    await expect(tenantRow).toHaveCount(1);
    await authPage.screenshot({ path: sc("tc03_pass"), fullPage: true });
    log("TC-03: PASS");
  });

  test("TC-04: Filtro por status ativo NAO exibe Residencial Teste QA", async () => {
    log("========================================");
    log("TC-04: Filtro por status ativo");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("[role='group']", { timeout: 15000 });

    // Click "Ativo" filter button
    const ativoBtn = authPage.locator("[role='group'] button").filter({ hasText: /^Ativo$/ });
    await ativoBtn.click();
    log("TC-04: Clicou no filtro 'Ativo'");

    await authPage.waitForTimeout(1000);
    await authPage.screenshot({ path: sc("tc04_filtro_ativo"), fullPage: true });

    const tenantRow = authPage.locator("table tbody tr").filter({ hasText: TENANT_NAME });
    const rowCountInActive = await tenantRow.count();
    log(`TC-04: '${TENANT_NAME}' aparece com filtro 'Ativo': ${rowCountInActive > 0 ? "SIM (INESPERADO)" : "NAO (ESPERADO)"}`);

    if (rowCountInActive > 0) {
      log(`TC-04 FAIL — Expected: '${TENANT_NAME}' NAO aparece com filtro 'Ativo' (status ainda e PreAtivo)`);
      log(`TC-04 FAIL — Actual: tenant apareceu no filtro 'Ativo' com rowCount=${rowCountInActive}`);
      await authPage.screenshot({ path: sc("tc04_fail"), fullPage: true });
    }

    await expect(tenantRow).toHaveCount(0);
    await authPage.screenshot({ path: sc("tc04_pass"), fullPage: true });
    log("TC-04: PASS");
  });

  test("TC-05: Busca por nome retorna o tenant correto", async () => {
    log("========================================");
    log("TC-05: Busca por nome");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("input[aria-label='Buscar condomínio'], input[type='search']", {
      timeout: 15000,
    });

    const searchInput = authPage.locator("input[aria-label='Buscar condomínio']").first();
    await searchInput.fill("Residencial Teste");
    log("TC-05: Digitou 'Residencial Teste' na busca");

    // Wait for debounce (300ms) + network
    await authPage.waitForTimeout(1500);
    await authPage.screenshot({ path: sc("tc05_busca_nome"), fullPage: true });

    const tenantRow = authPage.locator("table tbody tr").filter({ hasText: TENANT_NAME });
    const rowCount = await tenantRow.count();
    log(`TC-05: '${TENANT_NAME}' encontrado na busca por 'Residencial Teste': ${rowCount > 0 ? "SIM" : "NAO"}`);

    if (rowCount === 0) {
      const content = await authPage.locator("table tbody, [class*='emptyState']").textContent().catch(() => "");
      log(`TC-05 FAIL — Expected: '${TENANT_NAME}' no resultado da busca por 'Residencial Teste'`);
      log(`TC-05 FAIL — Actual: nenhuma linha encontrada. Conteudo: ${content}`);
      await authPage.screenshot({ path: sc("tc05_fail"), fullPage: true });
    }

    await expect(tenantRow).toHaveCount(1);
    await authPage.screenshot({ path: sc("tc05_pass"), fullPage: true });
    log("TC-05: PASS");
  });

  test("TC-06: Busca por CNPJ retorna o tenant correto", async () => {
    log("========================================");
    log("TC-06: Busca por CNPJ");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("input[aria-label='Buscar condomínio'], input[type='search']", {
      timeout: 15000,
    });

    // Clear any previous search
    const searchInput = authPage.locator("input[aria-label='Buscar condomínio']").first();
    await searchInput.clear();
    await authPage.waitForTimeout(500);

    await searchInput.fill(TENANT_CNPJ_PARTIAL);
    log(`TC-06: Digitou '${TENANT_CNPJ_PARTIAL}' na busca`);

    // Wait for debounce + network
    await authPage.waitForTimeout(1500);
    await authPage.screenshot({ path: sc("tc06_busca_cnpj"), fullPage: true });

    const tenantRow = authPage.locator("table tbody tr").filter({ hasText: TENANT_NAME });
    const rowCount = await tenantRow.count();
    log(`TC-06: '${TENANT_NAME}' encontrado na busca por CNPJ '${TENANT_CNPJ_PARTIAL}': ${rowCount > 0 ? "SIM" : "NAO"}`);

    if (rowCount === 0) {
      const content = await authPage.locator("table tbody, [class*='emptyState']").textContent().catch(() => "");
      log(`TC-06 FAIL — Expected: '${TENANT_NAME}' no resultado da busca por CNPJ '${TENANT_CNPJ_PARTIAL}'`);
      log(`TC-06 FAIL — Actual: nenhuma linha. Conteudo: ${content}`);
      await authPage.screenshot({ path: sc("tc06_fail"), fullPage: true });
    }

    await expect(tenantRow).toHaveCount(1);
    await authPage.screenshot({ path: sc("tc06_pass"), fullPage: true });
    log("TC-06: PASS");
  });

  test("TC-07: Clicar na linha redireciona para painel de detalhes correto", async () => {
    log("========================================");
    log("TC-07: Link para painel de detalhes");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table tbody tr", { timeout: 15000 });

    const tenantLink = authPage
      .locator("table tbody tr")
      .filter({ hasText: TENANT_NAME })
      .locator("a")
      .first();

    const href = await tenantLink.getAttribute("href");
    log(`TC-07: href do link: ${href}`);

    await tenantLink.click();

    await authPage.waitForTimeout(1000);
    const currentUrl = authPage.url();
    log(`TC-07: URL apos clique: ${currentUrl}`);

    await authPage.screenshot({ path: sc("tc07_apos_clique"), fullPage: true });

    const isUndefined = currentUrl.includes("/undefined");
    const hasCorrectId = currentUrl.includes(TENANT_ID);

    if (isUndefined) {
      log(`TC-07 FAIL — Expected: URL deve conter /${TENANT_ID}`);
      log(`TC-07 FAIL — Actual: URL contem '/undefined': ${currentUrl}`);
      await authPage.screenshot({ path: sc("tc07_fail_undefined"), fullPage: true });
    }
    if (!hasCorrectId) {
      log(`TC-07 FAIL — Expected: URL deve conter ID ${TENANT_ID}`);
      log(`TC-07 FAIL — Actual: URL atual nao contem o ID: ${currentUrl}`);
      await authPage.screenshot({ path: sc("tc07_fail_id"), fullPage: true });
    }

    expect(currentUrl, "URL nao deve conter '/undefined'").not.toContain("/undefined");
    expect(currentUrl, `URL deve conter o ID do tenant ${TENANT_ID}`).toContain(TENANT_ID);

    await authPage.screenshot({ path: sc("tc07_pass"), fullPage: true });
    log("TC-07: PASS");
  });

  test("TC-08: Paginacao — verificar presenca e comportamento", async () => {
    log("========================================");
    log("TC-08: Paginacao");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table, [class*='emptyState']", { timeout: 15000 });
    await authPage.screenshot({ path: sc("tc08_lista"), fullPage: true });

    const paginationNav = authPage.locator(
      "[role='navigation'][aria-label*='paginação'], [role='navigation'][aria-label*='Paginação']"
    );
    const paginationExists = await paginationNav.count();
    log(`TC-08: Controles de paginacao visiveis: ${paginationExists > 0 ? "SIM" : "NAO"}`);

    if (paginationExists > 0) {
      const prevBtn = paginationNav.locator('button:has-text("Anterior")');
      const prevDisabled = await prevBtn.isDisabled();
      log(`TC-08: Botao Anterior desabilitado na pagina 1: ${prevDisabled}`);
      expect(prevDisabled, "Botao Anterior deve estar desabilitado na pagina 1").toBe(true);

      const nextBtn = paginationNav.locator('button:has-text("Próxima")');
      const nextDisabled = await nextBtn.isDisabled();
      log(`TC-08: Botao Proxima desabilitado: ${nextDisabled}`);

      if (!nextDisabled) {
        await nextBtn.click();
        await authPage.waitForTimeout(1000);
        const pageInfoText = await authPage.locator("[class*='pageInfo']").textContent().catch(() => "");
        log(`TC-08: Info de pagina apos clique: ${pageInfoText}`);
        await authPage.screenshot({ path: sc("tc08_pagina2"), fullPage: true });
        expect(pageInfoText).toContain("Página 2");
      }
    } else {
      const totalItems = await authPage.locator("table tbody tr").count();
      log(`TC-08: Total de itens: ${totalItems} — paginacao nao exibida (PAGE_SIZE=20, totalItems<=${totalItems})`);
      expect(totalItems).toBeLessThanOrEqual(20);
    }

    await authPage.screenshot({ path: sc("tc08_pass"), fullPage: true });
    log("TC-08: PASS");
  });
});
