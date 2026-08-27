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

Status: pending

- Create and edit a simple workout from the curated catalogue.
- Keep validation and calculations deterministic.
- Add domain, API, persistence, and client tests appropriate to the slice.

### 3.4 Build active workout logging

Status: pending

- Optimize for minimal interaction during a session.
- Support sets, repetitions, load, completion, and notes.
- Measure interaction responsiveness once the flow is representative.
- Define interruption and offline behavior before claiming reliability.

### 3.5 Add history and basic progress

Status: pending

- Show accurate workout history and a small number of useful derived metrics.
- Do not invent scores or trends that cannot be explained.
- Benchmark important list and database queries with representative data.

## Phase 4 — Identity and account lifecycle

Status: pending

- Write an ADR comparing standards-based managed identity options and local-development ergonomics.
- Implement secure iOS authentication and API authorization.
- Add ownership tests for every user-owned resource.
- Design account export and deletion before public beta.

Identity may move earlier if cross-device persistence becomes necessary during Phase 3. That move requires a recorded decision, not silent scope expansion.

## Phase 5 — AI coach

### 5.1 Implement the provider-independent product boundary

Status: pending

- Define product-level request and response contracts.
- Use a fake provider for deterministic tests.
- Implement context minimization, timeouts, cancellation, usage accounting, and safe failure behavior.

### 5.2 Add read-only contextual coaching

Status: pending

- Answer questions using explicitly approved profile and workout context.
- Apply the safety rules in `docs/ai-safety.md`.
- Add adversarial, high-risk, privacy, and ordinary-use evaluation cases.

### 5.3 Add structured proposals with confirmation

Status: pending

- Allow the model to propose a typed workout or program change.
- Validate proposals using deterministic domain rules.
- Present a clear diff and require explicit user confirmation.
- Audit the accepted action without storing unnecessary sensitive reasoning.

## Phase 6 — Beta hardening

Status: pending

- Resolve offline synchronization and conflict behavior.
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

Perform Phase 3.3 only: define and build simple workout creation and editing from the curated catalogue, including deterministic validation and the minimum profile/exercise relationships it genuinely needs.
