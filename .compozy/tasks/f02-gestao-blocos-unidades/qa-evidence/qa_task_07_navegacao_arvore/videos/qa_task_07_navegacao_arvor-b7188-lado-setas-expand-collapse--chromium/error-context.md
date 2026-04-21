# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: qa_task_07_navegacao_arvore.spec.ts >> CF-07: Navegacao em Arvore Hierarquica — UI >> UT-03: Navegacao por teclado (setas expand/collapse)
- Location: specs/qa_task_07_navegacao_arvore.spec.ts:201:3

# Error details

```
Error: PRD keyboard navigation: Keyboard NOT responding. ArrowRight: true -> true (no change)

expect(received).toBe(expected) // Object.is equality

Expected: true
Received: false
```

# Test source

```ts
  179 |         detail = `aria-expanded did NOT change after click: before=${expandedBefore}, after=${expandedAfter}`;
  180 |       }
  181 |     } else {
  182 |       detail = `No aria-expanded buttons found. Bloco QA-01 visible: ${await page.getByText("Bloco QA-01").isVisible().catch(() => false)}`;
  183 |     }
  184 | 
  185 |     appendLog(`Result: ut02Pass=${ut02Pass}, detail=${detail}`);
  186 | 
  187 |     if (consoleLogs.length > 0 || pageErrors.length > 0) {
  188 |       appendLog(`--- BROWSER CONSOLE UT-02 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
  189 |     }
  190 | 
  191 |     if (ut02Pass) {
  192 |       appendLog("--- RESULTADO: PASS ---\n");
  193 |     } else {
  194 |       appendLog(`--- RESULTADO: FAIL ---\nDetail: ${detail}\n`);
  195 |     }
  196 | 
  197 |     await context.close();
  198 |     expect(ut02Pass, `Expand/collapse via click: ${detail}`).toBe(true);
  199 |   });
  200 | 
  201 |   test("UT-03: Navegacao por teclado (setas expand/collapse)", async ({ browser }) => {
  202 |     const consoleLogs: string[] = [];
  203 |     const pageErrors: string[] = [];
  204 | 
  205 |     const cookieA = readCookieFromFile(`${EVIDENCE_DIR}/cookies_sindico_a.txt`);
  206 |     const { context, page } = await getAuthenticatedPage(browser, cookieA);
  207 |     page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
  208 |     page.on("pageerror", (err) => pageErrors.push(err.message));
  209 | 
  210 |     await page.goto(`${SINDICO_APP_URL}/estrutura`);
  211 |     await page.waitForSelector("h1", { timeout: 15000 });
  212 |     await page.waitForTimeout(3000);
  213 |     await page.screenshot({ path: path.join(SS_DIR, "ut03_inicial.png"), fullPage: true });
  214 | 
  215 |     appendLog(`
  216 | ========================================
  217 | UT-03: Navegacao por teclado
  218 | Timestamp: ${new Date().toISOString()}
  219 | ========================================
  220 | PRD: "Acessibilidade: arvore navegavel por teclado (setas para expandir/colapsar)"
  221 | URL: ${page.url()}
  222 | `);
  223 | 
  224 |     const treeItems = page.locator('[role="treeitem"]');
  225 |     const treeItemCount = await treeItems.count();
  226 |     const treeExists = await page.locator('[role="tree"]').count() > 0;
  227 | 
  228 |     appendLog(`role="tree" found: ${treeExists}`);
  229 |     appendLog(`role="treeitem" count: ${treeItemCount}`);
  230 | 
  231 |     let keyboardWorking = false;
  232 |     let detail = "";
  233 | 
  234 |     if (treeItemCount > 0) {
  235 |       const firstItem = treeItems.first();
  236 |       await firstItem.focus();
  237 |       await page.waitForTimeout(200);
  238 | 
  239 |       const expandedBefore = await firstItem.getAttribute("aria-expanded");
  240 |       appendLog(`First treeitem aria-expanded before ArrowRight: ${expandedBefore}`);
  241 | 
  242 |       await page.keyboard.press("ArrowRight");
  243 |       await page.waitForTimeout(300);
  244 |       const expandedAfterRight = await firstItem.getAttribute("aria-expanded");
  245 |       appendLog(`After ArrowRight: ${expandedAfterRight}`);
  246 | 
  247 |       await page.screenshot({ path: path.join(SS_DIR, "ut03_apos_arrow_right.png"), fullPage: true });
  248 | 
  249 |       await page.keyboard.press("ArrowLeft");
  250 |       await page.waitForTimeout(300);
  251 |       const expandedAfterLeft = await firstItem.getAttribute("aria-expanded");
  252 |       appendLog(`After ArrowLeft: ${expandedAfterLeft}`);
  253 | 
  254 |       await page.screenshot({ path: path.join(SS_DIR, "ut03_apos_arrow_left.png"), fullPage: true });
  255 | 
  256 |       if (expandedAfterRight !== expandedBefore || expandedAfterLeft !== expandedAfterRight) {
  257 |         keyboardWorking = true;
  258 |         detail = `ArrowRight: ${expandedBefore} -> ${expandedAfterRight}; ArrowLeft: ${expandedAfterRight} -> ${expandedAfterLeft}`;
  259 |       } else {
  260 |         detail = `Keyboard NOT responding. ArrowRight: ${expandedBefore} -> ${expandedAfterRight} (no change)`;
  261 |       }
  262 |     } else {
  263 |       detail = `No role="treeitem" found (tree=${treeExists}). URL: ${page.url()}`;
  264 |     }
  265 | 
  266 |     appendLog(`Keyboard nav: working=${keyboardWorking}, detail: ${detail}`);
  267 | 
  268 |     if (consoleLogs.length > 0 || pageErrors.length > 0) {
  269 |       appendLog(`--- BROWSER CONSOLE UT-03 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
  270 |     }
  271 | 
  272 |     if (keyboardWorking) {
  273 |       appendLog("--- RESULTADO: PASS ---\n");
  274 |     } else {
  275 |       appendLog(`--- RESULTADO: FAIL ---\nPRD keyboard navigation requirement not met\nDetail: ${detail}\n`);
  276 |     }
  277 | 
  278 |     await context.close();
> 279 |     expect(keyboardWorking, `PRD keyboard navigation: ${detail}`).toBe(true);
      |                                                                   ^ Error: PRD keyboard navigation: Keyboard NOT responding. ArrowRight: true -> true (no change)
  280 |   });
  281 | 
  282 |   test("UT-04: Toggle filtro incluir inativos", async ({ browser }) => {
  283 |     const consoleLogs: string[] = [];
  284 |     const pageErrors: string[] = [];
  285 | 
  286 |     const cookieA = readCookieFromFile(`${EVIDENCE_DIR}/cookies_sindico_a.txt`);
  287 |     const { context, page } = await getAuthenticatedPage(browser, cookieA);
  288 |     page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
  289 |     page.on("pageerror", (err) => pageErrors.push(err.message));
  290 | 
  291 |     await page.goto(`${SINDICO_APP_URL}/estrutura`);
  292 |     await page.waitForSelector("h1", { timeout: 15000 });
  293 |     await page.waitForTimeout(3000);
  294 |     await page.screenshot({ path: path.join(SS_DIR, "ut04_inicial.png"), fullPage: true });
  295 | 
  296 |     appendLog(`
  297 | ========================================
  298 | UT-04: Toggle filtro incluir inativos
  299 | Timestamp: ${new Date().toISOString()}
  300 | ========================================
  301 | URL: ${page.url()}
  302 | `);
  303 | 
  304 |     const toggleLabel = page.getByText("Mostrar inativos");
  305 |     const toggleExists = await toggleLabel.isVisible().catch(() => false);
  306 |     appendLog(`Toggle "Mostrar inativos" visible: ${toggleExists}`);
  307 | 
  308 |     if (!toggleExists) {
  309 |       if (consoleLogs.length > 0 || pageErrors.length > 0) {
  310 |         appendLog(`--- BROWSER CONSOLE UT-04 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
  311 |       }
  312 |       appendLog(`--- RESULTADO: FAIL ---\nToggle not found. URL: ${page.url()}\n`);
  313 |       await context.close();
  314 |       expect(toggleExists, "Toggle 'Mostrar inativos' must be visible").toBe(true);
  315 |       return;
  316 |     }
  317 | 
  318 |     const inativoText1 = await page.getByText("Bloco Temp Pai Inativo QA").isVisible().catch(() => false);
  319 |     appendLog(`Bloco Temp Pai Inativo QA visible before toggle: ${inativoText1}`);
  320 | 
  321 |     const checkbox = page.locator('label').filter({ hasText: "Mostrar inativos" }).locator('input[type="checkbox"]');
  322 |     const checkboxCount = await checkbox.count();
  323 |     const cb = checkboxCount > 0 ? checkbox : page.locator('input[type="checkbox"]').first();
  324 | 
  325 |     await cb.check();
  326 |     await page.waitForTimeout(2000);
  327 |     await page.screenshot({ path: path.join(SS_DIR, "ut04_inativos_ligados.png"), fullPage: true });
  328 | 
  329 |     const inativoText2 = await page.getByText("Bloco Temp Pai Inativo QA").isVisible().catch(() => false);
  330 |     appendLog(`Bloco Temp Pai Inativo QA visible after toggle ON: ${inativoText2}`);
  331 | 
  332 |     await cb.uncheck();
  333 |     await page.waitForTimeout(2000);
  334 |     await page.screenshot({ path: path.join(SS_DIR, "ut04_inativos_desligados.png"), fullPage: true });
  335 | 
  336 |     const inativoText3 = await page.getByText("Bloco Temp Pai Inativo QA").isVisible().catch(() => false);
  337 |     appendLog(`Bloco Temp Pai Inativo QA visible after toggle OFF: ${inativoText3}`);
  338 | 
  339 |     if (consoleLogs.length > 0 || pageErrors.length > 0) {
  340 |       appendLog(`--- BROWSER CONSOLE UT-04 ---\n${[...consoleLogs, ...pageErrors].join("\n")}`);
  341 |     }
  342 | 
  343 |     const passCondition = !inativoText1 && inativoText2 && !inativoText3;
  344 |     appendLog(`Toggle: before=${inativoText1}, on=${inativoText2}, off=${inativoText3}`);
  345 | 
  346 |     if (passCondition) {
  347 |       appendLog("--- RESULTADO: PASS ---\n");
  348 |     } else {
  349 |       appendLog(`--- RESULTADO: FAIL ---\nbefore=${inativoText1}, on=${inativoText2}, off=${inativoText3}\n`);
  350 |     }
  351 | 
  352 |     await context.close();
  353 |     expect(passCondition, `Toggle: before=${inativoText1}, on=${inativoText2}, off=${inativoText3}`).toBe(true);
  354 |   });
  355 | 
  356 |   test("UT-05: Clicar num bloco abre painel/toolbar lateral com detalhes", async ({ browser }) => {
  357 |     const consoleLogs: string[] = [];
  358 |     const pageErrors: string[] = [];
  359 | 
  360 |     const cookieA = readCookieFromFile(`${EVIDENCE_DIR}/cookies_sindico_a.txt`);
  361 |     const { context, page } = await getAuthenticatedPage(browser, cookieA);
  362 |     page.on("console", (msg) => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
  363 |     page.on("pageerror", (err) => pageErrors.push(err.message));
  364 | 
  365 |     await page.goto(`${SINDICO_APP_URL}/estrutura`);
  366 |     await page.waitForSelector("h1", { timeout: 15000 });
  367 |     await page.waitForTimeout(3000);
  368 |     await page.screenshot({ path: path.join(SS_DIR, "ut05_inicial.png"), fullPage: true });
  369 | 
  370 |     appendLog(`
  371 | ========================================
  372 | UT-05: Painel lateral ao clicar num bloco
  373 | Timestamp: ${new Date().toISOString()}
  374 | ========================================
  375 | PRD: "painel lateral com detalhes e acoes contextuais"
  376 | URL: ${page.url()}
  377 | `);
  378 | 
  379 |     const blocoQA01 = page.getByText("Bloco QA-01").first();
```