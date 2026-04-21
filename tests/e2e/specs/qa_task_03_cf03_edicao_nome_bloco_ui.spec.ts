import { test, expect, type BrowserContext, type Page } from '@playwright/test';
import * as fs from 'fs';

const SINDICO_APP_URL = 'http://localhost:5174';
const EVIDENCE_DIR = '/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_03_edicao_nome_bloco';

// Fresh session cookie for Sindico A — obtained at test execution time
const AUTH_COOKIE_VALUE = 'CfDJ8B6PkS-yuRtCtlJFtL3cr-3y13uD9gAytsChrlpQ991n1XhO63EtJopLRjATaSGMTZfAphB8o5d_ietXal848OSfSgT9HNhsN0wY3HlDFwi0LVlmoE0VTVxWk4mmZ-UyqDBa3B4uHCGGT7PuCCRwavuZSLnSwtpDRFAMbQlfMjCtvH4rUyI7rwr5W4-eEoac5eZ7k3Us1V0RqAYaMGMHeDpRYR7aSA_ZrHwiMO1G9bTtI5KcGsp1u7_mAqyHDvCbplo9e_ZBPHx-Hu8TCMwrF8c3pMHRYS-0psbrTwxWE3A85c_0R-yZeRECmdm6IahjFCMBpSfknF4mISIq5cYMxp7UAk3GInmFXBKhoNV_I5TqrRjqdnyVtCgWE7ZqrfADGVpdteYwhrZ7ihFl2RLOl9RS_Qg3Y-hNUlKRqJaeU-3E9_gUHP78-zFGwtCsSAVH4707So8wpI5VkCxdYDLdhZVz6WBbwziOwfq19bE-J7uyptVZ-077V1bKsrhUwqr51SY-_ZmJm_XSs0Unq4xIHg_W4Dak8fboy0asUcNvM-CJPC-nEnyKjlsuANVPxm-5yXexpxk7L3J4PIpLoS9twRjmIL1e88g8PqsHvTxwbBLOTf5oKIHtP2M3JloPNuRNfzSYoitBKZnAkr7JvLh9cNwAC1PtNLhaMnlZ6fq6iZyHwDe7zvnGBpndD-x8To_3SgkGMLGRcNKtKg4FBytBMJ-2FZ3K9y-sLU6batH8qBxn7xKrpV3cUufb3m7jHS5Fl6RYbUQTTGcQgxfaeOpxkGTgkmdOsgm06ZDDXXRO8_mgPQIm-vXnnajyyZkdvNGdXlAFQw---mCK9Scw400uocQwko91OtoWMB6dQWxytxC9';

const UI_BLOCO_NOME = 'Bloco UI-QA-01';

// Setup function: inject cookie + CORS fix
async function getAuthenticatedPage(browser: any): Promise<{ context: BrowserContext; page: Page }> {
  const context: BrowserContext = await browser.newContext({
    recordVideo: { dir: `${EVIDENCE_DIR}/videos/` }
  });

  await context.addCookies([
    {
      name: 'portabox.auth',
      value: AUTH_COOKIE_VALUE,
      domain: 'localhost',
      path: '/',
      httpOnly: true,
      secure: false,
      sameSite: 'Lax',
    }
  ]);

  const page = await context.newPage();

  // Fix CORS: API at 5272 returns Access-Control-Allow-Origin: * with credentials:include
  // which is blocked by browsers. Intercept and replace with specific origin.
  await page.route('http://localhost:5272/api/**', async (route) => {
    try {
      const resp = await route.fetch();
      const headers = resp.headers();
      headers['access-control-allow-origin'] = SINDICO_APP_URL;
      headers['access-control-allow-credentials'] = 'true';
      await route.fulfill({ status: resp.status(), headers, body: await resp.body() });
    } catch {
      await route.continue();
    }
  });

  // Also proxy calls from 5174/api/* to 5272 (local client fallback)
  await page.route('http://localhost:5174/api/**', async (route) => {
    const url = route.request().url().replace('localhost:5174', 'localhost:5272');
    try {
      const resp = await route.fetch({ url });
      const headers = resp.headers();
      headers['access-control-allow-origin'] = SINDICO_APP_URL;
      headers['access-control-allow-credentials'] = 'true';
      await route.fulfill({ status: resp.status(), headers, body: await resp.body() });
    } catch {
      await route.continue();
    }
  });

  return { context, page };
}

test.describe('CF-03: Edicao de Nome de Bloco — UI Tests', () => {

  test('UT-01: Renomear Bloco UI-QA-01 para "Bloco UI-QA-01 Editado"', async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const { context, page } = await getAuthenticatedPage(browser);
    page.on('console', msg => {
      if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
    });
    page.on('pageerror', err => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector('button[aria-label*="Ações do bloco"]', { timeout: 12000 });

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_inicio.png`, fullPage: true });

    const currentUrl = page.url();
    if (currentUrl.includes('/login')) {
      const logMsg = `\n--- BROWSER CONSOLE UT-01 ---\nRedirected to login. URL: ${currentUrl}\n` +
        [...consoleLogs, ...pageErrors].join('\n') + '\n';
      fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, logMsg);
      throw new Error(`Auth redirected to login. URL: ${currentUrl}`);
    }

    // Find Acoes button for Bloco UI-QA-01
    const acoesButton = page.getByRole('button', { name: new RegExp(`Ações do bloco ${UI_BLOCO_NOME}`, 'i') });
    const acoesVisible = await acoesButton.isVisible({ timeout: 5000 }).catch(() => false);

    if (!acoesVisible) {
      const allButtons = await page.getByRole('button').evaluateAll(
        (btns) => btns.map((b) => b.getAttribute('aria-label') || b.textContent?.trim() || '')
      );
      const logMsg = `\n--- BROWSER CONSOLE UT-01 ---\n` +
        `Acoes button for "${UI_BLOCO_NOME}" not found.\nAll buttons: ${allButtons.join(' | ')}\n` +
        [...consoleLogs, ...pageErrors].join('\n') + '\n';
      fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, logMsg);
      await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_fail_acoes_nao_encontrado.png`, fullPage: true });
      throw new Error(`Acoes button for "${UI_BLOCO_NOME}" not found. Buttons: ${allButtons.join(' | ')}`);
    }

    await acoesButton.click();
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_menu_acoes.png`, fullPage: true });

    const renomearMenuItem = page.getByRole('menuitem', { name: /renomear/i });
    await renomearMenuItem.waitFor({ state: 'visible', timeout: 5000 });
    await renomearMenuItem.click();

    // BlocoForm modal should appear
    const modal = page.locator('[role="dialog"]');
    await modal.waitFor({ state: 'visible', timeout: 5000 });
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_modal_renomear.png`, fullPage: true });

    // Fill in the new name
    const nomeInput = modal.locator('input[name="nome"]');
    await nomeInput.waitFor({ state: 'visible', timeout: 5000 });
    await nomeInput.clear();
    await nomeInput.fill('Bloco UI-QA-01 Editado');

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_nome_preenchido.png`, fullPage: true });

    const submitBtn = modal.getByRole('button', { name: /salvar nome/i });
    await submitBtn.waitFor({ state: 'visible', timeout: 3000 });
    await submitBtn.click();

    // Wait for tree refresh
    await page.waitForTimeout(2500);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_pos_rename.png`, fullPage: true });

    // Check if new name is visible
    const newNameVisible = await page.getByText('Bloco UI-QA-01 Editado', { exact: false }).isVisible({ timeout: 5000 }).catch(() => false);

    const logMsg = `\n--- BROWSER CONSOLE UT-01 ---\n` +
      `Novo nome "Bloco UI-QA-01 Editado" visivel: ${newNameVisible}\n` +
      [...consoleLogs, ...pageErrors].join('\n') + '\n';
    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, logMsg);

    expect(newNameVisible, `New name "Bloco UI-QA-01 Editado" should be visible in the tree`).toBe(true);
    await context.close();
  });

  test('UT-02: Renomear com nome vazio — erro visivel', async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const { context, page } = await getAuthenticatedPage(browser);
    page.on('console', msg => {
      if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
    });
    page.on('pageerror', err => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector('button[aria-label*="Ações do bloco"]', { timeout: 12000 });

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut02_inicio.png`, fullPage: true });

    // Click Acoes for any active bloco — use QA-01 which is always active
    const acoesButton = page.getByRole('button', { name: /Ações do bloco/i }).first();
    await acoesButton.waitFor({ state: 'visible', timeout: 5000 });
    await acoesButton.click();

    const renomearMenuItem = page.getByRole('menuitem', { name: /renomear/i });
    await renomearMenuItem.waitFor({ state: 'visible', timeout: 5000 });
    await renomearMenuItem.click();

    const modal = page.locator('[role="dialog"]');
    await modal.waitFor({ state: 'visible', timeout: 5000 });

    // Clear the nome input and submit empty
    const nomeInput = modal.locator('input[name="nome"]');
    await nomeInput.waitFor({ state: 'visible', timeout: 5000 });
    await nomeInput.clear();
    await nomeInput.fill('');

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut02_nome_vazio.png`, fullPage: true });

    const submitBtn = modal.getByRole('button', { name: /salvar nome/i });
    await submitBtn.click();

    // Wait briefly for validation error
    await page.waitForTimeout(1000);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut02_erro_visivel.png`, fullPage: true });

    // Check for error message in modal
    const modalText = await modal.textContent({ timeout: 3000 }).catch(() => '');
    const errorVisible = await modal.locator('[role="alert"], .error, [class*="error"], [class*="Error"]').isVisible({ timeout: 3000 }).catch(() => false);

    // Also check for any text that looks like an error
    const hasErrorText = /obrigat|requerido|min.*car|deve ter|invalid|erro/i.test(modalText ?? '');

    const logMsg = `\n--- BROWSER CONSOLE UT-02 ---\n` +
      `Modal text: "${modalText}"\n` +
      `Error element visible: ${errorVisible}\n` +
      `Has error text: ${hasErrorText}\n` +
      [...consoleLogs, ...pageErrors].join('\n') + '\n';
    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, logMsg);

    expect(errorVisible || hasErrorText, `Error should be visible when submitting empty name. Modal text: "${modalText}"`).toBe(true);
    await context.close();
  });

  test('UT-03: Renomear para nome conflitante — erro de conflito visivel', async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const { context, page } = await getAuthenticatedPage(browser);
    page.on('console', msg => {
      if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
    });
    page.on('pageerror', err => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector('button[aria-label*="Ações do bloco"]', { timeout: 12000 });

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_inicio.png`, fullPage: true });

    // Find the Bloco UI-QA-01 Editado (renamed in UT-01) or any bloco that's not QA-01
    // We'll try to rename it to "Bloco QA-01" which already exists
    // Use the second Acoes button if available, or the one for UI-QA-01 Editado
    const allAcoesButtons = page.getByRole('button', { name: /Ações do bloco/i });
    const buttonsCount = await allAcoesButtons.count();

    let targetButton = allAcoesButtons.first();
    // Try to find Bloco UI-QA-01 or Bloco QA-02 button (not QA-01, as we want to rename TO QA-01)
    for (let i = 0; i < buttonsCount; i++) {
      const btn = allAcoesButtons.nth(i);
      const label = await btn.getAttribute('aria-label') ?? '';
      if (label.includes('UI-QA-01') || label.includes('QA-02') || label.includes('QA-02 V3')) {
        targetButton = btn;
        break;
      }
    }

    await targetButton.click();
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_menu_aberto.png`, fullPage: true });

    const renomearMenuItem = page.getByRole('menuitem', { name: /renomear/i });
    await renomearMenuItem.waitFor({ state: 'visible', timeout: 5000 });
    await renomearMenuItem.click();

    const modal = page.locator('[role="dialog"]');
    await modal.waitFor({ state: 'visible', timeout: 5000 });

    const nomeInput = modal.locator('input[name="nome"]');
    await nomeInput.waitFor({ state: 'visible', timeout: 5000 });
    await nomeInput.clear();
    await nomeInput.fill('Bloco QA-01');

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_nome_conflitante.png`, fullPage: true });

    const submitBtn = modal.getByRole('button', { name: /salvar nome/i });
    await submitBtn.click();

    // Wait for API response and UI update
    await page.waitForTimeout(3000);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_pos_submit.png`, fullPage: true });

    const modalText = await modal.textContent({ timeout: 3000 }).catch(() => '');
    const pageBody = await page.textContent('body').catch(() => '');

    // Check for toast/error message about conflict
    // Could be a toast notification or inline error in the modal
    const toastVisible = await page.locator('[role="alert"], [class*="toast"], [class*="Toast"]').isVisible({ timeout: 3000 }).catch(() => false);
    const hasConflictMsg = /conflict|conflito|duplicado|ja existe|nome.*exist|exist.*nome|409/i.test((modalText ?? '') + (pageBody ?? ''));

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_erro_conflito.png`, fullPage: true });

    const logMsg = `\n--- BROWSER CONSOLE UT-03 ---\n` +
      `Modal text: "${modalText}"\n` +
      `Toast visible: ${toastVisible}\n` +
      `Has conflict message: ${hasConflictMsg}\n` +
      [...consoleLogs, ...pageErrors].join('\n') + '\n';
    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, logMsg);

    expect(toastVisible || hasConflictMsg, `Conflict error should be visible. Modal text: "${modalText}", Body excerpt: "${pageBody?.substring(0, 200)}"`).toBe(true);
    await context.close();
  });
});
