# Architecture

## Status

Foundational direction. Specific libraries and hosting services remain undecided until an active increment needs them.

## System context

```text
┌─────────────────────────────┐
│ Expo iOS application        │
│ UI, local session state,    │
│ secure token storage        │
└──────────────┬──────────────┘
               │ HTTPS / generated API client
               ▼
┌─────────────────────────────┐
│ ASP.NET Core API            │
│ Identity boundary           │
│ Fitness domain              │
│ AI coach orchestration      │
└──────────┬───────────┬──────┘
           │           │ provider SDK/API
           ▼           ▼
┌──────────────────┐  ┌──────────────────────┐
│ PostgreSQL       │  │ AI provider          │
│ Source of truth  │  │ Untrusted generation │
└──────────────────┘  └──────────────────────┘
```

## Architectural style

Use a modular monolith with feature-oriented boundaries. Begin with one deployable API and one PostgreSQL database. A module owns its behavior and data access rules, but distributed services are not introduced without demonstrated scaling, isolation, or team requirements.

Likely product modules are:

- Identity and account lifecycle.
- Profile and preferences.
- Exercise catalogue.
- Workout planning.
- Workout sessions and history.
- Progress and derived metrics.
- Coach conversations and proposals.

These names are conceptual boundaries, not authorization to create empty projects or abstractions.

## Mobile client

The client will use React Native, Expo, and TypeScript, targeting iOS first.

Responsibilities:

- Render native, accessible interaction and navigation.
- Maintain ephemeral presentation and in-progress session state.
- Store authentication material using appropriate secure platform storage.
- Validate input for immediate feedback while treating the API as authoritative.
- Degrade safely when the API or AI provider is unavailable.
- Avoid containing privileged provider credentials or authoritative business rules.

The client uses Expo Router with its stable native stack and typed routes. Route files live under `frontend/src/app`; the initial graph is deliberately limited to onboarding, workout creation, an active workout, a session summary, and route-level loading, error, and unavailable states. Additional state, form, and component libraries should be selected only when the first concrete use case demonstrates their benefit.

Android portability should be retained through standard React Native patterns. Android-specific implementation and QA are deferred.

## API

The API uses ASP.NET Core on .NET 10 LTS with nullable reference types and strict build analysis enabled. The initial implementation is a Minimal API that exposes only `GET /health`. It emits JSON console logs and records only request method, path, response status, and duration; headers, query strings, and bodies are excluded. The health route is a liveness check and deliberately does not require database connectivity. ASP.NET Core publishes the versioned OpenAPI document in Development and generates the committed contract during the build. Authentication and product endpoints remain deferred to their explicit plan increments.

Responsibilities:

- Authenticate callers and enforce resource ownership.
- Validate requests and maintain domain invariants.
- Own database transactions and persistence.
- Produce the OpenAPI contract used to generate the TypeScript client.
- Assemble minimized, authorized AI context.
- Validate AI output and mediate all proposed actions.
- Apply rate limits, timeouts, cancellation, observability, and safe error handling.

Endpoints should be thin. Business behavior belongs in cohesive feature/application services and domain types that can be tested without HTTP.

## Persistence

PostgreSQL is the source of truth, accessed through EF Core and Npgsql.

Local development uses `postgres:18.6-alpine3.24` pinned to an immutable multi-architecture image digest through Docker Compose, bound to the IPv4 loopback interface on a configurable host port and backed by a named volume. The API receives its connection string only through `ConnectionStrings__Postgres`; no connection string or real credential is committed. EF migrations are explicit and are not applied automatically at API startup. This image is local/test infrastructure only and is not an approved production database image.

Persistence integration tests use Testcontainers to start a disposable instance of the same PostgreSQL image on an isolated dynamic port. Tests apply the committed migrations and verify connectivity against PostgreSQL rather than substituting an in-memory provider. The initial migration intentionally contains no product tables; it establishes and tests the migration pipeline before the first domain model exists.

Initial rules:

- Use migrations for schema evolution.
- Test persistence behavior against PostgreSQL, not an in-memory substitute.
- Keep queries bounded and project only needed columns.
- Use UTC instants for storage and retain the user's time-zone preference where local calendar behavior matters.
- Represent measurement units explicitly and avoid lossy conversions.
- Enforce ownership and integrity in both application behavior and database constraints where appropriate.
- Do not add pgvector or another vector store until a proven retrieval use case exists.

Progress photos, if later approved, should use private object storage rather than database blobs. That capability is not currently in scope.

## API contract

The API's OpenAPI 3.1 document is the canonical transport contract. The committed contract at `contracts/FitnessCoach.Api.json` generates TypeScript route and schema types under `frontend/src/api/generated`; the mobile application reaches them through a small typed fetch client rather than handwritten DTOs. The generator is isolated under `tools/api-contract` so its supported TypeScript 5 toolchain does not conflict with Expo's TypeScript 6 compiler.

Generation is deterministic. Local and CI checks regenerate the contract and client types in a temporary directory and fail when either committed artifact differs. Runtime OpenAPI is available only in Development, and no interactive documentation UI is included.

Domain entities will not be serialized directly. Transport types should expose only the data required by the client.

See [ADR-0005](adr/0005-api-contract-workflow.md).

## AI coach boundary

Application code will depend on a product-level interface such as an AI coach service, not on provider-specific response objects throughout the codebase.

The orchestration flow is:

1. Authenticate and authorize the user request.
2. Classify the request for scope and immediate safety handling.
3. Load only the approved, relevant context.
4. Create the versioned provider request with bounded tools and output.
5. Validate the provider response structurally and semantically.
6. Return advice or a proposed action.
7. Require explicit confirmation before applying a consequential action.
8. Apply the action through ordinary deterministic application services.

Conversation memory should be based on application-owned messages and structured summaries as needed. A provider conversation identifier may be retained as implementation metadata, but it must not become the only copy of user-visible conversation history.

See [ai-safety.md](ai-safety.md) and [ADR-0003](adr/0003-ai-coach-boundary.md).

## Authentication and authorization

The identity provider has not been selected. The eventual design should use established OAuth 2.0/OpenID Connect flows suitable for native applications and allow the API to validate access tokens without custom cryptography.

Requirements:

- Secure platform storage for refresh or session credentials.
- Short-lived access where practical and safe revocation behavior.
- Explicit ownership checks on every user-owned resource.
- Account deletion and data-export design before public beta.
- No authentication secrets embedded in the mobile bundle.

An ADR is required before implementing identity because provider choice affects user experience, local development, recurring cost, and account lifecycle.

## Offline behavior

Core workout logging should eventually tolerate ordinary mobile interruptions and temporary loss of connectivity. The exact synchronization model is deferred until the session data model is concrete.

Before implementation, define:

- What can be created and edited offline.
- Stable client-generated identifiers if required.
- Retry and idempotency behavior.
- Conflict policy and user-visible recovery.
- Protection of locally cached sensitive data.

## Observability

The API should use structured logs, traces, and metrics with correlation identifiers. Sensitive bodies, access tokens, raw prompts, and health context must not be logged by default.

Useful eventual signals include:

- API latency and error rate by route.
- Important query latency and row counts.
- Client crashes and responsiveness.
- AI provider latency, time to first useful output, token usage, refusal/safety outcome, and schema-validation failures.
- Accepted versus dismissed AI proposals, without recording unnecessary sensitive content.

Provider selection for monitoring is deferred.

## Deployment direction

Local development will use containerized PostgreSQL. The API should be container-friendly, but the hosting provider is intentionally unresolved. Avoid introducing cloud-specific services until deployment requirements and budget are known.

## Security boundaries

- Mobile input is untrusted.
- AI input and output are untrusted.
- Provider webhooks, if introduced, are untrusted until verified.
- PostgreSQL and object storage are private infrastructure, never accessed directly by the mobile client.
- Secrets belong in local or hosted secret stores.
- Test and demo environments use synthetic data.

## Decisions intentionally deferred

- Identity provider.
- Cloud and region.
- AI provider and model.
- Client state and UI libraries.
- Offline synchronization implementation.
- Analytics and crash reporting.
- Object storage.
- Background job mechanism.
