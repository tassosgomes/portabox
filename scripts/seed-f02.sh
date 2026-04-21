#!/usr/bin/env bash
# Seed script for F02 performance smoke test.
# Creates one bloco and N unidades in a given condominio tenant.
# Usage: ./scripts/seed-f02.sh --condominio <id> --token <bearer_token> [--unidades 300] [--api http://localhost:5000]
#
# The script creates units in a single block named "Perf Block".
# Unit numbers are generated as <floor><seq> (e.g., 1101..1900, 2101..2900, ...).
# p95 latency is measured with 20 iterations of GET /estrutura after seeding.

set -euo pipefail

API_URL="http://localhost:5000"
TOKEN=""
CONDOMINIO_ID=""
N_UNIDADES=300
BLOCO_NOME="Perf Block"

usage() {
  echo "Usage: $0 --condominio <id> --token <bearer_token> [--unidades N] [--api URL]"
  exit 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --condominio) CONDOMINIO_ID="$2"; shift 2 ;;
    --token)      TOKEN="$2"; shift 2 ;;
    --unidades)   N_UNIDADES="$2"; shift 2 ;;
    --api)        API_URL="$2"; shift 2 ;;
    *) usage ;;
  esac
done

[[ -z "$CONDOMINIO_ID" ]] && { echo "ERROR: --condominio is required"; usage; }
[[ -z "$TOKEN" ]] && { echo "ERROR: --token is required"; usage; }

BASE_URL="${API_URL}/api/v1/condominios/${CONDOMINIO_ID}"
AUTH_HEADER="Authorization: Bearer ${TOKEN}"

echo "==> Creating bloco '${BLOCO_NOME}'..."
BLOCO_RESP=$(curl -sf -X POST "${BASE_URL}/blocos" \
  -H "Content-Type: application/json" \
  -H "${AUTH_HEADER}" \
  -d "{\"nome\": \"${BLOCO_NOME}\"}" || true)

if [[ -z "$BLOCO_RESP" ]]; then
  echo "  Bloco already exists or creation failed silently; fetching from tree..."
  TREE=$(curl -sf "${BASE_URL}/estrutura" -H "${AUTH_HEADER}")
  BLOCO_ID=$(echo "$TREE" | python3 -c "import sys,json; d=json.load(sys.stdin); print(next(b['id'] for b in d['blocos'] if b['nome']=='${BLOCO_NOME}'))" 2>/dev/null || true)
else
  BLOCO_ID=$(echo "$BLOCO_RESP" | python3 -c "import sys,json; print(json.load(sys.stdin)['id'])" 2>/dev/null || true)
fi

if [[ -z "$BLOCO_ID" ]]; then
  echo "ERROR: Could not determine bloco id."
  exit 1
fi

echo "  Bloco id: ${BLOCO_ID}"
echo "==> Seeding ${N_UNIDADES} unidades..."

CREATED=0
SKIPPED=0
ANDAR=1
SEQ=1

for ((i = 1; i <= N_UNIDADES; i++)); do
  NUMERO="${SEQ}"
  HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST "${BASE_URL}/blocos/${BLOCO_ID}/unidades" \
    -H "Content-Type: application/json" \
    -H "${AUTH_HEADER}" \
    -d "{\"andar\": ${ANDAR}, \"numero\": \"${NUMERO}\"}")

  if [[ "$HTTP_STATUS" == "201" ]]; then
    CREATED=$((CREATED + 1))
  else
    SKIPPED=$((SKIPPED + 1))
  fi

  SEQ=$((SEQ + 1))
  # Keep numero ≤ 4 digits; roll to next floor
  if [[ $SEQ -gt 9999 ]]; then
    ANDAR=$((ANDAR + 1))
    SEQ=1
  fi

  if ((i % 50 == 0)); then
    echo "  Progress: ${i}/${N_UNIDADES} (created=${CREATED}, skipped=${SKIPPED})"
  fi
done

echo "==> Seeding complete. Created=${CREATED}, Skipped/Conflict=${SKIPPED}"

echo ""
echo "==> Measuring p95 latency for GET /estrutura (20 iterations)..."

LATENCIES=()
for ((j = 1; j <= 20; j++)); do
  T=$(curl -o /dev/null -sf -w "%{time_total}" \
    "${BASE_URL}/estrutura" \
    -H "${AUTH_HEADER}")
  LATENCIES+=("$T")
done

# Sort and compute p95 (index 19 in 0-based sorted array of 20 values)
P95=$(printf "%s\n" "${LATENCIES[@]}" | sort -n | awk 'NR==19{print $1}')

echo "  Latencies (seconds): ${LATENCIES[*]}"
echo ""
echo "  p95 latency: ${P95}s"

P95_MS=$(echo "$P95 * 1000" | bc -l | xargs printf "%.0f")
echo "  p95 latency: ${P95_MS}ms"

if (( $(echo "$P95 < 0.5" | bc -l) )); then
  echo "  ✅ PASS — p95 < 500ms"
else
  echo "  ❌ FAIL — p95 >= 500ms (target: < 500ms)"
  exit 1
fi
