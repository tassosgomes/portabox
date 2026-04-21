#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(dirname "$0")"
PACKAGE_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
CONTRACT_PATH="$PACKAGE_DIR/../../.compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml"
OUTPUT_PATH="$PACKAGE_DIR/src/generated.ts"
TMP_PATH="$PACKAGE_DIR/src/generated.tmp.ts"

cd "$PACKAGE_DIR"

pnpm exec openapi-typescript "$CONTRACT_PATH" -o "$TMP_PATH"

node - "$TMP_PATH" "$OUTPUT_PATH" <<'EOF'
const fs = require('node:fs')

const [inputPath, outputPath] = process.argv.slice(2)
const generated = fs.readFileSync(inputPath, 'utf8').replace(/\r\n/g, '\n')
const header = [
  '// Generated from .compozy/tasks/f02-gestao-blocos-unidades/api-contract.yaml.',
  '// Regenerate with: pnpm --filter @portabox/api-client generate:types',
  '',
].join('\n')

fs.writeFileSync(outputPath, `${header}${generated}`, 'utf8')
fs.rmSync(inputPath)
EOF
