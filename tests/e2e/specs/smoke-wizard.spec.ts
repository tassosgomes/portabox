import { test, expect, type Page, type APIRequestContext } from "@playwright/test";

const apiUrl = process.env["PLAYWRIGHT_API_URL"] ?? "http://localhost:5000";

const OPERATOR_EMAIL = "operator@portabox.dev";
const OPERATOR_PASSWORD = "PortaBox123!";

const SINDICO_EMAIL = `sindico-e2e-${Date.now()}@portabox.test`;
const SINDICO_PASSWORD = "Sindico@Pass123";

async function loginAsOperator(request: APIRequestContext): Promise<void> {
  const response = await request.post(`${apiUrl}/api/v1/auth/login`, {
    data: { email: OPERATOR_EMAIL, password: OPERATOR_PASSWORD },
  });
  expect(response.ok(), `Operator login failed: ${await response.text()}`).toBeTruthy();
}

async function waitForSelector(page: Page, selector: string, timeout = 10_000) {
  await page.waitForSelector(selector, { timeout });
}

test.describe("Smoke: F01 Wizard E2E", () => {
  test.beforeEach(async ({ request }) => {
    await loginAsOperator(request);
  });

  test("operator can log in and see the condominios list", async ({ page, request }) => {
    // Login via UI
    await page.goto("/login");
    await page.fill('[data-testid="email"], input[type="email"], input[name="email"]', OPERATOR_EMAIL);
    await page.fill('[data-testid="password"], input[type="password"], input[name="password"]', OPERATOR_PASSWORD);
    await page.click('[data-testid="login-submit"], button[type="submit"]');

    // After login, should be redirected to the condominios list or dashboard
    await page.waitForURL(/\/(condominios|dashboard|admin)/, { timeout: 10_000 });
    expect(page.url()).toMatch(/\/(condominios|dashboard|admin)/);
  });

  test("operator can create a condominio via wizard (API-level smoke)", async ({ request }) => {
    const cnpj = "45.723.174/0001-10";

    // Create via API directly to verify the endpoint works E2E with a real backend
    const createResponse = await request.post(`${apiUrl}/api/v1/admin/condominios`, {
      data: {
        nomeFantasia: "Residencial Smoke E2E",
        cnpj,
        enderecoLogradouro: null,
        enderecoNumero: null,
        enderecoComplemento: null,
        enderecoBairro: null,
        enderecoCidade: "São Paulo",
        enderecoUf: "SP",
        enderecoCep: null,
        administradoraNome: null,
        optIn: {
          dataAssembleia: "2026-04-01",
          quorumDescricao: "Maioria simples",
          signatarioNome: "Carlos da Silva",
          signatarioCpf: "529.982.247-25",
          dataTermo: "2026-04-02",
        },
        sindico: {
          nome: "Sindico Smoke",
          email: SINDICO_EMAIL,
          celularE164: "+5511999880001",
        },
      },
    });

    expect(
      createResponse.status(),
      `Create condominio failed: ${await createResponse.text()}`
    ).toBe(201);

    const body = await createResponse.json();
    expect(body.condominioId).toBeTruthy();
    expect(body.sindicoUserId).toBeTruthy();

    const condominioId = body.condominioId as string;

    // Verify it appears in the list
    const listResponse = await request.get(`${apiUrl}/api/v1/admin/condominios`);
    expect(listResponse.ok()).toBeTruthy();
    const list = await listResponse.json();
    expect(list.totalCount).toBeGreaterThanOrEqual(1);

    // Verify details
    const detailsResponse = await request.get(
      `${apiUrl}/api/v1/admin/condominios/${condominioId}`
    );
    expect(detailsResponse.ok()).toBeTruthy();
    const details = await detailsResponse.json();
    expect(details.nomeFantasia).toBe("Residencial Smoke E2E");
    expect(details.status).toBe(1); // PreAtivo

    // Activate
    const activateResponse = await request.post(
      `${apiUrl}/api/v1/admin/condominios/${condominioId}:activate`,
      { data: { note: "Smoke activation" } }
    );
    expect(activateResponse.ok(), `Activate failed: ${await activateResponse.text()}`).toBeTruthy();

    // Verify activated
    const detailsAfterResponse = await request.get(
      `${apiUrl}/api/v1/admin/condominios/${condominioId}`
    );
    const detailsAfter = await detailsAfterResponse.json();
    expect(detailsAfter.status).toBe(2); // Ativo
  });
});
