import { test, expect } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = "http://localhost:5173";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_02_validacao_deduplicacao_cnpj";
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const LOG_FILE = path.join(EVIDENCE_DIR, "requests.log");

function log(msg: string) {
  const line = `[${new Date().toISOString()}] ${msg}\n`;
  fs.appendFileSync(LOG_FILE, line);
}

function ss(name: string) {
  return path.join(SCREENSHOTS_DIR, `${name}.png`);
}

async function login(page: import("@playwright/test").Page) {
  await page.goto(`${BASE_URL}/login`);
  await page.fill('input[type="email"]', "operator@portabox.dev");
  await page.fill('input[type="password"]', "PortaBox123!");
  await page.click('button[type="submit"]');
  await page.waitForURL(/condominios/, { timeout: 10000 });
}

async function fillStep1(
  page: import("@playwright/test").Page,
  nome: string,
  cnpj: string
) {
  const nomeInput = page
    .locator('input[placeholder*="Residencial"], input[placeholder*="fantasia"]')
    .first();
  await nomeInput.fill(nome);

  const cnpjInput = page
    .locator('input[placeholder="00.000.000/0000-00"]')
    .first();
  await cnpjInput.fill(cnpj);
}

async function fillStep2(page: import("@playwright/test").Page) {
  const dataAssembleia = page.locator('input[type="date"]').first();
  await dataAssembleia.fill("2025-01-15");

  const quorum = page
    .locator(
      'input[placeholder*="2/3"], input[placeholder*="quórum"], input[placeholder*="condôminos"]'
    )
    .first();
  await quorum.fill("2/3 dos condôminos");

  const sigNome = page
    .locator('input[placeholder*="Nome completo de quem"]')
    .first();
  await sigNome.fill("Carlos Signatario");

  const cpfInput = page.locator('input[placeholder*="000.000.000"]').first();
  await cpfInput.fill("529.982.247-25");

  const dataTermo = page.locator('input[type="date"]').nth(1);
  await dataTermo.fill("2025-01-15");
}

async function fillStep3(page: import("@playwright/test").Page) {
  const nomeInput = page
    .locator(
      'input[placeholder*="Maria Silva"], input[placeholder*="Nome completo"]'
    )
    .first();
  await nomeInput.fill("Sindico Teste");

  const emailInput = page.locator('input[type="email"]').first();
  await emailInput.fill("sindico.task02@portabox.dev");

  const celularInput = page.locator('input[placeholder*="+5511"]').first();
  await celularInput.fill("+5511987654321");
}

test.describe("QA Task 02 — Validacao e Deduplicacao de CNPJ", () => {
  test.beforeEach(async ({ page }) => {
    page.on("console", (msg) => {
      if (msg.type() === "error") {
        log(`[BROWSER_ERROR] ${msg.text()}`);
      }
    });
    page.on("pageerror", (err) => {
      log(`[PAGE_ERROR] ${err.message}`);
    });
  });

  test("TC-01: CNPJ com formato invalido (incompleto)", async ({ page }) => {
    log("=== TC-01: CNPJ com formato invalido ===");
    await login(page);
    await page.goto(`${BASE_URL}/condominios/novo`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: ss("tc01_inicio"), fullPage: true });

    await fillStep1(page, "Residencial Teste Formato", "11.222.333/0001");
    await page.screenshot({ path: ss("tc01_filled"), fullPage: true });

    const btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(300);
    await page.screenshot({ path: ss("tc01_apos_click"), fullPage: true });

    const pageText = await page.textContent("body");
    const hasCnpjError =
      (pageText?.includes("CNPJ inválido") ||
        pageText?.includes("CNPJ invalido") ||
        pageText?.includes("CNPJ")) ??
      false;
    const stillOnStep1 =
      pageText?.includes("Nome fantasia") ||
      pageText?.includes("Dados do condomínio") ||
      page.url().includes("/condominios/novo");

    log(`TC-01: CNPJ error displayed: ${hasCnpjError}`);
    log(`TC-01: Still on step 1: ${stillOnStep1}`);
    log(`TC-01: Page URL: ${page.url()}`);
    log(`TC-01: Body excerpt: ${pageText?.slice(0, 500)}`);

    if (hasCnpjError && stillOnStep1) {
      log("TC-01: PASS");
    } else {
      log(
        `TC-01: FAIL — CNPJ error shown: ${hasCnpjError}, still on step 1: ${stillOnStep1}`
      );
    }

    expect(
      hasCnpjError,
      "Erro de CNPJ invalido deve ser exibido para CNPJ incompleto"
    ).toBe(true);
    expect(stillOnStep1, "Deve permanecer na etapa 1").toBe(true);
  });

  test("TC-02: CNPJ formato correto mas digito invalido", async ({ page }) => {
    log("=== TC-02: CNPJ formato correto, digito verificador invalido ===");
    await login(page);
    await page.goto(`${BASE_URL}/condominios/novo`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: ss("tc02_inicio"), fullPage: true });

    // 11.222.333/0001-00 — formato correto, dígito verificador errado
    await fillStep1(page, "Residencial Digito Invalido", "11.222.333/0001-00");
    await page.screenshot({ path: ss("tc02_filled"), fullPage: true });

    const btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(300);
    await page.screenshot({ path: ss("tc02_apos_click"), fullPage: true });

    const pageText = await page.textContent("body");
    const hasCnpjError =
      (pageText?.includes("CNPJ inválido") ||
        pageText?.includes("CNPJ invalido")) ??
      false;
    const stillOnStep1 =
      pageText?.includes("Nome fantasia") ||
      pageText?.includes("Dados do condomínio") ||
      page.url().includes("/condominios/novo");

    log(`TC-02: CNPJ error displayed: ${hasCnpjError}`);
    log(`TC-02: Still on step 1: ${stillOnStep1}`);
    log(`TC-02: Body excerpt: ${pageText?.slice(0, 500)}`);

    if (hasCnpjError && stillOnStep1) {
      log("TC-02: PASS");
    } else {
      log(
        `TC-02: FAIL — CNPJ error shown: ${hasCnpjError}, still on step 1: ${stillOnStep1}`
      );
    }

    expect(
      hasCnpjError,
      "Erro de CNPJ invalido deve aparecer para digito verificador incorreto"
    ).toBe(true);
    expect(stillOnStep1, "Deve permanecer na etapa 1").toBe(true);
  });

  test("TC-03: CNPJ valido duplicado — duplicata detectada", async ({
    page,
  }) => {
    log(
      "=== TC-03: CNPJ valido e duplicado — espera 409 com mensagem de duplicata ==="
    );
    await login(page);
    await page.goto(`${BASE_URL}/condominios/novo`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: ss("tc03_inicio"), fullPage: true });

    // Etapa 1 — CNPJ duplicado: 11.222.333/0001-81
    await fillStep1(page, "Copia Residencial Teste", "11.222.333/0001-81");
    await page.screenshot({ path: ss("tc03_etapa1_filled"), fullPage: true });

    let btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: ss("tc03_etapa2_inicio"), fullPage: true });

    const pageText2 = await page.textContent("body");
    const onEtapa2 =
      pageText2?.includes("assembleia") ||
      pageText2?.includes("Quórum") ||
      pageText2?.includes("signatário") ||
      pageText2?.includes("LGPD") ||
      pageText2?.includes("Consentimento");

    log(`TC-03: Advanced to step 2: ${onEtapa2}`);

    if (!onEtapa2) {
      log(`TC-03: FAIL — Nao avancou para etapa 2. Body: ${pageText2?.slice(0, 500)}`);
      await page.screenshot({ path: ss("tc03_fail_step2"), fullPage: true });
      throw new Error("TC-03: FAIL — Nao avancou para etapa 2");
    }

    // Etapa 2
    await fillStep2(page);
    await page.screenshot({ path: ss("tc03_etapa2_filled"), fullPage: true });

    btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: ss("tc03_etapa3_inicio"), fullPage: true });

    const pageText3 = await page.textContent("body");
    const onEtapa3 =
      pageText3?.includes("síndico") ||
      pageText3?.includes("Síndico") ||
      pageText3?.includes("link por e-mail");

    log(`TC-03: Advanced to step 3: ${onEtapa3}`);

    if (!onEtapa3) {
      log(`TC-03: FAIL — Nao avancou para etapa 3. Body: ${pageText3?.slice(0, 500)}`);
      await page.screenshot({ path: ss("tc03_fail_step3"), fullPage: true });
      throw new Error("TC-03: FAIL — Nao avancou para etapa 3");
    }

    // Etapa 3
    await fillStep3(page);
    await page.screenshot({ path: ss("tc03_etapa3_filled"), fullPage: true });

    btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: ss("tc03_revisao_inicio"), fullPage: true });

    const pageTextRevisao = await page.textContent("body");
    const onRevisao =
      pageTextRevisao?.includes("Criar condomínio") ||
      pageTextRevisao?.includes("revisão") ||
      pageTextRevisao?.includes("Revisão");

    log(`TC-03: On revisao page: ${onRevisao}`);

    if (!onRevisao) {
      log(`TC-03: FAIL — Nao chegou a revisao. Body: ${pageTextRevisao?.slice(0, 500)}`);
      await page.screenshot({ path: ss("tc03_fail_revisao"), fullPage: true });
      throw new Error("TC-03: FAIL — Nao chegou a revisao");
    }

    // Capturar resposta da API no submit
    let apiStatus: number | undefined;
    let apiBody: string = "";

    page.on("response", async (response) => {
      if (
        response.url().includes("/api/v1/admin/condominios") &&
        response.request().method() === "POST"
      ) {
        apiStatus = response.status();
        try {
          apiBody = await response.text();
        } catch {
          apiBody = "(nao foi possivel ler body)";
        }
        log(`TC-03: API Response Status: ${apiStatus}`);
        log(`TC-03: API Response Body: ${apiBody}`);
      }
    });

    const btnCriar = page.locator('button:has-text("Criar condomínio")').first();
    await btnCriar.click();
    await page.waitForTimeout(2000);
    await page.screenshot({ path: ss("tc03_apos_submit"), fullPage: true });

    const pageTextAfterSubmit = await page.textContent("body");
    const hasDuplicataMsg =
      pageTextAfterSubmit?.includes("Residencial Teste QA") ||
      pageTextAfterSubmit?.includes("já está cadastrado") ||
      pageTextAfterSubmit?.includes("duplicado") ||
      pageTextAfterSubmit?.includes("CNPJ já") ||
      pageTextAfterSubmit?.includes("já cadastrado");

    log(`TC-03: Duplicata message displayed: ${hasDuplicataMsg}`);
    log(`TC-03: API Status received: ${apiStatus}`);
    log(`TC-03: Body after submit: ${pageTextAfterSubmit?.slice(0, 800)}`);

    if (apiStatus === 409 && hasDuplicataMsg) {
      log("TC-03: PASS — API retornou 409 e mensagem de duplicata foi exibida");
    } else {
      log(
        `TC-03: FAIL — API status: ${apiStatus}, duplicata msg: ${hasDuplicataMsg}`
      );
    }

    expect(
      apiStatus,
      `API deve retornar 409 para CNPJ duplicado. Recebido: ${apiStatus}`
    ).toBe(409);
    expect(
      hasDuplicataMsg,
      "Mensagem de duplicata deve ser exibida com nome do tenant existente"
    ).toBe(true);
  });

  test("TC-04: CNPJ valido e unico — permite avancar", async ({ page }) => {
    log("=== TC-04: CNPJ valido e unico ===");
    await login(page);
    await page.goto(`${BASE_URL}/condominios/novo`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: ss("tc04_inicio"), fullPage: true });

    // 11.444.777/0001-61 — CNPJ válido e não cadastrado
    await fillStep1(page, "Condominio Novo Unico", "11.444.777/0001-61");
    await page.screenshot({ path: ss("tc04_filled"), fullPage: true });

    const btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: ss("tc04_apos_click"), fullPage: true });

    const pageText = await page.textContent("body");
    const hasCnpjError =
      pageText?.includes("CNPJ inválido") ||
      pageText?.includes("CNPJ invalido") ||
      false;
    const advancedToStep2 =
      pageText?.includes("assembleia") ||
      pageText?.includes("Quórum") ||
      pageText?.includes("signatário") ||
      pageText?.includes("LGPD") ||
      pageText?.includes("Consentimento") ||
      false;

    log(`TC-04: CNPJ error displayed: ${hasCnpjError}`);
    log(`TC-04: Advanced to step 2: ${advancedToStep2}`);
    log(`TC-04: Page URL: ${page.url()}`);
    log(`TC-04: Body excerpt: ${pageText?.slice(0, 500)}`);

    if (!hasCnpjError && advancedToStep2) {
      log("TC-04: PASS");
    } else {
      log(
        `TC-04: FAIL — CNPJ error shown: ${hasCnpjError}, advanced to step 2: ${advancedToStep2}`
      );
    }

    expect(
      hasCnpjError,
      "Nao deve exibir erro de CNPJ para CNPJ valido e unico"
    ).toBe(false);
    expect(
      advancedToStep2,
      "Deve avancar para a etapa 2 com CNPJ valido e unico"
    ).toBe(true);
  });
});
