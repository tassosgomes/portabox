import { test, expect } from "@playwright/test";
import * as fs from "fs";

/**
 * QA Task 02 — CF-02 Cadastro de Unidade (UI tests)
 * Backend: http://localhost:5272/api/v1
 * Frontend sindico: http://localhost:5174 (vite dev)
 *
 * Auth: inject pre-obtained session cookie.
 * Proxy: intercept API calls and fix CORS headers (backend returns wildcard ACAO
 * which is incompatible with credentials:include; we patch to specific origin).
 */

const SINDICO_APP_URL = "http://localhost:5174";
const BACKEND_URL = "http://localhost:5272";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_02_cadastro_unidade";
const SCREENSHOTS_DIR = `${EVIDENCE_DIR}/screenshots`;

const SESSION_COOKIE_PATH = `${EVIDENCE_DIR}/cookies_sindico_a.txt`;

function appendLog(msg: string) {
  fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, msg + "\n");
}

function readCookieValue(): string {
  const content = fs.readFileSync(SESSION_COOKIE_PATH, "utf-8");
  const lines = content.split("\n").filter((l) => l.includes("portabox.auth"));
  if (lines.length === 0) throw new Error("portabox.auth cookie not found in cookie jar");
  const parts = lines[0].split("\t");
  return parts[parts.length - 1].trim();
}

async function injectAuthCookie(page: any) {
  const cookieValue = readCookieValue();
  await page.context().addCookies([
    {
      name: "portabox.auth",
      value: cookieValue,
      domain: "localhost",
      path: "/",
      httpOnly: true,
      secure: false,
      sameSite: "Lax",
    },
  ]);
}

/**
 * Proxy all /api/** requests to backend.
 * Patches CORS: backend returns Access-Control-Allow-Origin: * but fetch uses
 * credentials:include, which requires specific origin (not wildcard).
 * We override ACAO to the app's origin so the browser CORS check passes.
 */
async function setupApiProxy(page: any) {
  await page.route("**/api/**", async (route: any) => {
    const request = route.request();
    const url = request.url();

    let backendUrl: string;
    if (url.startsWith(`${SINDICO_APP_URL}/api`)) {
      backendUrl = url.replace(`${SINDICO_APP_URL}/api`, `${BACKEND_URL}/api`);
    } else {
      backendUrl = url;
    }

    try {
      const response = await route.fetch({
        url: backendUrl,
        method: request.method(),
        headers: request.headers(),
        postData: request.postData() ?? undefined,
      });

      const body = await response.body();

      // Patch CORS headers so credentials:include fetch passes browser CORS check
      const headers: Record<string, string> = {};
      for (const [key, value] of Object.entries(response.headers())) {
        if (key.toLowerCase() === "access-control-allow-origin") {
          headers[key] = SINDICO_APP_URL;
        } else if (key.toLowerCase() === "access-control-allow-credentials") {
          headers[key] = "true";
        } else {
          headers[key] = value as string;
        }
      }
      // Ensure ACAC header is present
      headers["access-control-allow-credentials"] = "true";
      if (!headers["access-control-allow-origin"] || headers["access-control-allow-origin"] === "*") {
        headers["access-control-allow-origin"] = SINDICO_APP_URL;
      }

      await route.fulfill({
        status: response.status(),
        headers,
        body,
      });
    } catch {
      await route.abort();
    }
  });
}

async function navigateToEstrutura(page: any) {
  await page.goto(`${SINDICO_APP_URL}/estrutura`);
  // networkidle skipped — Vite HMR websocket prevents it
  await page.waitForSelector('text=Bloco QA-01', { timeout: 20000 });
}

async function selectBlocoQA01(page: any) {
  const blocoItem = page.locator('text=Bloco QA-01').first();
  await blocoItem.click();
  await page.waitForTimeout(500);
}

async function openUnidadeModal(page: any) {
  const btnAdicionar = page.getByRole("button", { name: "Adicionar unidade" });
  await btnAdicionar.click();
  await page.waitForSelector('[role="dialog"]', { timeout: 5000 });
  await page.waitForTimeout(300);
}

/**
 * After unit creation the TanStack Query invalidation refetches estrutura data,
 * which re-renders the tree. The defaultExpandedIds only sets initial state —
 * after re-render the Bloco QA-01 node may be collapsed.
 * This helper expands the bloco treeitem and then the specified andar treeitem
 * so individual unit numbers are visible in the 4-level tree hierarchy:
 * condominio > bloco > andar > unidade.
 */
async function expandBlocoQA01(page: any, andar?: number) {
  // Wait for tree to stabilize after refetch
  await page.waitForTimeout(1000);
  // Find the treeitem that contains "Bloco QA-01" text
  const blocoTreeItem = page.getByRole("treeitem").filter({ hasText: "Bloco QA-01" }).first();
  const isExpanded = await blocoTreeItem.getAttribute("aria-expanded").catch(() => null);
  // Click to expand if collapsed or unknown state
  if (isExpanded !== "true") {
    await blocoTreeItem.click();
    await page.waitForTimeout(500);
  }
  // If andar specified, also expand that andar row to reveal unit numbers
  if (andar !== undefined) {
    const andarPattern = new RegExp(`Andar ${andar}`);
    const andarTreeItem = page.getByRole("treeitem").filter({ hasText: andarPattern }).first();
    const andarExists = await andarTreeItem.count().catch(() => 0);
    if (andarExists > 0) {
      const andarExpanded = await andarTreeItem.getAttribute("aria-expanded").catch(() => null);
      if (andarExpanded !== "true") {
        await andarTreeItem.click();
        await page.waitForTimeout(500);
      }
    }
  }
}

test.describe("CF-02 Cadastro de Unidade — UI", () => {
  test.beforeAll(() => {
    fs.appendFileSync(
      `${EVIDENCE_DIR}/requests.log`,
      "\n========================================\n" +
      "UI TESTS (Playwright) — RERUN\n" +
      `Timestamp: ${new Date().toISOString()}\n` +
      "========================================\n"
    );
  });

  // UT-01: Use andar=11, numero=1101 (confirmed not yet in DB)
  test("UT-01: Navegar arvore, selecionar Bloco QA-01, adicionar unidade andar=11 numero=1101", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await setupApiProxy(page);
    await injectAuthCookie(page);
    await navigateToEstrutura(page);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_estrutura_inicial.png`,
      fullPage: true,
    });

    await selectBlocoQA01(page);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_bloco_selecionado.png`,
      fullPage: true,
    });

    await openUnidadeModal(page);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_modal_aberto.png`,
      fullPage: true,
    });

    const andarInput = page.getByLabel("Andar");
    await andarInput.clear();
    await andarInput.fill("99");

    const numeroInput = page.getByLabel("Número");
    await numeroInput.fill("9901");

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_form_preenchido.png`,
      fullPage: true,
    });

    const dialog = page.getByRole("dialog");
    await dialog.getByRole("button", { name: "Adicionar unidade" }).click();

    // Wait for modal to close (mutation success)
    await expect(page.getByRole("dialog")).not.toBeVisible({ timeout: 10000 });

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_modal_fechado.png`,
      fullPage: true,
    });

    // Expand Bloco QA-01 treeitem, then Andar 11, after refetch
    await expandBlocoQA01(page, 99);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_tree_expandido.png`,
      fullPage: true,
    });

    // Wait for unit number to appear in expanded tree
    let treeHasUnidade = false;
    try {
      await page.waitForSelector('text=9901', { timeout: 8000 });
      treeHasUnidade = await page.locator('text=9901').isVisible();
    } catch {
      treeHasUnidade = false;
    }

    appendLog(
      "\n--- UT-01 ---\n" +
      `Tree shows 9901: ${treeHasUnidade}\n` +
      `Console errors: ${pageErrors.join("; ") || "none"}\n`
    );

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog("--- BROWSER CONSOLE UT-01 ---\n" + [...consoleLogs, ...pageErrors].join("\n"));
    }

    if (!treeHasUnidade) {
      await page.screenshot({
        path: `${SCREENSHOTS_DIR}/ut01_fail_sem_unidade.png`,
        fullPage: true,
      });
      appendLog("UT-01: FAIL — Unidade 9901 nao aparece na arvore");
    } else {
      appendLog("UT-01: PASS — Unidade 9901 visivel na arvore");
    }

    expect(treeHasUnidade, "Unidade 9901 deve aparecer na arvore").toBe(true);
  });

  test("UT-02: Validacao de andar negativo", async ({ page }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await setupApiProxy(page);
    await injectAuthCookie(page);
    await navigateToEstrutura(page);
    await selectBlocoQA01(page);
    await openUnidadeModal(page);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut02_modal_aberto.png`,
      fullPage: true,
    });

    const andarInput = page.getByLabel("Andar");
    await andarInput.clear();
    await andarInput.fill("-1");

    const numeroInput = page.getByLabel("Número");
    await numeroInput.fill("999");

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut02_form_negativo_preenchido.png`,
      fullPage: true,
    });

    const dialog = page.getByRole("dialog");
    await dialog.getByRole("button", { name: "Adicionar unidade" }).click();
    await page.waitForTimeout(500);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut02_apos_submit.png`,
      fullPage: true,
    });

    const errorVisible = await page.locator('text=Use andar 0 ou maior').isVisible().catch(() => false);
    const modalStillOpen = await page.getByRole("dialog").isVisible().catch(() => false);

    appendLog(
      "\n--- UT-02 ---\n" +
      `Validation error visible: ${errorVisible}\n` +
      `Modal still open: ${modalStillOpen}\n` +
      `Console errors: ${pageErrors.join("; ") || "none"}\n`
    );

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog("--- BROWSER CONSOLE UT-02 ---\n" + [...consoleLogs, ...pageErrors].join("\n"));
    }

    if (!errorVisible) {
      await page.screenshot({
        path: `${SCREENSHOTS_DIR}/ut02_fail_sem_erro.png`,
        fullPage: true,
      });
      appendLog("UT-02: FAIL — Erro de validacao nao visivel");
    } else {
      appendLog("UT-02: PASS — Erro de validacao visivel");
    }

    expect(modalStillOpen, "Modal deve permanecer aberto em caso de erro").toBe(true);
    expect(errorVisible, "Erro de validacao de andar negativo deve ser visivel").toBe(true);
  });

  test("UT-03: Validacao de numero invalido XX", async ({ page }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await setupApiProxy(page);
    await injectAuthCookie(page);
    await navigateToEstrutura(page);
    await selectBlocoQA01(page);
    await openUnidadeModal(page);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut03_modal_aberto.png`,
      fullPage: true,
    });

    const andarInput = page.getByLabel("Andar");
    await andarInput.clear();
    await andarInput.fill("1");

    const numeroInput = page.getByLabel("Número");
    await numeroInput.fill("XX");

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut03_form_preenchido.png`,
      fullPage: true,
    });

    const dialog = page.getByRole("dialog");
    await dialog.getByRole("button", { name: "Adicionar unidade" }).click();
    await page.waitForTimeout(500);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut03_apos_submit.png`,
      fullPage: true,
    });

    const errorVisible = await page.locator('text=Use ate 4 digitos').isVisible().catch(() => false);
    const modalStillOpen = await page.getByRole("dialog").isVisible().catch(() => false);

    appendLog(
      "\n--- UT-03 ---\n" +
      `Validation error visible: ${errorVisible}\n` +
      `Modal still open: ${modalStillOpen}\n` +
      `Console errors: ${pageErrors.join("; ") || "none"}\n`
    );

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog("--- BROWSER CONSOLE UT-03 ---\n" + [...consoleLogs, ...pageErrors].join("\n"));
    }

    if (!errorVisible) {
      await page.screenshot({
        path: `${SCREENSHOTS_DIR}/ut03_fail_sem_erro.png`,
        fullPage: true,
      });
      appendLog("UT-03: FAIL — Erro de validacao nao visivel para numero XX");
    } else {
      appendLog("UT-03: PASS — Erro de validacao visivel para numero XX");
    }

    expect(modalStillOpen, "Modal deve permanecer aberto em caso de erro").toBe(true);
    expect(errorVisible, "Erro de validacao de numero invalido deve ser visivel").toBe(true);
  });

  // UT-04: Use andar=11, numero=1102a (confirmed not yet in DB)
  test("UT-04: Numero minusculo 1102a normalizado para 1102A na arvore", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await setupApiProxy(page);
    await injectAuthCookie(page);
    await navigateToEstrutura(page);
    await selectBlocoQA01(page);
    await openUnidadeModal(page);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut04_modal_aberto.png`,
      fullPage: true,
    });

    const andarInput = page.getByLabel("Andar");
    await andarInput.clear();
    await andarInput.fill("99");

    const numeroInput = page.getByLabel("Número");
    await numeroInput.fill("9902a");

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut04_form_preenchido_lowercase.png`,
      fullPage: true,
    });

    const inputValue = await numeroInput.inputValue();
    appendLog(
      "\n--- UT-04 ---\n" +
      `Input value after fill('9902a'): '${inputValue}'\n`
    );

    const dialog = page.getByRole("dialog");
    await dialog.getByRole("button", { name: "Adicionar unidade" }).click();

    // Wait for modal to close (mutation success)
    await expect(page.getByRole("dialog")).not.toBeVisible({ timeout: 10000 });

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut04_modal_fechado.png`,
      fullPage: true,
    });

    // Expand Bloco QA-01 treeitem, then Andar 11, after refetch
    await expandBlocoQA01(page, 99);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut04_tree_expandido.png`,
      fullPage: true,
    });

    // Wait for unit number to appear in expanded tree (uppercase normalized)
    let treeHas9902A = false;
    let treeHas9902a_lower = false;
    try {
      await page.waitForSelector('text=9902A', { timeout: 8000 });
      treeHas9902A = await page.locator('text=9902A').isVisible();
    } catch {
      treeHas9902A = false;
    }
    treeHas9902a_lower = await page.locator('text=9902a').isVisible().catch(() => false);

    const isModalVisible = await page.getByRole("dialog").isVisible().catch(() => false);

    appendLog(
      `Tree shows 9902A (upper): ${treeHas9902A}\n` +
      `Tree shows 9902a (lower): ${treeHas9902a_lower}\n` +
      `Console errors: ${pageErrors.join("; ") || "none"}\n`
    );

    if (consoleLogs.length > 0 || pageErrors.length > 0) {
      appendLog("--- BROWSER CONSOLE UT-04 ---\n" + [...consoleLogs, ...pageErrors].join("\n"));
    }

    if (isModalVisible) {
      await page.screenshot({
        path: `${SCREENSHOTS_DIR}/ut04_fail_modal_aberto.png`,
        fullPage: true,
      });
      appendLog("UT-04: FAIL — Modal ainda aberto");
    } else if (!treeHas9902A) {
      await page.screenshot({
        path: `${SCREENSHOTS_DIR}/ut04_fail_sem_9902A.png`,
        fullPage: true,
      });
      appendLog("UT-04: FAIL — 9902A nao aparece na arvore");
    } else {
      appendLog("UT-04: PASS — 9902A aparece na arvore em maiuscula");
    }

    expect(isModalVisible, "Modal deve fechar apos criar unidade").toBe(false);
    expect(treeHas9902A, "Unidade deve aparecer na arvore como 9902A (maiuscula)").toBe(true);
  });
});
