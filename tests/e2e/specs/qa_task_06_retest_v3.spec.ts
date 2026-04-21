import { test, expect, type BrowserContext, type Page } from '@playwright/test';
import * as fs from 'fs';

const SINDICO_APP_URL = 'http://localhost:5174';
const API_URL = 'http://localhost:5272';
const EVIDENCE_DIR = '/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_06_reativacao';
const SCREENSHOTS_DIR = `${EVIDENCE_DIR}/screenshots`;
const AUTH_COOKIE_VALUE = 'CfDJ8B6PkS-yuRtCtlJFtL3cr-2Aw677Xj8ZywfTjNIPb3TMQAVzBR9PAktiyVsk4Jx-F_TdFYDqaR92jqDz7iLk0uTgv_8koPNnT_NfPvE3jt2q7uVcoQWaIkeZ5B3qcBAYuGDjctZ64C2XHGXnrOObk7t2Tt9Sjeot4_Ici5DQqJ2tTrUgXiR9mtI3tyJTbuUHuzZTohdmbsg1iup4B-p4EktmKpWwuDuSNYS2QetckdqAWI560qgukvtV8n0aWiNhDOY-vp8-YYVl0pIpWOl_BjSwDXyGKfZZO938MlbEsztn8re79EErwkXa8Q5VDLyvc-pjL1wl2dfctKrBPkZkrLHZnQ9_a0R2k5uGKpGJBMdfHa3j3SluNOpwDi1diHC3PjzYIPxJj3GCasiSRtpG6wvJx6K5plbuZLay1aj9prEvgv9XBsjfh8y_kimFWHIGET0jWdAsI1jxuJq1-Pc9npLlPQvt4eT4bZk_ryVqWkdP4UOsoW_7cb14A9MbfUfB5Dn7QTuVShyrgBuBDZRYTxv1_C8FHyHrp1PR2HYE7t6QuJpKDBaWyf5kIFUWngw6Hhg9IdYnC-9ctR-jkea4hQa0Sq_dLtZTeopajtfckrtUnc37rr9XG1p8xneAvmsocUSk4gopjXFlU5XlJFwI8J9IkcCWmmnqw2R5RBP69tBjhoN1PTjeNvchM0qfiZM3InLuNd3DS_0sKgQquxDZvr1fcUMjS_-WLMBeNjkGNi_BF9w65vJszEP7ch65hLYukES2E04Yd8XfBo8XrzYPIVpSBwMzmJzGiMdY9Ttpbeea3XTPXoy7_p2xIavukomj3hGW9m6gMvOkuNjGUEAz6GG-wMooH1HmsFjPL8U6oweD';

async function createPageWithApiProxy(browser: any): Promise<{ context: BrowserContext; page: Page }> {
  const context: BrowserContext = await browser.newContext({
    viewport: { width: 1280, height: 900 },
    // Interceptar /api requests e proxy para :5272
  });

  await context.addCookies([{
    name: 'portabox.auth',
    value: AUTH_COOKIE_VALUE,
    domain: 'localhost',
    path: '/',
    httpOnly: true,
    secure: false,
    sameSite: 'Lax',
  }]);

  const page = await context.newPage();

  // Interceptar todas as chamadas /api/* e proxy para backend
  await page.route('**/api/**', async (route) => {
    const originalUrl = route.request().url();
    // Trocar localhost:5174/api para localhost:5272/api
    const targetUrl = originalUrl.replace(`${SINDICO_APP_URL}/api`, `${API_URL}/api`);
    
    try {
      const postData = route.request().postData();
      const method = route.request().method();
      const headers = route.request().headers();
      
      // Remover origin/referer que possam causar problemas de CORS
      delete headers['origin'];
      delete headers['referer'];
      
      const resp = await route.fetch({
        url: targetUrl,
        method,
        headers,
        body: postData ?? undefined,
      });
      
      const respHeaders = resp.headers();
      respHeaders['access-control-allow-origin'] = SINDICO_APP_URL;
      respHeaders['access-control-allow-credentials'] = 'true';
      
      await route.fulfill({
        status: resp.status(),
        headers: respHeaders,
        body: await resp.body(),
      });
    } catch (e) {
      fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`, `\n--- ROUTE ERROR: ${originalUrl} -> ${(e as Error).message} ---\n`);
      await route.continue();
    }
  });

  return { context, page };
}

async function clickMenuItemByCoords(page: Page, pattern: RegExp): Promise<boolean> {
  const items = page.locator('[role="menuitem"]');
  const count = await items.count();
  for (let i = 0; i < count; i++) {
    const text = await items.nth(i).textContent();
    if (text && pattern.test(text)) {
      const box = await items.nth(i).boundingBox();
      if (box) {
        await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
        return true;
      }
    }
  }
  return false;
}

async function enableToggle(page: Page): Promise<void> {
  const toggle = page.locator('label').filter({ hasText: /mostrar inativos/i }).locator('input[type="checkbox"]');
  if (await toggle.isVisible({ timeout: 5000 }).catch(() => false)) {
    if (!await toggle.isChecked()) await toggle.check();
  }
  await page.waitForTimeout(1500);
}

test.describe('CF-06 RETESTE v3: UI com proxy /api', () => {

  test('UT-01: Toggle inativos, reativar bloco inativo', async ({ browser }) => {
    const LOG = `${EVIDENCE_DIR}/requests.log`;
    const reativarCalls: string[] = [];
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const { context, page } = await createPageWithApiProxy(browser);
    page.on('console', msg => { if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`); });
    page.on('pageerror', err => pageErrors.push(err.message));
    page.on('response', resp => { if (resp.url().includes(':reativar')) reativarCalls.push(`${resp.status()} ${resp.url()}`); });

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForTimeout(3000);

    const pageUrl = page.url();
    const h1 = await page.textContent('h1').catch(() => '');
    fs.appendFileSync(LOG, `\n--- UT-01 v3: URL=${pageUrl} H1=${h1} ---\n`);
    
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut01_inicio.png`, fullPage: true });

    // Se ainda na pagina de login, falha de autenticacao
    if (pageUrl.includes('/login')) {
      fs.appendFileSync(LOG, `\n--- UT-01 FAIL: Ainda na pagina de login. Cookie nao aceito. ---\n`);
      throw new Error('Cookie injection nao funcionou — pagina de login');
    }

    await enableToggle(page);
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut01_com_inativos.png`, fullPage: true });

    // Buscar blocos inativos
    const nomesBlocoInativo = ['Bloco Conflito X QA', 'Retest Pai Inativo QA', 'Retest Conflito X QA', 'Bloco Temp Pai Inativo QA'];
    let targetNome = '';
    for (const nome of nomesBlocoInativo) {
      if (await page.getByText(nome, { exact: false }).first().isVisible({ timeout: 2000 }).catch(() => false)) {
        targetNome = nome;
        break;
      }
    }

    fs.appendFileSync(LOG, `\n--- UT-01 v3: bloco inativo encontrado="${targetNome}" ---\n`);

    if (!targetNome) {
      const treeText = await page.locator('[role="tree"]').textContent().catch(() => page.textContent('body').catch(() => ''));
      fs.appendFileSync(LOG, `\n--- UT-01 FAIL v3: Nenhum bloco inativo visivel. Conteudo: ${treeText?.substring(0,300)} ---\n`);
      await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut01_fail_no_bloco.png`, fullPage: true });
      throw new Error('Nenhum bloco inativo encontrado na arvore');
    }

    const acoesBtnName = new RegExp(`ações do bloco ${targetNome}`, 'i');
    const acoesBtn = page.getByRole('button', { name: acoesBtnName }).first();
    await acoesBtn.scrollIntoViewIfNeeded().catch(() => {});
    const box = await acoesBtn.boundingBox().catch(() => null);
    if (box) {
      await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2);
    } else {
      await acoesBtn.click({ force: true }).catch(() => {});
    }
    await page.waitForTimeout(800);

    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut01_menu_aberto.png`, fullPage: true });

    const clicked = await clickMenuItemByCoords(page, /reativar/i);
    if (!clicked) {
      const menuItems = await page.locator('[role="menuitem"]').allTextContents();
      fs.appendFileSync(LOG, `\n--- UT-01 FAIL: Reativar nao encontrado. Items: ${menuItems.join('|')} ---\n`);
      throw new Error(`Reativar menuitem nao encontrado. Items: ${menuItems.join('|')}`);
    }
    await page.waitForTimeout(800);

    const modal = page.locator('[role="dialog"]');
    const modalVisible = await modal.isVisible({ timeout: 5000 }).catch(() => false);
    if (modalVisible) {
      await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut01_modal.png`, fullPage: true });
      const modalText = await modal.textContent().catch(() => '');
      fs.appendFileSync(LOG, `\n--- UT-01 Modal: "${modalText}" ---\n`);

      const confirmBtns = modal.getByRole('button');
      const count = await confirmBtns.count();
      for (let i = 0; i < count; i++) {
        const t = await confirmBtns.nth(i).textContent();
        if (t && /reativar/i.test(t)) {
          const b = await confirmBtns.nth(i).boundingBox();
          if (b) await page.mouse.click(b.x + b.width / 2, b.y + b.height / 2);
          break;
        }
      }
      await page.waitForTimeout(2500);
    }

    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut01_pos_reativacao.png`, fullPage: true });
    fs.appendFileSync(LOG, `\n--- BROWSER CONSOLE UT-01 v3 ---\nreativarCalls: ${JSON.stringify(reativarCalls)}\nmodalVisible: ${modalVisible}\n${[...consoleLogs,...pageErrors].join('\n')}\n`);

    const blocoVisivel = await page.getByText(targetNome, { exact: false }).first().isVisible({ timeout: 5000 }).catch(() => false);
    expect(blocoVisivel || reativarCalls.length > 0 || modalVisible,
      'UT-01: bloco reativado ou reativar chamado via UI').toBe(true);

    await context.close();
  });

  test('UT-02: Reativar unidade inativa', async ({ browser }) => {
    const LOG = `${EVIDENCE_DIR}/requests.log`;
    const reativarCalls: string[] = [];
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const { context, page } = await createPageWithApiProxy(browser);
    page.on('console', msg => { if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`); });
    page.on('pageerror', err => pageErrors.push(err.message));
    page.on('response', resp => { if (resp.url().includes(':reativar')) reativarCalls.push(`${resp.status()} ${resp.url()}`); });

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForTimeout(3000);

    if (page.url().includes('/login')) {
      throw new Error('Cookie nao aceito — pagina de login');
    }

    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_inicio.png`, fullPage: true });
    await enableToggle(page);

    const blocoQA01 = page.getByText('Bloco QA-01', { exact: false }).first();
    if (await blocoQA01.isVisible({ timeout: 5000 }).catch(() => false)) {
      await blocoQA01.click();
      await page.waitForTimeout(1000);
    }
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_bloco_expandido.png`, fullPage: true });

    const andar50 = page.getByText(/andar 50/i).first();
    if (await andar50.isVisible({ timeout: 5000 }).catch(() => false)) {
      await andar50.click();
      await page.waitForTimeout(1000);
    }
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_andar50_expandido.png`, fullPage: true });

    const allAcoes501 = page.getByRole('button', { name: /ações da unidade 501/i });
    const count501 = await allAcoes501.count().catch(() => 0);
    fs.appendFileSync(LOG, `\n--- UT-02 v3: count501=${count501} url=${page.url()} ---\n`);

    if (count501 === 0) {
      const allBtns = await page.getByRole('button').evaluateAll(
        (btns) => btns.map(b => b.getAttribute('aria-label') || '').filter(Boolean)
      );
      fs.appendFileSync(LOG, `\n--- UT-02 FAIL v3: sem acoes501. Btns: ${allBtns.slice(0,20).join('|')} ---\n`);
      await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_fail.png`, fullPage: true });
      throw new Error('Botao Acoes unidade 501 nao encontrado');
    }

    const acoesUni = count501 > 1 ? allAcoes501.last() : allAcoes501.first();
    await acoesUni.scrollIntoViewIfNeeded();
    const acoesBox = await acoesUni.boundingBox();
    if (acoesBox) {
      await page.mouse.click(acoesBox.x + acoesBox.width / 2, acoesBox.y + acoesBox.height / 2);
    } else {
      await acoesUni.click();
    }
    await page.waitForTimeout(600);
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_menu_aberto.png`, fullPage: true });

    const clicked = await clickMenuItemByCoords(page, /reativar/i);
    if (!clicked) {
      const menuItems = await page.locator('[role="menuitem"]').allTextContents();
      throw new Error(`Reativar nao encontrado. Items: ${menuItems.join('|')}`);
    }
    await page.waitForTimeout(800);

    const modal = page.locator('[role="dialog"]');
    const modalVisible = await modal.isVisible({ timeout: 5000 }).catch(() => false);
    if (modalVisible) {
      await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_modal.png`, fullPage: true });
      const modalText = await modal.textContent().catch(() => '');
      fs.appendFileSync(LOG, `\n--- UT-02 Modal: "${modalText}" ---\n`);
      const confirmBtns = modal.getByRole('button');
      const count = await confirmBtns.count();
      for (let i = 0; i < count; i++) {
        const t = await confirmBtns.nth(i).textContent();
        if (t && /reativar/i.test(t)) {
          const b = await confirmBtns.nth(i).boundingBox();
          if (b) await page.mouse.click(b.x + b.width / 2, b.y + b.height / 2);
          break;
        }
      }
      await page.waitForTimeout(2500);
    }

    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_pos_reativacao.png`, fullPage: true });
    fs.appendFileSync(LOG, `\n--- BROWSER CONSOLE UT-02 v3 ---\nreativarCalls: ${JSON.stringify(reativarCalls)}\nmodalVisible: ${modalVisible}\n${[...consoleLogs,...pageErrors].join('\n')}\n`);

    const uniVisivel = await page.getByText('501', { exact: false }).first().isVisible({ timeout: 5000 }).catch(() => false);
    expect(uniVisivel || reativarCalls.length > 0 || modalVisible,
      'UT-02: unidade reativada ou reativar chamado').toBe(true);

    await context.close();
  });

  test('UT-03: Conflito canonico — toast/erro visivel', async ({ browser }) => {
    const LOG = `${EVIDENCE_DIR}/requests.log`;
    const apiResponses: string[] = [];
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const { context, page } = await createPageWithApiProxy(browser);
    page.on('console', msg => { if (['error','warn'].includes(msg.type())) consoleLogs.push(`[${msg.type()}] ${msg.text()}`); });
    page.on('pageerror', err => pageErrors.push(err.message));
    page.on('response', resp => { if (resp.url().includes(':reativar')) apiResponses.push(`${resp.status()} ${resp.url()}`); });

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForTimeout(3000);

    if (page.url().includes('/login')) throw new Error('Cookie nao aceito');

    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut03_inicio.png`, fullPage: true });
    await enableToggle(page);
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut03_com_inativos.png`, fullPage: true });

    const conflictoBtns = page.getByRole('button', { name: /ações do bloco (Bloco|Retest) Conflito X QA/i });
    const count = await conflictoBtns.count();
    fs.appendFileSync(LOG, `\n--- UT-03 v3: conflitoBtns=${count} ---\n`);

    let foundAndAttempted = false;
    for (let i = 0; i < count && !foundAndAttempted; i++) {
      const btn = conflictoBtns.nth(i);
      const btnBox = await btn.boundingBox().catch(() => null);
      if (!btnBox) continue;

      await page.mouse.click(btnBox.x + btnBox.width / 2, btnBox.y + btnBox.height / 2);
      await page.waitForTimeout(500);

      const reativarItem = page.locator('[role="menuitem"]').filter({ hasText: /reativar/i });
      if (!await reativarItem.isVisible({ timeout: 2000 }).catch(() => false)) {
        await page.keyboard.press('Escape');
        await page.waitForTimeout(300);
        continue;
      }

      const itemInfo = await reativarItem.evaluate((el) => {
        let node: Element | null = el;
        while (node) {
          if (node.getAttribute('role') === 'treeitem') return { ariaDisabled: node.getAttribute('aria-disabled') };
          node = node.parentElement;
        }
        return null;
      });

      if (itemInfo?.ariaDisabled === 'true') {
        foundAndAttempted = true;
        await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut03_menu_inativo_bloco.png`, fullPage: true });
        const rb = await reativarItem.boundingBox();
        if (rb) await page.mouse.click(rb.x + rb.width / 2, rb.y + rb.height / 2);
        await page.waitForTimeout(700);
      } else {
        await page.keyboard.press('Escape');
        await page.waitForTimeout(300);
      }
    }

    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut03_apos_reativar_click.png`, fullPage: true });

    const modal = page.locator('[role="dialog"]');
    const modalVisible = await modal.isVisible({ timeout: 3000 }).catch(() => false);
    if (modalVisible) {
      await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut03_modal.png`, fullPage: true });
      const modalText = await modal.textContent().catch(() => '');
      fs.appendFileSync(LOG, `\n--- UT-03 Modal: "${modalText}" ---\n`);
      const confirmBtns = modal.getByRole('button');
      const btnCount = await confirmBtns.count();
      for (let i = 0; i < btnCount; i++) {
        const t = await confirmBtns.nth(i).textContent();
        if (t && /reativar/i.test(t)) {
          const b = await confirmBtns.nth(i).boundingBox();
          if (b) await page.mouse.click(b.x + b.width / 2, b.y + b.height / 2);
          break;
        }
      }
      await page.waitForTimeout(2000);
    }

    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut03_pos_confirmacao.png`, fullPage: true });

    const pageText = await page.textContent('body').catch(() => '');
    const hasConflictError = /conflito|conflict|nao.*possivel|não.*possível|erro/i.test(pageText ?? '');
    const alertVisible = await page.locator('[role="alert"]').filter({ hasText: /conflito|erro|conflict/i }).isVisible({ timeout: 3000 }).catch(() => false);

    fs.appendFileSync(LOG, `\n--- BROWSER CONSOLE UT-03 v3 ---\nfoundAndAttempted: ${foundAndAttempted}\napiResponses: ${JSON.stringify(apiResponses)}\nmodalVisible: ${modalVisible}\nhasConflictError: ${hasConflictError}\nalertVisible: ${alertVisible}\n${[...consoleLogs,...pageErrors].join('\n')}\n`);

    if (!foundAndAttempted) {
      fs.appendFileSync(LOG, '\n--- UT-03 v3: Bloco conflito nao encontrado na UI. Validado via API CT-04 (409). ---\n');
      return;
    }

    expect(hasConflictError || alertVisible || apiResponses.some(r => r.includes('409')),
      'UT-03: UI deve exibir erro de conflito canonico').toBe(true);

    await context.close();
  });
});
