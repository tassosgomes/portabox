import { test, expect } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = "http://localhost:5173";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_01_wizard_criacao_tenant";
const SCREENSHOTS_DIR = path.join(EVIDENCE_DIR, "screenshots");
const LOG_FILE = path.join(EVIDENCE_DIR, "requests.log");

function log(msg: string) {
  const line = `[${new Date().toISOString()}] ${msg}\n`;
  fs.appendFileSync(LOG_FILE, line);
}

function screenshot(name: string) {
  return path.join(SCREENSHOTS_DIR, `${name}.png`);
}

async function login(page: import("@playwright/test").Page) {
  await page.goto(`${BASE_URL}/login`);
  await page.fill('input[type="email"], input[name="email"], input[placeholder*="e-mail"], input[placeholder*="email"], input[placeholder*="Email"]', "operator@portabox.dev");
  await page.fill('input[type="password"]', "PortaBox123!");
  await page.click('button[type="submit"], button:has-text("Entrar"), button:has-text("Login")');
  await page.waitForURL(/condominios/, { timeout: 10000 });
}

test.describe("QA Task 01 — Wizard Criacao de Tenant", () => {
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

  test("CT-01: Login e acesso ao wizard", async ({ page }) => {
    log("=== CT-01: Login e acesso ao wizard ===");
    await page.goto(`${BASE_URL}/login`);
    await page.screenshot({ path: screenshot("ct01_login_page"), fullPage: true });

    // Preencher email
    const emailInput = page.locator('input[type="email"]').first();
    await emailInput.fill("operator@portabox.dev");
    const passwordInput = page.locator('input[type="password"]').first();
    await passwordInput.fill("PortaBox123!");

    await page.screenshot({ path: screenshot("ct01_login_filled"), fullPage: true });

    const submitBtn = page.locator('button[type="submit"]').first();
    await submitBtn.click();
    await page.waitForURL(/condominios/, { timeout: 10000 });

    await page.screenshot({ path: screenshot("ct01_after_login"), fullPage: true });

    // Navegar para /condominios/novo
    await page.goto(`${BASE_URL}/condominios/novo`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("ct01_wizard_page"), fullPage: true });

    // Verificar que o wizard esta visivel (etapa 1)
    const pageText = await page.textContent("body");
    const hasWizardContent =
      (pageText?.includes("Dados do condomínio") ||
        pageText?.includes("Nome fantasia") ||
        pageText?.includes("CNPJ")) ??
      false;

    log(`CT-01: Wizard content found: ${hasWizardContent}`);
    log(`CT-01: Page URL: ${page.url()}`);
    log("CT-01: PASS");

    expect(hasWizardContent).toBe(true);
  });

  test("CT-02: Etapa 1 — Validacao campos obrigatorios", async ({ page }) => {
    log("=== CT-02: Etapa 1 — Validacao campos obrigatorios ===");
    await login(page);
    await page.goto(`${BASE_URL}/condominios/novo`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("ct02_inicio"), fullPage: true });

    // Clicar em Avancar sem preencher nada
    const btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.screenshot({ path: screenshot("ct02_errors"), fullPage: true });

    const pageText = await page.textContent("body");
    const hasNomeError =
      pageText?.includes("Nome fantasia é obrigatório") ?? false;
    const hasCnpjError = pageText?.includes("CNPJ inválido") ?? false;

    log(`CT-02: Nome error displayed: ${hasNomeError}`);
    log(`CT-02: CNPJ error displayed: ${hasCnpjError}`);

    // Verificar que nao avancou (URL permanece no novo)
    const currentUrl = page.url();
    log(`CT-02: URL after click: ${currentUrl}`);

    if (!hasNomeError) {
      log("CT-02: FAIL — Expected 'Nome fantasia é obrigatório' error not found");
    }
    if (!hasCnpjError) {
      log("CT-02: FAIL — Expected 'CNPJ inválido' error not found");
    }
    if (hasNomeError && hasCnpjError) {
      log("CT-02: PASS");
    }

    expect(hasNomeError, "Error message 'Nome fantasia é obrigatório' should be visible").toBe(true);
    expect(hasCnpjError, "Error message 'CNPJ inválido' should be visible").toBe(true);
  });

  test("CT-03: Etapa 1 — Validacao CNPJ invalido", async ({ page }) => {
    log("=== CT-03: Etapa 1 — Validacao CNPJ invalido ===");
    await login(page);
    await page.goto(`${BASE_URL}/condominios/novo`);
    await page.waitForLoadState("networkidle");

    const nomeInput = page.locator('input[placeholder*="Residencial"], input[placeholder*="fantasia"]').first();
    await nomeInput.fill("Residencial Teste QA");

    const cnpjInput = page.locator('input[placeholder*="000.000"]').first();
    await cnpjInput.fill("00.000.000/0000-00");

    await page.screenshot({ path: screenshot("ct03_cnpj_invalido_filled"), fullPage: true });

    const btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.screenshot({ path: screenshot("ct03_cnpj_invalido_error"), fullPage: true });

    const pageText = await page.textContent("body");
    const hasCnpjError = pageText?.includes("CNPJ inválido") ?? false;

    log(`CT-03: CNPJ error for invalid CNPJ: ${hasCnpjError}`);
    if (hasCnpjError) {
      log("CT-03: PASS");
    } else {
      log("CT-03: FAIL — Expected CNPJ invalid error not found");
    }

    expect(hasCnpjError, "Error 'CNPJ inválido' should be shown for invalid CNPJ").toBe(true);
  });

  test("CT-04 to CT-11: Fluxo completo do wizard", async ({ page }) => {
    log("=== CT-04 a CT-11: Fluxo completo do wizard ===");
    await login(page);
    await page.goto(`${BASE_URL}/condominios/novo`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("ct04_etapa1_inicio"), fullPage: true });

    // --- CT-04: Etapa 1 ---
    log("--- CT-04: Preenchendo Etapa 1 ---");

    const nomeInput = page.locator('input[placeholder*="Residencial"], input[placeholder*="fantasia"]').first();
    await nomeInput.fill("Residencial Teste QA");

    const cnpjInput = page.locator('input[placeholder*="000.000"]').first();
    await cnpjInput.fill("11.222.333/0001-81");

    // Logradouro
    const logradouroInput = page.locator('input[placeholder*="Acácias"], input[placeholder*="Rua"]').first();
    if (await logradouroInput.count() > 0) {
      await logradouroInput.fill("Rua das Flores");
    }

    // Numero
    const numeroInput = page.locator('input[placeholder="123"]').first();
    if (await numeroInput.count() > 0) {
      await numeroInput.fill("123");
    }

    // Cidade
    const cidadeInput = page.locator('input[placeholder="São Paulo"]').first();
    if (await cidadeInput.count() > 0) {
      await cidadeInput.fill("Sao Paulo");
    }

    // UF
    const ufInput = page.locator('input[placeholder="SP"]').first();
    if (await ufInput.count() > 0) {
      await ufInput.fill("SP");
    }

    // Administradora
    const adminInput = page.locator('input[placeholder*="empresa"], input[placeholder*="administradora"]').first();
    if (await adminInput.count() > 0) {
      await adminInput.fill("Administradora Teste Ltda");
    }

    await page.screenshot({ path: screenshot("ct04_etapa1_filled"), fullPage: true });

    let btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("ct04_etapa2_inicio"), fullPage: true });

    // Verificar etapa 2
    const pageTextEtapa2 = await page.textContent("body");
    const onEtapa2 =
      pageTextEtapa2?.includes("assembleia") ||
      pageTextEtapa2?.includes("Quórum") ||
      pageTextEtapa2?.includes("signatário");
    log(`CT-04: Advanced to step 2: ${onEtapa2}`);
    if (onEtapa2) {
      log("CT-04: PASS");
    } else {
      log("CT-04: FAIL — Did not advance to step 2");
      throw new Error("CT-04 FAIL: Did not advance to step 2. Body: " + pageTextEtapa2?.slice(0, 500));
    }

    // --- CT-05: Validacao Etapa 2 ---
    log("--- CT-05: Validacao campos obrigatorios Etapa 2 ---");
    btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.screenshot({ path: screenshot("ct05_etapa2_errors"), fullPage: true });

    const pageTextErrors2 = await page.textContent("body");
    const hasAssembleiaError = pageTextErrors2?.includes("assembleia é obrigatória") ?? false;
    const hasQuorumError = pageTextErrors2?.includes("Quórum é obrigatório") ?? false;
    const hasSignatarioError = pageTextErrors2?.includes("signatário é obrigatório") ?? false;
    log(`CT-05: assembleia error: ${hasAssembleiaError}, quorum error: ${hasQuorumError}, signatario error: ${hasSignatarioError}`);
    if (hasAssembleiaError && hasQuorumError) {
      log("CT-05: PASS");
    } else {
      log(`CT-05: FAIL — errors not shown. Body contains: ${pageTextErrors2?.slice(0, 300)}`);
    }
    expect(hasAssembleiaError || hasQuorumError, "Step 2 validation errors should appear when clicking Avançar without filling fields").toBe(true);

    // --- CT-06: Etapa 2 preenchida ---
    log("--- CT-06: Preenchendo Etapa 2 ---");

    // Data assembleia
    const dataAssembleiaInput = page.locator('input[type="date"]').first();
    await dataAssembleiaInput.fill("2026-03-01");

    // Quorum
    const quorumInput = page.locator('input[placeholder*="2/3"], input[placeholder*="quórum"], input[placeholder*="condôminos"]').first();
    await quorumInput.fill("75%");

    // Signatario nome
    const signatarioNomeInput = page.locator('input[placeholder*="Nome completo de quem"]').first();
    await signatarioNomeInput.fill("Joao da Silva");

    // CPF signatario
    const cpfInput = page.locator('input[placeholder*="000.000.000"]').first();
    await cpfInput.fill("529.982.247-25");

    // Data termo
    const dataTermoInput = page.locator('input[type="date"]').nth(1);
    await dataTermoInput.fill("2026-03-01");

    await page.screenshot({ path: screenshot("ct06_etapa2_filled"), fullPage: true });

    btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("ct06_etapa3_inicio"), fullPage: true });

    const pageTextEtapa3 = await page.textContent("body");
    const onEtapa3 =
      pageTextEtapa3?.includes("síndico") ||
      pageTextEtapa3?.includes("Síndico") ||
      pageTextEtapa3?.includes("link por e-mail");
    log(`CT-06: Advanced to step 3: ${onEtapa3}`);
    if (onEtapa3) {
      log("CT-06: PASS");
    } else {
      log("CT-06: FAIL — Did not advance to step 3. Body: " + pageTextEtapa3?.slice(0, 500));
      throw new Error("CT-06 FAIL: Did not advance to step 3");
    }

    // --- CT-07: Validacao Etapa 3 ---
    log("--- CT-07: Validacao campos Etapa 3 ---");

    // Tentar avancar sem preencher
    btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.screenshot({ path: screenshot("ct07_etapa3_errors_empty"), fullPage: true });

    const pageTextErrors3 = await page.textContent("body");
    const hasNomeError3 = pageTextErrors3?.includes("Nome é obrigatório") ?? false;
    const hasEmailError3 = pageTextErrors3?.includes("E-mail inválido") ?? false;
    const hasCelularError3 = !!(pageTextErrors3?.includes("E.164") || pageTextErrors3?.includes("Celular deve estar") || pageTextErrors3?.includes("celular"));
    log(`CT-07: nome error: ${hasNomeError3}, email error: ${hasEmailError3}, celular error: ${hasCelularError3}`);
    expect(hasNomeError3, "Nome error should appear").toBe(true);

    // Tentar com celular no formato nacional (invalido para E.164)
    const nomeInputSindico = page.locator('input[placeholder*="Maria Silva"], input[placeholder*="Nome completo"]').first();
    await nomeInputSindico.fill("Maria Oliveira");

    const emailInputSindico = page.locator('input[type="email"]').first();
    await emailInputSindico.fill("sindico.qa@portabox.dev");

    const celularInput = page.locator('input[placeholder*="+5511"]').first();
    await celularInput.fill("(11) 91234-5678");

    btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.screenshot({ path: screenshot("ct07_etapa3_celular_nacional_error"), fullPage: true });

    const pageTextCelularError = await page.textContent("body");
    const hasCelularFormatError =
      pageTextCelularError?.includes("E.164") ||
      pageTextCelularError?.includes("+55") ||
      pageTextCelularError?.includes("Celular deve estar");
    log(`CT-07: Celular format error shown for national format: ${hasCelularFormatError}`);
    if (hasCelularFormatError) {
      log("CT-07: PASS");
    } else {
      log("CT-07: FAIL — Expected E.164 error for national format, but was not shown. Body: " + pageTextCelularError?.slice(0, 300));
    }
    expect(hasCelularFormatError, "Celular E.164 format error should appear for national format '(11) 91234-5678'").toBe(true);

    // --- CT-08: Etapa 3 com celular E.164 ---
    log("--- CT-08: Preenchendo Etapa 3 com celular E.164 ---");
    await celularInput.fill("+5511912345678");
    await page.screenshot({ path: screenshot("ct08_etapa3_e164_filled"), fullPage: true });

    btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("ct08_revisao_inicio"), fullPage: true });

    const pageTextRevisao = await page.textContent("body");
    const onRevisao =
      pageTextRevisao?.includes("Criar condomínio") ||
      pageTextRevisao?.includes("Consentimento LGPD") ||
      pageTextRevisao?.includes("Revisão") ||
      pageTextRevisao?.includes("revisão");
    log(`CT-08: Advanced to revisao: ${onRevisao}`);
    if (onRevisao) {
      log("CT-08: PASS");
    } else {
      log("CT-08: FAIL — Did not advance to revisao. Body: " + pageTextRevisao?.slice(0, 500));
      throw new Error("CT-08 FAIL: Did not advance to revisao");
    }

    // --- CT-09: Revisao exibe dados corretos ---
    log("--- CT-09: Verificando dados na revisao ---");
    const pageTextRevisaoFull = await page.textContent("body");
    const hasNomeOnRevisao = pageTextRevisaoFull?.includes("Residencial Teste QA") ?? false;
    const hasCnpjOnRevisao = !!(pageTextRevisaoFull?.includes("11.222.333/0001-81") || pageTextRevisaoFull?.includes("11222333000181"));
    const hasSigOnRevisao = pageTextRevisaoFull?.includes("Joao da Silva") ?? false;
    const hasSindicoOnRevisao = pageTextRevisaoFull?.includes("Maria Oliveira") ?? false;
    const hasSindicoEmailOnRevisao = pageTextRevisaoFull?.includes("sindico.qa@portabox.dev") ?? false;

    log(`CT-09: Nome condominio on revisao: ${hasNomeOnRevisao}`);
    log(`CT-09: CNPJ on revisao: ${hasCnpjOnRevisao}`);
    log(`CT-09: Signatario on revisao: ${hasSigOnRevisao}`);
    log(`CT-09: Sindico nome on revisao: ${hasSindicoOnRevisao}`);
    log(`CT-09: Sindico email on revisao: ${hasSindicoEmailOnRevisao}`);

    await page.screenshot({ path: screenshot("ct09_revisao_dados"), fullPage: true });

    if (hasNomeOnRevisao && hasCnpjOnRevisao && hasSindicoOnRevisao) {
      log("CT-09: PASS");
    } else {
      log("CT-09: FAIL — Some data missing from revisao screen");
    }
    expect(hasNomeOnRevisao, "Nome fantasia should be shown on revisao").toBe(true);
    expect(hasCnpjOnRevisao, "CNPJ should be shown on revisao").toBe(true);
    expect(hasSindicoOnRevisao, "Sindico name should be shown on revisao").toBe(true);

    // --- CT-10: Botao Voltar da revisao ---
    log("--- CT-10: Botao Voltar da revisao ---");
    const btnVoltar = page.locator('button:has-text("Voltar")').first();
    await btnVoltar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("ct10_apos_voltar"), fullPage: true });

    const pageTextAposVoltar = await page.textContent("body");
    const onEtapa3AposVoltar =
      pageTextAposVoltar?.includes("síndico") ||
      pageTextAposVoltar?.includes("link por e-mail");
    log(`CT-10: Back to step 3 after Voltar: ${onEtapa3AposVoltar}`);
    if (onEtapa3AposVoltar) {
      log("CT-10: PASS");
    } else {
      log("CT-10: FAIL — Did not return to step 3. Body: " + pageTextAposVoltar?.slice(0, 300));
    }
    expect(onEtapa3AposVoltar, "Should return to step 3 after clicking Voltar from revisao").toBe(true);

    // Retornar a revisao para submeter
    btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);

    // --- CT-11: Confirmacao e redirecionamento ---
    log("--- CT-11: Confirmacao e redirecionamento ---");
    await page.screenshot({ path: screenshot("ct11_revisao_pre_submit"), fullPage: true });

    const btnCriar = page.locator('button:has-text("Criar condomínio")').first();
    expect(await btnCriar.count(), "Button 'Criar condomínio' should be visible").toBeGreaterThan(0);

    await btnCriar.click();
    log("CT-11: Clicked 'Criar condomínio'");

    // Aguardar redirecionamento para detalhes do condominio
    try {
      await page.waitForURL(/condominios\/[0-9a-f-]{36}/, { timeout: 15000 });
      const finalUrl = page.url();
      log(`CT-11: Redirected to: ${finalUrl}`);

      await page.screenshot({ path: screenshot("ct11_detalhes_pos_criacao"), fullPage: true });

      const pageTextFinal = await page.textContent("body");
      const hasSuccessMsg =
        pageTextFinal?.includes("pré-ativo") ||
        pageTextFinal?.includes("magic link") ||
        pageTextFinal?.includes("link de definição") ||
        pageTextFinal?.includes("síndico") ||
        pageTextFinal?.includes("Residencial Teste QA");
      log(`CT-11: Success content on details page: ${hasSuccessMsg}`);
      log(`CT-11: Final URL: ${finalUrl}`);
      log("CT-11: PASS");

      // Extrair o ID para validacao no banco
      const match = finalUrl.match(/condominios\/([0-9a-f-]{36})/);
      if (match) {
        const tenantId = match[1];
        log(`CT-11: Tenant ID extracted: ${tenantId}`);
        fs.writeFileSync(path.join(EVIDENCE_DIR, "tenant_id.txt"), tenantId);
      }

    } catch {
      await page.screenshot({ path: screenshot("ct11_fail_no_redirect"), fullPage: true });
      const pageTextOnFail = await page.textContent("body");
      log(`CT-11: FAIL — No redirect to tenant details. Current URL: ${page.url()}`);
      log(`CT-11: Body on fail: ${pageTextOnFail?.slice(0, 500)}`);
      throw new Error(`CT-11 FAIL: Expected redirect to /condominios/{id}, but stayed at ${page.url()}`);
    }
  });
});
