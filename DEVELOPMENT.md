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
Use Expo Router with the stable native stack and typed routes. Keep route modules under `apps/client/src/app`, with the root layout responsible for shared stack presentation and safe loading/error fallbacks. Use `+not-found` for unavailable-route recovery.

Rationale:
Expo Router is aligned with the Expo toolchain and provides file-based routes, native deep-link support, typed destinations, and route integration testing without a custom navigation abstraction.

Alternatives considered:
Manually configured React Navigation would duplicate route configuration without a current benefit. Expo Router's experimental stack was rejected because this increment needs a stable foundation.

Consequences / follow-up:
Add route files only for active product increments. Do not introduce route groups, tabs, or additional navigators until a concrete journey requires them.

Related ADR: [ADR-0004](docs/adr/0004-expo-router-navigation.md)

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
`npm audit --omit=dev --json`, `npm explain uuid`, and `npm ls uuid xcode @expo/config-plugins` on 2026-08-24. Result: 10 moderate, 0 high, 0 critical vulnerabilities; all reported paths originate from the current Expo dependency graph. Re-running the production audit after the Phase 1.3 Router install on 2026-08-25 produced the same totals and chain.

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
