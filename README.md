# Fitness Coach

Fitness Coach is the working title for an iOS-first fitness application that combines workout planning and logging with a context-aware AI coach. The product is intended to feel calm, trustworthy, fast, and genuinely useful during training.

The repository is currently in its foundation phase. No application code has been scaffolded yet.

## Product intent

The initial product will help adults:

- Set general strength and fitness goals.
- Create and follow structured workouts.
- Log sets with minimal friction.
- Review training history and progress.
- Ask an AI coach questions grounded in their approved profile and workout data.
- Review and explicitly approve any plan change proposed by the coach.

The AI coach is a general wellness feature. It will not diagnose injuries, prescribe treatment, or replace a qualified health or fitness professional.

## Initial scope

- iOS first, using React Native, Expo, and TypeScript.
- Android is a later target; portable architecture should be preserved where it does not add significant current cost.
- No browser application in the initial scope.
- ASP.NET Core API on .NET 10 LTS.
- EF Core with PostgreSQL.
- A backend-only AI service boundary supporting OpenAI or an equivalent provider.
- GitHub Actions for continuous integration is the current direction, to be confirmed when the projects are scaffolded.

Not in the first release: injury rehabilitation, diagnosis, disease management, prescriptive nutrition, social features, wearables, subscriptions, a vector database, microservices, or a public web application.

## Architecture at a glance

```text
Expo iOS app
      |
      v
ASP.NET Core API -----> AI provider
      |
      v
 PostgreSQL
```

The API owns authorization and business rules. PostgreSQL is the source of truth. AI output is treated as untrusted advice, validated by deterministic application rules, and cannot silently modify a user's program.

See [docs/architecture.md](docs/architecture.md) for the full boundary description.

## Proposed repository layout

Directories will be added only when their first increment begins.

```text
apps/client/          Expo mobile application
services/api/         ASP.NET Core API
tests/                Cross-system or performance tests when required
docs/                 Product, architecture, safety, and decision records
```

The generated TypeScript API client may later live under `packages/` if generation and consumption justify a separate package.

## How work is organized

- [AGENTS.md](AGENTS.md) — binding working, quality, security, and design rules.
- [PLAN.md](PLAN.md) — active execution plan and the next small increment.
- [DEVELOPMENT.md](DEVELOPMENT.md) — append-focused journal of decisions, issues, and lessons.
- [docs/product-brief.md](docs/product-brief.md) — users, product problem, MVP, and non-goals.
- [docs/architecture.md](docs/architecture.md) — system boundaries and technical direction.
- [docs/roadmap.md](docs/roadmap.md) — outcome-oriented product phases.
- [docs/ai-safety.md](docs/ai-safety.md) — AI scope, safety controls, and escalation behavior.
- [docs/testing-strategy.md](docs/testing-strategy.md) — automated, manual, security, AI, and performance validation.
- [docs/adr/README.md](docs/adr/README.md) — architectural decision record index.

## Development principles

- Deliver small, complete, testable increments.
- Keep the core fitness experience useful without AI.
- Measure performance before optimizing and retain meaningful baselines.
- Prefer deliberate native design over generic template or “AI app” styling.
- Make privacy and security part of design, not release cleanup.
- Record both successful decisions and approaches that failed.

## Current status

Foundation documentation is established. The next increment is to resolve the small set of product and infrastructure choices listed in `PLAN.md`, then scaffold the minimum Expo project with its quality gates and one passing test.

There are no build or run commands yet because application projects have not been created.
