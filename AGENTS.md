# Project Instructions

These rules apply throughout the repository. Read the nearest nested `AGENTS.md` before changing code in that area.

## Product boundaries

- Build an iOS-first fitness coaching app for adults 18 and over with Expo/React Native/TypeScript and ASP.NET Core/C#/EF Core/PostgreSQL.
- Preserve ordinary Android portability, but add no Android-specific scope or browser application unless scheduled.
- The AI coach is advisory only: it must not diagnose, treat, prescribe, or represent itself as a medical professional.

## Read only the context the change needs

- Read `README.md` and the relevant active section of `PLAN.md` before a substantive change.
- Read `docs/testing-strategy.md` before selecting verification.
- Read `docs/architecture.md` for structural, persistence, deployment, contract, or identity changes.
- Read `docs/ai-safety.md` for AI, coaching, health, privacy, or sensitive-user-data work.
- Consult `DEVELOPMENT.md` when investigating a known issue or recording a consequential decision, risk, failure, or performance result.
- Read the nearest nested instructions before working in `frontend/` or `backend/`.

## Working method

- Deliver one coherent, reviewable vertical increment at a time. Do not combine unrelated cleanup, upgrades, refactors, and feature work.
- Make safe reasonable assumptions; ask only when a choice materially affects product behavior, privacy, security, recurring cost, data ownership, or architecture.
- Preserve user changes and inspect the working tree before editing. Avoid speculative dependencies, folders, abstractions, services, or configuration.
- Prefer the simplest design that satisfies the current requirement without blocking a known next step.
- Match implementation effort and review depth to risk. Escalate careful reasoning for architecture, security, concurrency, native interaction, and other high-risk work; keep routine documentation, mechanical edits, and straightforward tests proportionate.
- Record only consequential discoveries, blockers, rejected approaches, decisions, and performance findings in `DEVELOPMENT.md`. Add an ADR for a long-term architectural decision.

## Verification

- Add or update focused tests with behavior changes; start a practical bug fix with a failing regression test.
- While iterating, run the closest relevant checks. At increment completion, run the complete affected frontend or backend suite and required static checks. At phase completion, run broader repository and integration checks.
- Use a simulator or device for visual, gesture, navigation, accessibility, or other native behavior that automated tests cannot adequately represent.
- Use real PostgreSQL-compatible integration tests for persistence behavior; mocked EF Core is insufficient.
- Keep AI tests deterministic with a fake provider and maintain separate model evaluations.
- Benchmark only when a performance-sensitive path changes; record meaningful baseline context and variance.
- Never report checks as passing unless they were run.

## Quality, security, and privacy

- Design deliberate, accessible native experiences: calm hierarchy, legible system typography, restrained color, and all meaningful loading, empty, error, offline, disabled, and permission states. Avoid generic AI-product styling and invented metrics or claims.
- Never commit credentials, tokens, signing material, private keys, connection strings, or real personal/health data. Use synthetic data in tests and demos.
- Treat profile data, workout history, constraints, photos, and coach conversations as sensitive. Do not log sensitive bodies, prompts, or responses by default.
- Authenticate protected endpoints and enforce ownership for every user-owned resource. Validate all trust-boundary input, including AI output and tool arguments.
- Keep paid AI-provider credentials server-side. Models receive minimum approved context, have no direct authority, and require deterministic validation plus explicit approval before consequential changes.
- Keep TypeScript strict and C# nullable. Avoid `any`, null-forgiving operators, warning suppressions, dead code, and unexplained magic values. Keep UI and API layers focused; pass cancellation where supported; generate TypeScript API types from OpenAPI.

## Done

An increment is complete only when its acceptance criteria are met without unrelated scope; appropriate tests and manual checks have run; security, privacy, accessibility, and performance impacts were considered; meaningful documentation is current; and no secrets, user data, generated junk, debug output, or unexplained TODOs were added.
