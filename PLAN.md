# Execution Plan

This is the living implementation plan. Work from top to bottom in small increments, update statuses as facts change, and do not pull later infrastructure forward without a current requirement.

Status values: `pending`, `in progress`, `complete`, `blocked`.

## Phase 0 — Foundation

### 0.1 Establish project operating documents

Status: complete

Deliverables:

- Repository instructions and quality standards.
- Product brief and scope.
- Foundational architecture and ADRs.
- AI safety boundary.
- Testing and performance strategy.
- Development journal and product roadmap.

Acceptance:

- Documents agree on iOS-first delivery, the core stack, AI limitations, and the incremental workflow.
- Open decisions are recorded rather than silently assumed.
- No application dependencies or unused scaffold are introduced.

### 0.2 Resolve pre-scaffold product choices

Status: complete

Decisions:

- Retain “Fitness Coach” as the working title until naming work is useful.
- Let users choose their own goals from a supported onboarding taxonomy rather than imposing one training focus.
- Prove a local prototype before introducing authentication.
- Make onboarding → workout creation → workout logging → session summary the first complete journey.
- Exclude progress photos, nutrition, wearables, and social features from the MVP.
- Use Fitbod as a directional style reference while creating original branding and interaction design.

Acceptance:

- Answers are recorded in `DEVELOPMENT.md`.
- Any consequential decision receives an ADR.
- The MVP remains narrow enough to demonstrate end-to-end quality.

## Phase 1 — iOS client foundation

Each item below is a separate increment.

### 1.1 Scaffold the minimum Expo TypeScript application

Status: complete

- Use the current stable Expo SDK and pin dependencies with a lockfile.
- Enable strict TypeScript, linting, formatting, and a focused unit-test setup.
- Add one meaningful smoke test for the application entry point.
- Confirm the app starts in an iOS simulator.

Acceptance:

- Install, type-check, lint, test, and iOS start commands are documented and pass.
- No product screens, state libraries, or UI framework are added yet.

Verification completed on 2026-08-24:

- Expo SDK 57.0.15 and React Native 0.86.2 installed from a committed npm lockfile.
- Formatting, strict TypeScript, Expo ESLint, and the Jest component smoke test pass.
- Expo dependency compatibility check passes against the locally bundled SDK map.
- A production iOS bundle exports successfully.
- Metro launches the project in Expo Go on an iPhone 16 Plus simulator running iOS 18.6 and serves the bundle successfully.
- The open transitive dependency advisory is recorded as `I-001` in `DEVELOPMENT.md`.

### 1.2 Establish design foundations

Status: complete

- Define semantic color, spacing, typography, radius, and motion tokens.
- Use the iOS system typeface unless an intentional identity decision supersedes it.
- Create only the primitives needed for the first screen.
- Document accessibility expectations and a basic visual review checklist.

Acceptance:

- Tokens have focused tests where logic exists.
- Light/dark behavior and Dynamic Type are reviewed in the simulator.
- The result does not resemble a generic AI dashboard or template.

Verification completed on 2026-08-24:

- Midnight Indigo, bright rust/orange, warm text, spacing, typography, radius, motion, and layout tokens are defined without adding a UI framework or custom font.
- Automated checks cover WCAG AA contrast for essential token pairings, the 44-point minimum touch target, and the scalable accessible application heading.
- The identity screen was reviewed on iPhone 16 Plus and iPhone 16e simulators in light and dark system appearances.
- Dynamic Type was reviewed at default and accessibility sizes; content reflows and remains scrollable without clipping.
- No performance benchmark was added because this increment introduces no representative performance-sensitive interaction.

### 1.3 Build the navigation shell

Status: complete

- Add only the routes needed for onboarding and the initial workout flow.
- Define loading, error, and unavailable states.
- Add navigation tests and an iOS simulator smoke check.

Verification completed on 2026-08-25:

- Expo Router's stable native stack and typed routes cover only onboarding, workout creation, active workout, session summary, and unavailable-route recovery.
- Route-level loading and error fallbacks provide clear, non-sensitive recovery states.
- Eleven tests pass across the route journey, navigation history, missing-route recovery, fallback states, primary-button behavior, accessibility-oriented design tokens, and touch targets.
- A clean lockfile install, formatting, strict TypeScript, Expo ESLint, Expo dependency validation, and a production iOS export pass.
- The onboarding route and a deep link to workout creation were visually verified in Expo Go on an iPhone 16 Plus simulator, including retained accessibility-size reflow and the restored standard text size.
- No performance benchmark was added because this shell has no representative performance-sensitive interaction; navigation performance will be measured when the workout flow contains real state and workload.

## Phase 2 — API and persistence foundation

### 2.1 Scaffold the ASP.NET Core API and tests

Status: complete

- Target .NET 10 LTS with nullable reference types and strict analyzers.
- Add a minimal health endpoint and integration test.
- Add structured logging with sensitive-body logging disabled.
- Document local build, test, and run commands.

Verification completed on 2026-08-25:

- The .NET 10 API and xUnit v3 integration-test projects are included in a solution with nullable reference types, implicit usings, recommended analyzers, code-style enforcement, and warnings treated as errors.
- NuGet dependencies are captured in lockfiles and restore successfully in locked mode.
- The API exposes only `GET /health`; PostgreSQL, EF Core, OpenAPI, authentication, and product endpoints remain deferred.
- Four integration tests verify the health response and cache protection, unknown-route behavior, the privacy-safe structured logging configuration, and actual middleware logging without query-string leakage.
- A real HTTPS process smoke test returned `200 Healthy` and emitted a JSON request record containing only method, path, status, and duration. A synthetic query-string marker was absent from the log.
- Formatting, Release build, tests, and dependency-vulnerability checks pass.
- No performance benchmark was added because an otherwise empty health endpoint is not representative of future application or persistence performance.

### 2.2 Add PostgreSQL development infrastructure

Status: complete

- Add a pinned local PostgreSQL container configuration.
- Configure EF Core through environment-based settings.
- Prove connectivity and migration behavior with integration tests against PostgreSQL.
- Commit no real connection strings or credentials.

Verification completed on 2026-08-25:

- Docker Compose and Testcontainers use PostgreSQL 18.6 on Alpine 3.24 pinned to the same immutable multi-architecture digest.
- Compose binds only to IPv4 loopback on configurable host port 55432, persists development data in a named volume, and requires values from the ignored local environment. The committed `.env.example` contains clearly fake placeholders only.
- EF Core 10.0.11 and Npgsql EF 10.0.3 are configured through `ConnectionStrings__Postgres`; the repository-local EF CLI is pinned, migrations are explicit, and the API does not migrate automatically at startup.
- The initial infrastructure migration deliberately adds no product tables. It establishes the migration history and pipeline before a domain schema exists.
- Six integration tests pass, including missing database configuration, a disposable real PostgreSQL container, connectivity, migration application, applied-migration detection, and absence of pending migrations.
- A Compose smoke test reached healthy state, accepted the migration, and reported exactly one migration-history row through `psql`; its temporary container, network, and volume were removed afterward.
- Locked restore, formatting, zero-warning Release build, Compose validation, NuGet vulnerability analysis, JSON checks, path checks, and secret-pattern checks pass. The open upstream findings in the local/test-only PostgreSQL image are documented as `I-008`; the lower-finding official variant was selected and is explicitly not approved for production.
- No performance benchmark was added because an empty schema and container startup do not represent future application query performance.

### 2.3 Establish the API contract workflow

Status: complete

- Publish an OpenAPI contract from the API.
- Generate a typed TypeScript client rather than duplicating DTOs.
- Add a CI check that detects contract or generated-client drift.

Verification completed on 2026-08-25:

- ASP.NET Core 10 publishes the versioned OpenAPI 3.1 document at `/openapi/v1.json` in Development only and emits the same canonical document during a non-incremental Release build.
- The committed contract includes the liveness route's stable operation identifier and typed `200` and `503` responses; integration tests verify its shape and confirm it is absent in Production.
- `openapi-typescript` 7.13.0 runs in an isolated TypeScript 5 tool package and produces the committed route/schema types consumed by an `openapi-fetch` client wrapper in the TypeScript 6 Expo application.
- The local drift command and the GitHub Actions workflow regenerate into a temporary directory and compare both committed artifacts without modifying them.
- Locked installs, formatting, strict TypeScript, Expo lint, the frontend suite, locked .NET restore, zero-warning Release build, the eight-test API integration suite, generation drift checks, and dependency vulnerability checks pass subject to the existing Expo advisory recorded as `I-001`.
- No performance benchmark was added because contract generation and a liveness request are build-time or non-representative glue paths rather than product performance paths.

## Phase 3 — Core fitness experience without AI

### 3.1 Define profile and onboarding

Status: complete

- Capture only information needed for the initial workout experience.
- Include goals, experience, available equipment, and units; defer exercise exclusions until stable catalogue identifiers exist.
- Avoid collecting medical detail not required by the product.

Verification completed on 2026-08-26:

- The closed onboarding taxonomy supports multiple goals (`buildStrength`, `buildMuscle`, and `generalFitness`), one experience level, one or more supported equipment choices, and metric or imperial units. It adds no free-text, injury, or medical fields; exercise exclusions remain deferred until catalogue identifiers exist.
- PostgreSQL stores profiles, goals, and equipment selections in normalized tables with stable identifiers, UTC creation time, string enum values, uniqueness constraints, foreign keys, and database checks. A committed EF Core migration owns the schema.
- Development-only `POST /profiles` and `GET /profiles/{profileId}` endpoints validate the trust boundary, reject numeric enums and unknown fields, avoid echoing sensitive invalid input, support cancellation, and never map in Production before authentication exists.
- The OpenAPI document and generated TypeScript types are the sole transport contract. The Expo onboarding route uses those types to submit the approved choices with bounded request time and safe loading, error, retry, disabled, and accessibility states.
- The full Release backend suite passes 15 tests against disposable PostgreSQL, and the frontend passes formatting, strict TypeScript, lint, all 20 tests, and an iOS production export. Contract drift verification passes.
- The initial screen was visually reviewed in Expo Go on an iPhone 16 Plus simulator. Its native type hierarchy, Midnight Indigo palette, legibility, touch controls, and scroll layout matched the established design direction; Expo's developer overlay is not application UI.
- No benchmark was added because onboarding is a one-time form and does not introduce a representative performance-sensitive path. The API request is one bounded write transaction and can be measured with realistic workloads if it later becomes material.

### 3.2 Establish the exercise catalogue

Status: complete

- Resolve source, licensing, taxonomy, and media policy first.
- Add a small curated set sufficient for the initial experience.
- Test search, filtering, validation, and stable identifiers.

Verification completed on 2026-08-27:

- The project owns a version-controlled, text-only manifest of 35 original common exercises covering every onboarding equipment value, the approved movement taxonomy, strength and cardio categories, and five deterministic tracking modes. No third-party content, API, package, media, rehabilitation content, difficulty rating, goal tag, or custom-exercise behavior was added.
- Every entry has a permanent UUID, unique slug and searchable names, required equipment, primary and secondary muscles, and bounded setup, execution, and general safety copy. The complete manifest validates before database access and is explicitly marked as requiring qualified fitness review before public release.
- An explicit, idempotent import command writes the validated catalogue to PostgreSQL in one transaction and records its version, canonical hash, review status, and import time. It refuses version rollback, same-version content changes, identifier reassignment, and silent removal.
- PostgreSQL stores filterable aliases, equipment, and muscles relationally with foreign keys, composite uniqueness, enum checks, and a committed migration. The shared equipment enum is the single vocabulary used by Profiles and Exercises.
- Development-only `GET /exercises` and `GET /exercises/{exerciseId}` endpoints provide escaped name/alias search, combined taxonomy filters, equipment-subset matching, stable ordering, bounded pagination, details, cancellation, safe validation, and no tracking queries. The generated TypeScript API wrapper preserves typed camel-case filters and uses the shared bounded request behavior.
- The Release backend suite passes 30 tests against disposable PostgreSQL, including manifest policy, import, escaped search, filters, invalid input, pagination, details, OpenAPI types, and Production-route coverage. The frontend passes formatting, strict TypeScript, lint, all 22 tests, and an iOS production export. Contract drift and migration-state checks pass.
- No simulator review was required because Phase 3.2 adds no screen or visual behavior. No performance benchmark was added for the fixed 35-row reference dataset; the query is bounded to 50 results and a stable offset, and representative search performance should be measured when the catalogue UI and realistic growth exist.

### 3.3 Build workout planning

Status: complete

- Create and edit a simple workout from the curated catalogue.
- Keep validation and calculations deterministic.
- Add domain, API, persistence, and client tests appropriate to the slice.

Verification completed on 2026-08-27:

- Profile-owned reusable workout templates support list, create, detail, and edit behavior with 1–20 unique curated exercises. Plans retain explicit order, tracking-mode-specific targets, and a monotonic revision; archive/delete and active-session behavior remain deferred.
- PostgreSQL stores plans and ordered prescriptions with foreign keys, bounded checks, UTC timestamps, canonical kilograms/metres/seconds, and optimistic concurrency. Development-only profile-scoped endpoints enforce profile boundaries, bounded pagination, stable ordering, deterministic validation, cancellation, and `409` conflict recovery.
- The Expo client carries the current local profile identifier through typed routes, lists saved plans, filters catalogue search by available equipment, shows exercise instructions before selection, edits explicit targets in focused sheets, and never invents training prescriptions. Compact rows support drag-handle reordering plus VoiceOver move actions.
- The OpenAPI contract and generated TypeScript client are current. The Release backend suite passes 43 PostgreSQL integration tests; the frontend passes formatting, strict TypeScript, lint, all 39 tests, and a production iOS export.
- The empty workout list and compact editor were visually reviewed in Expo Go on an iPhone 16 Pro simulator. The review found and corrected a missing iOS content inset. The significant local database migration workflow issue is retained as `I-013` in `DEVELOPMENT.md`.
- No synthetic benchmark was added: catalogue results are capped at 50 and plan size at 20, while micro-benchmarking array reordering would not represent touch responsiveness. Active-session interaction latency and realistic history/list query baselines remain required when those workloads exist.

### 3.4 Build active workout logging

Status: complete

- Optimize for minimal interaction during a session.
- Support sets, repetitions, load, completion, and notes.
- Measure interaction responsiveness once the flow is representative.
- Define interruption and offline behavior before claiming reliability.

Verification completed on 2026-08-28:

- Starting online creates one profile-owned active session from an immutable workout-plan snapshot. PostgreSQL stores snapshot prescriptions separately from canonical actuals, enforces the one-active invariant, and locks completed sessions. Stable client session, set, and mutation UUIDs make start and update retries idempotent; revisions prevent silent stale overwrites.
- The compact iOS logger supports all five catalogue tracking modes, explicit set completion and correction, add/remove set, skip/resume exercise, session and exercise notes, unfinished-workout confirmation, separate confirmed discard, elapsed time, and an optional adjustable in-app rest timer. Planned targets remain visible but are never silently recorded.
- Expo SQLite persists the active session after each edit. Set logging, notes, skips, timer state, and completion continue through temporary network loss; synchronization retries on demand and app activation. Conflicts retain the device copy until the user explicitly chooses the server version. Starting and discard intentionally remain online operations.
- The local profile association restores an interrupted active or pending-completion route after process termination. A completed local copy is cleared only after the API acknowledges it; another session cannot start against an unsynchronized completion.
- Migration `20260828011624_AddWorkoutSessions`, the committed OpenAPI contract, and generated TypeScript types are current. The Release backend suite passes 52 PostgreSQL integration tests. The frontend passes formatting, strict TypeScript, lint, all 50 tests, the contract drift check, and a production iOS export.
- The active logger was visually reviewed on an iPhone 16 Pro simulator with a synthetic two-exercise workout. The maximum supported 20-exercise × 20-set client edit plus JSON-serialization benchmark recorded a 0.050 ms median and 0.060 ms p95 over 10,000 operations on the development machine; it is a baseline, not a CI threshold or physical-device claim.

### 3.5 Add history and basic progress

Status: complete

- Show accurate workout history and a small number of useful derived metrics.
- Do not invent scores or trends that cannot be explained.
- Benchmark important list and database queries with representative data.

Verification completed on 2026-08-29:

- Completed history is profile-scoped, newest first, device-local date grouped, and loaded in explicit bounded pages. Compact rows show recorded duration, completed-set totals, skips, and correction state with dedicated empty, initial-error, and load-more-error behavior.
- Completed workout detail is read-only until the user enters correction mode. Revision-checked corrections may change only recorded completion, supported actuals, skips, and bounded notes; snapshot identity/order and session timing remain fixed, and the latest correction time is visible.
- The progress overview reports only completed workouts, completed sets, and recorded duration for the rolling four-week window. Exercise detail shows up to the latest 12 recorded appearances using tracking-mode-specific actuals, with explicit empty and insufficient-data states and no inferred scores, records, calories, or trends.
- History and corrections remain online-only. Server queries are bounded and projected, and completed-session retrieval is supported by a profile/status/finish-time index. Migrations, OpenAPI, and generated TypeScript types are current.
- The Release backend suite passes 56 PostgreSQL integration tests, and the frontend passes formatting, strict TypeScript, lint, all 60 tests, contract drift verification, and a production iOS export.
- With 200 synthetic completed sessions in disposable PostgreSQL, warm in-process requests measured a 1.16 ms median and 1.70 ms p95 for the first history page, and a 2.52 ms median and 2.90 ms p95 for progress overview across 30 samples. This is a development-machine baseline, not a CI threshold or production latency claim.
- Populated history, completed-detail, progress-overview, and single-exercise states were visually reviewed in Expo Go on an iPhone 16 Pro simulator. The review found and removed wording that implied a future inferred trend; the final state promises only additional recorded comparisons.

## Phase 4 — Identity and account lifecycle

Status: in progress

### 4.1 Select identity boundary and prototype migration policy

Status: complete

- Select a standards-based managed identity provider and native-flow approach.
- Define API token validation, application account identity, and one-time prototype-data migration requirements.
- Record the decision and local-development implications in an ADR.

Verification completed on 2026-08-30:

- Cognito managed login with authorization code flow and PKCE was selected for the Expo client. ASP.NET Core will validate scoped Cognito access tokens through JWT bearer middleware and the User Pool JWKS.
- The stable OIDC issuer and subject will identify application accounts; client profile UUIDs remain unauthenticated prototype state and cannot authorize requests.
- Existing prototype fitness data will transfer only through an explicit, atomic, idempotent server-side migration after authentication. Sign in with Apple is required before release with any third-party or social login option.
- See [ADR-0012](docs/adr/0012-cognito-identity-and-prototype-migration.md). Auth0's earlier selection remains recorded as superseded in ADR-0011.

### 4.2 Implement authenticated ownership and prototype migration

Status: complete

- Configure a local Cognito User Pool, public native app client, managed-login domain, and resource server with uncommitted local values.
- Add secure iOS sign-in, credential lifecycle, and authenticated API client behavior in a development build.
- Add application account persistence, profile ownership migration, and authorization to every user-owned API route.
- Add PostgreSQL integration tests for authentication failures, cross-account access, migration idempotency, and unclaimed-profile handling.

Verification completed on 2026-08-31:

- The local Cognito configuration remains uncommitted. The public iOS client uses managed login, authorization-code PKCE, secure credential storage, token refresh, and sign out with refresh-token revocation when available.
- The API validates scoped Cognito access tokens, resolves application accounts from stable issuer and subject claims, and enforces profile ownership on user-owned routes. A development-only migration command explicitly claims one unclaimed prototype profile and is idempotent for its owner.
- PostgreSQL integration tests cover anonymous access, cross-account rejection, account lookup, migration idempotency, and rejection of a second claim. The complete Release backend suite passed 58 tests; frontend formatting, strict type checking, linting, and all 67 tests passed; API contract drift verification passed.

### 4.3 Design account export and deletion

Status: complete

- Define the user-visible export format, account-deletion flow, provider deletion coordination, retention, and recovery boundaries before beta.

Verification completed on 2026-08-31:

- A versioned, direct JSON export will cover the current application account, profile, plans, sessions, snapshots/actuals, notes, corrections, and factual progress context. It uses canonical units and UTC instants, is ownership-scoped and no-store, and is sent through native share/save without a retained application export copy.
- Account deletion requires fresh Cognito authorization, typed and final confirmation, and Cognito's self-service user-management scope. It records a data-minimized deletion operation, then deletes the Cognito user and idempotently purges application-owned data. There is no user recovery after final confirmation.
- The design sets 45-day deletion-operation retention and a 35-day maximum encrypted-backup lifetime, with reconciliation before restored data is available. It records implementation, integration, physical-device, threat-model, provider-configuration, and backup-restore checks before beta.
- See [ADR-0013](docs/adr/0013-account-export-and-deletion-lifecycle.md). This increment is documentation-only; no runtime behavior or automated suite changed.

Identity may move earlier if cross-device persistence becomes necessary during Phase 3. That move requires a recorded decision, not silent scope expansion.

## Phase 5 — AI coach

### 5.1 Implement the provider-independent product boundary

Status: complete

- Define product-level request and response contracts.
- Use a fake provider for deterministic tests.
- Implement context minimization, timeouts, cancellation, usage accounting, and safe failure behavior.

Verification completed on 2026-08-31:

- A product-level coach service accepts a bounded question and passes only explicitly approved profile goals, experience, equipment, and unit preference to a provider-independent adapter. Account identifiers, notes, workout history, and arbitrary database access are excluded.
- A safety pre-check limits known urgent, diagnosis, rehabilitation, medication, pregnancy, pain, and disordered-eating signals before context loading or provider calls. The production default has no provider configured and safely reports coach unavailability.
- Provider calls are cancellation-aware, time-bounded to 15 seconds, output-bounded, and fail closed for errors, cancellation caused by timeout, and malformed responses. Metadata-only usage records contain provider, prompt version, outcome, latency, and token counts—never raw prompts, responses, or profile context.
- Four deterministic fake-provider tests cover minimized context, usage accounting, high-risk pre-check behavior, malformed output, and cancellation propagation. Formatting, Release build, and the complete 62-test backend suite pass.

### 5.2 Add read-only contextual coaching

Status: complete

- Answer questions using explicitly approved profile and workout context.
- Apply the safety rules in `docs/ai-safety.md`.
- Add adversarial, high-risk, privacy, and ordinary-use evaluation cases.

Verification completed on 2026-08-31:

- The authenticated API retains one user-deletable, profile-owned conversation and returns only read-only advice. Deleting the conversation cascades its messages; account deletion will cover this account data under ADR-0014.
- The context assembler includes the approved profile for every request and adds no more than five current workout plans or five recent completed workouts when deterministic question terms make that context relevant. Notes, account identifiers, raw provider payloads, and unrestricted database access remain excluded.
- The iOS coach screen identifies AI content, displays the factual context-source labels used for advice, supports loading, unavailable, and deletion states, and keeps workout access independent. Development uses a deterministic fake provider; non-development environments remain unavailable until a live provider is selected.
- Deterministic service, ownership, persistence, client API, high-risk, and safe-failure coverage passes. The full backend suite has 63 passing tests; the mobile suite has 69 passing tests, with type checking, lint, formatting, and API-contract drift checks passing.

### 5.3 Add structured proposals with confirmation

Status: complete

- Allow the model to propose a typed workout or program change.
- Validate proposals using deterministic domain rules.
- Present a clear diff and require explicit user confirmation.
- Audit the accepted action without storing unnecessary sensitive reasoning.

Verification completed on 2026-09-01:

- The OpenAI adapter requests strict `json_schema` output containing concise advice and, only when appropriate, one optional typed existing-workout proposal. The provider has no tools or write access; malformed output and proposals that fail deterministic validation are discarded.
- Pending proposals retain only the intended workout payload, short user-visible rationale, expected revision, and timestamps. The API validates profile ownership, curated exercise identifiers, tracking-mode-specific units, bounded prescriptions, and optimistic revision before it persists or confirms a proposal.
- The coach screen loads the current workout to show the current name and revision beside the proposed name, exercise count, and set count. Applying is an explicit button press; a separate authenticated confirmation endpoint invokes the ordinary workout aggregate update and records `confirmed_at` for the accepted action.
- A PostgreSQL integration test proves that a proposal does not alter a plan until confirmation. The complete backend suite passed 66 tests; the mobile suite passed 70 tests; frontend format, strict typing, and lint plus API-contract drift verification passed.

## Phase 6 — AI coach capability expansion

Status: pending

Build useful, bounded AI-assisted review and proposal experiences without giving the model direct application or database authority.

### 6.1 Review one named workout with an exercise-level proposal diff

Status: pending

- Assemble an approved, bounded snapshot for one explicitly named workout: workout identity and revision, exercise names and tracking prescriptions, and relevant approved equipment.
- Let the coach explain the recorded workout and draft conservative substitutions or prescription changes using strict structured output.
- Present an exercise-level diff showing additions, removals, substitutions, and prescription changes before the user can apply anything.
- Reuse deterministic ownership, catalogue, tracking-mode, bounds, revision, and confirmation checks; discard malformed, stale, or invalid proposals.

### 6.2 Add factual, bounded progress review

Status: pending

- Assemble only traceable recorded progress facts for an explicitly requested exercise or bounded recent period.
- Distinguish supplied facts from general coaching interpretation and avoid invented personal records, readiness, scores, diagnoses, or unsupported trend claims.
- Show the context sources and keep existing progress screens authoritative and independently usable.

### 6.3 Define scoped coach tasks and evaluation gates

Status: pending

- Define product-level task contracts for workout review, progress review, exercise substitution, and workout-update proposals.
- Keep model access server-owned and minimum-context; do not add unrestricted database access or direct write tools.
- Add deterministic, adversarial, privacy, stale-data, and confirmation-path evaluations before each task is enabled.

## Phase 7 — Beta hardening

Status: pending

- Harden authenticated multi-device synchronization and local-cache lifecycle behavior.
- Add monitoring, crash reporting, rate limits, backups, and restore verification.
- Complete accessibility, threat-model, privacy, and AI-safety reviews.
- Establish stable client, API, database, and AI performance baselines.
- Prepare synthetic demo data, app-store materials, and a manual release checklist.
- Test on supported physical iOS devices before beta distribution.

## Later, evidence-driven possibilities

- Android QA and release work.
- Wearable and health-platform integrations.
- Notifications and scheduling.
- Curated evidence retrieval if structured context is insufficient.
- A public marketing website.
- Payments or subscriptions.

These are not authorized implementation scope until promoted into an active phase.

## Immediate next increment

Implement Phase 6.1: review one explicitly named workout with an approved snapshot and exercise-level, confirmable proposal diff.
