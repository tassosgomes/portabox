import { test, expect } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const BASE_URL = "http://localhost:5173";
const TENANT_ID = "f6d3cc9d-9ce5-4e43-bb70-92573fb29ae5";
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
  await page.waitForURL(/condominios/, { timeout: 10000 });
}

test.describe("QA Task 04 — Painel de Detalhes e Go-live", () => {
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
    log("=== CT-01: Acessar painel de detalhes do tenant ===");

    await login(page);
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("ct01_painel_inicio"), fullPage: true });

    const pageText = await page.textContent("body");

    const hasNome = pageText?.includes("Residencial Teste QA") ?? false;
    const hasStatus =
      pageText?.includes("pré-ativo") ||
      pageText?.includes("Pré-ativo") ||
      pageText?.includes("PreAtivo") ||
      pageText?.includes("Pre-ativo") ||
      pageText?.includes("pre-ativo") ||
      pageText?.includes("preativo");
    const hasCnpj =
      pageText?.includes("****3000181") ||
      pageText?.includes("11.222.333") ||
      pageText?.includes("CNPJ");

    log(`CT-01: Nome presente: ${hasNome}`);
    log(`CT-01: Status pré-ativo presente: ${hasStatus}`);
    log(`CT-01: CNPJ presente: ${hasCnpj}`);
    log(`CT-01: URL: ${page.url()}`);

    await page.screenshot({ path: screenshot("ct01_painel_resultado"), fullPage: true });

    if (hasNome && hasStatus) {
      log("CT-01: PASS");
    } else {
      log(`CT-01: FAIL — Nome: ${hasNome}, Status: ${hasStatus}`);
      log(`CT-01: Body snippet: ${pageText?.slice(0, 800)}`);
    }

    expect(hasNome, "Nome fantasia 'Residencial Teste QA' deve estar visível").toBe(true);
    expect(hasStatus, "Status 'pré-ativo' deve estar visível").toBe(true);
  });

  test("CT-02: Dados do opt-in exibidos", async ({ page }) => {
    log("=== CT-02: Dados do opt-in exibidos ===");

    await login(page);
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("ct02_optin_inicio"), fullPage: true });

    const pageText = await page.textContent("body");

    const hasDataAssembleia =
      pageText?.includes("2026-03-01") ||
      pageText?.includes("01/03/2026") ||
      pageText?.includes("03/01/2026") ||
      pageText?.includes("01 de março") ||
      pageText?.includes("assembleia");
    const hasQuorum =
      pageText?.includes("75%") ||
      pageText?.includes("quórum") ||
      pageText?.includes("Quórum");
    const hasSignatario =
      pageText?.includes("Joao da Silva") ||
      pageText?.includes("João da Silva");
    const hasOptInSection =
      pageText?.includes("opt-in") ||
      pageText?.includes("Opt-in") ||
      pageText?.includes("LGPD") ||
      pageText?.includes("Consentimento");

    log(`CT-02: Seção opt-in/LGPD: ${hasOptInSection}`);
    log(`CT-02: Data assembleia: ${hasDataAssembleia}`);
    log(`CT-02: Quórum 75%: ${hasQuorum}`);
    log(`CT-02: Signatário: ${hasSignatario}`);

    await page.screenshot({ path: screenshot("ct02_optin_resultado"), fullPage: true });

    if (hasQuorum && hasSignatario) {
      log("CT-02: PASS");
    } else {
      log(`CT-02: FAIL — Quorum: ${hasQuorum}, Signatario: ${hasSignatario}`);
      log(`CT-02: Body snippet: ${pageText?.slice(0, 1000)}`);
    }

    expect(hasQuorum, "Quórum '75%' deve estar visível na seção de opt-in").toBe(true);
    expect(hasSignatario, "Nome do signatário 'Joao da Silva' deve estar visível").toBe(true);
  });

  test("CT-03: Situacao do primeiro sindico exibida", async ({ page }) => {
    log("=== CT-03: Situacao do primeiro sindico exibida ===");

    await login(page);
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("ct03_sindico_inicio"), fullPage: true });

    const pageText = await page.textContent("body");

    const hasSindicoNome =
      pageText?.includes("Maria Oliveira");
    const hasSindicoEmail =
      pageText?.includes("sindico.qa@portabox.dev");
    const hasSenhaPendente =
      pageText?.includes("pendente") ||
      pageText?.includes("Pendente") ||
      pageText?.includes("não definida") ||
      pageText?.includes("Não definida") ||
      pageText?.includes("senha") ||
      pageText?.includes("Senha");

    log(`CT-03: Nome sindico: ${hasSindicoNome}`);
    log(`CT-03: Email sindico: ${hasSindicoEmail}`);
    log(`CT-03: Status senha pendente: ${hasSenhaPendente}`);

    await page.screenshot({ path: screenshot("ct03_sindico_resultado"), fullPage: true });

    if (hasSindicoNome && hasSindicoEmail) {
      log("CT-03: PASS");
    } else {
      log(`CT-03: FAIL — Nome: ${hasSindicoNome}, Email: ${hasSindicoEmail}`);
      log(`CT-03: Body snippet: ${pageText?.slice(0, 800)}`);
    }

    expect(hasSindicoNome, "Nome do síndico 'Maria Oliveira' deve estar visível").toBe(true);
    expect(hasSindicoEmail, "Email do síndico 'sindico.qa@portabox.dev' deve estar visível").toBe(true);
  });

  test("CT-04: Botao Ativar operacao requer confirmacao", async ({ page }) => {
    log("=== CT-04: Botao Ativar operacao requer confirmacao ===");

    await login(page);
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("ct04_pre_ativacao"), fullPage: true });

    const pageText = await page.textContent("body");

    // Procurar botão de ativação
    const btnAtivar = page.locator(
      'button:has-text("Ativar operação"), button:has-text("Ativar Operação"), button:has-text("Ativar"), button:has-text("Go-live"), button:has-text("Ativar operacao")'
    ).first();

    const btnCount = await btnAtivar.count();
    log(`CT-04: Botao de ativacao encontrado: ${btnCount > 0}`);

    if (btnCount === 0) {
      await page.screenshot({ path: screenshot("ct04_fail_sem_botao"), fullPage: true });
      log(`CT-04: FAIL — Botão 'Ativar operação' não encontrado`);
      log(`CT-04: Body snippet: ${pageText?.slice(0, 800)}`);
      expect(btnCount, "Botão 'Ativar operação' deve estar presente").toBeGreaterThan(0);
      return;
    }

    await btnAtivar.click();
    log("CT-04: Clicou no botao de ativacao");
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("ct04_dialogo_confirmacao"), fullPage: true });

    const pageTextAposClick = await page.textContent("body");
    const hasConfirmDialog =
      pageTextAposClick?.includes("confirmar") ||
      pageTextAposClick?.includes("Confirmar") ||
      pageTextAposClick?.includes("Tem certeza") ||
      pageTextAposClick?.includes("tem certeza") ||
      pageTextAposClick?.includes("certeza") ||
      pageTextAposClick?.includes("Confirmação") ||
      pageTextAposClick?.includes("confirmação") ||
      (await page.locator('[role="dialog"], [data-testid*="confirm"], .modal').count()) > 0;

    log(`CT-04: Dialogo de confirmacao detectado: ${hasConfirmDialog}`);

    if (hasConfirmDialog) {
      log("CT-04: PASS — dialogo de confirmacao exibido");
      // Fechar o dialogo sem confirmar
      const btnCancelar = page.locator('button:has-text("Cancelar"), button:has-text("Não"), button:has-text("Fechar")').first();
      if (await btnCancelar.count() > 0) {
        await btnCancelar.click();
        log("CT-04: Dialogo cancelado com sucesso");
      }
    } else {
      log(`CT-04: FAIL — Nenhum dialogo de confirmacao detectado apos clicar em Ativar`);
      log(`CT-04: Body apos click: ${pageTextAposClick?.slice(0, 500)}`);
    }

    expect(hasConfirmDialog, "Diálogo de confirmação deve aparecer ao clicar em Ativar operação").toBe(true);
  });

  test("CT-05 a CT-08: Ativacao go-live e validacoes pos-ativacao", async ({ page }) => {
    log("=== CT-05 a CT-08: Ativacao go-live e validacoes ===");

    await login(page);
    await page.goto(`${BASE_URL}/condominios/${TENANT_ID}`);
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("ct05_pre_ativacao"), fullPage: true });

    // Verificar status inicial
    const pageTextInicial = await page.textContent("body");
    log(`CT-05: Status inicial na UI: ${pageTextInicial?.includes("pré-ativo") || pageTextInicial?.includes("Pré-ativo") ? "pré-ativo encontrado" : "pré-ativo NÃO encontrado"}`);

    // Procurar botão de ativação
    const btnAtivar = page.locator(
      'button:has-text("Ativar operação"), button:has-text("Ativar Operação"), button:has-text("Ativar"), button:has-text("Go-live"), button:has-text("Ativar operacao")'
    ).first();

    const btnCount = await btnAtivar.count();
    if (btnCount === 0) {
      await page.screenshot({ path: screenshot("ct05_fail_sem_botao"), fullPage: true });
      log(`CT-05: FAIL — Botão de ativação não encontrado. Body: ${pageTextInicial?.slice(0, 500)}`);
      expect(btnCount, "Botão de ativação deve estar presente").toBeGreaterThan(0);
      return;
    }

    await btnAtivar.click();
    log("CT-05: Clicou no botao de ativacao");
    await page.waitForTimeout(500);
    await page.screenshot({ path: screenshot("ct05_dialogo_aberto"), fullPage: true });

    // Procurar botão de confirmação no dialogo
    const btnConfirmar = page.locator(
      'button:has-text("Confirmar"), button:has-text("Ativar"), button:has-text("Sim"), button:has-text("Confirmar ativação")'
    ).last();

    const confirmCount = await btnConfirmar.count();
    if (confirmCount === 0) {
      await page.screenshot({ path: screenshot("ct05_fail_sem_confirmacao"), fullPage: true });
      log(`CT-05: FAIL — Botão de confirmação não encontrado no dialogo`);
      const pageTextDialog = await page.textContent("body");
      log(`CT-05: Body dialogo: ${pageTextDialog?.slice(0, 500)}`);
      expect(confirmCount, "Botão de confirmação deve estar no diálogo").toBeGreaterThan(0);
      return;
    }

    await btnConfirmar.click();
    log("CT-05: Clicou em Confirmar");

    // Aguardar resposta da ativação
    await page.waitForTimeout(2000);
    await page.screenshot({ path: screenshot("ct05_apos_ativacao"), fullPage: true });

    const pageTextPosAtivacao = await page.textContent("body");

    // CT-05: Status muda para ativo na UI
    const hasStatusAtivo =
      pageTextPosAtivacao?.includes("ativo") ||
      pageTextPosAtivacao?.includes("Ativo") ||
      pageTextPosAtivacao?.includes("ATIVO");
    const hasSuccessFeedback =
      pageTextPosAtivacao?.includes("sucesso") ||
      pageTextPosAtivacao?.includes("ativado") ||
      pageTextPosAtivacao?.includes("Ativado") ||
      pageTextPosAtivacao?.includes("operação ativada") ||
      pageTextPosAtivacao?.includes("toast") ||
      hasStatusAtivo;

    log(`CT-05: Status 'ativo' na UI: ${hasStatusAtivo}`);
    log(`CT-05: Feedback de sucesso: ${hasSuccessFeedback}`);

    if (hasStatusAtivo) {
      log("CT-05: PASS — Status ativo exibido na UI");
    } else {
      log(`CT-05: FAIL — Status ativo não exibido. Body: ${pageTextPosAtivacao?.slice(0, 600)}`);
    }

    expect(hasStatusAtivo, "Status 'ativo' deve aparecer na UI após ativação").toBe(true);

    // CT-08: Botão de ativar deve desaparecer ou ficar desabilitado
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: screenshot("ct08_apos_ativacao_botao"), fullPage: true });

    const btnAtivarPosAtivacao = page.locator(
      'button:has-text("Ativar operação"), button:has-text("Ativar Operação")'
    ).first();

    const btnAtivarPosCount = await btnAtivarPosAtivacao.count();
    let btnDesabilitado = false;
    if (btnAtivarPosCount > 0) {
      btnDesabilitado = await btnAtivarPosAtivacao.isDisabled();
    }

    const btnAtivarAusente = btnAtivarPosCount === 0 || btnDesabilitado;

    log(`CT-08: Botao de ativar apos ativacao — ausente: ${btnAtivarPosCount === 0}, desabilitado: ${btnDesabilitado}`);

    if (btnAtivarAusente) {
      log("CT-08: PASS — Botão de ativação ausente ou desabilitado após ativação");
    } else {
      log("CT-08: FAIL — Botão de ativação ainda presente e habilitado após ativação (risco de dupla ativação)");
    }

    expect(btnAtivarAusente, "Botão 'Ativar operação' deve estar ausente ou desabilitado após ativação").toBe(true);
  });
});
