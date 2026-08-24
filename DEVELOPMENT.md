Development Notes

This file records design choices, blockers, and why decisions were made.

Initial decisions (2026-08-24)
- Mobile-only; no browser app for initial scope.
- Use Expo + React Native (TypeScript) for rapid mobile iteration.
- Backend with ASP.NET Core Web API and EF Core for relational data.
- Database: PostgreSQL (managed on AWS RDS).
- AI: OpenAI API or equivalent; use a service-layer for prompts and RAG.
- Hosting: AWS.
- Test-first workflow enforced; CI gates will require tests to pass.
- Keep repository minimal: create files/folders only when needed.
- Performance and benchmarks are required for key workflows.

Open questions / TODOs
- Determine vector store approach (pgvector vs managed service).
- Choose CI provider and runner configuration on AWS.
