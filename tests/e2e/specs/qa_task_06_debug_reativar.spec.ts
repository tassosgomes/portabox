import { test, expect, type BrowserContext, type Page } from '@playwright/test';
import * as fs from 'fs';

const SINDICO_APP_URL = 'http://localhost:5174';
const EVIDENCE_DIR = '/home/tsgomes/log-portaria/.compozy/tasks/f02-gestao-blocos-unidades/qa-evidence/qa_task_06_reativacao';
const SCREENSHOTS_DIR = `${EVIDENCE_DIR}/screenshots`;
const LOG = `${EVIDENCE_DIR}/requests.log`;

const AUTH_COOKIE_VALUE = 'CfDJ8B6PkS-yuRtCtlJFtL3cr-2s42bIJ9t_I_LK1c9bqIuXJfpMCXOM54sFBNxsTnuHEkFbQhM2iKE1ppD-S5ZLjxquKjS5XjEu2BG-DkJxB5Lhm_AoYkDfvucylbhL8EGDOUICs6zBYebGqFvU-xZ5PqJu3hebkMqluHK0a2z_H3hsn3q4-Wca3RcCHMP0uL9cMkJxlr2ParXJmr1dQdGCDRj9eLq2NYwy4AJYakvntJKPVPsz6Lo5azTamdujYR12HsQ-lANQG0Qsd7gOrpWCYG1BTIW2CaE4P9qdNdikPOXEkCYNEk4AihsN-TiaubLrtk_SBXr7aL14Qwiis0zdYIhHyO6I5vGaBi4VNKnxoem3dEj7a9siEuuJ4a-v7JlCznFrVMkHZYBm4gPTQDRSoxarEr3xxTYp_HCcHNOcinKm7edtpz12-MUq1va_D8SkPX4H1cYsNTL-9DURMJwuQ6SbZ_2gi6ppck7tuFf4orXapgrObfmYv_PSn6rZruHBxWAl2I0WJjTrdAFxbc3Zh0L4uBCcqqjyS-580shnUWd7i-BftggYSWRK4geQA-X42sELhInDpT21vJHOgAsKDr6Ab-kGC5UFwFinajATfIaRuynskBoh-irfVdQGS9DZQpymhyWJvMqcSHQ_vcX61L0_SxiPQ5K5DfeYwJLKcC-r6KMNj31TSCkGjs9uKbxigYELr0hepoe39EJE04rIpEHP64zqF2M6L7VhkICiBJmD1D3lezAgIeDhM6peSL4jrG24iLJwXa7u2oDlb-x8yQPiSQhekUK6WB5wXhG4P6C6mbPHaBjv5Se8Th2fnlVMvoUTIp9MZ2L6cQDSjJpPupWAV5svwDwz6VldjAcle1__';

async function getAuthenticatedPage(browser: any): Promise<{ context: BrowserContext; page: Page }> {
  const context: BrowserContext = await browser.newContext();
  await context.addCookies([{
    name: 'portabox.auth', value: AUTH_COOKIE_VALUE,
    domain: 'localhost', path: '/', httpOnly: true, secure: false, sameSite: 'Lax',
  }]);
  const page = await context.newPage();
  await page.route('http://localhost:5272/api/**', async (route) => {
    try {
      const resp = await route.fetch();
      const headers = resp.headers();
      headers['access-control-allow-origin'] = SINDICO_APP_URL;
      headers['access-control-allow-credentials'] = 'true';
      await route.fulfill({ status: resp.status(), headers, body: await resp.body() });
    } catch { await route.continue(); }
  });
  await page.route('http://localhost:5174/api/**', async (route) => {
    const url = route.request().url().replace('localhost:5174', 'localhost:5272');
    try {
      const resp = await route.fetch({ url });
      const headers = resp.headers();
      headers['access-control-allow-origin'] = SINDICO_APP_URL;
      headers['access-control-allow-credentials'] = 'true';
      await route.fulfill({ status: resp.status(), headers, body: await resp.body() });
    } catch { await route.continue(); }
  });
  return { context, page };
}

test('DEBUG: Check inert attributes and full ancestor chain', async ({ browser }) => {
  const { context, page } = await getAuthenticatedPage(browser);

  await page.goto(`${SINDICO_APP_URL}/estrutura`);
  await page.waitForSelector('h1', { timeout: 15000 });
  await page.waitForTimeout(2000);

  const toggle = page.locator('label').filter({ hasText: /mostrar inativos/i }).locator('input[type="checkbox"]');
  if (await toggle.isVisible({ timeout: 3000 }).catch(() => false)) {
    await toggle.check();
    await page.waitForTimeout(1500);
  }

  const acoesBtn = page.getByRole('button', { name: /ações do bloco Bloco Temp Rename QA/i });
  await acoesBtn.waitFor({ state: 'visible', timeout: 10000 });
  await acoesBtn.click();
  await page.waitForTimeout(500);

  // Inspect full ancestor chain for inert/disabled
  const ancestorInfo = await page.locator('[role="menuitem"]').first().evaluate((el) => {
    const chain: any[] = [];
    let node: Element | null = el;
    while (node) {
      chain.push({
        tag: node.tagName,
        inert: (node as any).inert,
        disabled: (node as any).disabled,
        ariaDisabled: node.getAttribute('aria-disabled'),
        tabindex: node.getAttribute('tabindex'),
        role: node.getAttribute('role'),
        className: node.className.substring(0, 60),
      });
      node = node.parentElement;
      if (chain.length > 15) break;
    }
    return chain;
  });

  fs.appendFileSync(LOG, `\n--- DEBUG Ancestor Chain ---\n${JSON.stringify(ancestorInfo, null, 2)}\n`);
  console.log('Ancestor chain:', JSON.stringify(ancestorInfo, null, 2));

  await context.close();
});
