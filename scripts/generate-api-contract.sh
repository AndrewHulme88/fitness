#!/usr/bin/env bash

set -euo pipefail

contract_repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
contract_output_directory="${1:-${contract_repository_root}/contracts}"
client_output_file="${2:-${contract_repository_root}/frontend/src/api/generated/schema.ts}"
api_project="${contract_repository_root}/backend/FitnessCoach.Api/FitnessCoach.Api.csproj"
generator="${contract_repository_root}/tools/api-contract/node_modules/.bin/openapi-typescript"
formatter="${contract_repository_root}/frontend/node_modules/.bin/prettier"

if [[ ! -x "${generator}" ]]; then
  echo "OpenAPI generator dependencies are missing. Run: npm ci --prefix tools/api-contract" >&2
  exit 1
fi

if [[ ! -x "${formatter}" ]]; then
  echo "Frontend dependencies are missing. Run: npm ci --prefix frontend" >&2
  exit 1
fi

mkdir -p "${contract_output_directory}" "$(dirname "${client_output_file}")"

DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false \
ASPNETCORE_ENVIRONMENT=Development dotnet build "${api_project}" \
  --configuration Release \
  --disable-build-servers \
  --no-incremental \
  --no-restore \
  -p:OpenApiGenerateDocuments=true \
  -p:OpenApiDocumentsDirectory="${contract_output_directory}"

"${generator}" \
  "${contract_output_directory}/FitnessCoach.Api.json" \
  --output "${client_output_file}"

"${formatter}" --write \
  "${contract_output_directory}/FitnessCoach.Api.json" \
  "${client_output_file}"
