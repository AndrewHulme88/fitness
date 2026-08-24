Project Plan

High-level phases (small increments). Each phase below is decomposed into 1–4 focused, test-first tasks with clear acceptance criteria and a benchmark or test where applicable.

1. Docs & repo scaffold (complete)
	- Deliverables: `README.md`, `AGENT.md`, `DEVELOPMENT.md`, `PLAN.md`.
	- Acceptance: files exist and reflect current constraints.

2. Initialize Expo app (TypeScript)
	- Task A: scaffold Expo TypeScript app with linting and Jest unit test setup.
	  - Acceptance: `mobile/` contains working app skeleton; `npm test` runs and passes a trivial test.
	- Task B: add CI job to run tests and lint on PRs.

3. Create ASP.NET Core API skeleton
	- Task A: scaffold `server/` with API project + xUnit test project.
	  - Acceptance: `dotnet test` passes a trivial test and Swagger is available in dev.
	- Task B: add CI job to build and test the API.

4. Auth and user model
	- Implement secure JWT-based auth with refresh tokens (or integrate Cognito later).
	- Acceptance: signup/login endpoints with tests and protected endpoint requiring valid token.

5. Workout generator (AI prototype)
	- Task A: create AI service adapter (pluggable) with mocked responses and unit tests.
	- Task B: simple workout-generation endpoint and mobile UI consuming it.
	- Acceptance: deterministic tests using mocked AI adapter pass; end-to-end smoke test.

6. Persistence: Postgres + EF Core
	- Add models, migrations, and repository layer.
	- Acceptance: integration tests run against a Docker Postgres instance (CI) and pass.

7. RAG and personalization
	- Evaluate vector store: `pgvector` vs managed service; implement chosen option.
	- Acceptance: embeddings stored/retrieved reliably; unit tests for RAG assembly.

8. Reminders & background tasks
	- Implement scheduled jobs (AWS-friendly approach: Lambda + EventBridge or container job).
	- Acceptance: task scheduler invoked in dev; tests for job logic.

9. Performance testing and benchmarks
	- Microbenchmarks for critical paths (AI call latency, DB queries, mobile render hot paths).
	- Acceptance: benchmark suite runs locally/CI; perf regressions fail CI when above threshold.

10. Prepare MVP release
	- Harden auth/privacy, finalize onboarding, add analytics, and prepare store assets.
	- Acceptance: smoke tests, performance checks, and manual QA checklist completed.

Workflow rules
- Test-first: every task requires tests that pass locally and in CI before merge.
- Minimal surface area: do not add folders or dependencies until required by an accepted task.
- Docs update: every significant decision or blocker is recorded in `DEVELOPMENT.md`.

Immediate next small step
- Scaffold the Expo app skeleton (`mobile/`) with TypeScript and a passing unit test.

Confirmed choices and rationale
- Mobile platform: **iOS first**. Focused platform reduces QA surface and speeds iteration for the initial MVP.
- Auth: **AWS Cognito** for user management. Why: managed, secure, scales, integrates with AWS infra, avoids early custom auth maintenance.
- Vector store: **pgvector** in Postgres initially. Why: minimal infra, local/dev parity, low cost, easy migration to a managed vector DB later.
- CI & mobile builds: **GitHub Actions** for CI and **EAS Build** for iOS builds. Why: Actions integrates with the repo, EAS handles macOS/iOS cloud builds without self-hosted mac runners.
- AI provider: deferred decision — evaluate OpenAI, Anthropic, AWS Bedrock when we have an AI prototype and clearer cost/latency needs.

Performance & testing requirements
- Test-first: every task requires unit/integration tests and passing CI before merging.
- Benchmarks: include microbenchmarks for AI latency, DB queries, and mobile render hot paths; fail CI on significant regressions.

Docs & traceability
- Keep `DEVELOPMENT.md` updated with every design decision, blocker, and why alternatives were rejected.
- Minimal repo surface: create files/folders only when required by accepted tasks.


AI provider note
- We will defer a final AI provider choice until we have a working prototype and clearer cost/latency requirements.
- Candidates include OpenAI, Anthropic, and AWS Bedrock; evaluate on cost, latency, available models, safety tools, and integration path.
- Acceptance: document the evaluation in `DEVELOPMENT.md` and pick a provider before full RAG implementation.

