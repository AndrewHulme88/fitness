# Backend Instructions

Apply the repository-level instructions first.

- Keep ASP.NET Core endpoints thin and feature/domain behavior independently testable. Use nullable C#, pass cancellation through supported asynchronous operations, and avoid custom cryptography or identity protocols.
- PostgreSQL is the persistence source of truth. Use explicit EF migrations and prove persistence behavior with real PostgreSQL-compatible integration tests, not mocked or in-memory EF Core.
- Authenticate protected endpoints and enforce authorization and ownership for every user-owned resource. Validate inputs, bound queries and payloads, and avoid logging sensitive content.
- The API owns the OpenAPI contract. Regenerate and review the committed contract and frontend generated types after transport changes; do not edit generated artifacts directly.
- Keep AI provider credentials and calls server-side. Treat model output as untrusted, send the minimum authorized context, and require deterministic validation plus explicit user approval before consequential changes.
- Add benchmarks only when a representative API or database hot path changes. Use bounded, realistic data and record meaningful results in `DEVELOPMENT.md`.
