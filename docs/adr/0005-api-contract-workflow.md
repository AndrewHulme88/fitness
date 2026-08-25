# ADR-0005: Generate the mobile API client from committed OpenAPI

- Status: Accepted
- Date: 2026-08-25

## Context

The ASP.NET Core API and TypeScript mobile client need one transport contract. Maintaining request and response models independently would allow the two applications to compile while disagreeing at runtime.

The Expo client uses TypeScript 6. The selected `openapi-typescript` release supports TypeScript 5, so its generator cannot share the client's dependency graph without an unsupported peer override.

## Decision

The ASP.NET Core API owns a versioned OpenAPI 3.1 document generated at build time. Commit the document at `contracts/FitnessCoach.Api.json` and generate route and schema types at `frontend/src/api/generated/schema.ts`.

Use `openapi-typescript` in an isolated, lockfile-pinned tool package under `tools/api-contract`. The generated output is consumed by the TypeScript 6 client through a small `openapi-fetch` wrapper; transport types are not duplicated by hand.

Expose `/openapi/v1.json` only in the Development environment. Regeneration is explicit, while the local and CI drift check regenerates into a temporary directory and compares both committed artifacts without modifying them.

## Consequences

- Every endpoint must provide deliberate OpenAPI metadata and stable operation identifiers.
- Contract changes and the matching generated TypeScript change are reviewed together.
- CI rejects stale contract or generated-client artifacts.
- Developers install both frontend dependencies and the isolated generator dependencies before regeneration.
- The `openapi-fetch` dependency should be reassessed if its maintenance status, security posture, or product requirements change.

## Alternatives considered

- Handwritten duplicate TypeScript DTOs were rejected because drift would be discovered only at runtime.
- Suppressing `openapi-typescript`'s TypeScript peer check or downgrading the Expo compiler was rejected because either choice would move outside a supported dependency graph.
- A TypeScript 6-compatible full-client generator was evaluated, but its available dependency lines introduced current generator or YAML parser advisories.
- Publishing runtime OpenAPI in every environment was rejected because production does not currently need to expose implementation metadata.
