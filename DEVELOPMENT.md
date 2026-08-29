# Development Journal

This is the durable project journal. It records what happened, what was difficult, what was decided, and why. It complements the current-state documentation and ADRs; it should not be rewritten to make the project history appear cleaner than it was.

## How to maintain this file

- Add an entry in the same increment as a meaningful decision, blocker, failed approach, incident, or measured performance result.
- Preserve previous entries. Correct errors with a dated follow-up rather than silently rewriting history.
- Use `D-###` for decisions, `I-###` for issues, and `P-###` for performance findings.
- Link to an ADR when a decision changes a long-lived architectural boundary.
- Include evidence such as commands, test output, measurements, or source links when relevant.
- Do not record secrets, credentials, real user data, private prompts, or sensitive logs.

## Entry template

```text
### ID — YYYY-MM-DD — Short title

Status: proposed | accepted | superseded | resolved | open

Context:
What prompted the entry?

Decision or finding:
What did we decide or learn?

Rationale:
Why is this the best current choice?

Alternatives considered:
What else was considered and why was it not selected?

Consequences / follow-up:
What becomes easier, harder, or still needs action?

Evidence:
Tests, measurements, links, or reproduction details.
```

## Decision log

### D-001 — 2026-08-24 — Deliver iOS first

Status: accepted

Context:
Supporting browser, iOS, and Android at the beginning would multiply design, build, and QA work before the core product is proven.

Decision or finding:
Deliver the first usable version for iOS. Preserve normal React Native portability, but do not spend current increments on Android-specific testing or a browser experience.

Rationale:
A single initial platform provides a tighter feedback loop and allows more attention to product quality, native behavior, and reliability.

Alternatives considered:
Universal web/iOS/Android delivery was rejected for the initial scope because it increases layout, navigation, authentication, release, and QA surfaces. Native iOS with Swift was not selected because React Native and Expo retain a practical Android path.

Consequences / follow-up:
The iOS system typeface and interaction conventions are the initial design baseline. Platform-specific code is allowed when it materially improves iOS quality, but unnecessary lock-in should be avoided.

Related ADR: [ADR-0002](docs/adr/0002-ios-first-delivery.md)

### D-002 — 2026-08-24 — Use a modular monolith backend

Status: accepted

Context:
The product requires identity, workout data, progress, and AI orchestration, but does not yet have the scale or team boundaries that justify distributed services.

Decision or finding:
Use an ASP.NET Core modular monolith on .NET 10 LTS, EF Core, and PostgreSQL. Organize code around cohesive product features and keep endpoints thin.

Rationale:
This provides strong separation and testability without operational complexity. Modules can be extracted later only when evidence supports it.

Alternatives considered:
Microservices were rejected as premature. A fully TypeScript backend could reduce language switching, but C#/.NET is an intentional project and portfolio choice with strong server-side tooling.

Consequences / follow-up:
Module boundaries, transaction ownership, and API contracts must remain explicit. Do not introduce distributed infrastructure to simulate future scale.

Related ADR: [ADR-0001](docs/adr/0001-foundational-architecture.md)

### D-003 — 2026-08-24 — Make AI advisory and server-mediated

Status: accepted

Context:
The coach needs personalized context, but generative model output can be incorrect, unsafe, or structurally invalid.

Decision or finding:
All AI access will pass through a backend application service. The model receives minimum necessary context, cannot access the database directly, and cannot apply consequential changes without deterministic validation and explicit user approval.

Rationale:
This keeps credentials secure, establishes a reliable audit boundary, supports provider changes, and prevents model output from becoming an authority over fitness or account data.

Alternatives considered:
Direct client-to-provider calls were rejected because they expose credentials and weaken control. Autonomous program modification was rejected because it hides consequential changes and increases safety risk.

Consequences / follow-up:
AI response schemas, safety behavior, prompt versions, provider metadata, and evaluation cases must be designed before the first live model integration.

Related ADR: [ADR-0003](docs/adr/0003-ai-coach-boundary.md)

### D-004 — 2026-08-24 — Establish quality and traceability before scaffolding

Status: accepted

Context:
The project is intended for personal use, possible public release, and portfolio review. Unrecorded decisions and large generated changes would make the codebase difficult to trust or explain.

Decision or finding:
Define the product, architecture, safety boundary, testing approach, design expectations, execution plan, and development journal before application code is created.

Rationale:
This creates an explicit standard for future work and makes tradeoffs visible without prematurely selecting implementation details.

Alternatives considered:
Scaffolding first and documenting later was rejected because important defaults would become accidental decisions.

Consequences / follow-up:
Documentation must remain concise and current. It is not a substitute for working code or tests.

### D-005 — 2026-08-24 — Prove the core workout flow before identity and AI

Status: accepted

Context:
Authentication and AI both add external dependencies, failure modes, security work, and interface complexity before the core training experience has been validated.

Decision or finding:
Build the first complete prototype as a local iOS flow: onboarding with user-selected goals, workout creation, workout logging, and session summary. Defer authentication until this flow is proven and introduce AI afterward. Exclude progress photos, nutrition, wearables, and social features from the MVP.

Rationale:
This creates a useful, testable product loop with minimal infrastructure and ensures the workout experience—not account setup or chat—is the foundation of the application.

Alternatives considered:
Starting with account infrastructure was deferred because cross-device persistence is not required to validate the initial flow. Starting with AI was rejected because it would obscure whether the underlying fitness product is useful and dependable.

Consequences / follow-up:
The local data model should use stable identifiers and avoid assumptions that make later account synchronization unnecessarily difficult. Before authentication is added, its provider, migration path for local data, privacy behavior, and account lifecycle require a separate ADR.

### D-006 — 2026-08-25 — Use Expo Router's stable native stack

Status: accepted

Context:
Phase 1.3 requires a navigation foundation for onboarding and the first workout journey while preserving normal React Native portability.

Decision or finding:
Use Expo Router with the stable native stack and typed routes. Keep route modules under `frontend/src/app`, with the root layout responsible for shared stack presentation and safe loading/error fallbacks. Use `+not-found` for unavailable-route recovery.

Rationale:
Expo Router is aligned with the Expo toolchain and provides file-based routes, native deep-link support, typed destinations, and route integration testing without a custom navigation abstraction.

Alternatives considered:
Manually configured React Navigation would duplicate route configuration without a current benefit. Expo Router's experimental stack was rejected because this increment needs a stable foundation.

Consequences / follow-up:
Add route files only for active product increments. Do not introduce route groups, tabs, or additional navigators until a concrete journey requires them.

Related ADR: [ADR-0004](docs/adr/0004-expo-router-navigation.md)

### D-007 — 2026-08-25 — Separate persistent development PostgreSQL from disposable test PostgreSQL

Status: accepted

Context:
Phase 2.2 requires reproducible local PostgreSQL infrastructure and credible persistence tests without allowing tests to depend on a developer's mutable database state.

Decision or finding:
Use the official `postgres:18.6-alpine3.24` image pinned to digest `sha256:d3e1620b530c944afa6e887d22eb899824da68e19c52024bf98f5220c88a65b2` in two modes: Docker Compose with a named volume for ordinary development, and Testcontainers with a disposable database and dynamic host port for integration tests. Both paths apply the same committed EF Core migrations.

Rationale:
Developers retain useful local data between sessions, while tests remain isolated and repeatable against real PostgreSQL behavior. Pinning the database and Alpine versions prevents silent major or base-image changes.

Alternatives considered:
A manually installed PostgreSQL server was rejected because setup and versions would vary by machine. Reusing the Compose database in tests was rejected because test outcomes would depend on local state and fixed ports. EF Core's in-memory provider was rejected because it cannot verify PostgreSQL mappings, migrations, or SQL behavior.

Consequences / follow-up:
Docker is required for persistence integration tests. PostgreSQL image upgrades must update Compose and the Testcontainers image together and rerun migration verification. CI must provide a Docker-compatible runtime when the workflow is added.

### D-008 — 2026-08-25 — Keep liveness independent from database readiness

Status: accepted

Context:
The API currently exposes only a process liveness check, while database configuration is supplied by the environment and test hosts must be able to provide isolated connection strings.

Decision or finding:
Resolve and validate `ConnectionStrings__Postgres` when `FitnessCoachDbContext` is created rather than during top-level application construction. The existing `/health` route remains a liveness check and does not resolve the database context.

Rationale:
This preserves a useful process-health signal during database outages and permits integration tests to inject per-container configuration through the normal host configuration pipeline. Any attempt to use persistence without configuration still fails immediately with a clear message.

Alternatives considered:
Reading the connection string before building the host was rejected because it bypassed `WebApplicationFactory` configuration and prevented isolated test setup. Treating the current liveness route as database readiness was rejected because it would conflate two operational signals.

Consequences / follow-up:
Add a separate readiness check before deployment or when the first database-backed endpoint is introduced. Deployment orchestration should use liveness and readiness for their distinct purposes.

### D-009 — 2026-08-25 — Treat committed OpenAPI as the transport source of truth

Status: accepted

Context:
The .NET API and Expo client need to evolve together without maintaining duplicate request and response definitions or discovering incompatibility only at runtime.

Decision or finding:
Generate the versioned OpenAPI 3.1 document from ASP.NET Core at build time, commit it at `contracts/FitnessCoach.Api.json`, and generate the mobile route and schema types under `frontend/src/api/generated`. Consume those types through a small `openapi-fetch` wrapper. Keep runtime OpenAPI available in Development only, and verify both committed artifacts by regenerating into a temporary directory in local checks and GitHub Actions.

Rationale:
The server remains authoritative, contract changes are visible in review, the frontend compiler catches route and payload changes, and the drift check is deterministic without modifying the working tree.

Alternatives considered:
Handwritten TypeScript DTOs were rejected because they create two sources of truth. Runtime-only contract fetching was rejected because generation would depend on a separately running server. A full generated SDK was not selected after the available compatible generator lines introduced current security advisories.

Consequences / follow-up:
Endpoint metadata and operation identifiers are part of the reviewed API surface. Install the isolated contract tool dependencies before generation. Reassess the `openapi-fetch` runtime if its maintenance status, security posture, or product requirements change.

Related ADR: [ADR-0005](docs/adr/0005-api-contract-workflow.md)

### D-010 — 2026-08-26 — Keep the first onboarding profile closed, minimal, and development-only

Status: accepted

Context:
Phase 3.1 needs enough profile data to shape the first workout experience without introducing authentication early or collecting health information the product does not currently use.

Decision or finding:
Support multi-select strength, muscle-building, and general-fitness goals; one of three experience levels; one or more choices from a fixed equipment vocabulary; and metric or imperial units. Persist multi-select values as normalized rows with database-enforced integrity. Do not accept free text, medical or injury details, or exercise exclusions before stable exercise identifiers exist. Map the unauthenticated prototype endpoints only in Development.

Rationale:
A small closed vocabulary is deterministic, testable, easy to evolve through the canonical contract, and sufficient for the next workout increments. Data minimization avoids creating sensitive fields without a concrete use. Development-only routing preserves the planned local prototype while preventing random profile identifiers from becoming an accidental authorization scheme.

Alternatives considered:
Free-form goal detail and health constraints were rejected as unnecessary and difficult to use safely. Array columns were rejected in favor of relational integrity and future queryability. Production-visible anonymous routes were rejected because possession of an identifier does not prove ownership.

Consequences / follow-up:
Authentication, ownership, durable device association, and migration of local prototype data still require the Phase 4 identity ADR before public deployment. Phase 3.2 now provides the stable catalogue identifiers needed for a future exercise-exclusion feature. Build-time contract generation explicitly selects Development because the profile routes do not exist in Production. Database readiness remains deferred while database-backed routes are development-only; it must be added before any deployable environment maps them, as required by D-008.

Evidence:
The Release backend integration suite passes 15 tests against disposable PostgreSQL, including persistence, malformed input, duplicate selection, safe-error, unknown-record, and Production-route cases. The frontend passes all 20 tests plus formatting, strict TypeScript, lint, and iOS export. The API contract drift check regenerates the development contract and generated client without differences.

Related ADR: [ADR-0006](docs/adr/0006-minimum-onboarding-profile.md)

### D-011 — 2026-08-27 — Own and explicitly import the initial exercise catalogue

Status: accepted

Context:
Phase 3.2 requires stable exercise identity, deterministic filtering, and appropriate content provenance without committing the product to a third-party catalogue or unresolved media source.

Decision or finding:
Own a text-only manifest of 35 original common strength and cardio exercises. Validate it completely and import it into PostgreSQL only through an explicit transactional command. Version and hash the imported content, preserve permanent UUIDs, refuse ambiguous removal or identity reassignment, and keep the initial content marked as requiring qualified fitness review. Normalize fields used by search and filtering; keep bounded display instructions as scalar text. Share only the equipment vocabulary with Profiles.

Rationale:
This provides deterministic local data, readable review, stable workout references, and no catalogue API key, runtime vendor, recurring cost, or third-party schema dependency. Explicit import makes content mutation visible and recoverable. The shared equipment enum prevents onboarding and catalogue filtering from drifting without creating a broad common layer.

Alternatives considered:
A third-party API, scraped content, automatic startup seeding, EF model seeding, and a JSON-only runtime catalogue were rejected for the licensing, reliability, review, migration, or query-integrity reasons recorded in ADR-0007. Media and custom exercises were deferred because their source and lifecycle are unresolved.

Consequences / follow-up:
Run the importer after migrations in each local environment. Increment the manifest version for every imported content change. A qualified fitness professional must review the instruction set before public release. Exercise retirement, media, custom exercises, and production routing require separate decisions. Stable UUIDs can now support exclusions and workout history without treating names as identity.

Evidence:
The Release backend suite passes 30 tests against disposable PostgreSQL, including manifest policy, transactional idempotent import, escaped alias search, combined filters, equipment-subset matching, pagination, invalid inputs, detail retrieval, OpenAPI types, and Production-route coverage. The frontend passes formatting, strict TypeScript, lint, all 22 tests, and iOS export. Migration-state and contract-drift checks pass.

Related ADR: [ADR-0007](docs/adr/0007-internal-exercise-catalogue.md)

### D-012 — 2026-08-27 — Model workouts as explicit reusable templates

Status: accepted

Context:
Phase 3.3 needed workout creation and editing without prematurely defining active-session logging, generated coaching, deletion, or offline synchronization.

Decision or finding:
Store profile-owned workout templates with a name, explicit exercise order, and tracking-mode-specific prescriptions. Persist values canonically as kilograms, metres, and seconds while converting only at the client boundary for the profile's display units. Require 1–20 unique curated exercises and user-entered targets; do not generate default training recommendations. Use a monotonic revision as an EF concurrency token and return `409` when an edit was based on stale state. Keep the unauthenticated prototype routes Development-only.

On mobile, use a compact list rather than a card-heavy layout. Exercise discovery and prescription editing use focused iOS sheets. Long-press drag is the primary direct-manipulation reorder behavior, with VoiceOver adjustable move actions as an equivalent non-drag path.

Rationale:
Templates create a durable deterministic input for later workout sessions without conflating intended plans with completed training. Canonical storage prevents unit preference from changing historical meaning. Explicit prescriptions keep product rules explainable and avoid presenting arbitrary values as coaching. Revisions prevent silent last-write-wins data loss while identity and cross-device synchronization remain deferred.

Alternatives considered:
Storing a workout as one JSON document was rejected because exercise references, integrity, querying, and later history relationships belong in relational data. Position-specific unique constraints were deferred because in-place swaps would require temporary values or multi-step persistence without adding an invariant not already enforced by the aggregate. Automatic prescription defaults were rejected because no deterministic programming policy or qualified review has approved them. Delete/archive was deferred until history references and lifecycle behavior are concrete.

Consequences / follow-up:
Phase 3.4 can create sessions from stable templates without changing template semantics. Authentication must eventually replace the route-carried local profile identifier and authorize every profile-owned operation. Offline editing, archive/delete, progression rules, and template-to-session snapshot behavior remain explicit future decisions.

Evidence:
ADR-0008; migration `20260827063847_AddWorkoutPlanning`; 43 passing backend integration tests; 39 passing frontend tests; generated-contract drift verification; production iOS export; and iPhone 16 Pro simulator review.

Related ADR: [ADR-0008](docs/adr/0008-reusable-workout-templates.md)

### D-013 — 2026-08-28 — Separate recoverable session actuals from immutable plan snapshots

Status: accepted

Context:
Phase 3.4 needed low-friction workout logging that survives ordinary app interruption and temporary connectivity loss without changing the user's reusable plan or introducing a multi-device merge system before identity exists.

Decision or finding:
Create an online session from an immutable copy of one workout-plan revision and permit one active session per profile. Persist canonical actuals, explicit completion state, skips, and bounded session/exercise notes separately from planned values. On iOS, retain the complete bounded session in Expo SQLite and synchronize full-document updates using a revision plus client-generated mutation UUID. Keep a pending device copy after transport failure, make repeated mutations idempotent, and require an explicit choice before replacing a conflicted device copy. Starting and destructive discard remain online operations.

Rationale:
The active logger cannot put a network round trip on every interaction, while React state alone cannot protect a workout from process termination. A bounded 20-exercise by 20-set document makes a small durable outbox simpler to validate and reason about than an event log. Snapshotting preserves historical intent and lets templates evolve independently. Idempotency and visible conflict recovery prevent retry amplification or silent overwrite.

Alternatives considered:
Direct per-set API writes, ephemeral-only client state, mutating the reusable plan, last-write-wins updates, and an operation-log/CRDT design were rejected for reliability, lifecycle, data-loss, or premature-complexity reasons. Offline start and discard were deferred because they require additional authoritative lifecycle and tombstone behavior.

Consequences / follow-up:
Completed sessions are immutable until Phase 3.5 defines history correction. A completion must synchronize before its local copy is cleared or another workout starts. The current app-sandbox cache is a Development-only interruption aid, not authenticated ownership or a completed encryption/backup/deletion policy. Phase 4 and beta hardening must cover identity, platform data protection, multi-device behavior, account deletion, and cache lifecycle.

Evidence:
ADR-0009; migration `20260828011624_AddWorkoutSessions`; 52 passing PostgreSQL integration tests; 50 passing frontend tests; contract drift verification; production iOS export; and iPhone 16 Pro simulator review.

Related ADR: [ADR-0009](docs/adr/0009-recoverable-workout-sessions.md)

### D-014 — 2026-08-29 — Keep workout history and progress factual and bounded

Status: accepted

Context:
Phase 3.5 needed useful review and correction behavior without allowing completed records to lose their original plan context or presenting opaque progress claims as fact.

Decision or finding:
Use completed sessions as the source of truth. Load history newest first in bounded pages and group it into device-local calendar dates on iOS. Permit corrections only through a distinct optimistic-revision route that can change recorded completion, supported actuals, skips, and notes while preserving snapshot identity, order, and timing. Record the latest correction timestamp. Limit basic progress to rolling 28-day completed-workout, completed-set, and recorded-duration totals plus at most 12 actual appearances per exercise. Keep history and corrections online-only for now.

Rationale:
Every displayed value remains traceable to recorded session data. A separate correction boundary makes the difference between fixing an actual and rewriting historical intent explicit. Query and response caps provide predictable behavior before a large personal history exists.

Alternatives considered:
Reopening completed sessions, retaining an append-only correction event ledger, offline correction queuing, personal-record detection, streaks, generic scores, calorie estimates, and inferred trend lines were deferred or rejected because they weaken lifecycle clarity, add unjustified synchronization complexity, or require product rules that have not been approved.

Consequences / follow-up:
The latest correction is visible but this is not a complete audit history. Device-local grouping can change when the user travels, and the four-week calculation uses a rolling UTC boundary until an account time-zone preference exists. Phase 4 must authorize every history, correction, and progress query by authenticated ownership.

Evidence:
ADR-0010; migrations `20260829010505_AddWorkoutHistoryAndProgress` and `20260829013050_IndexWorkoutSessionHistory`; 56 passing PostgreSQL integration tests; 60 passing frontend tests; contract drift verification; and production iOS export.

Related ADR: [ADR-0010](docs/adr/0010-explainable-workout-history.md)

## Issue log

### I-001 — 2026-08-24 — Expo SDK 57 transitive uuid advisory

Status: open

Context:
After installing the current Expo SDK 57 scaffold, `npm audit --omit=dev` reported ten moderate findings. They resolve to one transitive chain: `expo@57.0.15` → `@expo/config-plugins@57.0.8` → `xcode@3.0.1` → `uuid@7.0.3`.

Decision or finding:
The underlying advisory is [GHSA-w5hq-g745-h8pq](https://github.com/advisories/GHSA-w5hq-g745-h8pq), affecting particular UUID buffer-writing APIs before `uuid@11.1.1`. The application does not call this transitive build-tool dependency directly. There are no high or critical findings. npm's proposed automatic resolution would downgrade Expo to version 46, which is incompatible with the selected SDK and is not an acceptable fix.

Rationale:
Do not use `npm audit fix --force`, downgrade Expo, or force an unverified major override into Expo's native configuration tooling. Retain the official current SDK dependency graph while tracking the upstream fix.

Alternatives considered:
Forcing `uuid@11.1.1` through an npm override was rejected because `xcode@3.0.1` declares the older API range and a major override could break native project generation. Downgrading Expo was rejected because it would abandon the current supported stack to satisfy an invalid automated remediation path.

Consequences / follow-up:
Re-run the production audit on Expo patch updates and before introducing native prebuild or release builds. Resolve the issue when Expo's supported dependency graph includes a patched UUID version. Escalate immediately if the advisory scope changes, direct runtime exposure is discovered, or severity increases.

Evidence:
`npm audit --omit=dev --json`, `npm explain uuid`, and `npm ls uuid xcode @expo/config-plugins` on 2026-08-24. Result: 10 moderate, 0 high, 0 critical vulnerabilities; all reported paths originate from the current Expo dependency graph. Re-running the production audit after the Phase 1.3 Router install and again after the Expo 57.0.16 maintenance update on 2026-08-25 produced the same totals and chain.

### I-002 — 2026-08-24 — Expo lint stack currently resolves to ESLint 9 after end of support

Status: open

Context:
The official Expo SDK 57 lint setup installed `eslint@9.39.5`. [ESLint's support policy](https://eslint.org/version-support/) marks the v9 release line end-of-life as of 2026-08-06, and clean installation emits a deprecation warning.

Decision or finding:
Keep the working Expo-supported lint configuration temporarily rather than force ESLint 10 through an incompatible peer graph or introduce a second linter. `eslint-config-expo@57.0.1` accepts ESLint 10, but its current `eslint-plugin-react@7.37.5` dependency declares support only through ESLint 9.

Rationale:
Linting currently passes and remains valuable, but a forced unsupported upgrade could make checks unreliable. Adding another lint system at scaffold time would create duplicate policy and unnecessary configuration.

Alternatives considered:
Forcing ESLint 10 was rejected because the installed React plugin does not declare compatibility. Replacing Expo ESLint with a different tool was rejected until the official stack can be reassessed on the next Expo patch or SDK update.

Consequences / follow-up:
Check Expo and `eslint-plugin-react` updates regularly. Upgrade to ESLint 10 as soon as the resolved Expo lint stack declares compatible peers and all lint checks pass. Treat new security findings in the EOL line as higher priority.

Evidence:
`npm ci --no-audit` deprecation output; installed package peer metadata inspected on 2026-08-24; formatting, type-checking, linting, and tests all pass with the current versions.

### I-003 — 2026-08-25 — Unpinned optional peers broke the Expo Router install and test stack

Status: resolved

Context:
Installing Expo Router into the SDK 57 client allowed npm to select newer peer dependency patches than the versions paired with the installed Expo and React releases. The first resolution selected `react-native-reanimated@4.6.0`, `react-native-worklets@0.12.1`, `react-dom@19.2.8`, and later `react-test-renderer@19.2.8`; those conflicted with Expo SDK 57's native module ranges or the client's `react@19.2.3`. React Native Testing Library 14 also changed `render` to an asynchronous contract that Expo Router 57's test helper does not yet support.

Decision or finding:
Pin Router-related native packages to Expo's SDK 57 compatibility map, pin `react-dom` and `react-test-renderer` to the exact React version, and use React Native Testing Library `~13.3.3` for Router integration tests.

Rationale:
These versions satisfy declared peers and preserve the official Router testing path. Ignoring peer errors or replacing navigation integration tests with mocks would make clean installation or test behavior less trustworthy.

Alternatives considered:
Using `--force` or `--legacy-peer-deps` was rejected because it would conceal an invalid graph. Retaining React Native Testing Library 14 was rejected because Expo Router 57's helper treats its Promise as a synchronous render result.

Consequences / follow-up:
Keep the direct peer pins until the Expo Router test helper supports the asynchronous renderer and the Expo SDK compatibility map advances. Reassess these pins during Expo SDK upgrades.

Evidence:
`npm ls`, npm peer-resolution errors, `npx expo install --check`, and the passing Router integration suite on 2026-08-25. The final graph uses `react-native-reanimated@4.5.1`, `react-native-worklets@0.10.1`, `react-dom@19.2.3`, `react-test-renderer@19.2.3`, and `@testing-library/react-native@13.3.3`.

### I-004 — 2026-08-25 — Generated localhost dynamic ports prevented the API from starting

Status: resolved

Context:
The .NET 10 web API template generated launch profiles using `localhost:0` so local runs could select an available port.

Decision or finding:
Kestrel does not support dynamic port binding for the `localhost` hostname and rejected both generated profiles at startup. The profiles now bind to the explicit IPv4 loopback address `127.0.0.1:0`, preserving dynamic port allocation without exposing the development server to the network.

Rationale:
Loopback-only dynamic ports avoid common local port conflicts and are compatible with Kestrel. Fixed ports provide no product benefit at this stage.

Alternatives considered:
Fixed localhost ports were rejected because parallel work and other local services can collide with them. Binding to all interfaces was rejected because it unnecessarily broadens development-server exposure.

Consequences / follow-up:
The selected ports are printed in the startup logs rather than being constant. Local tooling and documentation must not assume a fixed API port.

Evidence:
The original profile failed with `Dynamic port binding is not supported when binding to localhost`. The corrected HTTPS profile started both loopback listeners successfully, and a loopback HTTPS health request returned `200 Healthy`.

### I-005 — 2026-08-25 — Health endpoint short-circuiting bypassed request logging

Status: resolved

Context:
The initial health route used ASP.NET Core's endpoint short-circuit option. The route returned the expected response, but a real-process smoke test produced no HTTP request log.

Decision or finding:
Map the health check as a normal endpoint so it passes through the configured HTTP logging middleware. Keep the endpoint otherwise minimal and unauthenticated for future health probes.

Rationale:
Health traffic is operationally useful when diagnosing availability, and the selected log fields contain no headers, query strings, or bodies. Consistent middleware behavior is more valuable here than bypassing a negligible pipeline on an empty scaffold.

Alternatives considered:
Retaining short-circuiting and accepting missing request records was rejected because it contradicted the verified logging behavior. A separate custom logging path was rejected as unnecessary complexity.

Consequences / follow-up:
Future middleware added before the endpoint can run for health requests. Reassess the pipeline only if a measured performance or dependency-isolation requirement justifies a dedicated probe path.

Evidence:
Before the change, the real API returned `Healthy` without an HTTP logging record. After removing short-circuiting, the same request emitted JSON with method `GET`, path `/health`, status `200`, and duration; the synthetic query-string marker was absent.

### I-006 — 2026-08-25 — Private EF tooling assets caused runtime assembly conflicts in tests

Status: resolved

Context:
The first persistence build referenced EF Core Design 10.0.11 privately and Npgsql EF 10.0.3. Because design-time assets do not flow through the API project reference, the test project resolved the provider's lower minimum EF Core runtime versions instead of the versions used to compile the API.

Decision or finding:
Reference `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational` 10.0.11 explicitly in the API alongside the private design package. This makes the intended runtime versions transitive and aligns the API and tests without suppressing warnings or forcing restore behavior.

Rationale:
The application should declare the runtime it compiles against. Relying on a private tooling dependency to select runtime versions produced both an assembly-version compiler error and an unresolved relational-assembly warning.

Alternatives considered:
Adding version overrides only to the test project was rejected because it would conceal an incomplete API dependency declaration. Downgrading EF tooling to the provider's minimum dependency was rejected because the current compatible .NET 10 patch is available.

Consequences / follow-up:
Keep Microsoft EF Core runtime, relational, design, and repository-local CLI tool versions aligned during upgrades. Npgsql can advance on its own compatible 10.x patch line.

Evidence:
The initial build failed with `CS1705` for EF Core 10.0.4 versus 10.0.11 and then reported `MSB3277` for EF Core Relational. After the explicit runtime references and lockfile refresh, the build completed with zero warnings and zero errors.

### I-007 — 2026-08-25 — Default PostgreSQL host port conflicted with an existing service

Status: resolved

Context:
The first Compose smoke test mapped PostgreSQL to the conventional host port 5432, which was already allocated on the development machine.

Decision or finding:
Use host port 55432 in `.env.example` while keeping container port 5432. The host port remains configurable through `POSTGRES_PORT`, and the connection-string example uses the same value.

Rationale:
A non-default host port avoids colliding with common native PostgreSQL installations while preserving a conventional internal container configuration.

Alternatives considered:
Stopping the unrelated service was rejected because local infrastructure should not disrupt other projects. Selecting a random Compose port was rejected because the API connection string needs a stable, understandable development default.

Consequences / follow-up:
Developers can change both `POSTGRES_PORT` and the connection-string port in their ignored `.env` if 55432 is also occupied.

Evidence:
The first Compose start failed with `Bind for 0.0.0.0:5432 failed: port is already allocated`. After changing the example to 55432, the container became healthy, the migration applied, and `__EFMigrationsHistory` contained exactly one row. The temporary smoke-test volume was then removed.

### I-008 — 2026-08-25 — Official PostgreSQL images contain upstream high and critical findings

Status: open

Context:
Docker Scout scanned the current official PostgreSQL 18.6 variants before the image was finalized. The initially selected Alpine 3.23 variant reported 5 critical and 23 high findings across inherited `curl` and the Go standard library embedded in `gosu`.

Decision or finding:
Use the official Alpine 3.24 variant for local development and disposable tests because it removes the inherited `curl` findings and has the lowest result among the current official variants checked. Its remaining scan result is 2 critical and 20 high findings attributed to the Go 1.24.6 standard library in the upstream `gosu` artifact. This image is not approved for production deployment.

Rationale:
The database is restricted to loopback for local development or an isolated Docker network during tests, uses synthetic credentials, and is never shipped as an application artifact. No current official PostgreSQL 18.6 variant tested has a clean high/critical result: Alpine 3.24 is lower than Alpine 3.23, while Debian Trixie reported 4 critical and 22 high findings. Maintaining an ad hoc database image would add a larger supply-chain and patching burden at this stage.

Alternatives considered:
Alpine 3.23 was rejected because it adds unfixed `curl` findings. Debian Trixie was rejected because its result was worse and its image surface was larger. Building a custom image was deferred because the remaining findings originate in an upstream startup helper and this image has no production exposure.

Consequences / follow-up:
Re-scan on every PostgreSQL image update and at least before public beta. Replace the pinned digest as soon as the official image rebuilds with a corrected `gosu` toolchain. A production database deployment requires a separate image or managed-service security review and cannot inherit this local image decision.

Evidence:
`docker scout cves --exit-code --only-severity critical,high` with Docker Scout 1.22.0 on 2026-08-25. Results: Alpine 3.23 = 5 critical / 23 high; Alpine 3.24 = 2 critical / 20 high; Debian Trixie = 4 critical / 22 high. Representative remaining critical identifiers are CVE-2025-68121 and CVE-2026-39821.

### I-009 — 2026-08-25 — TypeScript 6 prevented a safe in-app contract generator install

Status: resolved

Context:
`openapi-typescript` 7.13.0 declares TypeScript 5 as its peer, while Expo SDK 57 uses TypeScript 6. A direct frontend install failed npm's dependency resolution. The evaluated TypeScript 6-compatible full-client generator versions either retained a generator advisory or pulled a `js-yaml` version with high-severity denial-of-service advisories.

Decision or finding:
Run `openapi-typescript` with TypeScript 5.9.3 in a separate, private, lockfile-pinned `tools/api-contract` package. Keep the Expo client on its supported TypeScript 6 compiler and pass only generated source between the two dependency graphs.

Rationale:
This avoids `--force`, `--legacy-peer-deps`, a client compiler downgrade, and known high-severity generator dependencies. The isolated generator installation reports zero vulnerabilities.

Alternatives considered:
Ignoring the peer declaration was rejected because it would conceal an unsupported tool graph. Downgrading the Expo compiler was rejected because it would depart from the selected SDK. The evaluated full-client generator was rejected after inspecting both its direct advisory and nested YAML parser findings.

Consequences / follow-up:
Contract generation requires `npm ci --prefix tools/api-contract` in addition to the frontend install. Re-evaluate the isolation when `openapi-typescript` officially supports TypeScript 6.

Evidence:
The direct install failed with `ERESOLVE` for `typescript@6.0.3` versus peer `^5.x`. npm advisory checks of the alternative generator found 4 high findings on its latest line. `npm audit` for the final isolated tool package reports 0 vulnerabilities; the frontend production audit remains at the 10 existing moderate Expo findings recorded in `I-001`.

### I-010 — 2026-08-25 — Health-check middleware produced an empty OpenAPI contract

Status: resolved

Context:
The first generated OpenAPI document contained no paths even after stable metadata was attached to `MapHealthChecks`. The health-check middleware endpoint is not surfaced through API Explorer as an ordinary route handler.

Decision or finding:
Map `/health` as a thin Minimal API handler over `HealthCheckService`. Preserve its `Healthy` response, unhealthy status mapping, no-store cache header, independence from PostgreSQL, and request logging while adding explicit OpenAPI response metadata.

Rationale:
The contract workflow needs one real route to prove endpoint discovery and typed response generation without inventing a product endpoint.

Alternatives considered:
Committing an empty contract was rejected because it would not prove endpoint generation. Adding a placeholder product route was rejected because Phase 2.3 does not authorize product behavior.

Consequences / follow-up:
The liveness adapter has direct HTTP and contract coverage. Add readiness separately when persistence-backed availability becomes relevant.

Evidence:
The initial generated document contained `"paths": { }`. After the adapter, it contains `GET /health` with operation `GetHealth` and typed `200` and `503` responses; existing health behavior tests and new OpenAPI integration tests pass.

### I-011 — 2026-08-25 — Incremental builds skipped temporary contract output

Status: resolved

Context:
The first drift check changed `OpenApiDocumentsDirectory` to a temporary directory, but MSBuild considered the API up to date and skipped document generation. The client generator then failed because the expected temporary contract did not exist.

Decision or finding:
Run the contract build with `--no-incremental` before generating client types.

Rationale:
Every drift check must materialize a fresh document in its requested directory, independent of previous build output and timestamps.

Alternatives considered:
Copying the committed contract into the temporary directory was rejected because it would compare generated client output against the artifact under test rather than freshly generated server metadata.

Consequences / follow-up:
Contract generation rebuilds the small API project each time. This adds under two seconds locally and removes reliance on stale MSBuild outputs.

Evidence:
Before the change, the drift command failed with `ENOENT` for the temporary `FitnessCoach.Api.json`. With `--no-incremental`, the document is emitted into the temporary directory and both comparisons pass.

### I-012 — 2026-08-27 — Default query enum binding disagreed with the JSON contract

Status: resolved

Context:
The first exercise filter endpoint used enum and enum-array handler parameters. ASP.NET Core's query binder interpreted those using C# enum spelling, while JSON and the generated TypeScript contract expose camel-case values such as `horizontalPush` and `squatRack`.

Decision or finding:
Accept raw query strings at the HTTP boundary, parse them against the exact camel-case enum vocabulary, and return standard validation problems for unsupported values. Apply an endpoint-specific OpenAPI transformer so generated TypeScript filters retain their enum unions rather than degrading to arbitrary strings.

Rationale:
Transport values should behave consistently whether they appear in JSON or a query string. Explicit parsing also rejects numeric enum values and prevents framework binding diagnostics from becoming the product error contract.

Alternatives considered:
Pascal-case query values were rejected because they would disagree with every generated response and client enum. Leaving query filters as untyped strings was rejected because the mobile compiler would no longer catch invalid catalogue filters. Custom wrapper types were not selected because they would add transport-only types and less predictable OpenAPI schemas.

Consequences / follow-up:
New enum query filters should reuse this explicit boundary approach or a future shared binder only when a second concrete use justifies extracting one. The OpenAPI integration test protects the generated camel-case filter values.

Evidence:
The first PostgreSQL endpoint run returned framework binding failures for repeated lower-camel equipment values. After explicit parsing and schema transformation, all 13 focused exercise endpoint tests and the full 30-test backend suite pass.

### I-013 — 2026-08-27 — EF no-build commands used a stale configuration assembly

Status: resolved

Context:
During end-to-end workout planning review, an EF migration update run with `--no-build` reported that the local database was current even though `workout_plans` did not exist. The verified build had been produced in Release, while the EF command used the older default Debug output.

Decision or finding:
`--no-build` is safe only when the EF command explicitly selects the same configuration that was just built. The migration was applied from the current Release assembly with `--configuration Release --no-build`. The documented ordinary migration command continues to build current source; any optimized no-build variant must state its matching configuration.

Rationale:
EF discovers migrations from the compiled startup assembly, not directly from source. A successful command against a stale assembly can therefore be factually correct for that binary while misleading for the working tree.

Alternatives considered:
Deleting build outputs was unnecessary and would not prevent recurrence. Editing migration history manually was rejected because the database accurately reflected the migrations known to the stale binary.

Consequences / follow-up:
Migration verification and application commands must either build or use an explicitly matched configuration. Diagnose schema/history disagreement by checking both `__EFMigrationsHistory` and the compiled configuration before changing database state.

Evidence:
The default no-build command saw only the first three migrations and reported no work. `dotnet ef database update --configuration Release --no-build ...` discovered and applied `20260827063847_AddWorkoutPlanning`; subsequent API requests returned `200`.

### I-014 — 2026-08-27 — Profile read produced a multiple-collection query warning

Status: resolved

Context:
The workout editor loads goals and equipment from the current profile. Simulator review showed EF's `MultipleCollectionIncludeWarning` because both collections were loaded through one joined query.

Decision or finding:
Use `AsSplitQuery` for this bounded profile read. This avoids cartesian row multiplication while preserving one cohesive no-tracking load and the existing response contract.

Rationale:
Goals and equipment are separate collections with small independent cardinalities. Three small indexed queries are more predictable than a cross-product result as either taxonomy grows.

Alternatives considered:
Ignoring the warning was rejected because this route now runs whenever a planner opens. A handwritten projection was not selected because the current aggregate mapping is simple and split-query behavior directly expresses the required loading strategy.

Consequences / follow-up:
The route makes additional database round trips but transfers bounded rows without multiplicative duplication. Revisit only with representative measurements if profile taxonomies or latency conditions change.

Evidence:
The Development runtime emitted `MultipleCollectionIncludeWarning` before the change. The query now opts into split behavior; the full PostgreSQL integration suite remains the regression boundary.

### I-015 — 2026-08-28 — Client-generated set identifiers were initially tracked as updates

Status: resolved

Context:
The first active-session update that appended a client-generated set returned an optimistic-concurrency conflict even though the session revision was current. EF Core attached the new child with its non-default UUID as `Modified`, then PostgreSQL correctly reported that no existing row matched the attempted update.

Decision or finding:
Capture the persisted set identifiers before applying the aggregate update. After synchronization, explicitly mark only newly introduced set entities as `Added`; existing and removed children retain EF's normal tracked states. Keep client-generated UUIDs because they are required for offline stability and retry idempotency.

Rationale:
Changing back to server-generated set identity would make offline additions and request retries ambiguous. Explicitly distinguishing new children at the application boundary preserves the aggregate API and makes the non-default-key behavior visible.

Alternatives considered:
Server-generated identifiers, replacing the entire set collection with raw SQL, and treating every requested set as new were rejected because they break offline identity, bypass aggregate persistence, or create duplicate rows.

Consequences / follow-up:
Any later aggregate that accepts client-generated keys for new EF children must test the tracked state on insertion. The session integration suite covers adding a set and idempotently retrying updates against PostgreSQL.

Evidence:
The failing integration request returned `DbUpdateConcurrencyException` with `WorkoutSessionSet:Modified`. After marking only the new set `Added`, all nine focused workout-session scenarios and the full 52-test backend suite pass.

### I-016 — 2026-08-29 — EF Core could not translate the first exercise-progress grouping projection

Status: resolved

Context:
The first exercise-progress query attempted to group completed session exercises and construct the response shape, including nested values, inside one EF Core projection. PostgreSQL integration testing rejected the expression because EF Core could not translate the nested grouped projection.

Decision or finding:
Project only grouped scalar values in SQL, materialize the bounded result, and map those rows into response records afterward. Exercise detail uses one bounded appearance query followed by a bounded set query for only those appearances.

Rationale:
The revised shape keeps filtering, grouping, ordering, and caps in PostgreSQL while avoiding unbounded client aggregation. It is clearer than relying on provider-specific expression behavior and transfers only the rows required by the response.

Alternatives considered:
Loading every completed session and aggregating in memory was rejected because cost would grow without a bound. Raw SQL was not necessary once a simple translatable projection expressed the required work.

Consequences / follow-up:
Changes to the progress projections must retain their explicit bounds and PostgreSQL integration coverage. Representative latency is recorded in `P-002`.

Evidence:
The original integration request failed with an EF Core query-translation exception. The revised queries pass the full 56-test PostgreSQL suite and the dedicated 200-session performance scenario.

### I-017 — 2026-08-29 — Development contract generation could hang on file watchers and stale build servers

Status: resolved

Context:
The OpenAPI generator starts the API in the Development environment. In the restricted automation environment, configuration reload file watching could prevent deterministic startup, and reused MSBuild/Roslyn build-server processes could leave generation waiting on stale workers.

Decision or finding:
Set `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false` only for the contract-generation process and invoke its build with `--disable-build-servers`.

Rationale:
Contract generation is a short-lived build workflow and does not need live configuration reload. Isolating the build avoids dependence on ambient worker state without changing ordinary API development behavior.

Alternatives considered:
Disabling reload globally was rejected because normal development can benefit from it. Requiring developers to kill shared build processes manually was rejected because it is stateful and unreliable.

Consequences / follow-up:
Contract generation may be a little slower because it cannot reuse build servers, but it is deterministic in local automation and still produces the same committed contract.

Evidence:
`bash scripts/generate-api-contract.sh` and the non-mutating `bash scripts/check-api-contract.sh` complete successfully after the scoped changes.

### I-018 — 2026-08-29 — Workout-planner touch reordering lost its active drag state

Status: resolved

Context:
Long-press dragging the handle in the workout planner did not move an exercise, although the separate VoiceOver adjustable actions worked. The original component test covered only that accessibility alternative and never drove the native pan-gesture lifecycle.

Decision or finding:
The pan callbacks closed over the render where no exercise was active. Starting a drag then updated React state and recreated the gesture, while its move callback still saw the inactive value and returned without selecting a destination. Keep active identity, layout, source index, and destination index in stable refs for the lifetime of the native gesture. Keep the gesture callbacks stable across overlay renders and apply the reorder only when the gesture finishes successfully.

Rationale:
Gesture progress is transient interaction state that must remain available to callbacks before React has committed another render. Refs preserve that live state without adding a dependency or moving high-frequency pointer updates into application state, while React state still drives the visible overlay and insertion marker.

Alternatives considered:
A third-party sortable-list dependency was rejected because the existing bounded list needs only one focused gesture and already has accessible move actions. Removing drag in favor of buttons was rejected because touch reordering is the selected planner interaction. Recreating the gesture after each state update was the cause, not a recovery mechanism.

Consequences / follow-up:
Touch drag and VoiceOver reordering now share the same final list update. Future gesture regressions must drive begin, active movement, and finalization rather than asserting only rendered handles. Physical-device drag behavior remains part of beta QA.

Evidence:
The new gesture-lifecycle regression test failed with zero reorder calls before the change and passes afterward. Formatting, strict TypeScript, lint, all 61 frontend tests, and a production iOS export pass.

### I-019 — 2026-08-29 — Simulator cancels the activated reorder pan at release

Status: resolved

Context:
After `I-018` stabilized the live drag state, touch reordering still did not commit in Expo Go on the iOS Simulator. The gesture-helper test ended with a successful state and therefore did not represent the host interaction.

Decision or finding:
A controlled pointer trace against the actual Simulator showed that the handle pan began, activated, moved from source index 0 to target index 1, and then finalized as cancelled when released inside the surrounding native scroll view. Finalization is now the drop boundary whenever an activated drag has a valid changed destination, regardless of the native success flag. A gesture that never activates or never crosses into another position remains a no-op.

Rationale:
By finalization, the user has deliberately held the dedicated handle and moved the item across a row boundary. Discarding that explicit destination because the Simulator/scroll container reports cancellation makes the selected interaction unusable. The reorder is visible and remains an unsaved planner edit, so the user can immediately inspect or reverse it.

Alternatives considered:
Disabling scrolling only after activation also cancelled the in-progress touch. Disabling the ScrollView's JavaScript pan responder globally did not change the native cancellation and would alter unrelated scrolling behavior. Adding another sortable-list dependency was not justified for this bounded list.

Consequences / follow-up:
The regression test now finalizes with `CANCELLED`, matching the observed Simulator lifecycle. Any future change must verify both successful and cancelled native finalization paths and retain the no-op behavior for unchanged targets. Physical-device drag remains part of beta QA.

Evidence:
Before the change, a real 500 ms hold and cross-row pointer drag left the three-exercise list unchanged. After the change, the same controlled Simulator gesture moved Barbell Bench Press from position 1 to position 2 and promoted Front Plank to position 1. Formatting, strict TypeScript, lint, all 61 frontend tests, and a production iOS export pass.

### I-020 — 2026-08-29 — Reorder cancellation workaround caused premature and erratic moves

Status: resolved

Context:
The `I-019` workaround made a cancelled pan commit any changed destination. Simulator review then showed that pressing the handle could move an exercise unexpectedly and that sustained dragging remained unreliable.

Decision or finding:
Two behaviors combined to cause the failure. The destination calculation selected the next row as soon as the dragged center met the source row's midpoint, so even a stationary first update or pointer jitter could change the target. The surrounding native scroll view also continued to terminate the gesture-handler pan on release. Replace that competing pan with a responder owned exclusively by the dedicated handle. Commit only on responder release, discard termination, and change destination only after the dragged row's center crosses an adjacent row's center. This entry supersedes `I-019`'s cancelled-finalization behavior while preserving it as investigation history.

Rationale:
The handle has one purpose, so capturing touches that begin on it does not make the rest of the planner harder to scroll. A row-center boundary gives each move an intentional physical threshold and behaves symmetrically in both directions. Separating release from termination restores a reliable commit/cancel distinction without depending on a native success flag that the Simulator did not provide.

Alternatives considered:
Coordinating a gesture-handler native-scroll gesture with `blocksExternalGesture`, changing the scroll view's iOS touch-cancellation property, and changing the pan's cancellation property were each tested against real pointer input; none changed the cancelled final state. Keeping the `I-019` workaround with a small arbitrary distance threshold was rejected because it still treated interruption as a successful drop.

Consequences / follow-up:
A touch beginning on the reorder handle is reserved for reordering; scrolling remains available from the rest of every row and screen. The interaction now starts immediately rather than requiring a long press, and its visible instruction reflects that. Tests cover release, termination, small movement, and the existing VoiceOver move actions. Physical-device drag remains part of beta QA.

Evidence:
On an iPhone 16 Pro Simulator, a plain handle tap preserved the three-exercise order. A gradual downward drag moved Barbell Bench Press from position 1 to position 2, and a reverse drag restored it to position 1. Formatting, strict TypeScript, lint, all 63 frontend tests, and a production iOS export pass.

## Performance log

### P-001 — 2026-08-28 — Active-session edit and serialization baseline

Status: accepted

Context:
Workout logging is the first interaction-sensitive product path. The client persists a complete bounded session document after each edit, so immutable state update plus JSON serialization is part of the local critical path.

Decision or finding:
Keep a separate `npm run benchmark:session` workflow that updates the last set in the maximum supported 20-exercise by 20-set session and serializes the result. Do not add a CI threshold until repeated environments establish stable variance.

Rationale:
The benchmark exercises the actual bounded reducer and persistence payload without confusing network, database, simulator, or debug-build variance with JavaScript state cost. It is a useful baseline but is not evidence of physical-device touch latency or API performance.

Evidence:
On 2026-08-28, the Jest/Node development environment completed 10,000 edit-plus-serialization operations with a 0.050 ms median, 0.060 ms p95, 0.050 ms minimum, and 0.080 ms maximum. The production iOS export also completed, and the logger was visually reviewed on an iPhone 16 Pro simulator. Physical-device responsiveness and representative API/database latency remain later baselines.

### P-002 — 2026-08-29 — Completed-history and progress-query baseline

Status: accepted

Context:
History and progress are the first account-data reads whose cost grows with completed sessions. Their database filters, projections, ordering, and bounds need a repeatable baseline before richer analytics are considered.

Decision or finding:
Keep a dedicated PostgreSQL integration benchmark that seeds 200 synthetic completed one-exercise, one-set sessions, performs five warm-up requests, and records 30 sequential warm-cache samples for the first history page and progress overview. Do not set a CI latency threshold while container startup, host load, and development hardware remain variable.

Rationale:
The scenario exercises the real ASP.NET Core, EF Core, Npgsql, migrations, and PostgreSQL query path while remaining quick enough to run deliberately when those queries or indexes change. It establishes comparative evidence without presenting in-process development latency as production performance.

Evidence:
On 2026-08-29, the history request measured a 1.16 ms median and 1.70 ms p95; progress overview measured a 2.52 ms median and 2.90 ms p95 across 30 samples. The dedicated test completed in about 6.5 seconds including setup. These figures are a local baseline, not a service-level objective.

## Open decisions

These choices are intentionally unresolved until their requirements are clearer:

- Product name and final visual identity.
- Authentication and identity provider.
- Hosting provider and deployment topology.
- AI provider and model selection.
- Multi-device workout synchronization and authenticated cache lifecycle.
- Whether and how licensed exercise media is added.
- Analytics and crash-reporting providers.
- Monetization, if the product proceeds beyond personal and portfolio use.
