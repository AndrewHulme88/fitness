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

## Issue log

No implementation issues have been encountered yet.

## Performance log

No executable paths exist yet, so no meaningful baselines have been recorded. Baselines will be added when the relevant client and API paths are introduced.

## Open decisions

These choices are intentionally unresolved until their requirements are clearer:

- Product name and final visual identity.
- The narrow initial training audience: general strength, hypertrophy, or a broader combination.
- Authentication and identity provider.
- Hosting provider and deployment topology.
- AI provider and model selection.
- Offline workout logging and synchronization design.
- Source and licensing for the exercise catalogue and any media.
- Analytics and crash-reporting providers.
- Whether progress photos belong in the MVP.
- Monetization, if the product proceeds beyond personal and portfolio use.
