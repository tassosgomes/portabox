import { test, expect, type BrowserContext, type Page } from '@playwright/test';
import * as fs from 'fs';

const SINDICO_APP_URL = 'http://localhost:5174';
const EVIDENCE_DIR = '/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_05_inativacao_bloco';

// Fresh session cookie — new login performed after rate limit cleared
const AUTH_COOKIE_VALUE = 'CfDJ8B6PkS-yuRtCtlJFtL3cr-2rP2MuLYSUx8sN-6OYHymvBnhYcYTQiAnlp8lOni3q0ON0rvw56vgVpgoDYslP6Z_Jl3OsqS6VpdoktvmmBr6XVk31xJdPNwvrUWpexKpYep3c9AmA7HgVRatfYfcDKGdaVnfdAJ-Es44_f0KVW8NOgm0eUfJriuedCCm92BWDjkh4zRSD33aAWzPoY2gjGJ1Y6IExaPXw1JeZHR-PeoeX28VJlshjum1fzowUEB-iP_HRRQtUeaU8Udt6R2Jn0tUfW6a0F4v5uhUnVl5Kmy5bOhRvTx1LawVvaWkhvdQjzIoXjOSaZiPDjn_0o4xxFYtQzyGUjNJYrqcxxj-tL5ss9zwDtsqv8obZrU5BbwkEdi4UCraO8dMCtbg67VsWOEFFJ-DZ4ZHXlE7JiiJf-ynjpVcIBvKMiTztm0zkOUpGF3onRKy_owaRempze_ASSNgZHZCUij3nOjbSDPU9Y4UTE7nPDVTIM7KKz9PxhZs5nevROIBSRqJbMTdsMx29ZUpI4rS9WOVxPbNRpyTtlfX0RA0gOqnYmtXRYvDNnZhcRPukXgYAsS_b2BnpY7vKfjMu_Zcf2vOXPcCsMAMhpBTBF8A2dTitp_hnr5AAnw5BzIuXaJNRaCVQkZimF5pFoTTZi6DIpcLcRyqvCyNxrZTkYUjJPlqpRgQiBddZQAMCX3G9ubQMfo2cD4hwhpC30HSx5aTL6nNF2osl7ZyJXK2-DHqW2Rh9IxLKtgNK9IVgWBO31IcRYg8pbtZCzSbgXjuYwdNLbKQf5dT87rlgIycL4oPE2Up2GnO5ECcaEXACb2K50CPYj4QN_-Ks70QJRvjTPbAVUMgPvVxk7JIY-NkG';

const UI_BLOCO_NOME = 'Bloco UI Inativar QA';

// Setup function: inject cookie + CORS fix (API at 5272 uses wildcard CORS which
// is incompatible with credentials:include from origin 5174)
async function getAuthenticatedPage(browser: any): Promise<{ context: BrowserContext; page: Page }> {
  const context: BrowserContext = await browser.newContext();

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

test.describe('CF-05: Inativacao de Bloco — UI Tests', () => {

  test('UT-01: Inativar bloco via UI — modal confirma, bloco some da arvore padrao', async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const { context, page } = await getAuthenticatedPage(browser);
    page.on('console', msg => {
      if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
    });
    page.on('pageerror', err => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    // Wait for tree to load (CORS fix allows API calls to succeed)
    await page.waitForSelector('button[aria-label*="Ações do bloco"]', { timeout: 10000 });

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_inicio.png`, fullPage: true });

    const currentUrl = page.url();
    if (currentUrl.includes('/login')) {
      const logMsg = `\n--- BROWSER CONSOLE UT-01 ---\nAuth redirected to login. URL: ${currentUrl}\n` +
        [...consoleLogs, ...pageErrors].join('\n') + '\n';
      fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, logMsg);
      throw new Error(`Auth redirected to login. URL: ${currentUrl}`);
    }

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_estrutura_com_arvore.png`, fullPage: true });

    // Find Acoes button for Bloco UI Inativar QA
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
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_menu_acoes_aberto.png`, fullPage: true });

    const inativarMenuItem = page.getByRole('menuitem', { name: /inativar/i });
    await inativarMenuItem.waitFor({ state: 'visible', timeout: 5000 });
    await inativarMenuItem.click();

    const modal = page.locator('[role="dialog"]');
    await modal.waitFor({ state: 'visible', timeout: 5000 });
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_modal_confirmacao.png`, fullPage: true });

    const confirmBtn = page.getByRole('button', { name: /inativar bloco/i });
    await confirmBtn.waitFor({ state: 'visible', timeout: 5000 });
    await confirmBtn.click();

    // Wait for tree to refresh
    await page.waitForTimeout(2500);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_pos_inativacao.png`, fullPage: true });

    // Bloco should NOT appear in default view (includeInactive=false)
    const blocoAfter = page.getByText(UI_BLOCO_NOME, { exact: false });
    const blocoAfterVisible = await blocoAfter.isVisible({ timeout: 3000 }).catch(() => false);

    const logMsg = `\n--- BROWSER CONSOLE UT-01 ---\n` +
      `Bloco "${UI_BLOCO_NOME}" visible after inativacao (default view): ${blocoAfterVisible}\n` +
      [...consoleLogs, ...pageErrors].join('\n') + '\n';
    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, logMsg);

    expect(blocoAfterVisible, `Bloco "${UI_BLOCO_NOME}" should NOT appear in default tree after inativacao`).toBe(false);
    await context.close();
  });

  test('UT-02: Toggle Mostrar inativos — bloco inativo reaparece', async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const { context, page } = await getAuthenticatedPage(browser);
    page.on('console', msg => {
      if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
    });
    page.on('pageerror', err => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    // Wait for tree (may have fewer buttons if bloco is already inactive from UT-01)
    await page.waitForSelector('h1', { timeout: 10000 });
    await page.waitForTimeout(2000);

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut02_default_sem_inativos.png`, fullPage: true });

    // Find Mostrar inativos toggle
    const toggleCheckbox = page.locator('label').filter({ hasText: /mostrar inativos/i }).locator('input[type="checkbox"]');
    const toggleVisible = await toggleCheckbox.isVisible({ timeout: 5000 }).catch(() => false);

    if (!toggleVisible) {
      const pageText = (await page.textContent('body') ?? '').substring(0, 500);
      throw new Error(`"Mostrar inativos" toggle not found. Page text: ${pageText}`);
    }

    await toggleCheckbox.check();
    await page.waitForTimeout(2000);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut02_com_inativos.png`, fullPage: true });

    // Bloco inativo should now appear
    const blocoInativo = page.getByText(UI_BLOCO_NOME, { exact: false });
    const blocoVisible = await blocoInativo.isVisible({ timeout: 5000 }).catch(() => false);

    const logMsg = `\n--- BROWSER CONSOLE UT-02 ---\n` +
      `Bloco "${UI_BLOCO_NOME}" visible with Mostrar inativos=true: ${blocoVisible}\n` +
      [...consoleLogs, ...pageErrors].join('\n') + '\n';
    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, logMsg);

    expect(blocoVisible, `Bloco "${UI_BLOCO_NOME}" should be visible with "Mostrar inativos" toggled on`).toBe(true);
    await context.close();
  });

  test('UT-03: Modal de confirmacao tem copy pt-BR sobre nao-cascata', async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const { context, page } = await getAuthenticatedPage(browser);
    page.on('console', msg => {
      if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
    });
    page.on('pageerror', err => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector('button[aria-label*="Ações do bloco"]', { timeout: 10000 });

    // Click first active bloco's Acoes button
    const anyAcoesBtn = page.getByRole('button', { name: /ações do bloco/i }).first();
    const acoesVisible = await anyAcoesBtn.isVisible({ timeout: 5000 }).catch(() => false);

    if (!acoesVisible) {
      const allButtons = await page.getByRole('button').evaluateAll(
        (btns) => btns.map((b) => b.getAttribute('aria-label') || b.textContent?.trim() || '')
      );
      throw new Error(`No "Acoes do bloco" button found. Buttons: ${allButtons.join(' | ')}`);
    }

    const blocoAriaLabel = await anyAcoesBtn.getAttribute('aria-label') ?? '';
    await anyAcoesBtn.click();
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_menu_acoes.png`, fullPage: true });

    const inativarMenu = page.getByRole('menuitem', { name: /inativar/i });
    await inativarMenu.waitFor({ state: 'visible', timeout: 5000 });
    await inativarMenu.click();

    const modal = page.locator('[role="dialog"]');
    await modal.waitFor({ state: 'visible', timeout: 5000 });
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_modal_copy.png`, fullPage: true });

    const modalText = await modal.textContent({ timeout: 5000 }).catch(() => '');

    const logMsg = `\n--- BROWSER CONSOLE UT-03 ---\n` +
      `Bloco: ${blocoAriaLabel}\n` +
      `Modal full text: "${modalText}"\n` +
      [...consoleLogs, ...pageErrors].join('\n') + '\n';
    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, logMsg);

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_modal_texto_completo.png`, fullPage: true });

    // Cancel without confirming
    const cancelBtn = modal.getByRole('button', { name: /cancelar/i });
    await cancelBtn.waitFor({ state: 'visible', timeout: 3000 }).catch(() => {});
    await cancelBtn.click().catch(() => {});

    // Assertions
    const hasPtBrInativar = /inativar/i.test(modalText ?? '');
    const hasNoCascadeMsg = /separadamente|unidades.*permanecem|permanecem.*unidades/i.test(modalText ?? '');
    const hasCancelBtn = /cancelar/i.test(modalText ?? '');

    expect(hasPtBrInativar, `Modal must contain "Inativar" in pt-BR. Full modal: "${modalText}"`).toBe(true);
    expect(hasNoCascadeMsg, `Modal must mention unidades need to be inativadas separately. Full modal: "${modalText}"`).toBe(true);
    expect(hasCancelBtn, `Modal must have Cancelar button. Full modal: "${modalText}"`).toBe(true);

    await context.close();
  });
});
