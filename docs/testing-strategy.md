# Testing and Performance Strategy

## Objectives

Testing should provide fast feedback during small increments and credible evidence that the product is correct, secure, safe, accessible, and responsive. Test quantity is not the goal; coverage of meaningful behavior and failure modes is.

## Increment workflow

For each behavior change:

1. Define observable acceptance criteria.
2. Add or update the smallest test that proves the behavior. For a bug, reproduce it with a failing regression test when practical.
3. Implement the minimum change.
4. Run only the closest relevant checks while iterating.
5. At increment completion, run the full affected frontend or backend suite, required static checks, and manual verification that automation cannot represent.
6. At phase completion, run the broader repository and integration checks appropriate to the phase.
7. Record only important issues, decisions, or measurements in `DEVELOPMENT.md`.

Documentation-only changes require link, consistency, formatting, and secret checks rather than artificial unit tests.

Simulator or physical-device checks are required for visual, gesture, navigation, accessibility, or other native behavior that automated tests cannot adequately establish. Do not launch them routinely for changes already covered by focused tests.

## Client tests

Use the smallest appropriate layer:

- Unit tests for pure formatting, validation, calculations, reducers, and state transitions.
- Component tests for user-visible behavior, accessibility labels, loading, error, and interaction states.
- Navigation/integration tests for important multi-screen behavior.
- A small number of end-to-end tests for critical journeys once the journeys stabilize.
- Manual simulator and physical-device checks for native behavior that automated tests do not represent well.

Avoid large snapshot files as the primary assertion. Prefer assertions about behavior, semantics, and visible outcomes.

Initial critical client journeys will include:

- Complete focused onboarding.
- Find an exercise and create a workout.
- Start a workout, log sets, recover from interruption, and finish.
- Review recorded history.
- Ask the coach a question and safely handle provider failure.
- Review, reject, edit, or accept a proposed plan change.

## API and domain tests

- Unit tests for domain invariants, calculations, validation, and authorization policies that can run without infrastructure.
- HTTP integration tests for routing, serialization, validation, authentication, authorization, error shape, and cancellation.
- Persistence integration tests against PostgreSQL for mappings, constraints, concurrency, transactions, migrations, and representative queries.
- Contract tests or deterministic generation checks for the OpenAPI document and TypeScript client.
- Negative tests for cross-user access on every user-owned resource type.

Do not use mocked EF Core behavior as evidence that PostgreSQL behavior is correct.

Persistence integration tests use Testcontainers with the same exact PostgreSQL image as local Compose development. Each test database is disposable and isolated from the developer's persistent Compose volume. Docker is therefore a prerequisite for the backend integration suite.

## AI tests and evaluations

Automated application tests must use a fake provider and deterministic fixtures. They should verify:

- Correct context selection and redaction.
- Timeout, cancellation, retry, quota, and outage behavior.
- Strict schema handling and rejection of malformed output.
- Deterministic domain validation of proposed actions.
- Explicit confirmation before action execution.
- Privacy boundaries and per-user isolation.

Model behavior is evaluated separately with a versioned case set. The evaluation suite described in `ai-safety.md` runs before changes to provider, model, prompt, tools, context assembly, or response schemas.

Live-model evaluations must have explicit cost limits and must not contain real user data.

## Security verification

Security testing grows with the feature surface and includes:

- Secret scanning and dependency vulnerability checks in CI.
- Authentication and authorization negative cases.
- Ownership and tenant-isolation checks.
- Input size, malformed input, and injection tests at trust boundaries.
- Rate-limit and abuse-control verification.
- Log inspection to ensure tokens and sensitive bodies are absent.
- Mobile storage and transport review before beta.
- A lightweight threat model for identity, AI tools, offline data, photos, and account deletion when those capabilities are introduced.

## Accessibility and visual quality

Automated checks cannot establish visual quality alone. Each affected screen should be reviewed for:

- Dynamic Type and text truncation.
- VoiceOver names, roles, values, order, and actions.
- Contrast and non-color state indicators.
- Touch target size and one-handed use where relevant.
- Light and dark appearance.
- Loading, empty, error, offline, disabled, and interrupted states.
- Keyboard behavior and reduced motion where applicable.
- Multiple supported iPhone sizes.

Material UI changes should include simulator screenshots in the change record or review workflow once one exists.

## Performance methodology

Performance work begins with a user-relevant question and a representative workload. Record:

- Device or machine and build configuration.
- Dataset and scenario.
- Tool and command.
- Warm-up and sample approach.
- Median and a tail measure where appropriate.
- Expected environmental variance.
- Baseline and result.

Candidate client measures:

- Cold and warm application startup.
- Time to interactive for the initial and active-workout screens.
- Input response while logging a set.
- Render count for frequently updated components.
- Scrolling behavior with representative workout history.
- Memory behavior during a long session.

Candidate server measures:

- Latency and allocation for meaningful domain hot paths.
- API latency under representative concurrency.
- Important PostgreSQL query plans, rows examined, and latency.
- Payload size and number of round trips for critical journeys.

Candidate AI measures:

- End-to-end latency and time to first useful output.
- Input and output tokens by use case.
- Context size growth over a conversation.
- Provider errors, schema failures, and retry amplification.
- Cost per representative coaching task.

Use a microbenchmark framework only for stable in-process code. Use integration or load tooling for APIs and databases. Do not compare debug and release results or treat simulator results as final physical-device evidence.

## Performance gates

- Establish a repeatable baseline before setting a numeric regression threshold.
- Keep performance suites separate from fast unit tests when their runtime or variance warrants it.
- Do not fail CI on noisy measurements until the environment and threshold are demonstrably stable.
- A known material regression must be fixed, explicitly accepted with rationale, or block the increment.
- Store important findings as `P-###` entries in `DEVELOPMENT.md`.

## Continuous integration direction

As projects are added, CI should grow incrementally to include:

- Formatting and lint checks.
- TypeScript type checking.
- Client unit and component tests.
- .NET restore, build with warnings enforced, and tests.
- PostgreSQL integration tests.
- OpenAPI/client generation drift checks.
- Secret and dependency checks.
- Selected end-to-end, AI evaluation, and performance workflows when stable enough.

CI configuration should not be added before there is a real command for it to run.

## Definition of done for code increments

- Acceptance criteria are covered by the appropriate test level.
- Relevant focused and full affected suites pass.
- Manual verification is completed where automation is insufficient.
- Failure paths, authorization, accessibility, and data handling were considered.
- Relevant performance behavior was measured when the change touches a sensitive path.
- Test commands and results are reported accurately.
- The development journal and other documentation are updated when the increment produced a meaningful decision or lesson.
