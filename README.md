Fitness Coach (mobile-first)

Personal coaching app that generates personalized workouts and coaching via AI.

Tech stack (initial):
- Mobile: React Native + Expo (TypeScript)
- Backend: ASP.NET Core Web API (C# / .NET)
- Database: PostgreSQL (via EF Core)
- AI: OpenAI API or equivalent (via a dedicated AI service layer)
- Hosting: AWS (managed services)

Key constraints and priorities
- Mobile-only (no separate browser app for initial scope)
- Test-first: every new feature must include tests that pass
- Small, focused increments; only create files/folders when needed
- Performance and benchmarks are a priority
- Clean, professional code and UX; avoid AI-styled gimmicks

Repository layout (will evolve):
- /mobile — Expo app
- /server — ASP.NET Core API
- /docs — documentation and operational notes

Next steps
1. Scaffold repo and add core docs (done).
2. Initialize Expo app with TypeScript and tests.
3. Create ASP.NET Core API skeleton with test project.

See `PLAN.md` and `DEVELOPMENT.md` for details and rationale.
