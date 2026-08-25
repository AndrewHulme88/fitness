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

Status: pending

- Target .NET 10 LTS with nullable reference types and strict analyzers.
- Add a minimal health endpoint and integration test.
- Add structured logging with sensitive-body logging disabled.
- Document local build, test, and run commands.

### 2.2 Add PostgreSQL development infrastructure

Status: pending

- Add a pinned local PostgreSQL container configuration.
- Configure EF Core through environment-based settings.
- Prove connectivity and migration behavior with integration tests against PostgreSQL.
- Commit no real connection strings or credentials.

### 2.3 Establish the API contract workflow

Status: pending

- Publish an OpenAPI contract from the API.
- Generate a typed TypeScript client rather than duplicating DTOs.
- Add a CI check that detects contract or generated-client drift.

## Phase 3 — Core fitness experience without AI

### 3.1 Define profile and onboarding

Status: pending

- Capture only information needed for the initial workout experience.
- Include goals, experience, available equipment, units, and relevant self-declared constraints.
- Avoid collecting medical detail not required by the product.

### 3.2 Establish the exercise catalogue

Status: pending

- Resolve source, licensing, taxonomy, and media policy first.
- Add a small curated set sufficient for the initial experience.
- Test search, filtering, validation, and stable identifiers.

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

Perform Phase 2.1 only: scaffold the ASP.NET Core API and its test project with a minimal health endpoint, structured logging defaults, and documented local commands.
