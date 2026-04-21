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
  await page.fill(
    'input[type="email"], input[name="email"]',
    "operator@portabox.dev"
  );
  await page.fill('input[type="password"]', "PortaBox123!");
  await page.click('button[type="submit"]');
  await page.waitForURL(/condominios/, { timeout: 10000 });
}

test.describe("TC-11 Rerun — Confirmacao e redirecionamento apos correcao do bug", () => {
  test("TC-11: Criacao do condominio redireciona para /condominios/{uuid-valido}", async ({
    page,
  }) => {
    const consoleLogs: string[] = [];
    const pageErrors: string[] = [];
    page.on("console", (msg) => {
      consoleLogs.push(`[${msg.type()}] ${msg.text()}`);
      if (msg.type() === "error") {
        log(`[BROWSER_ERROR] ${msg.text()}`);
      }
    });
    page.on("pageerror", (err) => {
      pageErrors.push(err.message);
      log(`[PAGE_ERROR] ${err.message}`);
    });

    log("========================================");
    log("TC-11 RERUN: Confirmacao e redirecionamento");
    log(`Timestamp: ${new Date().toISOString()}`);
    log("Bug corrigido: result.id -> result.condominioId em NovoCondominioPage.tsx linha 65");
    log("========================================");

    // LOGIN
    log("--- PASSO 1: Login ---");
    await page.goto(`${BASE_URL}/login`);
    await page.screenshot({ path: screenshot("rerun_ct11_login"), fullPage: true });

    await page.fill('input[type="email"]', "operator@portabox.dev");
    await page.fill('input[type="password"]', "PortaBox123!");
    await page.click('button[type="submit"]');
    await page.waitForURL(/condominios/, { timeout: 10000 });
    log(`Login OK. URL: ${page.url()}`);

    // NAVEGAR PARA WIZARD
    log("--- PASSO 2: Acessar wizard ---");
    await page.goto(`${BASE_URL}/condominios/novo`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("rerun_ct11_etapa1_inicio"), fullPage: true });
    log(`Wizard carregado. URL: ${page.url()}`);

    // ETAPA 1 — Dados do condominio
    log("--- PASSO 3: Preencher Etapa 1 — Dados do condominio ---");

    // Nome fantasia
    const nomeInput = page
      .locator(
        'input[placeholder*="Residencial"], input[placeholder*="fantasia"], input[placeholder*="condomínio"]'
      )
      .first();
    await nomeInput.fill("Residencial Rerun QA");

    // CNPJ — 11.444.777/0001-61 (valido, diferente do existente)
    const cnpjInput = page.locator('input[placeholder*="000.000"]').first();
    await cnpjInput.fill("11.444.777/0001-61");

    // Logradouro
    const logradouroInput = page
      .locator('input[placeholder*="Acácias"], input[placeholder*="Rua"], input[placeholder*="logradouro"]')
      .first();
    if ((await logradouroInput.count()) > 0) {
      await logradouroInput.fill("Av. Paulista");
    }

    // Numero
    const numeroInput = page.locator('input[placeholder="123"]').first();
    if ((await numeroInput.count()) > 0) {
      await numeroInput.fill("1000");
    }

    // Cidade
    const cidadeInput = page.locator('input[placeholder="São Paulo"]').first();
    if ((await cidadeInput.count()) > 0) {
      await cidadeInput.fill("São Paulo");
    }

    // UF
    const ufInput = page.locator('input[placeholder="SP"]').first();
    if ((await ufInput.count()) > 0) {
      await ufInput.fill("SP");
    }

    // CEP
    const cepInput = page
      .locator('input[placeholder*="00000"], input[placeholder*="CEP"]')
      .first();
    if ((await cepInput.count()) > 0) {
      await cepInput.fill("01310-100");
    }

    // Administradora
    const adminInput = page
      .locator('input[placeholder*="empresa"], input[placeholder*="administradora"]')
      .first();
    if ((await adminInput.count()) > 0) {
      await adminInput.fill("Admin Rerun Ltda");
    }

    await page.screenshot({ path: screenshot("rerun_ct11_etapa1_preenchida"), fullPage: true });
    log("Etapa 1 preenchida: nome=Residencial Rerun QA, cnpj=11.444.777/0001-61");

    let btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("rerun_ct11_etapa2_inicio"), fullPage: true });

    const bodyEtapa2 = await page.textContent("body");
    const onEtapa2 = !!(
      bodyEtapa2?.includes("assembleia") ||
      bodyEtapa2?.includes("Quórum") ||
      bodyEtapa2?.includes("signatário")
    );
    log(`Avancou para etapa 2: ${onEtapa2}`);
    if (!onEtapa2) {
      log(`FAIL: Nao avancou para etapa 2. Body: ${bodyEtapa2?.slice(0, 300)}`);
      await page.screenshot({ path: screenshot("rerun_ct11_fail_etapa1"), fullPage: true });
      throw new Error(`TC-11 RERUN FAIL: Nao avancou para etapa 2. Body: ${bodyEtapa2?.slice(0, 300)}`);
    }

    // ETAPA 2 — Opt-In / LGPD
    log("--- PASSO 4: Preencher Etapa 2 --- Opt-In LGPD ---");

    const dataAssembleiaInput = page.locator('input[type="date"]').first();
    await dataAssembleiaInput.fill("2026-03-15");

    const quorumInput = page
      .locator('input[placeholder*="2/3"], input[placeholder*="quórum"], input[placeholder*="condôminos"]')
      .first();
    await quorumInput.fill("60%");

    const signatarioNomeInput = page
      .locator('input[placeholder*="Nome completo de quem"]')
      .first();
    await signatarioNomeInput.fill("Carlos Rerun");

    const cpfInput = page.locator('input[placeholder*="000.000.000"]').first();
    await cpfInput.fill("529.982.247-25");

    const dataTermoInput = page.locator('input[type="date"]').nth(1);
    await dataTermoInput.fill("2026-03-15");

    await page.screenshot({ path: screenshot("rerun_ct11_etapa2_preenchida"), fullPage: true });
    log("Etapa 2 preenchida: dataAssembleia=2026-03-15, quorum=60%, signatario=Carlos Rerun");

    btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("rerun_ct11_etapa3_inicio"), fullPage: true });

    const bodyEtapa3 = await page.textContent("body");
    const onEtapa3 = !!(
      bodyEtapa3?.includes("síndico") ||
      bodyEtapa3?.includes("Síndico") ||
      bodyEtapa3?.includes("link por e-mail")
    );
    log(`Avancou para etapa 3: ${onEtapa3}`);
    if (!onEtapa3) {
      log(`FAIL: Nao avancou para etapa 3. Body: ${bodyEtapa3?.slice(0, 300)}`);
      await page.screenshot({ path: screenshot("rerun_ct11_fail_etapa2"), fullPage: true });
      throw new Error(`TC-11 RERUN FAIL: Nao avancou para etapa 3. Body: ${bodyEtapa3?.slice(0, 300)}`);
    }

    // ETAPA 3 — Sindico
    log("--- PASSO 5: Preencher Etapa 3 — Sindico ---");

    const nomeInputSindico = page
      .locator('input[placeholder*="Maria Silva"], input[placeholder*="Nome completo"]')
      .first();
    await nomeInputSindico.fill("Sindico Rerun");

    const emailInputSindico = page.locator('input[type="email"]').first();
    await emailInputSindico.fill("sindico.rerun@portabox.dev");

    const celularInput = page.locator('input[placeholder*="+5511"]').first();
    await celularInput.fill("+5511987654321");

    await page.screenshot({ path: screenshot("rerun_ct11_etapa3_preenchida"), fullPage: true });
    log("Etapa 3 preenchida: nome=Sindico Rerun, email=sindico.rerun@portabox.dev, celular=+5511987654321");

    btnAvancar = page.locator('button:has-text("Avançar")').first();
    await btnAvancar.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("rerun_ct11_revisao_inicio"), fullPage: true });

    const bodyRevisao = await page.textContent("body");
    const onRevisao = !!(
      bodyRevisao?.includes("Criar condomínio") ||
      bodyRevisao?.includes("Revisão") ||
      bodyRevisao?.includes("revisão")
    );
    log(`Avancou para revisao: ${onRevisao}`);
    if (!onRevisao) {
      log(`FAIL: Nao avancou para revisao. Body: ${bodyRevisao?.slice(0, 300)}`);
      await page.screenshot({ path: screenshot("rerun_ct11_fail_etapa3"), fullPage: true });
      throw new Error(`TC-11 RERUN FAIL: Nao avancou para revisao. Body: ${bodyRevisao?.slice(0, 300)}`);
    }

    // REVISAO — submeter
    log("--- PASSO 6: Tela de revisao — clicar em Criar condominio ---");
    await page.screenshot({ path: screenshot("rerun_ct11_pre_submit"), fullPage: true });

    const btnCriar = page.locator('button:has-text("Criar condomínio")').first();
    const btnCriarCount = await btnCriar.count();
    log(`Botao 'Criar condominio' encontrado: ${btnCriarCount > 0}`);
    expect(btnCriarCount, "Botao 'Criar condominio' deve estar visivel na revisao").toBeGreaterThan(0);

    // Interceptar a requisicao para logar body e response
    const [response] = await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes("/condominios") && resp.request().method() === "POST",
        { timeout: 15000 }
      ),
      btnCriar.click(),
    ]);

    const respStatus = response.status();
    let respBody = "";
    try {
      respBody = await response.text();
    } catch {
      respBody = "(could not read body)";
    }

    log(`API Response status: ${respStatus}`);
    log(`API Response body: ${respBody}`);

    if (respStatus !== 201) {
      await page.screenshot({ path: screenshot("rerun_ct11_fail_api"), fullPage: true });
      const pageBody = await page.textContent("body");
      log(`TC-11 RERUN FAIL: API returned ${respStatus} instead of 201`);
      log(`Page body: ${pageBody?.slice(0, 500)}`);
      throw new Error(`TC-11 RERUN FAIL: API returned ${respStatus}. Body: ${respBody}`);
    }

    log("API retornou 201. Aguardando redirecionamento...");

    // Aguardar redirecionamento para /condominios/{uuid}
    try {
      await page.waitForURL(/condominios\/[0-9a-f-]{36}/, { timeout: 15000 });
      const finalUrl = page.url();
      log(`TC-11: Redirecionado para: ${finalUrl}`);

      await page.screenshot({ path: screenshot("rerun_ct11_redirect"), fullPage: true });

      // Verificar que nao e /condominios/undefined
      expect(
        finalUrl,
        "URL nao deve conter 'undefined' — bug foi corrigido"
      ).not.toContain("undefined");

      // Verificar que contem um UUID valido
      const uuidMatch = finalUrl.match(/condominios\/([0-9a-f-]{36})/);
      expect(uuidMatch, "URL deve conter UUID valido no formato 8-4-4-4-12").toBeTruthy();
      const tenantId = uuidMatch![1];
      log(`TC-11: Tenant ID extraido da URL: ${tenantId}`);

      // Aguardar pagina carregar
      await page.waitForLoadState("networkidle");
      await page.screenshot({ path: screenshot("rerun_ct11_detalhes_pos_criacao"), fullPage: true });

      const pageTextFinal = await page.textContent("body");
      const hasCondominioName = pageTextFinal?.includes("Residencial Rerun QA") ?? false;
      const hasSuccessMsg =
        (pageTextFinal?.includes("pré-ativo") ||
          pageTextFinal?.includes("link de definição") ||
          pageTextFinal?.includes("síndico") ||
          pageTextFinal?.includes("Condomínio criado")) ??
        false;

      log(`Conteudo da pagina de detalhes:`);
      log(`  - Nome do condominio visivel: ${hasCondominioName}`);
      log(`  - Mensagem de sucesso visivel: ${hasSuccessMsg}`);
      log(`  - Body (primeiros 500 chars): ${pageTextFinal?.slice(0, 500)}`);

      if (consoleLogs.length > 0 || pageErrors.length > 0) {
        log("--- BROWSER CONSOLE TC-11 RERUN ---");
        [...consoleLogs, ...pageErrors].forEach((l) => log(l));
      }

      // Salvar tenant_id
      fs.writeFileSync(
        path.join(EVIDENCE_DIR, "tenant_id.txt"),
        tenantId
      );
      log(`TC-11 RERUN: PASS — Redirect para /condominios/${tenantId}`);

    } catch (err: unknown) {
      await page.screenshot({ path: screenshot("rerun_ct11_fail_redirect"), fullPage: true });
      const currentUrl = page.url();
      const pageBodyOnFail = await page.textContent("body");

      log(`TC-11 RERUN: FAIL — Sem redirecionamento valido`);
      log(`URL atual: ${currentUrl}`);
      log(`Body na falha: ${pageBodyOnFail?.slice(0, 500)}`);

      if (consoleLogs.length > 0 || pageErrors.length > 0) {
        log("--- BROWSER CONSOLE TC-11 RERUN (FAIL) ---");
        [...consoleLogs, ...pageErrors].forEach((l) => log(l));
      }

      const errMsg = err instanceof Error ? err.message : String(err);
      throw new Error(
        `TC-11 RERUN FAIL: Expected redirect to /condominios/{uuid}, URL atual: ${currentUrl}. ${errMsg}`
      );
    }
  });
});
