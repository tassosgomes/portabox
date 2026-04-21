import { test, expect, type BrowserContext, type Page } from '@playwright/test';
import * as fs from 'fs';

const SINDICO_APP_URL = 'http://localhost:5174';
const EVIDENCE_DIR = '/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_04_inativacao_unidade';

const AUTH_COOKIE_VALUE = 'CfDJ8B6PkS-yuRtCtlJFtL3cr-0VMOY6TUg01SHyF-VwNFQ-NX0QvS0cKeFGfd4M1V9tYFRm8pDCeXzDqYlNIPmpbnFOPGqjyTfsY6y3VnBlOIMkqLNhc_qHj0kmQRkVSx-W6mhdazwpIY6rpcepoYx-tRKW49KCECBjcYrIpOzm1lmIV6eLI5rhd9ibn1tsbnzbI2IfBnNHIZl5WA5zx__Ad3ChkSCEG2-c8OIV8YhKXdmpt-z7tb0Z9wioP1Chitz_9yfE8blI6inH-XuLM8SJRX-1eImP1cdiGQ_dzgQkh9jvlEbuC-ElEM0jJgFjIi5366VbvxMwguXAYHdNKxGpXDcSadaeyVvZ0jXafh92nGkC62oQpLJFhz5Kq70vX3iCYy1-z30skoxyJRseq-iFAL20-a3zsVeNoyepDGbeIdTtp83_m-ORPEMI_0YZxlBZNkvgsm-FLBeoqKKFPURBg6rDIrrDyoNDo5lFIQGBcO7duufBKGxQ1x5afqe4sEoGO3_YazBHqhV9m8aAyD3z_Qrt0gi9mm0tF0ngJ6gGrnElCs1mDujm3kOxjPuOTYho-hqe26UvGtwkBjJI3_sNwZBGqGW_nQa3J7i8jfwn11LqPwJJfK5wfqmCQaI3H6BXJtaGZfzH70tvyMV9uPNm0XUwnWqxNRgxOghVhqHX0Y2BUb41bkWGHViXjg9yaw1ANHvohD7a2d6Mh2PnAdBAvywaHmZ4f5L0ldRV-RkpkYVEuDgW_pnCJpmlcAf0m_MIm4kJbq26PuYpDvQI-bEgrtsSkTYPq6Alx8i13SCnqBEp5aXlPQcs2VLvYeqoO-ZdewOzYxepfHLxxB-QJpUWbpvgoIlXB1Wjb4KTgF-6N81i';

const TENANT_A = '4cce551d-4f18-474b-a42a-2deb6c2a0451';
const BLOCO_QA01 = '88037273-d560-4415-a1e2-b45a00dc5be4';

// Tree node IDs
const TREE_BLOCO_QA01 = `tree-item-bloco-${BLOCO_QA01}`;

async function getAuthenticatedPage(browser: any): Promise<{ context: BrowserContext; page: Page }> {
  const context: BrowserContext = await browser.newContext({
    recordVideo: { dir: `${EVIDENCE_DIR}/videos/` }
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

  // Fix CORS
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

// Helper: expand bloco QA-01 in the tree
// The tree item has id="tree-item-bloco-{blocoId}" and clicking it toggles expand
async function expandBlocoQA01(page: Page) {
  const blocoNode = page.locator(`#${TREE_BLOCO_QA01}`);
  await blocoNode.waitFor({ state: 'visible', timeout: 10000 });
  // Check if already expanded
  const ariaExpanded = await blocoNode.getAttribute('aria-expanded');
  if (ariaExpanded !== 'true') {
    await blocoNode.click();
    await page.waitForTimeout(800);
  }
}

// Helper: expand an andar node given its andar number and bloco id
async function expandAndar(page: Page, blocoId: string, andar: number) {
  const andarNodeId = `tree-item-bloco-${blocoId}-andar-${andar}`;
  const andarNode = page.locator(`#${andarNodeId}`);
  const andarVisible = await andarNode.isVisible({ timeout: 3000 }).catch(() => false);
  if (!andarVisible) return;
  const ariaExpanded = await andarNode.getAttribute('aria-expanded');
  if (ariaExpanded !== 'true') {
    await andarNode.click();
    await page.waitForTimeout(800);
  }
}

test.describe('CF-04: Inativacao de Unidade — UI Tests', () => {

  test('UT-01: Inativar unidade via modal de confirmacao — unidade some da arvore', async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    const { context, page } = await getAuthenticatedPage(browser);
    page.on('console', msg => {
      if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
    });
    page.on('pageerror', err => pageErrors.push(err.message));

    // Pre-step: criar unidade nova via API (andar=8, numero=802) para inativar na UI
    // (801 pode ter sido criada em run anterior — usar 802 para evitar conflito)
    let ut01UnidadeId = '';
    let ut01Numero = '';
    for (const numero of ['801', '802', '803', '804', '805']) {
      const createResp = await page.request.post(
        `http://localhost:5272/api/v1/condominios/${TENANT_A}/blocos/${BLOCO_QA01}/unidades`,
        {
          headers: { 'Content-Type': 'application/json' },
          data: { andar: 8, numero }
        }
      );
      if (createResp.ok()) {
        const body = await createResp.json();
        ut01UnidadeId = body.id;
        ut01Numero = body.numero;
        break;
      }
    }

    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`,
      `\n--- UT-01 PRE-STEP: Criou unidade andar=8 numero=${ut01Numero} id=${ut01UnidadeId} ---\n`
    );

    if (!ut01UnidadeId) {
      throw new Error('Nao foi possivel criar unidade para UT-01');
    }

    // Andar node id para o andar 8
    const ANDAR_8_ID = `bloco-${BLOCO_QA01}-andar-8`;

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector('[aria-label*="Ações do bloco"]', { timeout: 15000 });

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_inicio.png`, fullPage: true });

    const currentUrl = page.url();
    if (currentUrl.includes('/login')) {
      throw new Error(`Auth redirected to login: ${currentUrl}`);
    }

    // Expandir Bloco QA-01
    await expandBlocoQA01(page);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_bloco_expandido.png`, fullPage: true });

    // Expandir Andar 8
    await expandAndar(page, BLOCO_QA01, 8);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_andar8_expandido.png`, fullPage: true });

    // Verificar que a unidade esta visivel
    // Label do item = "Unidade {numero}" (ver toTreeItems.ts: label: `Unidade ${unidade.numero}`)
    const unidadeLabel = `Unidade ${ut01Numero}`;
    const unidadeItem = page.locator(`#tree-item-unidade-${ut01UnidadeId}`);
    const unidadeVisible = await unidadeItem.isVisible({ timeout: 5000 }).catch(() => false);

    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`,
      `\n--- UT-01: Unidade "${unidadeLabel}" (id=${ut01UnidadeId}) visivel: ${unidadeVisible} ---\n`
    );

    if (!unidadeVisible) {
      const bodyText = await page.locator('body').textContent().catch(() => '');
      fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`,
        `\n--- UT-01: Unidade nao visivel. Body snippet: ${bodyText?.substring(0, 600)} ---\n`
      );
      await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_fail_unidade_nao_visivel.png`, fullPage: true });
      throw new Error(`Unidade "${unidadeLabel}" (${ut01UnidadeId}) nao visivel na arvore`);
    }

    // Clicar no botao "Ações" da unidade
    // aria-label="Ações da unidade {numero}" — conforme UnidadeActionsMenu.tsx
    const acoesBtn = page.getByRole('button', { name: `Ações da unidade ${ut01Numero}` });
    await acoesBtn.waitFor({ state: 'visible', timeout: 5000 });
    await acoesBtn.click();

    await page.waitForTimeout(500);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_menu_acoes_aberto.png`, fullPage: true });

    // Clicar Inativar no menu
    const inativarMenuItem = page.getByRole('menuitem', { name: 'Inativar' });
    await inativarMenuItem.waitFor({ state: 'visible', timeout: 5000 });
    await inativarMenuItem.click();

    await page.waitForTimeout(500);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_modal_confirmacao.png`, fullPage: true });

    // Modal de confirmacao deve aparecer
    const modal = page.locator('[role="dialog"]');
    const modalVisible = await modal.isVisible({ timeout: 5000 }).catch(() => false);

    if (!modalVisible) {
      await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_fail_modal_nao_apareceu.png`, fullPage: true });
      throw new Error('Modal de confirmacao nao apareceu');
    }

    // Verificar copy sobre moradores
    // Texto: "Moradores associados permanecem vinculados; inative-os separadamente em F03 se necessario."
    const modalText = await modal.textContent().catch(() => '');
    const hasMoradoresCopy = /morador/i.test(modalText ?? '');

    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`,
      `\n--- UT-01: Modal visivel=${modalVisible}, texto: "${modalText?.substring(0, 400)}"\nHas moradores copy: ${hasMoradoresCopy} ---\n`
    );

    // Confirmar — botao "Inativar unidade"
    const confirmarBtn = modal.getByRole('button', { name: 'Inativar unidade' });
    await confirmarBtn.waitFor({ state: 'visible', timeout: 5000 });
    await confirmarBtn.click();

    await modal.waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {});
    await page.waitForTimeout(2500);

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut01_pos_inativacao.png`, fullPage: true });

    // Unidade NAO deve aparecer na arvore (sem inativos)
    const unidadeAposInativacao = await page.locator(`#tree-item-unidade-${ut01UnidadeId}`).isVisible({ timeout: 3000 }).catch(() => false);

    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`,
      `\n--- BROWSER CONSOLE UT-01 ---\n` +
      `Unidade visivel apos inativacao (esperado false): ${unidadeAposInativacao}\n` +
      `Modal menciona moradores: ${hasMoradoresCopy}\n` +
      `ID unidade: ${ut01UnidadeId}\n` +
      [...consoleLogs, ...pageErrors].join('\n') + '\n'
    );

    fs.appendFileSync(`${EVIDENCE_DIR}/created_resources.txt`,
      `\nUT-01: id=${ut01UnidadeId} andar=8 numero=${ut01Numero} status=INATIVA (inativada via UI)\n`
    );

    expect(modalVisible, 'Modal de confirmacao deve aparecer').toBe(true);
    expect(hasMoradoresCopy, 'Modal deve mencionar moradores (copy PRD CF-04)').toBe(true);
    expect(unidadeAposInativacao, `Unidade ${ut01Numero} deve sumir da arvore apos inativacao`).toBe(false);

    await context.close();
  });

  test('UT-02: Toggle "Mostrar inativos" mostra unidade inativada', async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    // CT-01 inativou a unidade 701 (d05aca01). Usaremos ela para validar toggle.
    const CT01_UNIDADE_ID = 'd05aca01-c7b2-41b0-8b1b-c2d3b7e45eee';
    const CT01_ANDAR = 7;

    const { context, page } = await getAuthenticatedPage(browser);
    page.on('console', msg => {
      if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
    });
    page.on('pageerror', err => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector('[aria-label*="Ações do bloco"]', { timeout: 15000 });

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut02_inicio.png`, fullPage: true });

    const currentUrl = page.url();
    if (currentUrl.includes('/login')) {
      throw new Error(`Auth redirected to login: ${currentUrl}`);
    }

    // Expandir bloco QA-01 e andar 7 para confirmar que unidade 701 nao aparece sem toggle
    await expandBlocoQA01(page);
    await expandAndar(page, BLOCO_QA01, CT01_ANDAR);

    const unidade701AntesToogle = await page.locator(`#tree-item-unidade-${CT01_UNIDADE_ID}`).isVisible({ timeout: 3000 }).catch(() => false);

    // Andar 7 pode nao aparecer se nao tem unidades ativas — verificar
    const andar7VisibleAntes = await page.locator(`#tree-item-bloco-${BLOCO_QA01}-andar-${CT01_ANDAR}`).isVisible({ timeout: 3000 }).catch(() => false);

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut02_antes_toggle.png`, fullPage: true });

    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`,
      `\n--- UT-02: Andar 7 visivel antes toggle: ${andar7VisibleAntes}\nUnidade 701 visivel antes toggle (esperado false): ${unidade701AntesToogle} ---\n`
    );

    // Clicar no toggle "Mostrar inativos" — é um <label><input type="checkbox"><span>Mostrar inativos</span></label>
    // O span "Mostrar inativos" está dentro de um label que é clicável
    const toggleSpan = page.getByText('Mostrar inativos', { exact: true });
    await toggleSpan.waitFor({ state: 'visible', timeout: 5000 });
    await toggleSpan.click();
    await page.waitForTimeout(2000);

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut02_apos_toggle.png`, fullPage: true });

    // Re-expandir bloco e andar (arvore pode ter colapsado ao recarregar dados)
    await expandBlocoQA01(page);
    await expandAndar(page, BLOCO_QA01, CT01_ANDAR);
    await page.waitForTimeout(500);

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut02_arvore_com_inativos.png`, fullPage: true });

    // Agora unidade 701 deve aparecer
    const unidade701AposToggle = await page.locator(`#tree-item-unidade-${CT01_UNIDADE_ID}`).isVisible({ timeout: 5000 }).catch(() => false);

    // Verificar que o andar 7 aparece agora (mesmo que antes nao estivesse por nao ter ativas)
    const andar7VisibleApos = await page.locator(`#tree-item-bloco-${BLOCO_QA01}-andar-${CT01_ANDAR}`).isVisible({ timeout: 3000 }).catch(() => false);

    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`,
      `\n--- BROWSER CONSOLE UT-02 ---\n` +
      `Andar 7 visivel apos toggle: ${andar7VisibleApos}\n` +
      `Unidade 701 (${CT01_UNIDADE_ID}) visivel apos toggle (esperado true): ${unidade701AposToggle}\n` +
      [...consoleLogs, ...pageErrors].join('\n') + '\n'
    );

    expect(unidade701AposToggle, 'Unidade 701 (inativa) deve aparecer com toggle Mostrar inativos ativo').toBe(true);

    await context.close();
  });

  test('UT-03: Cancelar modal — unidade continua ativa', async ({ browser }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];

    // Usar unidade 101 (f2a0b7cc) em andar 1 do Bloco QA-01 — ativa
    const UNIDADE_101_ID = 'f2a0b7cc-13d3-4c18-a36e-b5ba9fcfce33';
    const ANDAR_1 = 1;

    const { context, page } = await getAuthenticatedPage(browser);
    page.on('console', msg => {
      if (msg.type() === 'error') consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
    });
    page.on('pageerror', err => pageErrors.push(err.message));

    await page.goto(`${SINDICO_APP_URL}/estrutura`);
    await page.waitForSelector('[aria-label*="Ações do bloco"]', { timeout: 15000 });

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_inicio.png`, fullPage: true });

    const currentUrl = page.url();
    if (currentUrl.includes('/login')) {
      throw new Error(`Auth redirected to login: ${currentUrl}`);
    }

    // Expandir Bloco QA-01 > Andar 1
    await expandBlocoQA01(page);
    await expandAndar(page, BLOCO_QA01, ANDAR_1);

    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_bloco_expandido.png`, fullPage: true });

    // Verificar que unidade 101 esta visivel
    const unidade101Antes = await page.locator(`#tree-item-unidade-${UNIDADE_101_ID}`).isVisible({ timeout: 5000 }).catch(() => false);

    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`,
      `\n--- UT-03: Unidade 101 visivel antes: ${unidade101Antes} ---\n`
    );

    if (!unidade101Antes) {
      const bodyText = await page.locator('body').textContent().catch(() => '');
      fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`,
        `\n--- UT-03 FAIL: Unidade 101 nao visivel. Body: ${bodyText?.substring(0, 400)} ---\n`
      );
      await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_fail_unidade_101_nao_visivel.png`, fullPage: true });
      throw new Error(`Unidade 101 nao visivel antes do cancelamento`);
    }

    // Clicar em Acoes da unidade 101
    const acoesBtn = page.locator(`#tree-item-unidade-${UNIDADE_101_ID}`).getByRole('button', { name: 'Ações da unidade 101', exact: true });
    await acoesBtn.waitFor({ state: 'visible', timeout: 5000 });
    await acoesBtn.click();

    await page.waitForTimeout(500);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_menu_acoes.png`, fullPage: true });

    const inativarMenuItem = page.getByRole('menuitem', { name: 'Inativar' });
    await inativarMenuItem.waitFor({ state: 'visible', timeout: 5000 });
    await inativarMenuItem.click();

    await page.waitForTimeout(500);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_modal_confirmacao.png`, fullPage: true });

    const modal = page.locator('[role="dialog"]');
    const modalVisible = await modal.isVisible({ timeout: 5000 }).catch(() => false);

    if (!modalVisible) {
      throw new Error('Modal de confirmacao nao apareceu');
    }

    // Clicar Cancelar
    const cancelarBtn = modal.getByRole('button', { name: 'Cancelar' });
    await cancelarBtn.waitFor({ state: 'visible', timeout: 5000 });
    await cancelarBtn.click();

    await page.waitForTimeout(1500);
    await page.screenshot({ path: `${EVIDENCE_DIR}/screenshots/ut03_pos_cancelar.png`, fullPage: true });

    // Modal deve fechar
    const modalAposCancelar = await modal.isVisible({ timeout: 3000 }).catch(() => false);

    // Unidade 101 deve continuar visivel
    const unidade101Apos = await page.locator(`#tree-item-unidade-${UNIDADE_101_ID}`).isVisible({ timeout: 5000 }).catch(() => false);

    fs.appendFileSync(`${EVIDENCE_DIR}/requests.log`,
      `\n--- BROWSER CONSOLE UT-03 ---\n` +
      `Unidade 101 antes: ${unidade101Antes}, apos cancelar: ${unidade101Apos}\n` +
      `Modal apos cancelar (esperado false): ${modalAposCancelar}\n` +
      [...consoleLogs, ...pageErrors].join('\n') + '\n'
    );

    expect(modalVisible, 'Modal de confirmacao deve aparecer ao clicar Inativar').toBe(true);
    expect(modalAposCancelar, 'Modal deve fechar apos clicar Cancelar').toBe(false);
    expect(unidade101Apos, 'Unidade 101 deve continuar visivel apos cancelar').toBe(true);

    await context.close();
  });
});
