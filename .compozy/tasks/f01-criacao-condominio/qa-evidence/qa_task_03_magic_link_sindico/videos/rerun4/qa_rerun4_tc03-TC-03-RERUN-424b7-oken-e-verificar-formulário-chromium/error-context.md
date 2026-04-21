# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: qa_rerun4_tc03.spec.ts >> TC-03 RERUN4 — Página de definição de senha acessível via magic link >> TC-03: Navegar para /password-setup?token=<token> e verificar formulário
- Location: specs/qa_rerun4_tc03.spec.ts:17:3

# Error details

```
Error: FAIL — Assertion 2: Nenhum input[type=password] encontrado. Total inputs: 0. Body vazio: true
```

# Test source

```ts
  17  |   test("TC-03: Navegar para /password-setup?token=<token> e verificar formulário", async ({
  18  |     page,
  19  |   }) => {
  20  |     const consoleLogs: string[] = [];
  21  |     const pageErrors: string[] = [];
  22  | 
  23  |     page.on("console", (msg) =>
  24  |       consoleLogs.push(`[${msg.type()}] ${msg.text()}`)
  25  |     );
  26  |     page.on("pageerror", (err) => pageErrors.push(err.message));
  27  | 
  28  |     const url = `${BASE_URL}/password-setup?token=${TOKEN}`;
  29  | 
  30  |     appendLog("========================================");
  31  |     appendLog("TC-03 RERUN4: Página de definição de senha via magic link");
  32  |     appendLog(`Timestamp: ${new Date().toISOString()}`);
  33  |     appendLog("========================================");
  34  |     appendLog(`--- NAVEGAÇÃO ---`);
  35  |     appendLog(`URL: ${url}`);
  36  |     appendLog(`Token: [REDACTED — comprimento ${TOKEN.length}]`);
  37  |     appendLog(`Token fonte: Mailpit — e-mail mais recente 2026-04-20T17:05:56Z`);
  38  |     appendLog(`Banco: consumed_at=NULL, invalidated_at=NULL — token válido`);
  39  | 
  40  |     // Navegar para a URL do magic link
  41  |     await page.goto(url, { waitUntil: "domcontentloaded" });
  42  | 
  43  |     // Screenshot imediato após navegação
  44  |     await page.screenshot({
  45  |       path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_inicio.png"),
  46  |       fullPage: true,
  47  |     });
  48  |     appendLog(`Screenshot: rerun4_tc03_inicio.png`);
  49  | 
  50  |     // Aguardar estabilização (networkidle ou 3s)
  51  |     await page.waitForTimeout(3000);
  52  | 
  53  |     // Screenshot após espera
  54  |     await page.screenshot({
  55  |       path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_apos_wait.png"),
  56  |       fullPage: true,
  57  |     });
  58  |     appendLog(`Screenshot: rerun4_tc03_apos_wait.png`);
  59  | 
  60  |     // Capturar estado atual
  61  |     const currentUrl = page.url();
  62  |     const pageTitle = await page.title();
  63  |     const bodyText = await page.locator("body").innerText().catch(() => "");
  64  |     const allInputs = await page.locator("input").count();
  65  |     const passwordInputs = await page
  66  |       .locator('input[type="password"]')
  67  |       .count();
  68  | 
  69  |     appendLog(`--- ESTADO DA PÁGINA ---`);
  70  |     appendLog(`URL final: ${currentUrl}`);
  71  |     appendLog(`Título: ${pageTitle}`);
  72  |     appendLog(`Body text length: ${bodyText.length}`);
  73  |     appendLog(`Total inputs: ${allInputs}`);
  74  |     appendLog(`Inputs type=password: ${passwordInputs}`);
  75  | 
  76  |     // Screenshot pré-assertion
  77  |     await page.screenshot({
  78  |       path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_pre_assert.png"),
  79  |       fullPage: true,
  80  |     });
  81  |     appendLog(`Screenshot: rerun4_tc03_pre_assert.png`);
  82  | 
  83  |     // --- ASSERTION 1: Não redirecionou para /login ---
  84  |     const redirectedToLogin =
  85  |       currentUrl.includes("/login") || currentUrl.includes("/signin");
  86  |     appendLog(`--- ASSERTION 1: URL não é /login ---`);
  87  |     appendLog(`  URL atual: ${currentUrl}`);
  88  |     appendLog(`  Redirecionou para /login: ${redirectedToLogin}`);
  89  | 
  90  |     if (redirectedToLogin) {
  91  |       appendLog(`  RESULTADO: FAIL — redirecionamento para /login detectado`);
  92  |       await page.screenshot({
  93  |         path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_fail_redirect.png"),
  94  |         fullPage: true,
  95  |       });
  96  |       appendLog(`--- BROWSER CONSOLE TC-03 RERUN4 ---`);
  97  |       appendLog([...consoleLogs, ...pageErrors].join("\n"));
  98  |       throw new Error(
  99  |         `FAIL — Assertion 1: URL redirecionou para /login. URL atual: ${currentUrl}`
  100 |       );
  101 |     }
  102 |     appendLog(`  RESULTADO: PASS — URL mantida em /password-setup`);
  103 | 
  104 |     // --- ASSERTION 2: Formulário visível (ao menos 1 input[type=password]) ---
  105 |     appendLog(`--- ASSERTION 2: Formulário com input[type=password] visível ---`);
  106 |     appendLog(`  Inputs password encontrados: ${passwordInputs}`);
  107 | 
  108 |     if (passwordInputs === 0) {
  109 |       appendLog(`  RESULTADO: FAIL — nenhum input[type=password] encontrado`);
  110 |       appendLog(`  Body text (primeiros 500 chars): ${bodyText.substring(0, 500)}`);
  111 |       await page.screenshot({
  112 |         path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_fail_no_form.png"),
  113 |         fullPage: true,
  114 |       });
  115 |       appendLog(`--- BROWSER CONSOLE TC-03 RERUN4 ---`);
  116 |       appendLog([...consoleLogs, ...pageErrors].join("\n"));
> 117 |       throw new Error(
      |             ^ Error: FAIL — Assertion 2: Nenhum input[type=password] encontrado. Total inputs: 0. Body vazio: true
  118 |         `FAIL — Assertion 2: Nenhum input[type=password] encontrado. Total inputs: ${allInputs}. Body vazio: ${bodyText.length === 0}`
  119 |       );
  120 |     }
  121 |     appendLog(`  RESULTADO: PASS — ${passwordInputs} input(s) password encontrado(s)`);
  122 | 
  123 |     // Screenshot final (PASS)
  124 |     await page.screenshot({
  125 |       path: path.join(SCREENSHOTS_DIR, "rerun4_tc03_pass.png"),
  126 |       fullPage: true,
  127 |     });
  128 |     appendLog(`Screenshot: rerun4_tc03_pass.png`);
  129 | 
  130 |     appendLog(`--- RESULTADO GERAL: PASS ---`);
  131 |     appendLog(`  URL: ${currentUrl} (não redirecionou para /login)`);
  132 |     appendLog(`  Formulário: ${passwordInputs} input(s) password visíveis`);
  133 | 
  134 |     // Log do console ao final
  135 |     appendLog(`--- BROWSER CONSOLE TC-03 RERUN4 ---`);
  136 |     if (consoleLogs.length > 0 || pageErrors.length > 0) {
  137 |       appendLog([...consoleLogs, ...pageErrors].join("\n"));
  138 |     } else {
  139 |       appendLog("(sem logs)");
  140 |     }
  141 | 
  142 |     // Assertions formais Playwright
  143 |     expect(redirectedToLogin).toBe(false);
  144 |     expect(passwordInputs).toBeGreaterThan(0);
  145 |   });
  146 | });
  147 | 
```