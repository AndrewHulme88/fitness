# Development Record

This is a curated record of the project's consequential engineering history. It captures major architectural choices, difficult failures with reusable lessons, unresolved technical or security risks, and measured performance baselines. Current-state instructions belong in `README.md`, planned work belongs in `PLAN.md`, and full architectural rationale belongs in `docs/adr/`.

## Inclusion standard

Add or retain an entry only when it meets at least one of these thresholds:

- It changes a long-lived product, security, data, or architectural boundary.
- It records a non-obvious failure whose cause and resolution will help future work.
- It records an unresolved risk that must be revisited.
- It records a representative performance measurement or consequential performance decision.

Do not record routine implementation progress, ordinary test totals, minor visual adjustments, local command reminders, or facts already clear from code and current documentation. Consolidate related investigations into one accurate entry and prune details that no longer provide engineering value. Identifier gaps are intentional where older routine entries were removed during curation.

## Major decisions

### D-001 — 2026-08-24 — Deliver iOS first

Status: accepted

Deliver the first usable product for iOS and preserve normal React Native portability without spending current increments on Android-specific or browser work. This keeps design and native QA focused while retaining a practical later Android path.

Related ADR: [ADR-0002](docs/adr/0002-ios-first-delivery.md)

### D-002 — 2026-08-24 — Use a modular monolith backend

Status: accepted

Use an ASP.NET Core modular monolith with feature-oriented code, EF Core, and PostgreSQL. The product does not yet have the scale, deployment needs, or team boundaries that justify microservices; module and transaction boundaries should remain explicit so extraction is possible only if evidence later supports it.

Related ADR: [ADR-0001](docs/adr/0001-foundational-architecture.md)

### D-003 — 2026-08-24 — Keep AI advisory and server-mediated

Status: accepted

All model access will pass through a backend application boundary. The model receives minimum approved context, has no direct database authority, and cannot apply consequential changes without deterministic validation and explicit user approval. This protects credentials, user data, and fitness-safety boundaries while keeping provider choice replaceable.

Related ADR: [ADR-0003](docs/adr/0003-ai-coach-boundary.md)

### D-005 — 2026-08-24 — Prove the core workout loop before identity and AI

Status: accepted

Build onboarding, workout planning, active logging, history, and basic progress before authentication or live AI integration. This ensures the underlying training product is useful and reliable before adding external dependencies and lets later identity work start from concrete local-data migration requirements.

### D-007 — 2026-08-25 — Separate development and test PostgreSQL lifecycles

Status: accepted

Use a persistent loopback-bound Compose database for development and disposable Testcontainers databases for integration tests. Both use the same digest-pinned PostgreSQL image and committed EF migrations. Tests must never depend on a developer's mutable database or EF's in-memory provider. Process liveness remains independent from database readiness; a deployment must add and use a distinct readiness signal.

### D-009 — 2026-08-25 — Treat committed OpenAPI as the transport source of truth

Status: accepted

Generate OpenAPI from ASP.NET Core, commit the reviewed contract, and generate the Expo client's route and schema types rather than maintaining handwritten DTOs. The generator has its own pinned TypeScript 5 tool package because its supported compiler graph conflicts with Expo's TypeScript 6 graph and evaluated alternatives contained higher-severity advisories. Drift checks force fresh output and isolate build workers so stale incremental builds or development file watchers cannot make generation nondeterministic.

Related ADR: [ADR-0005](docs/adr/0005-api-contract-workflow.md)

### D-010 — 2026-08-26 — Keep the prototype profile minimal and Development-only

Status: accepted

The initial profile stores only closed goal, experience, equipment, and unit vocabularies. It collects no free-text medical or injury data. Until authentication and ownership exist, profile-owned API routes are mapped only in Development; possession of a random identifier is not treated as authorization.

Related ADR: [ADR-0006](docs/adr/0006-minimum-onboarding-profile.md)

### D-011 — 2026-08-27 — Own and explicitly import the exercise catalogue

Status: accepted

Own a versioned, text-only catalogue of curated common exercises instead of depending on a runtime vendor, scraped content, or unresolved media licensing. Validate the complete manifest before an explicit transactional import, preserve permanent UUIDs, and refuse silent removal or identifier reassignment. The content still requires qualified fitness review before public release.

Related ADR: [ADR-0007](docs/adr/0007-internal-exercise-catalogue.md)

### D-012 — 2026-08-27 — Model workouts as explicit reusable templates

Status: accepted

Store profile-owned templates with explicit exercise order and tracking-mode-specific prescriptions. Persist canonical kilograms, metres, and seconds while converting only at the client boundary. Use optimistic revisions to reject stale edits and never invent training targets before a reviewed deterministic programming policy exists. Templates remain distinct from completed training actuals.

Related ADR: [ADR-0008](docs/adr/0008-reusable-workout-templates.md)

### D-013 — 2026-08-28 — Separate recoverable session actuals from immutable plan snapshots

Status: accepted

An active session snapshots one workout revision, then stores planned context separately from actual sets, skips, completion, and notes. The iOS client keeps the bounded active document in SQLite and synchronizes revisioned, idempotent full-document mutations using client-generated UUIDs. Conflicts require an explicit user choice; the app does not silently overwrite a newer copy.

This is interruption recovery, not a finished multi-device synchronization or encrypted-backup policy. Identity, cache ownership, account deletion, and cross-device behavior remain required before beta.

Related ADR: [ADR-0009](docs/adr/0009-recoverable-workout-sessions.md)

### D-014 — 2026-08-29 — Keep history and progress factual and bounded

Status: accepted

Completed sessions are the source of truth. Corrections use a distinct optimistic-revision boundary and cannot rewrite the original plan snapshot. Progress exposes only traceable rolling totals and bounded exercise appearances; it does not infer personal records, calories, streaks, scores, or trends without approved rules.

The first nested EF grouping projection could not be translated. The final design performs filtering, scalar grouping, ordering, and caps in PostgreSQL, then maps the bounded materialized rows into response records. This avoids both provider-specific projection failures and unbounded client-side aggregation.

Related ADR: [ADR-0010](docs/adr/0010-explainable-workout-history.md)

### D-019 — 2026-08-30 — Use managed OIDC identity with explicit prototype-data transfer

Status: superseded by D-020

Auth0 Universal Login with authorization code flow and PKCE was initially selected for iOS. Application ownership was to be keyed by stable issuer and subject claims, never by email or the client-held profile UUID.

Existing prototype data transfers only after sign-in and explicit user confirmation through an atomic, idempotent server-side link to one unclaimed profile. This preserves the local prototype's useful data without making UUID possession a claim mechanism. The project owner chose AWS instead because Cognito consolidates managed identity with the existing cloud account.

Related ADR: [ADR-0011](docs/adr/0011-managed-identity-and-prototype-migration.md)

### D-020 — 2026-08-30 — Consolidate managed identity on Amazon Cognito

Status: accepted

Use an Amazon Cognito User Pool with managed login, authorization code flow, and PKCE for the public iOS client. The API accepts only scoped Cognito access tokens validated by standard JWT bearer middleware; it verifies the User Pool issuer, signature, expiry, access-token use, client identifier, and required scope.

The application's account and explicit prototype-data migration design remain unchanged. Cognito is selected because the project owner already uses AWS, which consolidates identity's billing, IAM administration, and regional controls without putting AWS credentials in the mobile client. Sign in with Apple remains a release requirement when third-party or social login is offered.

Related ADR: [ADR-0012](docs/adr/0012-cognito-identity-and-prototype-migration.md)

### D-021 — 2026-08-31 — Export directly and coordinate irreversible deletion through Cognito

Status: accepted

Export application-held account data as a versioned JSON attachment generated directly from PostgreSQL, without email delivery, object-storage copies, or server-side export retention. Require fresh authorization for export and deletion. Deletion persists a data-minimized, short-lived operation before Cognito self-service user deletion, then purges the PostgreSQL account transactionally. A protected retry path completes a purge interrupted after Cognito success, while a 45-day keyed-identity tombstone prevents a backup restore from resurrecting deleted data.

The final deletion confirmation is irreversible: Cognito identity, application fitness data, active local session state, and local credentials are removed with no recovery or grace period. Encrypted production backups must expire within 35 days and reconcile deletion operations before restored data is available. This is a design decision only; implementation, provider configuration, threat model, and backup-restore verification remain beta gates.

Related ADR: [ADR-0013](docs/adr/0013-account-export-and-deletion-lifecycle.md)

### D-022 — 2026-08-31 — Retain user-deletable coach conversations as account data

Status: accepted

Retain one application-owned, user-visible coach conversation per profile until the user explicitly deletes it or deletes their account. Store only messages shown to the user, timestamps, response outcome, and displayed context-source labels; do not retain provider payloads, hidden reasoning, credentials, or unrestricted context. This gives users review and deletion control without making the provider conversation ID authoritative.

Account export and deletion must include coach conversation data when those designed capabilities are implemented.

Related ADR: [ADR-0014](docs/adr/0014-retained-coach-conversations.md)

## Major issues and open risks

### I-001 — 2026-08-24 — Expo transitive UUID advisory

Status: open

`npm audit --omit=dev` reports ten moderate findings through Expo's native build-tool chain: Expo config plugins → `xcode@3.0.1` → `uuid@7.0.3`. The application does not call this dependency directly, while npm's automatic remediation would downgrade Expo to an incompatible release and an unverified major override could break native project generation.

Keep the supported Expo dependency graph, do not use `npm audit fix --force`, and re-check on Expo updates and before native release builds. Escalate if runtime exposure or severity changes. Last verified on 2026-08-25: 10 moderate, 0 high, 0 critical production findings.

### I-002 — 2026-08-24 — Expo lint stack uses an end-of-life ESLint major

Status: open

Expo SDK 57's supported lint graph resolves to ESLint 9, whose release line reached end of support. Forcing ESLint 10 would place the installed React lint plugin outside its declared peer compatibility, while replacing the official stack would create duplicate tooling without a demonstrated benefit.

Retain the working Expo configuration temporarily and upgrade as soon as the supported Expo/plugin graph declares ESLint 10 compatibility. Treat any new security finding in the EOL line as higher priority.

### I-003 — 2026-08-25 — Expo Router peer resolution selected incompatible patches

Status: resolved

An unconstrained Router installation selected React Native Reanimated, Worklets, React DOM, React Test Renderer, and Testing Library versions that did not match Expo SDK 57 or React 19.2.3. Clean install and Router tests failed even though each package was individually current.

Pin native modules to Expo's compatibility map, pin React renderer packages to the exact React version, and keep React Native Testing Library on the Router-compatible 13.3 line. Never conceal this graph with `--force` or `--legacy-peer-deps`; reassess the direct pins during an Expo SDK upgrade.

### I-008 — 2026-08-25 — Official PostgreSQL images retain upstream high and critical findings

Status: open

Docker Scout found high and critical vulnerabilities in every evaluated official PostgreSQL 18.6 image. Alpine 3.24 had the lowest result—2 critical and 20 high—attributed to the Go standard library embedded in the upstream `gosu` helper, compared with worse Alpine 3.23 and Debian results.

The digest-pinned Alpine 3.24 image is approved only for loopback local development and isolated tests with synthetic credentials. Re-scan on image updates and before beta. Production requires a separate managed-service or image security review and cannot inherit this decision.

### I-013 — 2026-08-27 — EF no-build command used a stale migration assembly

Status: resolved

The local migration command reported the database as current even though `workout_plans` did not exist. A Release build had been verified, but `dotnet ef --no-build` loaded the older default Debug assembly and correctly saw only the migrations compiled into that stale binary.

EF migration commands must build current source or explicitly select the same configuration that was just built. When schema and migration history disagree, inspect `__EFMigrationsHistory` and the compiled configuration before changing database state; never repair this by editing history manually.

### I-015 — 2026-08-28 — Client-generated set identifiers were tracked as updates

Status: resolved

Adding a set to an active session produced an optimistic-concurrency failure because EF attached the new child, which already had a client-generated UUID, as `Modified` and attempted to update a row that did not exist.

Capture persisted set identifiers before aggregate synchronization and explicitly mark only newly introduced children as `Added`. Retaining client-generated identity is important for offline stability and retry idempotency; switching to server-generated IDs would make pending additions and repeated mutations ambiguous.

### I-018 — 2026-08-29–30 — Workout reordering was unreliable and scrolled the planner

Status: resolved

The initial handle drag failed in several distinct ways during real Simulator use:

- Gesture callbacks captured the render where no drag was active, so movement could not see the state created at gesture start.
- The gesture-handler pan was cancelled by the surrounding native `ScrollView` on release. Treating cancellation as a successful drop made taps and jitter reorder rows unexpectedly.
- The first destination calculation changed rows as soon as the dragged centre met its own midpoint instead of requiring it to cross a neighbouring row.
- Removing the explicit edge auto-scroll stopped programmatic movement, but the native `ScrollView` could still pan during the same vertical gesture.

The final implementation keeps transient drag identity and layout in stable refs, gives the dedicated handle a React Native `PanResponder`, commits only on release, treats termination as cancellation, and changes destination only after crossing an adjacent row's centre. The handle reports drag state to the planner, which sets `scrollEnabled={false}` from responder grant until release or termination. Explicit edge auto-scroll was removed. VoiceOver adjustable move actions remain available as the non-drag equivalent.

This approach intentionally requires source and destination rows to be visible before a drag begins. Attempts to coordinate external gesture-handler scroll relations, change iOS cancellation flags, accept cancelled drops, or rely on responder ownership without explicitly disabling the `ScrollView` did not provide reliable behavior.

Evidence: controlled Simulator drags work in both directions; taps, small movements, and terminated gestures do not reorder; and on 2026-08-30 the user confirmed that the planner now remains locked while dragging and reordering.

## Performance baselines

### P-001 — 2026-08-28 — Active-session edit and serialization

Status: accepted baseline

The benchmark updates the last set in the maximum supported 20-exercise by 20-set active session and serializes the complete bounded document. Across 10,000 operations in the Jest/Node development environment, it measured 0.050 ms median, 0.060 ms p95, 0.050 ms minimum, and 0.080 ms maximum.

This isolates reducer and persistence-payload cost; it is not evidence of physical-device touch latency. Keep the benchmark separate and do not add a CI threshold until repeated environments establish stable variance.

### P-002 — 2026-08-29 — Completed-history and progress queries

Status: accepted baseline

A PostgreSQL integration benchmark seeds 200 completed one-exercise, one-set sessions, performs five warm-ups, and records 30 sequential warm-cache samples through the real ASP.NET Core, EF Core, Npgsql, and PostgreSQL path.

History measured 1.16 ms median and 1.70 ms p95; progress overview measured 2.52 ms median and 2.90 ms p95. These local development measurements are comparative baselines, not service-level objectives. Retain explicit query bounds and rerun the scenario when those projections or indexes change.
