# Project Instructions

These instructions apply to every change in this repository. They exist to keep the project focused, secure, testable, and professionally presented.

## Product direction

- Build an iOS-first fitness coaching application for adults aged 18 and over.
- Use React Native with Expo and TypeScript for the mobile client.
- Use ASP.NET Core, C#, EF Core, and PostgreSQL for the backend.
- Keep Android portability in mind, but do not add Android-specific scope until it is intentionally scheduled.
- A browser application is not part of the initial scope.
- The AI coach is advisory. It must not diagnose, treat, or represent itself as a medical professional.

## Required reading

Before making a substantive change, read the relevant parts of:

- `README.md` for product scope and repository orientation.
- `PLAN.md` for the active increment and sequencing.
- `DEVELOPMENT.md` for prior issues and decisions.
- `docs/architecture.md` for system boundaries.
- `docs/ai-safety.md` for any AI, coaching, health, or user-data work.
- `docs/testing-strategy.md` for validation expectations.

## Working method

- Work in small, focused, independently verifiable increments.
- Prefer one vertical slice or one clearly bounded technical change at a time.
- Do not combine unrelated cleanup, dependency upgrades, refactors, and feature work.
- Avoid app-wide edits unless they are necessary and discussed first.
- Preserve existing user changes and inspect the working tree before editing.
- Do not add speculative folders, dependencies, abstractions, services, or configuration.
- Use the simplest design that satisfies the current requirement without blocking a known next step.
- Ask before proceeding when a decision materially affects product behavior, privacy, security, recurring cost, external services, data ownership, or broad architecture.
- Record meaningful discoveries, blockers, rejected approaches, and decisions in `DEVELOPMENT.md` during the same increment.
- Add or update an ADR in `docs/adr/` when a decision has long-term architectural consequences.

## Quality standard

“No AI slop” means every result must look and read as if it was deliberately designed and reviewed by an experienced product team.

- Do not ship generic template screens, placeholder dashboards, canned motivational text, or invented metrics.
- Avoid the stereotypical AI-product aesthetic: gratuitous purple/blue gradients, glowing borders, excessive glass effects, sparkles, oversized pill controls, excessive cards, and animation without purpose.
- Do not default to a fashionable font because it appears in common AI-generated designs. Use the iOS system typeface initially; adopt another typeface only through an explicit, documented design decision with appropriate licensing.
- Prefer calm hierarchy, excellent spacing, legible typography, native interaction patterns, and restrained color.
- Design loading, empty, error, offline, disabled, and permission-denied states—not only the happy path.
- Use real product language. Avoid filler, hype, fake testimonials, and vague coaching claims.
- Keep code equally deliberate: clear names, cohesive modules, no unexplained magic values, no dead code, and no premature generalization.
- Generated code and assets must be reviewed, adapted to the project, and held to the same standard as handwritten work.

## Testing and verification

- Tests are part of implementation, not a later phase.
- Write tests before or alongside behavior changes. Start bug fixes with a failing regression test when practical.
- Run the smallest relevant test set while iterating and the full affected suite before completing an increment.
- Never report a test as passing unless it was actually run.
- Cover domain rules, authorization boundaries, failure paths, and data ownership—not only successful requests.
- Use integration tests with a real PostgreSQL-compatible environment for persistence behavior; do not rely only on mocked EF Core behavior.
- Keep AI tests deterministic by replacing provider calls with fakes. Maintain separate evaluation cases for prompt and model behavior.
- Add benchmarks when a performance-sensitive path is introduced or changed. Do not create meaningless microbenchmarks for ordinary glue code.
- Record benchmark environment, inputs, baseline, and variance. Do not introduce a CI performance threshold until the baseline is stable enough to avoid flaky failures.
- Follow the full requirements in `docs/testing-strategy.md`.

## Performance

- Treat responsiveness as a product requirement, especially during active workout logging.
- Avoid unnecessary renders, network round trips, large payloads, unbounded queries, and repeated AI context.
- Measure before optimizing. Record the evidence and the result of meaningful performance work in `DEVELOPMENT.md`.
- Establish baselines for startup, key screen interactions, API latency, important database queries, and AI time-to-first-useful-output as those paths become available.
- Prefer pagination, bounded queries, cancellation, timeouts, and explicit resource limits.

## Security and privacy

- Never commit credentials, tokens, signing material, private keys, connection strings, or real personal/health data.
- Keep local secrets in ignored files or an approved secret store. Commit only clearly fake examples.
- Keep AI provider credentials on the server; the mobile client must never call a paid AI provider with a privileged key.
- Treat profile data, body measurements, workout history, progress photos, health constraints, and coach conversations as sensitive.
- Minimize collection and retention. Do not log sensitive request or prompt bodies by default.
- Authenticate every protected endpoint and enforce ownership or authorization on every user-owned resource.
- Validate all input at trust boundaries, including AI output and tool-call arguments.
- Use established cryptographic and identity libraries. Do not invent authentication protocols or encryption.
- Use secure mobile storage for tokens and platform security features where appropriate.
- Use synthetic data in tests, screenshots, fixtures, demos, and bug reports.
- Include abuse controls, rate limits, bounded output, and privacy-preserving user identifiers in the AI integration.

## Code conventions

- Enable strict TypeScript and nullable reference types in C#.
- Avoid `any`, null-forgiving operators, and warning suppressions unless the reason is local and documented.
- Keep UI components focused and business rules outside presentation components.
- Keep API endpoints thin and domain/application behavior independently testable.
- Pass cancellation through asynchronous server operations where supported.
- Generate the TypeScript API client from the backend OpenAPI contract rather than maintaining duplicate DTOs by hand.
- Pin dependencies through lockfiles and keep the dependency surface small.
- Treat compiler warnings, lint errors, and test failures as incomplete work.

## AI behavior

- The database and deterministic domain services are the source of truth.
- The model receives only the minimum approved context needed for the current request.
- Model output is untrusted and must pass schema and domain validation.
- The model may propose a consequential change, but the user must explicitly approve it before the application applies it.
- Keep progression limits, unit conversions, authorization, and safety gates deterministic.
- Do not let the model diagnose injuries, prescribe treatment or medication, encourage dangerous exercise, or facilitate disordered eating.
- Follow the escalation behavior and evaluation requirements in `docs/ai-safety.md`.

## Definition of done

An increment is complete only when:

- Its acceptance criteria are met without unrelated scope.
- Relevant automated tests pass and their commands are reported.
- Relevant manual or simulator checks are recorded.
- Security, privacy, accessibility, and performance impacts were considered.
- Documentation and the development journal reflect meaningful changes.
- No secrets, real user data, generated junk, debug output, or unexplained TODOs were added.

