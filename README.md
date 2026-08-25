# Fitness Coach

Fitness Coach is the working title for an iOS-first fitness application that combines workout planning and logging with a context-aware AI coach. The product is intended to feel calm, trustworthy, fast, and genuinely useful during training.

The repository has completed its foundation phase and now contains a verified iOS client foundation with the initial navigation shell. Product behavior remains intentionally skeletal while the backend foundation is established.

## Product intent

The initial product will help adults:

- Set general strength and fitness goals.
- Choose the training goals that matter to them rather than being placed into one fixed program type.
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

Not in the first release: injury rehabilitation, diagnosis, disease management, prescriptive nutrition, progress photos, social features, wearables, subscriptions, a vector database, microservices, or a public web application.

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

Directories are added only when their first increment begins.

```text
apps/client/          Expo mobile application
services/api/         ASP.NET Core API
tests/                Cross-system or performance tests when required
docs/                 Product, architecture, safety, and decision records
```

The generated TypeScript API client may later live under `packages/` if generation and consumption justify a separate package.

Only `apps/client/` exists today. The other paths describe the intended structure and will not be created until needed.

## Client development

Prerequisites:

- Node.js 22.13 or newer. Use a maintained Node.js release compatible with Expo SDK 57.
- npm.
- Xcode with an installed iOS Simulator runtime for native development on macOS.

Install exactly from the lockfile:

```bash
cd apps/client
npm ci
```

Run the quality checks:

```bash
npm run format:check
npm run typecheck
npm run lint
npm test
```

Start the iOS application:

```bash
npm run ios
```

`npm run start` starts Metro without automatically selecting a platform. Product-facing Android and browser scripts are intentionally absent from the initial iOS scope.

## How work is organized

- [AGENTS.md](AGENTS.md) — binding working, quality, security, and design rules.
- [PLAN.md](PLAN.md) — active execution plan and the next small increment.
- [DEVELOPMENT.md](DEVELOPMENT.md) — append-focused journal of decisions, issues, and lessons.
- [docs/product-brief.md](docs/product-brief.md) — users, product problem, MVP, and non-goals.
- [docs/architecture.md](docs/architecture.md) — system boundaries and technical direction.
- [docs/roadmap.md](docs/roadmap.md) — outcome-oriented product phases.
- [docs/ai-safety.md](docs/ai-safety.md) — AI scope, safety controls, and escalation behavior.
- [docs/testing-strategy.md](docs/testing-strategy.md) — automated, manual, security, AI, and performance validation.
- [docs/design-system.md](docs/design-system.md) — selected visual direction, design tokens, accessibility, and visual review requirements.
- [docs/adr/README.md](docs/adr/README.md) — architectural decision record index.

## Development principles

- Deliver small, complete, testable increments.
- Keep the core fitness experience useful without AI.
- Measure performance before optimizing and retain meaningful baselines.
- Prefer deliberate native design over generic template or “AI app” styling.
- Make privacy and security part of design, not release cleanup.
- Record both successful decisions and approaches that failed.

## Current status

Foundation documentation, the Expo SDK 57 client, the Midnight Indigo design system, and the initial Expo Router shell are established. The shell covers onboarding, workout creation, an active workout, session summary, and safe loading, error, and unavailable states. Formatting, strict TypeScript, linting, focused route/component/token tests, clean installation, iOS bundling, deep linking, Dynamic Type behavior, and simulator layouts have been verified. The next increment is the minimal ASP.NET Core API scaffold described in `PLAN.md`.
