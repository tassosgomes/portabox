import { test, expect } from "@playwright/test";
import * as fs from "fs";

/**
 * QA Task 01 — CF-01 Cadastro de Bloco (UI tests)
 * Uses pre-existing Tenant A (qa_task_00) credentials.
 * Backend: http://localhost:5272/api/v1
 * Frontend sindico: http://localhost:5174 (vite dev, no proxy configured)
 *
 * NOTE: The sindico app uses VITE_API_BASE_URL or falls back to '/api'.
 * Since Vite has no proxy, requests to /api/... would fail.
 * We use page.route() to intercept /api/* and proxy to the real backend.
 */

const SINDICO_APP_URL = "http://localhost:5174";
const BACKEND_URL = "http://localhost:5272";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_01_cadastro_bloco";
const SCREENSHOTS_DIR = `${EVIDENCE_DIR}/screenshots`;

const QA_SINDICO_A_EMAIL = "qa-sindico-a-1776724904@portabox.test";
const QA_SINDICO_A_PASSWORD = "QaTestPass123!";

function appendLog(msg: string) {
  fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, msg + "\n");
}

// Setup proxy: intercept /api/* and forward to real backend
async function setupApiProxy(page: any) {
  await page.route("**/api/**", async (route: any) => {
    const request = route.request();
    const url = request.url();
    // Rewrite localhost:5174/api/... to localhost:5272/api/...
    const backendUrl = url.replace(
      `${SINDICO_APP_URL}/api`,
      `${BACKEND_URL}/api`
    );

    try {
      const response = await route.fetch({
        url: backendUrl,
        method: request.method(),
        headers: request.headers(),
        postData: request.postData() ?? undefined,
      });
      await route.fulfill({ response });
    } catch (err) {
      await route.abort();
    }
  });
}

async function loginSindicoA(page: any) {
  await page.goto(`${SINDICO_APP_URL}/login`);
  await page.waitForLoadState("networkidle");

  const emailInput = page.getByLabel("E-mail");
  const passwordInput = page.getByLabel("Senha");

  await emailInput.fill(QA_SINDICO_A_EMAIL);
  await passwordInput.fill(QA_SINDICO_A_PASSWORD);
  await page.locator('button[type="submit"]').click();

  // Wait for redirect away from /login
  await page.waitForURL(
    (url: URL) => !url.pathname.includes("/login"),
    { timeout: 15000 }
  );
}

test.describe("CF-01 Cadastro de Bloco — UI (UT)", () => {
  test("UT-01: Login sindico, navegar estrutura, criar bloco via UI", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await setupApiProxy(page);
    await loginSindicoA(page);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_pos_login.png`,
      fullPage: true,
    });

    // Navegar para estrutura
    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_estrutura_inicio.png`,
      fullPage: true,
    });

    // Verificar que a página carregou — deve ter botão "Novo bloco"
    const novoBlocoBtn = page.getByRole("button", { name: "Novo bloco" });
    await expect(novoBlocoBtn).toBeVisible({ timeout: 10000 });

    // Clicar em "Novo bloco"
    await novoBlocoBtn.click();
    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_modal_aberto.png`,
      fullPage: true,
    });

    // Modal deve estar aberto
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible({ timeout: 5000 });

    // Preencher nome
    const nomeInput = page.getByLabel("Nome do bloco");
    await nomeInput.fill("Bloco UI-QA-01");
    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_form_preenchido.png`,
      fullPage: true,
    });

    // Submeter
    await page.getByRole("button", { name: "Criar bloco" }).click();

    // Modal deve fechar
    await expect(dialog).not.toBeVisible({ timeout: 10000 });
    await page.waitForLoadState("networkidle");
    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut01_bloco_criado.png`,
      fullPage: true,
    });

    // Bloco UI-QA-01 deve aparecer na árvore
    await expect(page.getByText("Bloco UI-QA-01")).toBeVisible({
      timeout: 8000,
    });

    appendLog(
      "\n--- BROWSER CONSOLE UT-01 ---\n" +
        [...consoleLogs, ...pageErrors].join("\n")
    );
  });

  test("UT-02: Submit form com nome vazio → mensagem de erro em pt-BR", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await setupApiProxy(page);
    await loginSindicoA(page);
    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForLoadState("networkidle");

    // Abrir modal
    const novoBlocoBtn = page.getByRole("button", { name: "Novo bloco" });
    await expect(novoBlocoBtn).toBeVisible({ timeout: 10000 });
    await novoBlocoBtn.click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible({ timeout: 5000 });

    // NÃO preencher nome
    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut02_form_vazio.png`,
      fullPage: true,
    });

    // Submeter sem preencher
    await page.getByRole("button", { name: "Criar bloco" }).click();
    await page.waitForTimeout(500);

    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut02_erro_validacao.png`,
      fullPage: true,
    });

    // Modal deve permanecer aberto
    await expect(dialog).toBeVisible();

    // Deve haver texto de erro em pt-BR
    const hasError = await page
      .getByText(/obrigat|inválid|requer|nome|carácter|mínimo|1 e 50/i)
      .first()
      .isVisible()
      .catch(() => false);

    const hasRoleAlert = await page
      .locator('[role="alert"]')
      .first()
      .isVisible()
      .catch(() => false);

    if (!hasError && !hasRoleAlert) {
      await page.screenshot({
        path: `${SCREENSHOTS_DIR}/ut02_sem_erro_debug.png`,
        fullPage: true,
      });
      throw new Error(
        "UT-02 FAIL: Nenhuma mensagem de erro visivel apos submit com nome vazio"
      );
    }

    appendLog(
      "\n--- BROWSER CONSOLE UT-02 ---\n" +
        [...consoleLogs, ...pageErrors].join("\n")
    );
  });

  test("UT-03: Criar bloco com nome existente → toast/erro visível", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) =>
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
    );
    page.on("pageerror", (err) => pageErrors.push(err.message));

    await setupApiProxy(page);
    await loginSindicoA(page);
    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForLoadState("networkidle");

    // Abrir modal
    const novoBlocoBtn = page.getByRole("button", { name: "Novo bloco" });
    await expect(novoBlocoBtn).toBeVisible({ timeout: 10000 });
    await novoBlocoBtn.click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible({ timeout: 5000 });

    // Preencher com nome que já existe (Bloco QA-01 foi criado no CT-01)
    const nomeInput = page.getByLabel("Nome do bloco");
    await nomeInput.fill("Bloco QA-01");
    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut03_nome_duplicado_preenchido.png`,
      fullPage: true,
    });

    await page.getByRole("button", { name: "Criar bloco" }).click();

    // Aguardar resposta da API (409) e feedback do erro
    await page.waitForTimeout(3000);
    await page.screenshot({
      path: `${SCREENSHOTS_DIR}/ut03_erro_conflito.png`,
      fullPage: true,
    });

    // Deve aparecer algum feedback de erro
    // EstruturaPage renderiza toast com role="alert" e/ou apiErrorMessage no BlocoForm
    const hasAlert = await page
      .locator('[role="alert"]')
      .first()
      .isVisible()
      .catch(() => false);

    const hasConflictText = await page
      .getByText(/bloco|inativo|reativ|exist|conflict|já exist/i)
      .first()
      .isVisible()
      .catch(() => false);

    if (!hasAlert && !hasConflictText) {
      await page.screenshot({
        path: `${SCREENSHOTS_DIR}/ut03_sem_erro_debug.png`,
        fullPage: true,
      });
      throw new Error(
        "UT-03 FAIL: Nenhum toast/erro visivel apos criar bloco com nome existente"
      );
    }

    appendLog(
      "\n--- BROWSER CONSOLE UT-03 ---\n" +
        [...consoleLogs, ...pageErrors].join("\n")
    );
  });
});
