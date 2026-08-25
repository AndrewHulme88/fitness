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

## Performance log

No representative performance-sensitive path exists yet, so no meaningful baseline has been recorded. Baselines will be added when client and API paths contain realistic state, data, and workload.

## Open decisions

These choices are intentionally unresolved until their requirements are clearer:

- Product name and final visual identity.
- The supported goal taxonomy and whether free-form goal detail is allowed.
- Authentication and identity provider.
- Hosting provider and deployment topology.
- AI provider and model selection.
- Offline workout logging and synchronization design.
- Source and licensing for the exercise catalogue and any media.
- Analytics and crash-reporting providers.
- Monetization, if the product proceeds beyond personal and portfolio use.
