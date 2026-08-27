# Fitness Coach

Fitness Coach is the working title for an iOS-first fitness application that combines workout planning and logging with a context-aware AI coach. The product is intended to feel calm, trustworthy, fast, and genuinely useful during training.

The repository has completed its foundation phase and the first three core-fitness increments. It now supports local onboarding, an internally owned exercise catalogue, and reusable workout planning across the Expo client, API, and PostgreSQL.

## Product intent

The initial product will help adults:

- Choose strength, muscle-building, and general-fitness goals.
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
- GitHub Actions for continuous integration, beginning with deterministic API contract drift checks.

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

## Repository layout

Directories are added only when their first increment begins.

```text
frontend/                                   Expo mobile application
backend/FitnessCoach.Api/                   ASP.NET Core API
backend/FitnessCoach.Api/Domain/            Small genuinely shared domain vocabulary
tests/FitnessCoach.Api.IntegrationTests/    API integration tests
contracts/FitnessCoach.Api.json             Committed canonical OpenAPI document
tools/api-contract/                         Isolated TypeScript contract generator
scripts/                                    Contract generation and verification commands
.github/workflows/                          Continuous integration workflows
docs/                                       Product, architecture, safety, and decision records
.config/dotnet-tools.json                   Pinned repository-local .NET tools
.env.example                                Fake local environment template
compose.yaml                                Local PostgreSQL service
Directory.Build.props                       Shared .NET build and analysis policy
FitnessCoach.slnx                           .NET solution
global.json                                 .NET SDK selection policy
```

Generated API types live with their only consumer under `frontend/src/api/generated`. They may move to a package only if another real consumer requires one.

## Client development

Prerequisites:

- Node.js 22.13 or newer. Use a maintained Node.js release compatible with Expo SDK 57.
- npm.
- Xcode with an installed iOS Simulator runtime for native development on macOS.

Install exactly from the lockfile:

```bash
cd frontend
npm ci
```

Run the quality checks:

```bash
npm run format:check
npm run typecheck
npm run lint
npm test
```

The onboarding form calls the local API. After starting the API as described below, copy the public-only client example and replace its port with the HTTP port printed by `dotnet run`:

```bash
cp .env.example .env.local
```

`frontend/.env.local` is ignored by Git. `EXPO_PUBLIC_` values are embedded in the application bundle and must never contain credentials; the API URL is public configuration, not a secret.

Then start the iOS application:

```bash
npm run ios
```

`npm run start` starts Metro without automatically selecting a platform. Product-facing Android and browser scripts are intentionally absent from the initial iOS scope.

## API development

Prerequisites:

- .NET 10 SDK. The repository's `global.json` accepts installed .NET 10 feature bands while preventing an accidental major-version change.
- Docker Desktop or another Docker-compatible engine with Compose support.
- A trusted ASP.NET Core development certificate only if you choose to test local HTTPS. The Expo simulator workflow below uses loopback HTTP to avoid local certificate-trust failures.

Restore the repository-pinned EF Core tool and NuGet dependencies, then run the quality checks. The integration suite starts its own disposable PostgreSQL container, so Docker must be running; it does not use or modify the development database.

```bash
dotnet tool restore
dotnet restore FitnessCoach.slnx --locked-mode
dotnet format FitnessCoach.slnx --verify-no-changes --no-restore
dotnet build FitnessCoach.slnx --configuration Release --no-restore
dotnet test FitnessCoach.slnx --configuration Release --no-restore --no-build
```

Configure the local database once:

```bash
cp .env.example .env
```

Replace the placeholder password in both `POSTGRES_PASSWORD` and `ConnectionStrings__Postgres` with the same local-only value. Never commit `.env`. Then load the environment, start PostgreSQL, and apply pending migrations:

```bash
set -a
source .env
set +a
docker compose up --detach --wait database
dotnet tool run dotnet-ef database update \
  --configuration Release \
  --project backend/FitnessCoach.Api/FitnessCoach.Api.csproj \
  --startup-project backend/FitnessCoach.Api/FitnessCoach.Api.csproj
```

Do not add `--no-build` unless the selected `--configuration` matches a build you just completed. EF discovers migrations from the compiled assembly; a stale configuration can incorrectly appear current for that older binary.

Import the versioned exercise catalogue explicitly after migrations are current:

```bash
dotnet run --project backend/FitnessCoach.Api/FitnessCoach.Api.csproj \
  --no-launch-profile --no-restore -- \
  --import-exercise-catalogue
```

The importer validates the entire embedded manifest before writing, runs transactionally, and is safe to repeat. Catalogue content changes must increment `catalogueVersion`; removal is refused until exercise retirement and workout-history behavior are designed.

Start the API over loopback HTTP from the same shell for Expo simulator development:

```bash
dotnet run --project backend/FitnessCoach.Api/FitnessCoach.Api.csproj --launch-profile http --no-restore
```

The launch profile selects an available loopback port and prints it at startup; use that HTTP port in `frontend/.env.local`. Plain HTTP is only approved for this loopback-only local workflow. `GET /health` is a liveness endpoint and does not query PostgreSQL. Console logs use JSON; HTTP request logging is limited to method, path, response status, and duration. Headers, query strings, and request or response bodies are excluded because they can contain sensitive fitness or authentication data.

Stop PostgreSQL while retaining local data with `docker compose down`. To deliberately reset the development database, use `docker compose down --volumes`; this permanently deletes the local Compose database volume.

The pinned PostgreSQL image is for loopback-bound development and isolated tests only, not production deployment guidance. Current upstream image findings and follow-up requirements are recorded as `I-008` in `DEVELOPMENT.md`.

## API contract workflow

The API owns the transport contract. ASP.NET Core generates the committed OpenAPI 3.1 document, and the mobile client consumes generated TypeScript route and schema types through a small typed fetch wrapper. Do not edit either generated artifact directly.

Install the two locked JavaScript dependency sets and restore .NET before generating:

```bash
npm ci --prefix frontend
npm ci --prefix tools/api-contract
dotnet restore FitnessCoach.slnx --locked-mode
```

After changing an endpoint or its metadata, regenerate and review both files:

```bash
bash scripts/generate-api-contract.sh
```

Before completing the increment, run the same non-mutating drift check used by CI:

```bash
bash scripts/check-api-contract.sh
```

The runtime document and unauthenticated local-prototype Profile, Exercise, and Workout endpoints are available only when the API runs in the Development environment. Contract generation also selects Development explicitly so those routes remain represented in the mobile contract. No interactive API documentation UI is installed.

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
- [docs/exercise-catalogue-policy.md](docs/exercise-catalogue-policy.md) — exercise ownership, licensing, identity, review, and media rules.
- [docs/adr/README.md](docs/adr/README.md) — architectural decision record index.

## Development principles

- Deliver small, complete, testable increments.
- Keep the core fitness experience useful without AI.
- Measure performance before optimizing and retain meaningful baselines.
- Prefer deliberate native design over generic template or “AI app” styling.
- Make privacy and security part of design, not release cleanup.
- Record both successful decisions and approaches that failed.

## Current status

Foundation work through Phase 3.3 is complete. The Expo client has accessible onboarding, a saved-workout list, catalogue discovery, explicit prescription editing, and drag plus VoiceOver reordering. The .NET API validates and persists profiles, an internally owned catalogue of 35 common exercises, and revisioned profile-owned workout templates through PostgreSQL. Prototype product routes remain Development-only until deployment and identity boundaries are intentionally introduced. The next increment to define is active workout logging in Phase 3.4.
