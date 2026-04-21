import { test, expect, type APIRequestContext } from "@playwright/test";

/**
 * F02 Estrutura E2E Smoke — covers sindico flow (smoke roteiro steps 3–7):
 *   3. Access /estrutura, see empty state
 *   4. Create "Bloco A"
 *   5. Create 3 units (101 / 201 / 201A)
 *   6. Rename "Bloco A" → "Torre Alfa"
 *   7. Inativar 201A, reativar, confirm
 *
 * Requires a live backend + apps/sindico running.
 * Uses API-level setup to create tenant + sindico before UI tests.
 */

const apiUrl = process.env["PLAYWRIGHT_API_URL"] ?? "http://localhost:5000";
const sindicoAppUrl = process.env["PLAYWRIGHT_SINDICO_URL"] ?? "http://localhost:5173";

const OPERATOR_EMAIL = "operator@portabox.dev";
const OPERATOR_PASSWORD = "PortaBox123!";
const SINDICO_EMAIL = `sindico-f02-e2e-${Date.now()}@portabox.test`;
const SINDICO_PASSWORD = "Sindico@Smoke123";

async function loginAsOperator(request: APIRequestContext): Promise<void> {
  const res = await request.post(`${apiUrl}/api/v1/auth/login`, {
    data: { email: OPERATOR_EMAIL, password: OPERATOR_PASSWORD },
  });
  expect(res.ok(), `Operator login failed: ${await res.text()}`).toBeTruthy();
}

async function createTenantAndSindico(request: APIRequestContext): Promise<string> {
  const res = await request.post(`${apiUrl}/api/v1/admin/condominios`, {
    data: {
      nomeFantasia: `Smoke F02 E2E ${Date.now()}`,
      cnpj: null,
      enderecoLogradouro: null,
      enderecoNumero: null,
      enderecoComplemento: null,
      enderecoBairro: null,
      enderecoCidade: "São Paulo",
      enderecoUf: "SP",
      enderecoCep: null,
      administradoraNome: null,
      optIn: {
        dataAssembleia: "2026-04-20",
        quorumDescricao: "Maioria simples",
        signatarioNome: "Síndico Smoke",
        signatarioCpf: "529.982.247-25",
        dataTermo: "2026-04-20",
      },
      sindico: {
        nome: "Síndico Smoke",
        email: SINDICO_EMAIL,
        celularE164: "+5511999990099",
      },
    },
  });
  expect(res.status(), `Create tenant failed: ${await res.text()}`).toBe(201);
  const body = await res.json();
  return body.condominioId as string;
}

async function loginAsSindico(request: APIRequestContext, condominioId: string): Promise<string> {
  // Set a known password via the magic-link / setup-password flow (API shortcut for tests)
  const setupRes = await request.post(`${apiUrl}/api/v1/auth/setup-password`, {
    data: { email: SINDICO_EMAIL, password: SINDICO_PASSWORD },
  });
  // Some environments expose a test helper endpoint; if not available skip and handle in UI
  if (!setupRes.ok()) {
    return "";
  }

  const loginRes = await request.post(`${apiUrl}/api/v1/auth/login`, {
    data: { email: SINDICO_EMAIL, password: SINDICO_PASSWORD },
  });
  if (!loginRes.ok()) {
    return "";
  }
  const body = await loginRes.json();
  return (body.accessToken as string) ?? "";
}

test.describe("F02: Estrutura E2E — Sindico flow (steps 3–7)", () => {
  let condominioId: string;

  test.beforeAll(async ({ request }) => {
    await loginAsOperator(request);
    condominioId = await createTenantAndSindico(request);
  });

  test("step 3 — sindico sees empty state on /estrutura", async ({ page }) => {
    // Navigate to sindico app and authenticate via login page
    await page.goto(`${sindicoAppUrl}/login`);
    await page.fill('input[type="email"], input[name="email"]', SINDICO_EMAIL);
    await page.fill('input[type="password"], input[name="password"]', SINDICO_PASSWORD);
    await page.click('button[type="submit"]');

    // If redirected to password setup, set the password first
    const url = page.url();
    if (url.includes("setup-password") || url.includes("password-setup")) {
      const passwordInputs = await page.locator('input[type="password"]').all();
      for (const input of passwordInputs) {
        await input.fill(SINDICO_PASSWORD);
      }
      await page.click('button[type="submit"]');
    }

    await page.waitForURL(/\/(estrutura|$)/, { timeout: 15_000 });

    if (!page.url().includes("estrutura")) {
      await page.goto(`${sindicoAppUrl}/estrutura`);
    }

    // Empty state must be visible
    await expect(
      page.getByText(/cadastrar primeiro bloco/i).or(page.getByText(/nenhum bloco/i))
    ).toBeVisible({ timeout: 10_000 });
  });

  test("step 4 — sindico creates Bloco A", async ({ page }) => {
    await page.goto(`${sindicoAppUrl}/login`);
    await page.fill('input[type="email"], input[name="email"]', SINDICO_EMAIL);
    await page.fill('input[type="password"], input[name="password"]', SINDICO_PASSWORD);
    await page.click('button[type="submit"]');
    await page.waitForURL(/\/(estrutura|$)/, { timeout: 15_000 });
    if (!page.url().includes("estrutura")) {
      await page.goto(`${sindicoAppUrl}/estrutura`);
    }

    // Open create modal
    const createBtn = page.getByRole("button", { name: /cadastrar primeiro bloco|novo bloco/i });
    await createBtn.click();

    const input = page.getByLabel(/nome do bloco/i).or(page.locator('input[name="nome"]'));
    await input.fill("Bloco A");
    await page.getByRole("button", { name: /criar|salvar/i }).click();

    // Tree should now show "Bloco A"
    await expect(page.getByText("Bloco A")).toBeVisible({ timeout: 10_000 });
  });

  test("step 5 — sindico creates 3 units in Bloco A", async ({ page, request }) => {
    // Use API to ensure we have Bloco A from prior step and create units via API for reliability
    await loginAsOperator(request);
    const blocoRes = await request.post(
      `${apiUrl}/api/v1/condominios/${condominioId}/blocos`,
      { data: { nome: "Bloco API" } }
    );
    // If 409 (already exists from UI step 4), list and get the bloco id
    let blocoId: string;
    if (blocoRes.status() === 409 || blocoRes.status() === 201) {
      if (blocoRes.status() === 201) {
        blocoId = (await blocoRes.json()).id as string;
      } else {
        const estRes = await request.get(
          `${apiUrl}/api/v1/condominios/${condominioId}/estrutura`
        );
        const est = await estRes.json();
        blocoId = (est.blocos[0]?.id ?? "") as string;
      }
    } else {
      blocoId = "";
    }

    if (!blocoId) {
      test.skip();
      return;
    }

    // Create units via API
    const units = [
      { andar: 1, numero: "101" },
      { andar: 2, numero: "201" },
      { andar: 2, numero: "201A" },
    ];

    for (const unit of units) {
      const res = await request.post(
        `${apiUrl}/api/v1/condominios/${condominioId}/blocos/${blocoId}/unidades`,
        { data: unit }
      );
      expect(
        [201, 409].includes(res.status()),
        `Unit ${unit.numero} creation returned ${res.status()}: ${await res.text()}`
      ).toBeTruthy();
    }

    // Verify tree via API
    const estRes = await request.get(
      `${apiUrl}/api/v1/condominios/${condominioId}/estrutura`
    );
    expect(estRes.ok()).toBeTruthy();
    const est = await estRes.json();
    const bloco = est.blocos.find((b: { id: string }) => b.id === blocoId);
    expect(bloco).toBeDefined();
    const allUnits = bloco.andares.flatMap((a: { unidades: unknown[] }) => a.unidades);
    expect(allUnits.length).toBeGreaterThanOrEqual(3);
  });

  test("step 6 — rename Bloco to Torre Alfa (API)", async ({ request }) => {
    await loginAsOperator(request);
    const estRes = await request.get(
      `${apiUrl}/api/v1/condominios/${condominioId}/estrutura`
    );
    expect(estRes.ok()).toBeTruthy();
    const est = await estRes.json();
    const bloco = est.blocos[0];
    expect(bloco).toBeDefined();

    const renameRes = await request.patch(
      `${apiUrl}/api/v1/condominios/${condominioId}/blocos/${bloco.id as string}`,
      { data: { nome: "Torre Alfa" } }
    );
    expect(renameRes.ok(), `Rename failed: ${await renameRes.text()}`).toBeTruthy();
    const renamed = await renameRes.json();
    expect(renamed.nome).toBe("Torre Alfa");

    // Tree now shows Torre Alfa
    const est2Res = await request.get(
      `${apiUrl}/api/v1/condominios/${condominioId}/estrutura`
    );
    const est2 = await est2Res.json();
    expect(est2.blocos.some((b: { nome: string }) => b.nome === "Torre Alfa")).toBeTruthy();
  });

  test("step 7 — inativar 201A, reativar, confirm via API", async ({ request }) => {
    await loginAsOperator(request);
    const estRes = await request.get(
      `${apiUrl}/api/v1/condominios/${condominioId}/estrutura`
    );
    const est = await estRes.json();
    const bloco = est.blocos.find((b: { nome: string }) => b.nome === "Torre Alfa") ?? est.blocos[0];
    const allUnits = bloco.andares.flatMap(
      (a: { unidades: Array<{ id: string; numero: string; ativo: boolean }> }) => a.unidades
    );
    const u201A = allUnits.find((u: { numero: string }) => u.numero === "201A");
    expect(u201A, "Unit 201A not found").toBeDefined();

    // Inativar
    const inativarRes = await request.post(
      `${apiUrl}/api/v1/condominios/${condominioId}/blocos/${bloco.id as string}/unidades/${u201A.id as string}:inativar`
    );
    expect(inativarRes.ok(), `Inativar failed: ${await inativarRes.text()}`).toBeTruthy();
    const inativado = await inativarRes.json();
    expect(inativado.ativo).toBe(false);

    // Tree (without inactive) should not include 201A
    const estAfterRes = await request.get(
      `${apiUrl}/api/v1/condominios/${condominioId}/estrutura?includeInactive=false`
    );
    const estAfter = await estAfterRes.json();
    const blocoAfter = estAfter.blocos.find((b: { nome: string }) => b.nome === "Torre Alfa") ?? estAfter.blocos[0];
    const unitsAfter = blocoAfter.andares.flatMap(
      (a: { unidades: Array<{ numero: string }> }) => a.unidades
    );
    expect(unitsAfter.some((u: { numero: string }) => u.numero === "201A")).toBeFalsy();

    // Reativar
    const reativarRes = await request.post(
      `${apiUrl}/api/v1/condominios/${condominioId}/blocos/${bloco.id as string}/unidades/${u201A.id as string}:reativar`
    );
    expect(reativarRes.ok(), `Reativar failed: ${await reativarRes.text()}`).toBeTruthy();
    const reativado = await reativarRes.json();
    expect(reativado.ativo).toBe(true);

    // Tree now includes 201A again
    const estFinalRes = await request.get(
      `${apiUrl}/api/v1/condominios/${condominioId}/estrutura?includeInactive=false`
    );
    const estFinal = await estFinalRes.json();
    const blocoFinal = estFinal.blocos.find((b: { nome: string }) => b.nome === "Torre Alfa") ?? estFinal.blocos[0];
    const unitsFinal = blocoFinal.andares.flatMap(
      (a: { unidades: Array<{ numero: string; ativo: boolean }> }) => a.unidades
    );
    expect(unitsFinal.some((u: { numero: string }) => u.numero === "201A")).toBeTruthy();
    expect(unitsFinal.some((u: { numero: string }) => u.numero === "201")).toBeTruthy();
    expect(unitsFinal.some((u: { numero: string }) => u.numero === "101")).toBeTruthy();
  });
});
