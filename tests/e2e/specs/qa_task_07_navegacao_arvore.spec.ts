import { test, expect, type BrowserContext, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const SINDICO_APP_URL = "http://localhost:5174";
const API_URL = "http://localhost:5272";

const SINDICO_A_EMAIL = "qa-sindico-a-1776724904@portabox.test";
const SINDICO_B_EMAIL = "qa-sindico-b-1776724968@portabox.test";
const TENANT_A_ID = "4cce551d-4f18-474b-a42a-2deb6c2a0451";
const TENANT_B_ID = "23fb219d-460a-4eee-a9e7-308d7665350b";

const EVIDENCE_DIR = "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_07_navegacao_arvore";
const SS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const BROWSER_LOG_FILE = path.join(EVIDENCE_DIR, "requests.log");

function appendLog(msg: string) {
  fs.appendFileSync(BROWSER_LOG_FILE, msg + "\n");
}

function readCookieFromFile(filePath: string): string {
  try {
    return fs.readFileSync(filePath, "utf8")
      .split("\n")
      .find(l => l.includes("portabox.auth"))
      ?.split("\t").pop()?.trim() ?? "";
  } catch {
    return "";
  }
}

async function getAuthenticatedPage(browser: import("@playwright/test").Browser, cookieValue: string, viewport?: { width: number; height: number }): Promise<{ context: BrowserContext; page: Page }> {
  const ctxOptions = viewport ? { viewport } : {};
  const context: BrowserContext = await browser.newContext(ctxOptions);
  await context.addCookies([{
    name: "portabox.auth",
    value: cookieValue,
    domain: "localhost",
    path: "/",
    httpOnly: true,
    secure: false,
    sameSite: "Lax",
  }]);
  const page = await context.newPage();

  // Intercept requests to 5174/api/** (Vite has no proxy config; Playwright creates it here)
  // This proxies relative API calls (/api/v1/...) from the frontend to the real backend
  await page.route(`${SINDICO_APP_URL}/api/**`, async (route) => {
    const url = route.request().url().replace("localhost:5174", "localhost:5272");
    try {
      const resp = await route.fetch({ url });
      const headers = resp.headers();
      headers["access-control-allow-origin"] = SINDICO_APP_URL;
      headers["access-control-allow-credentials"] = "true";
      await route.fulfill({ status: resp.status(), headers, body: await resp.body() });
    } catch {
      await route.continue();
    }
  });

  // Also intercept 5272/api/** (for absolute API calls from @portabox/api-client)
  // to fix CORS wildcard + credentials issue
  await page.route(`${API_URL}/api/**`, async (route) => {
    try {
      const resp = await route.fetch();
      const headers = resp.headers();
      headers["access-control-allow-origin"] = SINDICO_APP_URL;
      headers["access-control-allow-credentials"] = "true";
      await route.fulfill({ status: resp.status(), headers, body: await resp.body() });
    } catch {
      await route.continue();
    }
  });

  return { context, page };
}

test.describe("CF-07: Navegacao em Arvore Hierarquica — UI", () => {

  test("UT-01: Arvore renderiza com blocos, andares e unidades em ordem correta", async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const cookieA = readCookieFromFile(`${EVIDENCE_DIR}/cookies_sindico_a.txt`);
    const { context, page } = await getAuthenticatedPage(browser, cookieA);
    page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    page.on("pageerror", (err) => pageErrors.push(err.message));

    appendLog(`
========================================
UT-01: Arvore renderiza visualmente
Timestamp: ${new Date().toISOString()}
========================================
`);

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector("h1", { timeout: 15000 });
    await page.screenshot({ path: path.join(SS_DIR, "ut01_inicio.png"), fullPage: true });
    await page.waitForTimeout(3000);
    await page.screenshot({ path: path.join(SS_DIR, "ut01_arvore_carregada.png"), fullPage: true });

    const pageUrl = page.url();
    appendLog(`URL: ${pageUrl}`);

    const qa01Visible = await page.getByText("Bloco QA-01").isVisible().catch(() => false);
    const qa02Visible = await page.getByText("Bloco QA-02 V3").isVisible().catch(() => false);

    appendLog(`Bloco QA-01 visible: ${qa01Visible}`);
    appendLog(`Bloco QA-02 V3 visible: ${qa02Visible}`);

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog(`--- BROWSER CONSOLE UT-01 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
    }

    if (qa01Visible) {
      appendLog("--- RESULTADO: PASS ---\n");
    } else {
      appendLog(`--- RESULTADO: FAIL ---\nBloco QA-01 not visible. URL: ${pageUrl}\n`);
    }

    await page.screenshot({ path: path.join(SS_DIR, "ut01_final.png"), fullPage: true });
    await context.close();

    expect(qa01Visible, `Bloco QA-01 should be visible in the tree (URL: ${pageUrl})`).toBe(true);
  });

  test("UT-02: Expandir e colapsar bloco via click", async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const cookieA = readCookieFromFile(`${EVIDENCE_DIR}/cookies_sindico_a.txt`);
    const { context, page } = await getAuthenticatedPage(browser, cookieA);
    page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector("h1", { timeout: 15000 });
    await page.waitForTimeout(3000);
    await page.screenshot({ path: path.join(SS_DIR, "ut02_inicial.png"), fullPage: true });

    const expandButtons = page.locator('button[aria-expanded], [role="button"][aria-expanded]');
    const expandCount = await expandButtons.count();

    appendLog(`
========================================
UT-02: Expandir/colapsar via click
Timestamp: ${new Date().toISOString()}
========================================
URL: ${page.url()}
Expandable buttons found: ${expandCount}
`);

    let ut02Pass = false;
    let detail = "";

    if (expandCount > 0) {
      const firstExpandBtn = expandButtons.first();
      const expandedBefore = await firstExpandBtn.getAttribute("aria-expanded");
      appendLog(`First button aria-expanded before click: ${expandedBefore}`);

      await firstExpandBtn.click();
      await page.waitForTimeout(500);
      const expandedAfter = await firstExpandBtn.getAttribute("aria-expanded");
      appendLog(`First button aria-expanded after click: ${expandedAfter}`);

      await page.screenshot({ path: path.join(SS_DIR, "ut02_apos_click.png"), fullPage: true });

      await firstExpandBtn.click();
      await page.waitForTimeout(500);
      const expandedAfter2 = await firstExpandBtn.getAttribute("aria-expanded");
      appendLog(`First button aria-expanded after 2nd click: ${expandedAfter2}`);

      await page.screenshot({ path: path.join(SS_DIR, "ut02_apos_colapso.png"), fullPage: true });

      if (expandedBefore !== expandedAfter) {
        ut02Pass = true;
        detail = `aria-expanded: ${expandedBefore} -> ${expandedAfter} (expand), -> ${expandedAfter2} (collapse)`;
      } else {
        detail = `aria-expanded did NOT change after click: before=${expandedBefore}, after=${expandedAfter}`;
      }
    } else {
      detail = `No aria-expanded buttons found. Bloco QA-01 visible: ${await page.getByText("Bloco QA-01").isVisible().catch(() => false)}`;
    }

    appendLog(`Result: ut02Pass=${ut02Pass}, detail=${detail}`);

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog(`--- BROWSER CONSOLE UT-02 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
    }

    if (ut02Pass) {
      appendLog("--- RESULTADO: PASS ---\n");
    } else {
      appendLog(`--- RESULTADO: FAIL ---\nDetail: ${detail}\n`);
    }

    await context.close();
    expect(ut02Pass, `Expand/collapse via click: ${detail}`).toBe(true);
  });

  test("UT-03: Navegacao por teclado (setas expand/collapse)", async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const cookieA = readCookieFromFile(`${EVIDENCE_DIR}/cookies_sindico_a.txt`);
    const { context, page } = await getAuthenticatedPage(browser, cookieA);
    page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector("h1", { timeout: 15000 });
    await page.waitForTimeout(3000);
    await page.screenshot({ path: path.join(SS_DIR, "ut03_inicial.png"), fullPage: true });

    appendLog(`
========================================
UT-03: Navegacao por teclado
Timestamp: ${new Date().toISOString()}
========================================
PRD: "Acessibilidade: arvore navegavel por teclado (setas para expandir/colapsar)"
URL: ${page.url()}
`);

    const treeItems = page.locator('[role="treeitem"]');
    const treeItemCount = await treeItems.count();
    const treeExists = await page.locator('[role="tree"]').count() > 0;

    appendLog(`role="tree" found: ${treeExists}`);
    appendLog(`role="treeitem" count: ${treeItemCount}`);

    let keyboardWorking = false;
    let detail = "";

    if (treeItemCount > 0) {
      const firstItem = treeItems.first();
      await firstItem.focus();
      await page.waitForTimeout(200);

      const expandedBefore = await firstItem.getAttribute("aria-expanded");
      appendLog(`First treeitem aria-expanded before ArrowRight: ${expandedBefore}`);

      await page.keyboard.press("ArrowRight");
      await page.waitForTimeout(300);
      const expandedAfterRight = await firstItem.getAttribute("aria-expanded");
      appendLog(`After ArrowRight: ${expandedAfterRight}`);

      await page.screenshot({ path: path.join(SS_DIR, "ut03_apos_arrow_right.png"), fullPage: true });

      await page.keyboard.press("ArrowLeft");
      await page.waitForTimeout(300);
      const expandedAfterLeft = await firstItem.getAttribute("aria-expanded");
      appendLog(`After ArrowLeft: ${expandedAfterLeft}`);

      await page.screenshot({ path: path.join(SS_DIR, "ut03_apos_arrow_left.png"), fullPage: true });

      if (expandedAfterRight !== expandedBefore || expandedAfterLeft !== expandedAfterRight) {
        keyboardWorking = true;
        detail = `ArrowRight: ${expandedBefore} -> ${expandedAfterRight}; ArrowLeft: ${expandedAfterRight} -> ${expandedAfterLeft}`;
      } else {
        detail = `Keyboard NOT responding. ArrowRight: ${expandedBefore} -> ${expandedAfterRight} (no change)`;
      }
    } else {
      detail = `No role="treeitem" found (tree=${treeExists}). URL: ${page.url()}`;
    }

    appendLog(`Keyboard nav: working=${keyboardWorking}, detail: ${detail}`);

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog(`--- BROWSER CONSOLE UT-03 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
    }

    if (keyboardWorking) {
      appendLog("--- RESULTADO: PASS ---\n");
    } else {
      appendLog(`--- RESULTADO: FAIL ---\nPRD keyboard navigation requirement not met\nDetail: ${detail}\n`);
    }

    await context.close();
    expect(keyboardWorking, `PRD keyboard navigation: ${detail}`).toBe(true);
  });

  test("UT-04: Toggle filtro incluir inativos", async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const cookieA = readCookieFromFile(`${EVIDENCE_DIR}/cookies_sindico_a.txt`);
    const { context, page } = await getAuthenticatedPage(browser, cookieA);
    page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector("h1", { timeout: 15000 });
    await page.waitForTimeout(3000);
    await page.screenshot({ path: path.join(SS_DIR, "ut04_inicial.png"), fullPage: true });

    appendLog(`
========================================
UT-04: Toggle filtro incluir inativos
Timestamp: ${new Date().toISOString()}
========================================
URL: ${page.url()}
`);

    const toggleLabel = page.getByText("Mostrar inativos");
    const toggleExists = await toggleLabel.isVisible().catch(() => false);
    appendLog(`Toggle "Mostrar inativos" visible: ${toggleExists}`);

    if (!toggleExists) {
      if (consoleLogs.length > 0 || pageErrors.length > 0) {
        appendLog(`--- BROWSER CONSOLE UT-04 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
      }
      appendLog(`--- RESULTADO: FAIL ---\nToggle not found. URL: ${page.url()}\n`);
      await context.close();
      expect(toggleExists, "Toggle 'Mostrar inativos' must be visible").toBe(true);
      return;
    }

    const inativoText1 = await page.getByText("Bloco Temp Pai Inativo QA").isVisible().catch(() => false);
    appendLog(`Bloco Temp Pai Inativo QA visible before toggle: ${inativoText1}`);

    const checkbox = page.locator('label').filter({ hasText: "Mostrar inativos" }).locator('input[type="checkbox"]');
    const checkboxCount = await checkbox.count();
    const cb = checkboxCount > 0 ? checkbox : page.locator('input[type="checkbox"]').first();

    await cb.check();
    await page.waitForTimeout(2000);
    await page.screenshot({ path: path.join(SS_DIR, "ut04_inativos_ligados.png"), fullPage: true });

    const inativoText2 = await page.getByText("Bloco Temp Pai Inativo QA").isVisible().catch(() => false);
    appendLog(`Bloco Temp Pai Inativo QA visible after toggle ON: ${inativoText2}`);

    await cb.uncheck();
    await page.waitForTimeout(2000);
    await page.screenshot({ path: path.join(SS_DIR, "ut04_inativos_desligados.png"), fullPage: true });

    const inativoText3 = await page.getByText("Bloco Temp Pai Inativo QA").isVisible().catch(() => false);
    appendLog(`Bloco Temp Pai Inativo QA visible after toggle OFF: ${inativoText3}`);

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog(`--- BROWSER CONSOLE UT-04 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
    }

    const passCondition = !inativoText1 && inativoText2 && !inativoText3;
    appendLog(`Toggle: before=${inativoText1}, on=${inativoText2}, off=${inativoText3}`);

    if (passCondition) {
      appendLog("--- RESULTADO: PASS ---\n");
    } else {
      appendLog(`--- RESULTADO: FAIL ---\nbefore=${inativoText1}, on=${inativoText2}, off=${inativoText3}\n`);
    }

    await context.close();
    expect(passCondition, `Toggle: before=${inativoText1}, on=${inativoText2}, off=${inativoText3}`).toBe(true);
  });

  test("UT-05: Clicar num bloco abre painel/toolbar lateral com detalhes", async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const cookieA = readCookieFromFile(`${EVIDENCE_DIR}/cookies_sindico_a.txt`);
    const { context, page } = await getAuthenticatedPage(browser, cookieA);
    page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector("h1", { timeout: 15000 });
    await page.waitForTimeout(3000);
    await page.screenshot({ path: path.join(SS_DIR, "ut05_inicial.png"), fullPage: true });

    appendLog(`
========================================
UT-05: Painel lateral ao clicar num bloco
Timestamp: ${new Date().toISOString()}
========================================
PRD: "painel lateral com detalhes e acoes contextuais"
URL: ${page.url()}
`);

    const blocoQA01 = page.getByText("Bloco QA-01").first();
    const blocoVisible = await blocoQA01.isVisible().catch(() => false);
    appendLog(`Bloco QA-01 visible: ${blocoVisible}`);

    if (blocoVisible) {
      await blocoQA01.click();
      await page.waitForTimeout(1500);
    }

    await page.screenshot({ path: path.join(SS_DIR, "ut05_apos_click.png"), fullPage: true });

    const selectedBlocoText = await page.getByText("Bloco selecionado").isVisible().catch(() => false);
    const adicionarUnidade = await page.getByText("Adicionar unidade").isVisible().catch(() => false);
    const criadaPorVisible = await page.getByText(/Criada por/i).isVisible().catch(() => false);

    appendLog(`"Bloco selecionado" visible: ${selectedBlocoText}`);
    appendLog(`"Adicionar unidade" visible: ${adicionarUnidade}`);
    appendLog(`"Criada por" audit text visible: ${criadaPorVisible}`);

    const panelAppeared = selectedBlocoText || adicionarUnidade;

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog(`--- BROWSER CONSOLE UT-05 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
    }

    await page.screenshot({ path: path.join(SS_DIR, "ut05_final.png"), fullPage: true });

    if (panelAppeared) {
      appendLog(`--- RESULTADO: PASS ---\nPanel/toolbar: selected=${selectedBlocoText}, adicionarUnidade=${adicionarUnidade}\nAudit "Criada por": ${criadaPorVisible}\n`);
    } else {
      appendLog(`--- RESULTADO: FAIL ---\nNo panel/toolbar after click. blocoVisible=${blocoVisible}\n`);
    }

    await context.close();
    expect(panelAppeared, `Clicking bloco should show panel/toolbar. blocoVisible=${blocoVisible}`).toBe(true);
  });

  test("UT-06: Responsividade tablet 768x1024", async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const cookieA = readCookieFromFile(`${EVIDENCE_DIR}/cookies_sindico_a.txt`);
    const { context, page } = await getAuthenticatedPage(browser, cookieA, { width: 768, height: 1024 });
    page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector("h1", { timeout: 15000 });
    await page.waitForTimeout(3000);
    await page.screenshot({ path: path.join(SS_DIR, "ut06_tablet_768x1024.png"), fullPage: true });

    appendLog(`
========================================
UT-06: Responsividade tablet 768x1024
Timestamp: ${new Date().toISOString()}
========================================
Viewport: 768x1024, URL: ${page.url()}
`);

    const blocoVisible = await page.getByText("Bloco QA-01").isVisible().catch(() => false);
    const structTitle = await page.getByText("Estrutura do condomínio").isVisible().catch(() => false);

    appendLog(`Bloco QA-01 visible at 768x1024: ${blocoVisible}`);
    appendLog(`Title "Estrutura do condomínio" visible: ${structTitle}`);

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog(`--- BROWSER CONSOLE UT-06 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
    }

    if (blocoVisible) {
      appendLog("--- RESULTADO: PASS ---\n");
    } else {
      appendLog(`--- RESULTADO: FAIL ---\nTree not visible at 768x1024. URL: ${page.url()}\n`);
    }

    await context.close();
    expect(blocoVisible, `Tree must be functional on 768x1024. URL: ${page.url()}`).toBe(true);
  });

  test("UT-07: Empty state (Tenant B)", async ({ browser }) => {
    appendLog(`
========================================
UT-07: Empty state
Timestamp: ${new Date().toISOString()}
========================================
`);

    // Check via direct API call if Tenant B has blocks
    const cookieB = readCookieFromFile(`${EVIDENCE_DIR}/cookies_sindico_b.txt`);
    const estruturaRes = await fetch(`${API_URL}/api/v1/condominios/${TENANT_B_ID}/estrutura`, {
      headers: { "Cookie": `portabox.auth=${cookieB}` },
    });

    if (estruturaRes.ok) {
      const body = await estruturaRes.json() as { blocos: unknown[] };
      appendLog(`Tenant B blocos count: ${body.blocos.length}`);
      if (body.blocos.length > 0) {
        appendLog(`BLOCKED: Tenant B has ${body.blocos.length} bloco(s) — empty state cannot be tested\n`);
        test.skip();
        return;
      }
    } else {
      appendLog(`BLOCKED: Tenant B estrutura check HTTP ${estruturaRes.status}\n`);
      test.skip();
      return;
    }

    // Tenant B has no blocos
    const { context, page } = await getAuthenticatedPage(browser, cookieB);
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector("h1", { timeout: 15000 });
    await page.waitForTimeout(3000);
    await page.screenshot({ path: path.join(SS_DIR, "ut07_empty_state.png"), fullPage: true });

    const emptyStateVisible = await page.getByText(/cadastrar primeiro bloco|nenhum bloco/i).isVisible().catch(() => false);
    appendLog(`Empty state visible (Tenant B): ${emptyStateVisible}`);

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog(`--- BROWSER CONSOLE UT-07 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
    }

    if (emptyStateVisible) {
      appendLog("--- RESULTADO: PASS ---\n");
    } else {
      appendLog("--- RESULTADO: FAIL ---\nEmpty state not visible for Tenant B\n");
    }

    await context.close();
    expect(emptyStateVisible, "Empty state must be visible for tenant with no blocos").toBe(true);
  });
});
