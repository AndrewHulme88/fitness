# ADR-0017: Use Neon PostgreSQL for the closed MVP validation release

- Status: Accepted
- Date: 2026-09-04

## Context

The RDS, ECS, ALB, WAF, and multi-AZ production design is appropriate for public beta, but its fixed operating cost is disproportionate before the product has evidence of paying demand. The application already uses standard PostgreSQL through EF Core and Npgsql, has explicit migrations, and does not require direct client access to the database.

## Decision

- Use Neon Launch PostgreSQL in the Sydney region for the closed MVP validation release. It is a standard PostgreSQL endpoint; application code and migrations must not depend on Neon-only features.
- Keep the database connection string only in the API host's secret store. Mobile clients never receive it or connect to PostgreSQL directly. Require TLS with certificate verification and use a dedicated application role rather than an owner role.
- Accept Neon's Launch recovery window of up to seven days for this limited release. Run a synthetic restore and deletion-tombstone reconciliation check before inviting users and monthly while the release remains active.
- Limit this posture to a closed MVP validation release. Public beta remains blocked until a 35-day recovery design, private database networking, coarse ingress protection, alerting, and an isolated restore drill are implemented. Amazon RDS remains the approved public-beta database direction.
- Keep migrations portable by using standard PostgreSQL features, committed EF migrations, and a tested logical export/restore path to RDS. A later RDS migration uses a maintenance window, integrity checks, deletion reconciliation, and a connection-secret cutover.

## Consequences

- The MVP avoids the immediate fixed cost of RDS and its surrounding AWS runtime/networking stack, but has a shorter recovery window and provider-managed public database endpoint.
- The MVP API host must still protect its own secrets, use HTTPS, emit privacy-minimized logs, authenticate all production routes, and apply the existing per-account rate limits. The abbreviated recovery posture is not suitable for an open public beta.
- Neon terms, data-region selection, access roles, retention, and incident-notification controls must be reviewed before live data is stored. No connection string, endpoint, account identifier, or backup identifier belongs in this repository.

## Alternatives considered

### Retain RDS with ECS, ALB, WAF, NAT gateways, and multi-AZ from the MVP

Deferred to public beta. It offers a stronger operational boundary, but the fixed cost is not justified before demand is proven.

### Neon free plan

Rejected for live MVP data. Its limited recovery window and development-oriented lifecycle are insufficient for even a closed validation release.

### Self-host PostgreSQL on a small VPS

Rejected. It may lower the invoice, but transfers patching, backup, access-control, encryption, recovery, and incident responsibilities to the product team.

