# ADR-0003: AI coach boundary

- Status: Accepted
- Date: 2026-08-24

## Context

The product's differentiating feature is a personal AI coach grounded in the user's training. Generative models can hallucinate, mishandle instructions, expose supplied data, and produce unsafe health guidance. Mobile-embedded provider credentials would also be recoverable by an attacker.

## Decision

- Access AI providers only through the backend.
- Depend on a product-level coach interface rather than spreading provider-specific types through the application.
- Keep PostgreSQL and deterministic domain services authoritative.
- Minimize and authorize context for each request.
- Treat model output and tool arguments as untrusted input.
- Use strict structured output for proposed actions.
- Validate every proposal using deterministic authorization, identity, unit, exercise, and progression rules.
- Show the user the proposed change and require explicit confirmation before execution.
- Prohibit diagnosis, treatment, rehabilitation, medication advice, dangerous exercise guidance, and disordered-eating assistance.
- Version prompts, schemas, models, and evaluation cases.
- Preserve core workout functionality when the provider is unavailable.

Detailed operational rules are defined in [`../ai-safety.md`](../ai-safety.md).

## Consequences

- The backend carries orchestration, validation, usage-control, and observability responsibilities.
- AI latency and outages must have explicit user experiences.
- Provider replacement is possible at the product boundary, but provider capabilities will not be forced into a false lowest-common-denominator abstraction.
- Live-model behavior requires evaluations in addition to deterministic unit and integration tests.
- Additional privacy review is required before sending real user context to a provider.
- The coach may be less autonomous, but its actions are more understandable, testable, and reversible.

## Alternatives considered

### Direct mobile-to-provider integration

Rejected because privileged keys cannot be protected in a distributed mobile application and server-side safety, budget, and audit controls would be weakened.

### Autonomous agent with database tools

Rejected because it would give probabilistic output too much authority over sensitive and consequential user data.

### Provider-specific implementation throughout the API

Rejected because it couples product logic, persistence, and tests to one vendor. The boundary will be product-oriented while still allowing provider-specific capability behind the adapter.

### No AI until after the complete fitness product

Not selected as a roadmap rule. The core workout loop will be established first, but an early bounded prototype remains useful for evaluating product value, latency, cost, and safety.

