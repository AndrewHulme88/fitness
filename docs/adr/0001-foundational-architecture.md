# ADR-0001: Foundational application architecture

- Status: Accepted
- Date: 2026-08-24

## Context

The product needs a native mobile experience, relational workout data, secure identity, deterministic fitness-domain behavior, and controlled access to an AI provider. It is being built by a small project team and must remain understandable, demonstrable, and economical before public release is certain.

## Decision

- Build the mobile client with React Native, Expo, and strict TypeScript.
- Build one ASP.NET Core API on .NET 10 LTS.
- Structure the API as a feature-oriented modular monolith.
- Use EF Core with PostgreSQL as the source of truth.
- Publish an OpenAPI contract and generate the TypeScript transport client.
- Keep deployment cloud-neutral until hosting requirements are known.
- Add infrastructure only when an active product increment requires it.

## Consequences

- The team works across TypeScript and C#, and transport contracts require generation rather than direct type sharing.
- One API and database reduce deployment, debugging, transaction, and local-development complexity.
- Feature boundaries must be maintained through code organization and ownership rather than network separation.
- Android remains technically approachable through React Native even though it is not an initial delivery target.
- A module may later be extracted if evidence demonstrates an independent scaling, reliability, security, or ownership need.

## Alternatives considered

### TypeScript backend

This could simplify language sharing but was not selected because ASP.NET Core and C# are intentional technical and portfolio choices. OpenAPI generation addresses transport duplication without coupling domain models to the client.

### Microservices

Rejected for the initial product. They would add network failure modes, distributed tracing, deployment coordination, and data-consistency complexity without a demonstrated requirement.

### Native Swift client

This could maximize iOS-specific integration, but it would discard the desired React Native experience and increase the future cost of Android delivery.

### Universal browser client

Deferred. A browser application expands responsive design, navigation, authentication, and QA scope before the mobile training loop is proven.

