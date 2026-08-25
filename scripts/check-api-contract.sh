#!/usr/bin/env bash

set -euo pipefail

contract_repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
contract_check_directory="$(mktemp -d "${TMPDIR:-/tmp}/fitness-coach-contract.XXXXXX")"

cleanup_contract_check() {
  rm -rf "${contract_check_directory}"
}

trap cleanup_contract_check EXIT

generated_contract_directory="${contract_check_directory}/contracts"
generated_client_file="${contract_check_directory}/schema.ts"

bash "${contract_repository_root}/scripts/generate-api-contract.sh" \
  "${generated_contract_directory}" \
  "${generated_client_file}"

contract_has_drift=false

if ! diff -u \
  "${contract_repository_root}/contracts/FitnessCoach.Api.json" \
  "${generated_contract_directory}/FitnessCoach.Api.json"; then
  contract_has_drift=true
fi

if ! diff -u \
  "${contract_repository_root}/frontend/src/api/generated/schema.ts" \
  "${generated_client_file}"; then
  contract_has_drift=true
fi

if [[ "${contract_has_drift}" == true ]]; then
  echo "API contract drift detected. Run: bash scripts/generate-api-contract.sh" >&2
  exit 1
fi

echo "API contract and generated TypeScript types are current."
