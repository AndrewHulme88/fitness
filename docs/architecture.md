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
- Maintain presentation state and a bounded durable copy of the active session.
- Store authentication material using appropriate secure platform storage.
- Validate input for immediate feedback while treating the API as authoritative.
- Degrade safely when the API or AI provider is unavailable.
- Avoid containing privileged provider credentials or authoritative business rules.

The client uses Expo Router with its stable native stack and typed routes. Route files live under `frontend/src/app`; the current graph covers onboarding, the saved workout list and editor, active-workout logging, completion summary, and route-level loading, error, and unavailable states. Expo SQLite retains the local prototype profile association and recoverable session payload; Expo Crypto supplies stable UUIDs. This association is convenience state, not identity or authorization. Additional state, form, and component libraries should be selected only when the first concrete use case demonstrates their benefit.

Android portability should be retained through standard React Native patterns. Android-specific implementation and QA are deferred.

## API

The API uses ASP.NET Core on .NET 10 LTS with nullable reference types and strict build analysis enabled. It is a Minimal API with feature-oriented modules. `GET /health` is a liveness check and deliberately does not require database connectivity. The Profile, Exercise, Workout, and Session features expose development-only endpoints for the unauthenticated local prototype; production does not map those routes. It emits JSON console logs and records only request method, path, response status, and duration; headers, query strings, and bodies are excluded. ASP.NET Core publishes the versioned OpenAPI document in Development and generates the committed contract during a build that explicitly uses the Development environment.

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

Persistence integration tests use Testcontainers to start a disposable instance of the same PostgreSQL image on an isolated dynamic port. Tests apply the committed migrations and verify connectivity against PostgreSQL rather than substituting an in-memory provider. The Profile feature stores one profile row plus normalized goal and equipment selections. The Exercise feature stores catalogue entries plus searchable aliases, equipment, muscles, and versioned import state. The Workout feature stores reusable profile-owned plans and ordered exercise prescriptions. The Session feature stores immutable plan snapshots, bounded mutable sets, canonical actuals, notes, completion state, and idempotent mutation metadata. Enum values are stored as readable strings, and database checks and composite keys reinforce the API's supported values and uniqueness rules.

Initial rules:

- Use migrations for schema evolution.
- Test persistence behavior against PostgreSQL, not an in-memory substitute.
- Keep queries bounded and project only needed columns.
- Use UTC instants for storage and retain the user's time-zone preference where local calendar behavior matters.
- Represent measurement units explicitly and avoid lossy conversions.
- Enforce ownership and integrity in both application behavior and database constraints where appropriate.
- Do not add pgvector or another vector store until a proven retrieval use case exists.

Progress photos, if later approved, should use private object storage rather than database blobs. That capability is not currently in scope.

## Exercise catalogue

The project owns a curated manifest embedded in the Exercises feature. An explicit command validates the complete file before importing it into PostgreSQL; API startup never seeds or migrates the database automatically. PostgreSQL remains the runtime source of truth.

The initial catalogue contains 35 common strength and cardio entries. Stable UUIDs are identity; names and slugs are presentation and search fields. Searchable and filterable relationships are normalized, while bounded setup, execution, and safety instructions remain scalar text. Search uses escaped case-insensitive matching, deterministic ordering, equipment-subset semantics, and a maximum page size of 50.

Equipment is a genuine shared domain vocabulary used by onboarding and the catalogue. Other exercise taxonomies remain owned by the Exercises feature. Media and custom exercises are not represented until their licensing, storage, accessibility, ownership, and lifecycle behavior are approved.

The manifest is marked as requiring qualified exercise-content review. Structural validation and code review are not a substitute for that release gate. See [exercise-catalogue-policy.md](exercise-catalogue-policy.md) and [ADR-0007](adr/0007-internal-exercise-catalogue.md).

## Workout planning

Workout plans are reusable profile-owned templates, not completed sessions. A plan contains a bounded name, monotonic revision, UTC timestamps, and 1–20 unique curated exercises in explicit order. Each planned exercise stores sets and only the repetitions, load, duration, or distance targets permitted by its catalogue tracking mode.

PostgreSQL stores canonical kilograms, metres, and seconds; the mobile boundary converts those values into the profile's metric or imperial display units. The planner never invents target values. API validation is authoritative, with matching client validation for immediate feedback and database checks for numeric and relational integrity.

List, create, detail, and update routes are profile-scoped and Development-only until authentication exists. Updates use optimistic revisions and return `409` for stale edits. The current mobile profile identifier is local prototype state and must not be treated as authorization. Delete, archive, offline plan editing, and generated progression remain deferred.

The mobile list and editor use compact rows and restrained dividers. Exercise discovery and target editing use focused sheets. Drag handles support long-press reordering, while VoiceOver users receive equivalent adjustable move actions. See [ADR-0008](adr/0008-reusable-workout-templates.md).

## Active workout sessions

Starting a workout creates a new session from one exact workout-plan revision. Snapshot fields preserve the plan name, exercise identity and order, tracking mode, muscles, and prescriptions; template edits never rewrite a session. PostgreSQL permits only one active session per profile. Completed sessions are immutable until the later history phase intentionally defines correction behavior.

Actual values use the exercise tracking mode: repetitions; repetitions plus load; duration; distance plus duration; or distance, duration, and load. Planned values remain visible suggestions and are never silently recorded as actuals. Users can explicitly complete or correct a set, add or remove a set, skip an exercise, and add session or exercise notes. Canonical storage remains kilograms, metres, and seconds.

The client stores the bounded session document in Expo SQLite after each interaction and synchronizes it to a profile-scoped API route. Client-generated session, set, and mutation UUIDs make creation and retries stable. Full-document updates use an optimistic revision plus mutation idempotency; a conflict retains the local copy and requires an explicit user choice before loading the server copy. See [ADR-0009](adr/0009-recoverable-workout-sessions.md).

## API contract

The API's OpenAPI 3.1 document is the canonical transport contract. The committed contract at `contracts/FitnessCoach.Api.json` generates TypeScript route and schema types under `frontend/src/api/generated`; the mobile application reaches them through a small typed fetch client rather than handwritten DTOs. The generator is isolated under `tools/api-contract` so its supported TypeScript 5 toolchain does not conflict with Expo's TypeScript 6 compiler.

Generation is deterministic. Local and CI checks regenerate the contract and client types in a temporary directory and fail when either committed artifact differs. Runtime OpenAPI is available only in Development, and no interactive documentation UI is included.

Domain entities will not be serialized directly. Transport types should expose only the data required by the client.

The initial profile contract deliberately contains only training goals, experience, available equipment, and unit preference. It contains no medical notes, injury history, body measurements, or free-form text. Exercise exclusion is deferred until the exercise catalogue supplies stable identifiers. See [ADR-0006](adr/0006-minimum-onboarding-profile.md).

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

The identity provider has not been selected. The eventual design should use established OAuth 2.0/OpenID Connect flows suitable for native applications and allow the API to validate access tokens without custom cryptography. A database readiness probe must accompany the first deployable database-backed routes; it is not required for the development-only profile prototype.

Requirements:

- Secure platform storage for refresh or session credentials.
- Short-lived access where practical and safe revocation behavior.
- Explicit ownership checks on every user-owned resource.
- Account deletion and data-export design before public beta.
- No authentication secrets embedded in the mobile bundle.

An ADR is required before implementing identity because provider choice affects user experience, local development, recurring cost, and account lifecycle. Until that work is complete, user-owned prototype routes must remain unavailable outside Development and no local identifier is treated as authorization.

## Offline behavior

After an online start, active-session set edits, notes, skips, set-count changes, timer state, and completion are durable locally and can be made during temporary network loss. Synchronization retries when requested or when the app becomes active. Starting and destructive discard require the API. A completion must synchronize before its recoverable local copy is cleared or another session begins.

The current app-sandbox cache contains fitness data and must be treated as sensitive. It is suitable only for the Development-only, unauthenticated prototype. Authentication, platform data-protection review, backup behavior, multi-device conflict policy, account deletion, and cache cleanup remain release gates rather than implied properties of this implementation.

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
- Multi-device synchronization and authenticated local-cache lifecycle.
- Analytics and crash reporting.
- Object storage.
- Background job mechanism.
