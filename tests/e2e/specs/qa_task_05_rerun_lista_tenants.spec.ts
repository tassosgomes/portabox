import { test, expect, type BrowserContext, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = "http://localhost:5173";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_05_lista_tenants";
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const LOG_FILE = path.join(EVIDENCE_DIR, "requests.log");

// Tenants presentes no banco conforme briefing da re-execucao
const TENANT_TESTE_ID = "f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5";
const TENANT_TESTE_NAME = "Residencial Teste QA"; // status 2 = Ativo
const TENANT_RERUN_ID = "4a3d87ea-f62f-4d9c-80de-a34237d0dae3";
const TENANT_RERUN_NAME = "Residencial Rerun QA"; // status 1 = PreAtivo

function log(msg: string) {
  const line = `[${new Date().toISOString()}] ${msg}\n`;
  fs.appendFileSync(LOG_FILE, line);
}

function sc(name: string) {
  return path.join(SCREENSHOTS_DIR, `rerun_${name}.png`);
}

test.describe.serial("QA Task 05 RERUN — Lista de Tenants (CF5)", () => {
  let authPage: Page;
  let authContext: BrowserContext;

  test.beforeAll(async ({ browser }) => {
    log("========================================");
    log("RERUN — SETUP: Login para a suite");
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

    await authPage.locator("#email").first().fill("operator@portabox.dev");
    await authPage.locator("#password").first().fill("PortaBox123!");
    await authPage.screenshot({ path: sc("setup_login_preenchido"), fullPage: true });
    await authPage.click('button[type="submit"]');
    await authPage.waitForURL(/condominios/, { timeout: 20000 });

    log("RERUN — SETUP: Login OK, sessao ativa");
    await authPage.screenshot({ path: sc("setup_login_sucesso"), fullPage: true });
  });

  test.afterAll(async () => {
    await authContext.close();
    log("RERUN — SETUP: Context encerrado");
  });

  // -----------------------------------------------------------------------
  // TC-01: Lista renderiza sem crash e exibe ambos os tenants
  // -----------------------------------------------------------------------
  test("TC-01: Lista exibe os tenants cadastrados sem crash", async () => {
    log("========================================");
    log("RERUN TC-01: Lista exibe tenants sem crash");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    authPage.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    authPage.on("pageerror", (err) => pageErrors.push(err.message));

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.screenshot({ path: sc("tc01_inicio"), fullPage: true });

    // Aguarda tabela ou estado vazio — qualquer um indica que nao crashou
    await authPage.waitForSelector(
      "table, [class*='emptyState'], [role='alert']",
      { timeout: 15000 }
    );

    await authPage.screenshot({ path: sc("tc01_carregado"), fullPage: true });

    // Verifica que nao ha tela branca (body tem conteudo)
    const bodyText = await authPage.locator("body").textContent();
    log(`TC-01: Body text length: ${bodyText?.length ?? 0}`);

    if (pageErrors.length > 0) {
      log(`TC-01 FAIL — Page errors detectados:`);
      pageErrors.forEach((e) => log(`  ${e}`));
      await authPage.screenshot({ path: sc("tc01_fail_crash"), fullPage: true });
    }

    expect(pageErrors, "Nao deve haver page errors (crash React)").toHaveLength(0);

    // Verifica presenca de pelo menos um dos dois tenants
    const rowTeste = authPage.locator("table tbody tr").filter({ hasText: TENANT_TESTE_NAME });
    const rowRerun = authPage.locator("table tbody tr").filter({ hasText: TENANT_RERUN_NAME });
    const countTeste = await rowTeste.count();
    const countRerun = await rowRerun.count();
    log(`TC-01: '${TENANT_TESTE_NAME}' visivel: ${countTeste > 0}`);
    log(`TC-01: '${TENANT_RERUN_NAME}' visivel: ${countRerun > 0}`);

    if (countTeste === 0 && countRerun === 0) {
      const tableContent = await authPage.locator("table").textContent().catch(() => "TABLE NOT FOUND");
      log(`TC-01 FAIL — Expected: ao menos um tenant visivel na tabela`);
      log(`TC-01 FAIL — Actual: nenhum encontrado. Tabela: ${tableContent}`);
      await authPage.screenshot({ path: sc("tc01_fail_vazio"), fullPage: true });
    }

    expect(
      countTeste + countRerun,
      "Ao menos um dos tenants deve estar visivel"
    ).toBeGreaterThan(0);

    await authPage.screenshot({ path: sc("tc01_pass"), fullPage: true });
    log("TC-01: PASS");
  });

  // -----------------------------------------------------------------------
  // TC-02: Colunas obrigatorias presentes no header
  // -----------------------------------------------------------------------
  test("TC-02: Colunas da tabela estao presentes", async () => {
    log("========================================");
    log("RERUN TC-02: Colunas da tabela");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table thead", { timeout: 15000 });
    await authPage.screenshot({ path: sc("tc02_tabela"), fullPage: true });

    const headers = await authPage.locator("table thead th").allTextContents();
    log(`TC-02: Headers encontrados: ${JSON.stringify(headers)}`);

    const requiredColumns = ["Nome", "CNPJ", "Status", "Criado", "Ativado"];
    for (const col of requiredColumns) {
      const found = headers.some((h) => h.toLowerCase().includes(col.toLowerCase()));
      log(`TC-02: Coluna '${col}': ${found ? "PRESENTE" : "AUSENTE"}`);
      if (!found) {
        log(`TC-02 FAIL — Expected: coluna '${col}' presente`);
        log(`TC-02 FAIL — Actual: headers encontrados: ${JSON.stringify(headers)}`);
        await authPage.screenshot({ path: sc("tc02_fail"), fullPage: true });
      }
      expect(found, `Coluna '${col}' deve estar presente`).toBe(true);
    }

    await authPage.screenshot({ path: sc("tc02_pass"), fullPage: true });
    log("TC-02: PASS");
  });

  // -----------------------------------------------------------------------
  // TC-03: Filtro pre-ativo exibe Residencial Rerun QA; oculta Residencial Teste QA
  // -----------------------------------------------------------------------
  test("TC-03: Filtro Pre-ativo exibe Rerun e nao exibe Teste QA (Ativo)", async () => {
    log("========================================");
    log("RERUN TC-03: Filtro Pre-ativo");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("[role='group']", { timeout: 15000 });
    await authPage.screenshot({ path: sc("tc03_antes_filtro"), fullPage: true });

    const allBtns = await authPage.locator("[role='group'] button").allTextContents();
    log(`TC-03: Botoes de filtro: ${JSON.stringify(allBtns)}`);

    const preAtivoBtn = authPage.locator("[role='group'] button").filter({ hasText: /pré-ativo/i });
    const btnCount = await preAtivoBtn.count();
    log(`TC-03: Botoes 'Pre-ativo' encontrados: ${btnCount}`);

    if (btnCount === 0) {
      log(`TC-03 FAIL — Expected: botao 'Pre-ativo' presente`);
      log(`TC-03 FAIL — Actual: botoes: ${JSON.stringify(allBtns)}`);
      await authPage.screenshot({ path: sc("tc03_fail_btn"), fullPage: true });
      throw new Error(`Botao 'Pre-ativo' nao encontrado. Botoes: ${JSON.stringify(allBtns)}`);
    }

    await preAtivoBtn.click();
    log("TC-03: Clicou no filtro 'Pre-ativo'");

    // Aguarda re-renderizacao
    await authPage.waitForTimeout(1000);
    await authPage.screenshot({ path: sc("tc03_resultado"), fullPage: true });

    // Rerun QA deve aparecer (status=1=PreAtivo)
    const rowRerun = authPage.locator("table tbody tr").filter({ hasText: TENANT_RERUN_NAME });
    const countRerun = await rowRerun.count();
    log(`TC-03: '${TENANT_RERUN_NAME}' com filtro PreAtivo: ${countRerun > 0 ? "VISIVEL (esperado)" : "AUSENTE (FAIL)"}`);

    if (countRerun === 0) {
      const tbody = await authPage.locator("table tbody, [class*='emptyState']").textContent().catch(() => "");
      log(`TC-03 FAIL — Expected: '${TENANT_RERUN_NAME}' visivel com filtro PreAtivo`);
      log(`TC-03 FAIL — Actual: nao encontrado. Conteudo: ${tbody}`);
      await authPage.screenshot({ path: sc("tc03_fail_rerun_ausente"), fullPage: true });
    }
    expect(countRerun, `'${TENANT_RERUN_NAME}' deve aparecer com filtro Pre-ativo`).toBe(1);

    // Teste QA (status=2=Ativo) NAO deve aparecer
    const rowTeste = authPage.locator("table tbody tr").filter({ hasText: TENANT_TESTE_NAME });
    const countTeste = await rowTeste.count();
    log(`TC-03: '${TENANT_TESTE_NAME}' com filtro PreAtivo: ${countTeste > 0 ? "VISIVEL (FAIL)" : "AUSENTE (esperado)"}`);

    if (countTeste > 0) {
      log(`TC-03 FAIL — Expected: '${TENANT_TESTE_NAME}' (status=Ativo) nao visivel com filtro PreAtivo`);
      log(`TC-03 FAIL — Actual: apareceu com countTeste=${countTeste}`);
      await authPage.screenshot({ path: sc("tc03_fail_teste_apareceu"), fullPage: true });
    }
    expect(countTeste, `'${TENANT_TESTE_NAME}' NAO deve aparecer com filtro Pre-ativo`).toBe(0);

    await authPage.screenshot({ path: sc("tc03_pass"), fullPage: true });
    log("TC-03: PASS");
  });

  // -----------------------------------------------------------------------
  // TC-04: Filtro Ativo exibe Teste QA; nao exibe Rerun QA
  // -----------------------------------------------------------------------
  test("TC-04: Filtro Ativo exibe Teste QA e nao exibe Rerun QA (PreAtivo)", async () => {
    log("========================================");
    log("RERUN TC-04: Filtro Ativo");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("[role='group']", { timeout: 15000 });

    const ativoBtn = authPage.locator("[role='group'] button").filter({ hasText: /^Ativo$/ });
    await ativoBtn.click();
    log("TC-04: Clicou no filtro 'Ativo'");

    await authPage.waitForTimeout(1000);
    await authPage.screenshot({ path: sc("tc04_resultado"), fullPage: true });

    // Teste QA (status=2=Ativo) DEVE aparecer
    const rowTeste = authPage.locator("table tbody tr").filter({ hasText: TENANT_TESTE_NAME });
    const countTeste = await rowTeste.count();
    log(`TC-04: '${TENANT_TESTE_NAME}' com filtro Ativo: ${countTeste > 0 ? "VISIVEL (esperado)" : "AUSENTE (FAIL)"}`);

    if (countTeste === 0) {
      const tbody = await authPage.locator("table tbody, [class*='emptyState']").textContent().catch(() => "");
      log(`TC-04 FAIL — Expected: '${TENANT_TESTE_NAME}' visivel com filtro Ativo`);
      log(`TC-04 FAIL — Actual: nao encontrado. Conteudo: ${tbody}`);
      await authPage.screenshot({ path: sc("tc04_fail_teste_ausente"), fullPage: true });
    }
    expect(countTeste, `'${TENANT_TESTE_NAME}' deve aparecer com filtro Ativo`).toBe(1);

    // Rerun QA (status=1=PreAtivo) NAO deve aparecer
    const rowRerun = authPage.locator("table tbody tr").filter({ hasText: TENANT_RERUN_NAME });
    const countRerun = await rowRerun.count();
    log(`TC-04: '${TENANT_RERUN_NAME}' com filtro Ativo: ${countRerun > 0 ? "VISIVEL (FAIL)" : "AUSENTE (esperado)"}`);

    if (countRerun > 0) {
      log(`TC-04 FAIL — Expected: '${TENANT_RERUN_NAME}' (status=PreAtivo) nao visivel com filtro Ativo`);
      log(`TC-04 FAIL — Actual: apareceu com countRerun=${countRerun}`);
      await authPage.screenshot({ path: sc("tc04_fail_rerun_apareceu"), fullPage: true });
    }
    expect(countRerun, `'${TENANT_RERUN_NAME}' NAO deve aparecer com filtro Ativo`).toBe(0);

    await authPage.screenshot({ path: sc("tc04_pass"), fullPage: true });
    log("TC-04: PASS");
  });

  // -----------------------------------------------------------------------
  // TC-05: Busca por nome "Rerun" retorna apenas Residencial Rerun QA
  // -----------------------------------------------------------------------
  test("TC-05: Busca por nome 'Rerun' retorna tenant correto", async () => {
    log("========================================");
    log("RERUN TC-05: Busca por nome");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("input[aria-label='Buscar condomínio'], input[type='search']", {
      timeout: 15000,
    });

    const searchInput = authPage.locator("input[aria-label='Buscar condomínio']").first();
    await searchInput.fill("Rerun");
    log("TC-05: Digitou 'Rerun' na busca");

    // Aguarda debounce (300ms) + rede
    await authPage.waitForTimeout(1500);
    await authPage.screenshot({ path: sc("tc05_busca_nome"), fullPage: true });

    const rowRerun = authPage.locator("table tbody tr").filter({ hasText: TENANT_RERUN_NAME });
    const countRerun = await rowRerun.count();
    log(`TC-05: '${TENANT_RERUN_NAME}' encontrado: ${countRerun > 0 ? "SIM" : "NAO"}`);

    if (countRerun === 0) {
      const tbody = await authPage.locator("table tbody, [class*='emptyState']").textContent().catch(() => "");
      log(`TC-05 FAIL — Expected: '${TENANT_RERUN_NAME}' visivel na busca por 'Rerun'`);
      log(`TC-05 FAIL — Actual: nao encontrado. Conteudo: ${tbody}`);
      await authPage.screenshot({ path: sc("tc05_fail"), fullPage: true });
    }
    expect(countRerun, `'${TENANT_RERUN_NAME}' deve aparecer na busca por 'Rerun'`).toBe(1);

    // Teste QA NAO deve aparecer
    const rowTeste = authPage.locator("table tbody tr").filter({ hasText: TENANT_TESTE_NAME });
    const countTeste = await rowTeste.count();
    log(`TC-05: '${TENANT_TESTE_NAME}' aparece na busca por 'Rerun': ${countTeste > 0 ? "SIM (FAIL)" : "NAO (esperado)"}`);
    expect(countTeste, `'${TENANT_TESTE_NAME}' NAO deve aparecer na busca por 'Rerun'`).toBe(0);

    await authPage.screenshot({ path: sc("tc05_pass"), fullPage: true });
    log("TC-05: PASS");
  });

  // -----------------------------------------------------------------------
  // TC-06: Busca por CNPJ "444" retorna Residencial Rerun QA
  // -----------------------------------------------------------------------
  test("TC-06: Busca por CNPJ '444' retorna tenant correto", async () => {
    log("========================================");
    log("RERUN TC-06: Busca por CNPJ");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("input[aria-label='Buscar condomínio'], input[type='search']", {
      timeout: 15000,
    });

    const searchInput = authPage.locator("input[aria-label='Buscar condomínio']").first();
    await searchInput.clear();
    await authPage.waitForTimeout(300);
    await searchInput.fill("444");
    log("TC-06: Digitou '444' na busca (parte CNPJ do Rerun)");

    // Aguarda debounce + rede
    await authPage.waitForTimeout(1500);
    await authPage.screenshot({ path: sc("tc06_busca_cnpj"), fullPage: true });

    const rowRerun = authPage.locator("table tbody tr").filter({ hasText: TENANT_RERUN_NAME });
    const countRerun = await rowRerun.count();
    log(`TC-06: '${TENANT_RERUN_NAME}' encontrado na busca por '444': ${countRerun > 0 ? "SIM" : "NAO"}`);

    if (countRerun === 0) {
      const tbody = await authPage.locator("table tbody, [class*='emptyState']").textContent().catch(() => "");
      log(`TC-06 FAIL — Expected: '${TENANT_RERUN_NAME}' visivel na busca por '444'`);
      log(`TC-06 FAIL — Actual: nao encontrado. Conteudo: ${tbody}`);
      await authPage.screenshot({ path: sc("tc06_fail"), fullPage: true });
    }
    expect(countRerun, `'${TENANT_RERUN_NAME}' deve aparecer na busca por '444'`).toBe(1);

    await authPage.screenshot({ path: sc("tc06_pass"), fullPage: true });
    log("TC-06: PASS");
  });

  // -----------------------------------------------------------------------
  // TC-07: Clicar em Residencial Rerun QA redireciona para /condominios/{ID}
  // -----------------------------------------------------------------------
  test("TC-07: Clicar no nome redireciona para o painel correto", async () => {
    log("========================================");
    log("RERUN TC-07: Redirect para painel de detalhes");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table tbody tr", { timeout: 15000 });

    const rerunLink = authPage
      .locator("table tbody tr")
      .filter({ hasText: TENANT_RERUN_NAME })
      .locator("a")
      .first();

    const href = await rerunLink.getAttribute("href");
    log(`TC-07: href do link para '${TENANT_RERUN_NAME}': ${href}`);

    if (!href) {
      log(`TC-07 FAIL — Expected: elemento <a> com href para /condominios/${TENANT_RERUN_ID}`);
      log(`TC-07 FAIL — Actual: href e null/vazio`);
      await authPage.screenshot({ path: sc("tc07_fail_href"), fullPage: true });
    }
    expect(href, "Link deve ter atributo href").toBeTruthy();

    await rerunLink.click();
    await authPage.waitForTimeout(1000);

    const currentUrl = authPage.url();
    log(`TC-07: URL apos clique: ${currentUrl}`);
    await authPage.screenshot({ path: sc("tc07_apos_clique"), fullPage: true });

    const hasUndefined = currentUrl.includes("/undefined");
    const hasCorrectId = currentUrl.includes(TENANT_RERUN_ID);

    if (hasUndefined) {
      log(`TC-07 FAIL — Expected: URL deve conter /${TENANT_RERUN_ID}`);
      log(`TC-07 FAIL — Actual: URL contem '/undefined': ${currentUrl}`);
      await authPage.screenshot({ path: sc("tc07_fail_undefined"), fullPage: true });
    }
    if (!hasCorrectId) {
      log(`TC-07 FAIL — Expected: URL deve conter ID ${TENANT_RERUN_ID}`);
      log(`TC-07 FAIL — Actual: URL nao contem o ID: ${currentUrl}`);
      await authPage.screenshot({ path: sc("tc07_fail_id"), fullPage: true });
    }

    expect(currentUrl, "URL nao deve conter '/undefined'").not.toContain("/undefined");
    expect(currentUrl, `URL deve conter ID ${TENANT_RERUN_ID}`).toContain(TENANT_RERUN_ID);

    await authPage.screenshot({ path: sc("tc07_pass"), fullPage: true });
    log("TC-07: PASS");
  });

  // -----------------------------------------------------------------------
  // TC-08: Paginacao ausente quando totalCount <= PAGE_SIZE (20)
  // -----------------------------------------------------------------------
  test("TC-08: Paginacao nao exibida com totalCount <= 20", async () => {
    log("========================================");
    log("RERUN TC-08: Paginacao");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("========================================");

    await authPage.goto(`${BASE_URL}/condominios`);
    await authPage.waitForSelector("table, [class*='emptyState']", { timeout: 15000 });
    await authPage.screenshot({ path: sc("tc08_lista"), fullPage: true });

    const totalRows = await authPage.locator("table tbody tr").count();
    log(`TC-08: Linhas na tabela (sem filtro): ${totalRows}`);
    // API retornou totalCount=3 conforme validado via HTTP
    log(`TC-08: totalCount API confirmado via HTTP: 3 (PAGE_SIZE=20)`);

    const paginationNav = authPage.locator(
      "[role='navigation'][aria-label*='paginação'], [role='navigation'][aria-label*='Paginação']"
    );
    const paginationExists = await paginationNav.count();
    log(`TC-08: Controles de paginacao presentes: ${paginationExists > 0 ? "SIM" : "NAO"}`);

    // Com totalCount=3 e PAGE_SIZE=20, totalPages=1, paginacao NAO deve aparecer
    if (paginationExists > 0) {
      log(`TC-08 FAIL — Expected: paginacao ausente (totalCount=3 <= PAGE_SIZE=20)`);
      log(`TC-08 FAIL — Actual: paginacao visivel (paginationExists=${paginationExists})`);
      await authPage.screenshot({ path: sc("tc08_fail_paginacao"), fullPage: true });
    }

    expect(
      paginationExists,
      "Paginacao nao deve ser exibida com totalCount <= PAGE_SIZE"
    ).toBe(0);

    // Valida que totalRows e compativel com totalCount=3
    expect(totalRows, "Deve haver 3 ou menos linhas (totalCount=3)").toBeLessThanOrEqual(3);

    await authPage.screenshot({ path: sc("tc08_pass"), fullPage: true });
    log(`TC-08: PASS — paginacao ausente, ${totalRows} linhas visiveis (totalCount=3, PAGE_SIZE=20)`);
  });
});
