# Closed MVP Operations

This runbook applies only to the closed MVP validation release. It permits a lower-cost Neon Launch PostgreSQL posture while preserving the application's existing privacy, authentication, and ownership boundaries. It does not authorize an open public beta or replace [production-operations.md](production-operations.md).

## Required controls

- Create the Neon project in the Sydney region on the Launch plan. Do not use the free plan for live MVP data.
- Host one API Machine on Fly.io in the Sydney (`syd`) region. Fly Proxy terminates public HTTPS and has a service-level `GET /health/ready` check; the API listens on an internal port only.
- Select the provider's strict TLS connection mode with certificate verification. Store the connection string only in the API host's secret manager or encrypted deployment secret; never in the Expo app, source control, logs, screenshots, or support tickets.
- Create a dedicated non-owner database role for the API. Run explicit EF migrations with a separate deployment role; the application runtime does not use the Neon owner role.
- The mobile client reaches only the HTTPS API. It never receives a PostgreSQL connection string or uses Neon directly.
- Keep protected API endpoints authenticated and preserve per-account API rate limits. Before invitations, place Cloudflare or an equivalent coarse per-IP protection layer in front of the Fly custom domain; Fly Proxy alone is not a WAF. Continue omitting headers, query strings, bodies, tokens, prompts, fitness data, account subjects, and IP addresses from server logs.
- Set only the `fitness_api` connection string as Fly's `ConnectionStrings__Postgres` application secret. Use the migration-role connection only for an explicit schema deployment, never as a Fly application secret.
- Review Neon’s current terms, Sydney data-region selection, account access roles, incident notification, and seven-day recovery retention before inviting people. No provider or support tool is an account-export or deletion source.

## Recovery and deletion check

Before invitations and at least monthly, use only synthetic data to:

1. Record the recovery point and start time without placing user data in the record.
2. Restore or branch from a point within the seven-day recovery window into an isolated Neon database.
3. Run the deployed migration-state check and synthetic integrity probes against that database.
4. Reconcile deletion-operation tombstones before any restored data could become available. A recently deleted account must remain unavailable after reconciliation.
5. Record duration, integrity, and deletion-reconciliation results in the restricted operations record, without fitness content.
6. Destroy the isolated restored database or branch.

An unsuccessful check, missing alert recipient, missing secret boundary, or unreviewed provider controls blocks MVP invitations.

## Fly.io deployment

The committed `backend/FitnessCoach.Api/Dockerfile` publishes the API as a non-root .NET 10 container on port 8080. `fly.toml` deploys one shared-CPU, 1 GB Machine in Sydney, forces HTTPS, trusts Fly's forwarded HTTPS scheme, and uses `GET /health/ready` as the traffic-serving check. It deliberately contains a placeholder app name and no secrets.

Before the first deploy:

1. Create a globally unique Fly app in the intended Fly organization and replace the `app` value in `fly.toml`. Do not use a personal name, account ID, or database identifier in the app name.
2. Set `ConnectionStrings__Postgres` from the `fitness_api` connection only as a Fly secret. Never set the `fitness_migrator` connection as an application secret.
3. Set the production Cognito configuration (`Cognito__Region`, `Cognito__UserPoolId`, `Cognito__AppClientId`, and `Cognito__RequiredScope`) and, when live coaching is enabled, `OpenAi__ApiKey` as Fly secrets. `OpenAi__Model` is non-secret configuration in `fly.toml`.
4. Deploy with Fly's remote builder when local Docker is unavailable. Verify the Fly service check and the public `/health/ready` response before configuring the Expo production API URL.

The service never runs EF migrations at startup. Apply reviewed migrations explicitly with the `fitness_migrator` connection before an API deployment, then grant the `fitness_api` role access to newly created tables and sequences. An API started without valid Cognito configuration maps only health endpoints and is not ready for invitations.

## Promotion to public beta

Before public beta, migrate to the AWS/RDS boundary in [production-operations.md](production-operations.md): private database networking, 35-day recovery retention, AWS ingress rate protection, production alerts, and the isolated RDS restore drill. Use a tested logical export/restore, a maintenance window, integrity and deletion-reconciliation checks, then change only the API connection secret.
