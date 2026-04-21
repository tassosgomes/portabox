import { test, expect } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

const BASE_URL = 'http://localhost:5174';
const TOKEN = 'uZWYNMGiYckEXNQeOIdHmGTkDhPOYuMzlYsq3BG4nq4';
const EVIDENCE_DIR = '/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_03_magic_link_sindico';
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, 'screenshots');
const LOG_FILE = path.join(EVIDENCE_DIR, 'requests.log');

function log(msg: string) {
  const line = msg + '\n';
  fs.appendFileSync(LOG_FILE, line);
  console.log(msg);
}

test.describe('TC-03 RERUN3 — Página /password-setup via magic link', () => {

  test('TC-03: Página de definição de senha acessível via magic link', async ({ page }) => {

    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on('console', msg => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    page.on('pageerror', err => pageErrors.push(err.message));

    const timestamp = new Date().toISOString();
    log('');
    log('========================================');
    log('TC-03 RERUN3 — Página /password-setup via magic link');
    log(`Timestamp: ${timestamp}`);
    log('Token usado: ' + TOKEN.substring(0, 8) + '...[redacted]');
    log('URL: ' + BASE_URL + '/password-setup?token=[token]');
    log('========================================');

    // Passo 1: navegar para /password-setup?token=...
    const magicUrl = `${BASE_URL}/password-setup?token=${TOKEN}`;
    await page.goto(magicUrl, { waitUntil: 'networkidle' });

    // Screenshot inicial
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, 'rerun3_tc03_inicio.png'),
      fullPage: true
    });

    const urlAfterGoto = page.url();
    log(`URL após navegação: ${urlAfterGoto}`);

    // Aguardar estabilização
    await page.waitForTimeout(2000);

    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, 'rerun3_tc03_apos_wait.png'),
      fullPage: true
    });

    const urlAfterWait = page.url();
    log(`URL após 2s de espera: ${urlAfterWait}`);
    log(`Título da página: ${await page.title()}`);
    log(`Texto visível (primeiros 300 chars): ${(await page.innerText('body')).substring(0, 300)}`);

    // Assertion 1: URL não redirecionou para /login
    const wasRedirectedToLogin = urlAfterWait.includes('/login');
    log(`Redirecionou para /login: ${wasRedirectedToLogin}`);

    // Screenshot pre-assert
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, 'rerun3_tc03_pre_assert.png'),
      fullPage: true
    });

    // Assertion 2: URL contém /password-setup
    const urlContainsPasswordSetup = urlAfterWait.includes('/password-setup');
    log(`URL contém /password-setup: ${urlContainsPasswordSetup}`);

    // Verificar presença de campos de senha
    const passwordInputs = page.locator('input[type="password"]');
    const passwordInputCount = await passwordInputs.count();
    log(`Campos input[type="password"] encontrados: ${passwordInputCount}`);

    // Verificar qualquer campo de input
    const allInputs = page.locator('input');
    const allInputCount = await allInputs.count();
    log(`Total de inputs na página: ${allInputCount}`);

    // Log do conteúdo da página para diagnóstico
    const bodyText = await page.innerText('body');
    log(`Conteúdo completo da página (primeiros 500 chars):\n${bodyText.substring(0, 500)}`);

    // Screenshot final
    await page.screenshot({
      path: path.join(SCREENSHOTS_DIR, 'rerun3_tc03_final.png'),
      fullPage: true
    });

    // Log do console do browser
    log('--- BROWSER CONSOLE TC-03 RERUN3 ---');
    [...consoleLogs, ...pageErrors].forEach(l => log(l));

    // Resultado
    if (wasRedirectedToLogin) {
      log('--- RESULTADO: FAIL ---');
      log(`Expected: URL contém /password-setup, formulário de senha visível`);
      log(`Actual: redirecionado para /login — URL: ${urlAfterWait}`);
      log(`Password inputs encontrados: ${passwordInputCount}`);
    } else if (!urlContainsPasswordSetup) {
      log('--- RESULTADO: FAIL ---');
      log(`Expected: URL contém /password-setup`);
      log(`Actual: URL inesperada — ${urlAfterWait}`);
    } else if (passwordInputCount < 1) {
      log('--- RESULTADO: FAIL ---');
      log(`Expected: ao menos 1 campo input[type="password"] visível`);
      log(`Actual: ${passwordInputCount} campos de senha encontrados`);
      log(`URL final: ${urlAfterWait}`);
    } else {
      log('--- RESULTADO: PASS ---');
      log(`URL final: ${urlAfterWait}`);
      log(`Campos de senha encontrados: ${passwordInputCount}`);
    }

    // Assertions Playwright (determinam PASS/FAIL do teste)
    expect(wasRedirectedToLogin, `FAIL: redirecionou para /login. URL atual: ${urlAfterWait}`).toBe(false);
    expect(urlContainsPasswordSetup, `FAIL: URL não contém /password-setup. URL atual: ${urlAfterWait}`).toBe(true);
    await expect(passwordInputs.first(), `FAIL: nenhum campo de senha visível na página`).toBeVisible();
  });
});
