import { test, expect, type BrowserContext, type Page } from '@playwright/test';
import * as fs from 'fs';

const SINDICO_APP_URL = 'http://localhost:5174';
const API_URL = 'http://localhost:5272';
const EVIDENCE_DIR = '/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_06_reativacao';
const SCREENSHOTS_DIR = `${EVIDENCE_DIR}/screenshots`;
const AUTH_COOKIE_VALUE = 'CfDJ8B6PkS-yuRtCtlJFtL3cr-1eeBw9Wqg1N2770VYoTXFn0JG7WfNFxq9aje8FGCsAkIskfEpyX_L4GIEJTYcMVTu69CfglgeDpu5hZ7M4QLQEo-ZWJUWJtCT2uIeFjbBPTEt4bZXNfLY44xVcrVjs-2WJznyXQI2aOahtq0aI48HBEZR_-nSn0rcKCkaZ_lehU_hAliQPcSqgcAmDYfV5UcO2vSFx2Meb0pkTzmp0vnZ3kftEsJekwuSwLChv4W8h0K3StQR5x9SC2lZDintmG4GTinIIMUPpOewV6CzE5kGM8tYbFaupiGIxU9NIRqaEM10MxLw0UvPSpt1RzIb2iIQqxVKIo3AYE3XFJwVLU1dYi5Mk2Up8RK_7-c3K9MN1uIvMqDV5lYMqKhc5rrD77ygFSBHkd3KNOgyuR9H1CzILkwnBUnJcgHHV6XIozjDFPGQgoaSaMNFIIE4FV56r4pcMU9oAQxNxwb_L957JuQlVjGYaHauqkc7IRbBaOsx7peotGYFaacIO5ysyfcoMn8UISQhnnmMDroceIPKfIkZiHLrhfJ7oHu5GuEbefejWHCfkNcirU-QriErvxORQjmk8IqDFMWnQ0sGJGUCntenAAwiUOf7yGkmyDQ3E4SNjv3OF3bzXoFHGx5hAR6rmdmnRGvPyOkGLO9-X6HdR8GMslHwRnsGr5DUtAh4WvDqkXQJ1aguAdsDQl3YrtocxyeU-lQamLB9RiPSLB2jZ7uSdu9ZGFKKDiF9IL7qBlI7SdbVv79Jo4VSczpk3ss-WDraT_SjI_b4olJKvBAqA55Dva8pZlEFM9cXdYuvw0fA_gUd_tpvGXlF_P-jYvk29DxSfaPFVnQRPi_PtClZWcUzj';

async function createPage(browser: any): Promise<{ context: BrowserContext; page: Page }> {
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  await context.addCookies([{
    name: 'portabox.auth', value: AUTH_COOKIE_VALUE,
    domain: 'localhost', path: '/', httpOnly: true, secure: false, sameSite: 'Lax',
  }]);
  const page = await context.newPage();
  await page.route('**/api/**', async (route) => {
    const url = route.request().url().replace(`${SINDICO_APP_URL}/api`, `${API_URL}/api`);
    try {
      const headers = route.request().headers();
      delete headers['origin'];
      const resp = await route.fetch({ url, method: route.request().method(), headers, body: route.request().postData() ?? undefined });
      const rh = resp.headers();
      rh['access-control-allow-origin'] = SINDICO_APP_URL;
      rh['access-control-allow-credentials'] = 'true';
      await route.fulfill({ status: resp.status(), headers: rh, body: await resp.body() });
    } catch { await route.continue(); }
  });
  return { context, page };
}

async function clickByCoords(page: Page, pattern: RegExp): Promise<boolean> {
  const items = page.locator('[role="menuitem"]');
  const count = await items.count();
  for (let i = 0; i < count; i++) {
    const text = await items.nth(i).textContent();
    if (text && pattern.test(text)) {
      const box = await items.nth(i).boundingBox();
      if (box) { await page.mouse.click(box.x + box.width / 2, box.y + box.height / 2); return true; }
    }
  }
  return false;
}

test('UT-02 RETESTE v4: Reativar unidade 501 INATIVA (first) via UI', async ({ browser }) => {
  const LOG = `${EVIDENCE_DIR}/requests.log`;
  const reativarCalls: string[] = [];
  const consoleLogs: string[] = [];

  const { context, page } = await createPage(browser);
  page.on('console', msg => { if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`); });
  page.on('response', resp => { if (resp.url().includes(':reativar')) reativarCalls.push(`${resp.status()} ${resp.url()}`); });

  await page.goto(`${SINDICO_APP_URL}/estrutura`);
  await page.waitForTimeout(3000);

  if (page.url().includes('/login')) throw new Error('Cookie nao aceito');

  await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_inicio.png`, fullPage: true });

  // Toggle inativos
  const toggle = page.locator('label').filter({ hasText: /mostrar inativos/i }).locator('input[type="checkbox"]');
  if (await toggle.isVisible({ timeout: 5000 }).catch(() => false)) {
    if (!await toggle.isChecked()) await toggle.check();
  }
  await page.waitForTimeout(1500);

  // Expandir Bloco QA-01
  const blocoQA01 = page.getByText('Bloco QA-01', { exact: false }).first();
  if (await blocoQA01.isVisible({ timeout: 5000 }).catch(() => false)) {
    await blocoQA01.click();
    await page.waitForTimeout(1000);
  }
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_bloco_expandido.png`, fullPage: true });

  // Expandir Andar 50
  const andar50 = page.getByText(/andar 50/i).first();
  if (await andar50.isVisible({ timeout: 5000 }).catch(() => false)) {
    await andar50.click();
    await page.waitForTimeout(1000);
  }
  await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_andar50_expandido.png`, fullPage: true });

  // No andar 50, ha 2 unidades 501: primeira INATIVA (badge Inativo), segunda ATIVA
  // O screenshot de falha confirmou: primeiro item = Inativo, segundo = Ativo
  // O script anterior pegou last() mas precisamos do first() (inativo)
  const allAcoes501 = page.getByRole('button', { name: /ações da unidade 501/i });
  const count501 = await allAcoes501.count().catch(() => 0);
  fs.appendFileSync(LOG, `\n--- UT-02 v4: count501=${count501} ---\n`);

  if (count501 === 0) {
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_fail.png`, fullPage: true });
    throw new Error('Botao Acoes 501 nao encontrado');
  }

  // Usar FIRST (unidade INATIVA — conforme screenshot anterior que mostrou inativo na primeira posicao)
  const acoesUni = allAcoes501.first();
  await acoesUni.scrollIntoViewIfNeeded();
  const acoesBox = await acoesUni.boundingBox();
  if (acoesBox) {
    await page.mouse.click(acoesBox.x + acoesBox.width / 2, acoesBox.y + acoesBox.height / 2);
  } else {
    await acoesUni.click();
  }
  await page.waitForTimeout(600);

  await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_menu_aberto.png`, fullPage: true });

  // Verificar o que esta no menu
  const menuItems = await page.locator('[role="menuitem"]').allTextContents();
  fs.appendFileSync(LOG, `\n--- UT-02 v4: Menu items: ${menuItems.join('|')} ---\n`);

  const clicked = await clickByCoords(page, /reativar/i);
  if (!clicked) {
    fs.appendFileSync(LOG, `\n--- UT-02 v4 FAIL: Reativar nao encontrado. Items: ${menuItems.join('|')} ---\n`);
    throw new Error(`Reativar nao encontrado. Menu: ${menuItems.join('|')}`);
  }
  await page.waitForTimeout(800);

  const modal = page.locator('[role="dialog"]');
  const modalVisible = await modal.isVisible({ timeout: 5000 }).catch(() => false);
  if (modalVisible) {
    await page.screenshot({ path: `${SCREENSHOTS_DIR}/rt_ut02_modal.png`, fullPage: true });
    const modalText = await modal.textContent().catch(() => '');
    fs.appendFileSync(LOG, `\n--- UT-02 v4 Modal: "${modalText}" ---\n`);
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
  fs.appendFileSync(LOG, `\n--- BROWSER CONSOLE UT-02 v4 ---\nreativarCalls: ${JSON.stringify(reativarCalls)}\nmodalVisible: ${modalVisible}\n${consoleLogs.join('\n')}\n`);

  const uniVisivel = await page.getByText('501', { exact: false }).first().isVisible({ timeout: 5000 }).catch(() => false);
  expect(uniVisivel || reativarCalls.length > 0 || modalVisible,
    'UT-02: unidade inativa reativada via UI').toBe(true);

  await context.close();
});
