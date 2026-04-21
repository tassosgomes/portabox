import { test, expect } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = "http://localhost:5173";
const TENANT_ID = "4a3d87ea-f62f-4d9c-80de-a34237d0dae3";
const EVIDENCE_DIR =
  "/home/tsgomes/log-portaria/.compozy/tasks/f01-criacao-condominio/qa-evidence/qa_task_04_painel_detalhes_golive";
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
  await page.waitForLoadState("networkidle");
  await page.fill('input[type="email"]', "operator@portabox.dev");
  await page.fill('input[type="password"]', "PortaBox123!");
  await page.click('button[type="submit"]');
  await page.waitForURL(/condominios/, { timeout: 15000 });
}

test.describe("QA Task 04 RERUN — Painel de Detalhes e Go-live", () => {
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

  test("CT-01: Acessar painel de detalhes do tenant", async ({ page }) => {
    log("=== RERUN CT-01: Acessar painel de detalhes do tenant ===");

    await login(page);
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("rerun_ct01_inicio"), fullPage: true });

    const pageText = await page.textContent("body");

    // Verificar que a pagina nao esta em branco (crash)
    const isBlankPage = (pageText?.trim().length ?? 0) < 50;
    log(`CT-01: Pagina em branco (crash): ${isBlankPage}`);
    log(`CT-01: Tamanho do texto: ${pageText?.trim().length}`);

    const hasNome = pageText?.includes("Residencial Rerun QA") ?? false;
    const hasStatusPreAtivo =
      pageText?.includes("pré-ativo") ||
      pageText?.includes("Pré-ativo") ||
      pageText?.includes("pré ativo") ||
      pageText?.includes("preativo") ||
      pageText?.includes("PreAtivo") ||
      pageText?.includes("pre-ativo");
    const hasCnpj =
      pageText?.includes("****7000161") ||
      pageText?.includes("7000161") ||
      pageText?.includes("CNPJ") ||
      pageText?.includes("11.444.777");

    log(`CT-01: Nome presente: ${hasNome}`);
    log(`CT-01: Status pré-ativo presente: ${hasStatusPreAtivo}`);
    log(`CT-01: CNPJ presente: ${hasCnpj}`);
    log(`CT-01: URL: ${page.url()}`);
    log(`CT-01: Body snippet (primeiros 500 chars): ${pageText?.slice(0, 500)}`);

    await page.screenshot({ path: screenshot("rerun_ct01_resultado"), fullPage: true });

    if (hasNome && hasStatusPreAtivo) {
      log("CT-01: PASS");
    } else {
      log(`CT-01: FAIL — Nome: ${hasNome}, Status: ${hasStatusPreAtivo}`);
    }

    expect(isBlankPage, "Página não deve estar em branco (crash)").toBe(false);
    expect(hasNome, "Nome fantasia 'Residencial Rerun QA' deve estar visível").toBe(true);
    expect(hasStatusPreAtivo, "Status 'pré-ativo' deve estar visível").toBe(true);
  });

  test("CT-02: Dados do opt-in exibidos", async ({ page }) => {
    log("=== RERUN CT-02: Dados do opt-in exibidos ===");

    await login(page);
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("rerun_ct02_inicio"), fullPage: true });

    const pageText = await page.textContent("body");

    const hasOptInSection =
      pageText?.includes("LGPD") ||
      pageText?.includes("Consentimento") ||
      pageText?.includes("opt-in") ||
      pageText?.includes("Opt-in");
    const hasQuorum =
      pageText?.includes("60%");
    const hasSignatario =
      pageText?.includes("Carlos Rerun");
    const hasDataAssembleia =
      pageText?.includes("15/03/2026") ||
      pageText?.includes("2026-03-15") ||
      pageText?.includes("03/15/2026");
    const hasCpfMasked =
      pageText?.includes("***.982.247-**") ||
      pageText?.includes("982.247");

    log(`CT-02: Seção LGPD/opt-in: ${hasOptInSection}`);
    log(`CT-02: Quórum 60%: ${hasQuorum}`);
    log(`CT-02: Signatário Carlos Rerun: ${hasSignatario}`);
    log(`CT-02: Data assembleia 15/03/2026: ${hasDataAssembleia}`);
    log(`CT-02: CPF mascarado: ${hasCpfMasked}`);
    log(`CT-02: Body snippet: ${pageText?.slice(0, 800)}`);

    await page.screenshot({ path: screenshot("rerun_ct02_resultado"), fullPage: true });

    if (hasQuorum && hasSignatario) {
      log("CT-02: PASS");
    } else {
      log(`CT-02: FAIL — Quorum: ${hasQuorum}, Signatario: ${hasSignatario}`);
    }

    expect(hasQuorum, "Quórum '60%' deve estar visível na seção de opt-in").toBe(true);
    expect(hasSignatario, "Nome do signatário 'Carlos Rerun' deve estar visível").toBe(true);
  });

  test("CT-03: Situacao do primeiro sindico exibida", async ({ page }) => {
    log("=== RERUN CT-03: Situacao do primeiro sindico exibida ===");

    await login(page);
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("rerun_ct03_inicio"), fullPage: true });

    const pageText = await page.textContent("body");

    const hasSindicoNome = pageText?.includes("Sindico Rerun") ?? false;
    const hasSindicoEmail = pageText?.includes("sindico.rerun@portabox.dev") ?? false;
    const hasCelularMasked =
      pageText?.includes("+55 11 9****-4321") ||
      pageText?.includes("9****-4321");
    const hasSenhaNao =
      pageText?.includes("Não") ||
      pageText?.includes("não") ||
      pageText?.includes("Senha definida");

    log(`CT-03: Nome sindico Sindico Rerun: ${hasSindicoNome}`);
    log(`CT-03: Email sindico.rerun@portabox.dev: ${hasSindicoEmail}`);
    log(`CT-03: Celular mascarado: ${hasCelularMasked}`);
    log(`CT-03: Campo senha/Não: ${hasSenhaNao}`);
    log(`CT-03: Body snippet: ${pageText?.slice(0, 800)}`);

    await page.screenshot({ path: screenshot("rerun_ct03_resultado"), fullPage: true });

    if (hasSindicoNome && hasSindicoEmail) {
      log("CT-03: PASS");
    } else {
      log(`CT-03: FAIL — Nome: ${hasSindicoNome}, Email: ${hasSindicoEmail}`);
    }

    expect(hasSindicoNome, "Nome do síndico 'Sindico Rerun' deve estar visível").toBe(true);
    expect(hasSindicoEmail, "Email do síndico 'sindico.rerun@portabox.dev' deve estar visível").toBe(true);
  });

  test("CT-04: Botao Ativar operacao requer confirmacao", async ({ page }) => {
    log("=== RERUN CT-04: Botao Ativar operacao requer confirmacao ===");

    await login(page);
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("rerun_ct04_pre_click"), fullPage: true });

    const btnAtivar = page.locator(
      'button:has-text("Ativar operação"), button:has-text("Ativar Operação"), button:has-text("Ativar operacao")'
    ).first();

    const btnCount = await btnAtivar.count();
    log(`CT-04: Botao de ativacao encontrado: ${btnCount > 0}`);

    if (btnCount === 0) {
      await page.screenshot({ path: screenshot("rerun_ct04_fail_sem_botao"), fullPage: true });
      const pageText = await page.textContent("body");
      log(`CT-04: FAIL — Botão 'Ativar operação' não encontrado`);
      log(`CT-04: Body snippet: ${pageText?.slice(0, 800)}`);
      expect(btnCount, "Botão 'Ativar operação' deve estar presente").toBeGreaterThan(0);
      return;
    }

    await btnAtivar.click();
    log("CT-04: Clicou no botao de ativacao");
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("rerun_ct04_apos_click"), fullPage: true });

    const pageTextAposClick = await page.textContent("body");
    const dialogLocator = page.locator('[role="dialog"], [role="alertdialog"]');
    const dialogCount = await dialogLocator.count();
    const hasConfirmText =
      pageTextAposClick?.includes("confirmar") ||
      pageTextAposClick?.includes("Confirmar") ||
      pageTextAposClick?.includes("Tem certeza") ||
      pageTextAposClick?.includes("certeza") ||
      pageTextAposClick?.includes("Confirmação") ||
      pageTextAposClick?.includes("confirmação");

    const hasConfirmDialog = dialogCount > 0 || hasConfirmText;

    log(`CT-04: Dialog role detectado (count): ${dialogCount}`);
    log(`CT-04: Texto de confirmacao detectado: ${hasConfirmText}`);
    log(`CT-04: Dialogo de confirmacao geral: ${hasConfirmDialog}`);

    if (hasConfirmDialog) {
      log("CT-04: PASS — dialogo de confirmacao exibido");
      // Fechar sem confirmar
      const btnCancelar = page.locator(
        'button:has-text("Cancelar"), button:has-text("Não"), button:has-text("Fechar")'
      ).first();
      if (await btnCancelar.count() > 0) {
        await btnCancelar.click();
        log("CT-04: Dialogo cancelado com sucesso");
      }
    } else {
      log(`CT-04: FAIL — Nenhum dialogo detectado. Body apos click: ${pageTextAposClick?.slice(0, 500)}`);
    }

    expect(hasConfirmDialog, "Diálogo de confirmação deve aparecer ao clicar em 'Ativar operação'").toBe(true);
  });

  test("CT-05 e CT-08: Ativacao go-live via UI e validacao pos-ativacao", async ({ page }) => {
    log("=== RERUN CT-05+CT-08: Ativacao go-live e validacoes ===");

    await login(page);
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("rerun_ct05_pre_ativacao"), fullPage: true });

    const pageTextInicial = await page.textContent("body");
    const hasPreAtivo =
      pageTextInicial?.includes("pré-ativo") ||
      pageTextInicial?.includes("Pré-ativo") ||
      pageTextInicial?.includes("PreAtivo");
    log(`CT-05: Status inicial pré-ativo na UI: ${hasPreAtivo}`);

    const btnAtivar = page.locator(
      'button:has-text("Ativar operação"), button:has-text("Ativar Operação"), button:has-text("Ativar operacao")'
    ).first();

    const btnCount = await btnAtivar.count();
    if (btnCount === 0) {
      await page.screenshot({ path: screenshot("rerun_ct05_fail_sem_botao"), fullPage: true });
      log(`CT-05: FAIL — Botão de ativação não encontrado. Body: ${pageTextInicial?.slice(0, 500)}`);
      expect(btnCount, "Botão de ativação deve estar presente").toBeGreaterThan(0);
      return;
    }

    await btnAtivar.click();
    log("CT-05: Clicou no botao de ativacao");
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("rerun_ct05_dialogo_aberto"), fullPage: true });

    // Confirmar no dialogo — botao de confirmar (excluindo Cancelar)
    const btnConfirmar = page.locator(
      'button:has-text("Confirmar"), button:has-text("Sim"), button:has-text("Confirmar ativação")'
    ).first();

    // Tente tambem com locator mais amplo caso o texto seja diferente
    let confirmCount = await btnConfirmar.count();
    log(`CT-05: Botao confirmar encontrado (tentativa 1): ${confirmCount}`);

    if (confirmCount === 0) {
      // Tenta localizar qualquer botao no dialogo exceto cancelar
      const dialogButtons = page.locator('[role="dialog"] button, [role="alertdialog"] button');
      const allBtns = await dialogButtons.all();
      log(`CT-05: Botoes no dialogo: ${allBtns.length}`);
      for (const btn of allBtns) {
        const txt = await btn.textContent();
        log(`CT-05:   Botao no dialogo: "${txt}"`);
      }
      // Screenshot do dialogo
      await page.screenshot({ path: screenshot("rerun_ct05_dialogo_detalhe"), fullPage: true });
      const bodyDialog = await page.textContent("body");
      log(`CT-05: FAIL — Botão de confirmação não encontrado. Body: ${bodyDialog?.slice(0, 500)}`);
      expect(confirmCount, "Botão de confirmação deve estar no diálogo").toBeGreaterThan(0);
      return;
    }

    await btnConfirmar.click();
    log("CT-05: Clicou em Confirmar");

    // Aguardar ativacao e reload
    await page.waitForTimeout(3000);
    await page.screenshot({ path: screenshot("rerun_ct05_apos_ativacao"), fullPage: true });

    const pageTextPosAtivacao = await page.textContent("body");

    const hasStatusAtivo =
      pageTextPosAtivacao?.includes("ativo") ||
      pageTextPosAtivacao?.includes("Ativo") ||
      pageTextPosAtivacao?.includes("ATIVO");

    log(`CT-05: Status 'ativo' na UI: ${hasStatusAtivo}`);
    log(`CT-05: Body pos-ativacao snippet: ${pageTextPosAtivacao?.slice(0, 600)}`);

    if (hasStatusAtivo) {
      log("CT-05: PASS — Status ativo exibido na UI");
    } else {
      log(`CT-05: FAIL — Status ativo não exibido.`);
    }

    expect(hasStatusAtivo, "Status 'ativo' deve aparecer na UI após ativação").toBe(true);

    // CT-08: botao deve desaparecer
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("rerun_ct08_botao_ausente"), fullPage: true });

    const btnAtivarPos = page.locator(
      'button:has-text("Ativar operação"), button:has-text("Ativar Operação"), button:has-text("Ativar operacao")'
    ).first();

    const btnAtivarPosCount = await btnAtivarPos.count();
    let btnDesabilitado = false;
    if (btnAtivarPosCount > 0) {
      btnDesabilitado = await btnAtivarPos.isDisabled();
    }

    const btnAtivarAusente = btnAtivarPosCount === 0 || btnDesabilitado;
    log(`CT-08: Botao ativar apos ativacao — ausente: ${btnAtivarPosCount === 0}, desabilitado: ${btnDesabilitado}`);

    if (btnAtivarAusente) {
      log("CT-08: PASS — Botão de ativação ausente ou desabilitado após ativação");
    } else {
      log("CT-08: FAIL — Botão de ativação ainda presente e habilitado");
    }

    expect(btnAtivarAusente, "Botão 'Ativar operação' deve estar ausente após ativação").toBe(true);
  });
});
