# ADR-0018: Host the closed MVP API on Fly.io in Sydney

- Status: Accepted
- Date: 2026-09-04

## Context

The closed MVP uses Neon PostgreSQL in Sydney. The earlier ECS/ALB/WAF design is deferred until public beta because its fixed AWS networking and runtime cost is disproportionate before demand is proven. The API still needs an HTTPS host that can run an ASP.NET Core container, inject encrypted secrets, perform database readiness checks, and stay geographically close to the database.

## Decision

- Deploy the ASP.NET Core API as one Fly.io Machine in the Sydney (`syd`) region for the closed MVP.
- Use Fly Proxy for HTTPS and configure a service-level `GET /health/ready` check. The API listens privately on port 8080; Fly terminates public TLS.
- Inject only the `fitness_api` connection string as the `ConnectionStrings__Postgres` Fly secret. Migration credentials are used only for an explicit, one-off migration deployment step and are never added as an application secret.
- Keep one Machine running initially to avoid cold starts during closed validation. Reassess autostop/autostart only after observing actual demand and acceptable resume behavior.
- Retain application per-account rate limits and place Cloudflare or an equivalent edge protection layer in front of the Fly custom domain before invitations. This is a coarse public-ingress layer, not a replacement for authenticated API limits.

## Consequences

- Co-locating the API and Neon database in Sydney avoids the additional application-to-database latency of a Singapore-hosted MVP service.
- The MVP has one running instance and is not highly available. Fly deployment roles, organization access, secrets, logs, health checks, cost limits, and incident notifications need review before invitations.
- A Dockerfile and `fly.toml` become deployment artifacts. They must not contain connection strings, Fly tokens, Sentry tokens, or other secrets.
- Public beta remains gated on the private AWS RDS/ingress/recovery architecture in [production-operations.md](../production-operations.md).

## Alternatives considered

### Railway in Singapore

Rejected for the closed MVP. It is simpler to operate but places the API away from the Sydney Neon database.

### Render in Singapore

Rejected. It has the same regional-latency trade-off without a compensating advantage for this API.

### Google Cloud Run in Sydney

Deferred. Its scale-to-zero model is attractive, but adds another cloud account and deployment model. Fly.io is already available to the product owner and better fits the immediate MVP goal.

### AWS ECS and ALB

Deferred to public beta as recorded in ADR-0016 and ADR-0017.
