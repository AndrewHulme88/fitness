# Production Operations

This runbook defines the minimum public-beta operation boundary. The closed MVP validation release instead follows [mvp-operations.md](mvp-operations.md) and is not approved for public beta. This AWS boundary applies only after the production AWS account, Sentry project, and deployment automation are configured. Local Compose and Testcontainers remain development/test tooling and are not production recovery systems.

## Approved services

- Run the API in the existing AWS account and approved launch region as an ECS Fargate service in private subnets. An internet-facing Application Load Balancer terminates HTTPS and is the only public API ingress; it has an AWS WAF web ACL. The deployment uses private networking to reach PostgreSQL. See [ADR-0016](adr/0016-ecs-fargate-production-runtime.md).
- Use Amazon RDS for PostgreSQL. Enable encryption at rest and in transit, automated backups, point-in-time recovery, and a 35-day backup-retention period. Do not retain manual snapshots beyond that period unless they are subject to the same expiry control.
- Send server JSON logs, metrics, and traces to CloudWatch through the deployment runtime. Logs contain only operational metadata: route template, HTTP status, duration, deployment version, and bounded AI usage metadata. They must not include headers, query strings, bodies, tokens, raw prompts/responses, fitness data, account subjects, IP addresses, or database connection strings.
- Use Sentry only for iOS crash reporting. The mobile client initializes it only for non-development builds when `EXPO_PUBLIC_SENTRY_DSN` is configured. The DSN is public configuration; the source-map upload token is an EAS secret. Disable session replay, performance tracing, automatic session tracking, breadcrumbs, user data, request context, and custom fitness context.

Before enabling either provider, review its current data-processing terms, hosting region, retention controls, access roles, and incident-notification configuration. Do not use support exports or observability tools as a substitute for account export or deletion.

## Rate limits

The API enforces limits outside the Development environment and partitions them by authenticated Cognito `sub`. Neither the subject nor an IP address is logged. Limits are configuration rather than source-code constants:

| Policy | Default | Applies to |
|---|---:|---|
| Standard API | 120 requests / 60 seconds | Ordinary API routes |
| Active-session writes | 30 requests / 60 seconds | Start, synchronize, and discard session routes |
| Coach messages | 6 requests / 10 minutes | AI coach message route |

Rejected requests return `429` with `Retry-After`; requests are never queued. The in-process limiter is a per-instance guard, not a DDoS control. Production ingress must add AWS WAF or equivalent coarse per-IP protection before the public endpoint is exposed. Load-test the chosen limits with synthetic data before beta and adjust configuration only with a recorded rationale.

## Monitoring and alerting

Configure alert recipients before beta for:

- Sustained API 5xx rate, elevated route latency, and unhealthy deployment instances.
- RDS storage, connection, CPU, backup, and failed backup/restore signals.
- Rate-limit rejections by policy, without account or IP dimensions.
- New Sentry fatal crash issues by signed release/version.
- AI provider unavailability, timeout, malformed-output, and safety-limited outcome counts without content.

Use `GET /health` only for process liveness; it does not query PostgreSQL. Use the unauthenticated, non-rate-limited `GET /health/ready` for deployment readiness. It returns only `Ready` or `Unavailable`, never a database error, and has `Cache-Control: no-store`.

Alerts and dashboards must use aggregate counts, route names, release identifiers, and bounded categories only. Access is least-privilege, audited, and limited to the beta operations team.

## Restore verification drill

Run this drill with synthetic data before beta and at least monthly during beta:

1. Record the backup identifier, chosen recovery point, operator, start time, and expected recovery-point objective. Do not place production data in the drill record.
2. Restore the RDS backup to a new, isolated private instance. Do not attach it to the production API, public networking, analytics, or crash reporting.
3. Run the deployed migration-state check and synthetic integrity probes against the restored instance.
4. Reconcile deletion-operation tombstones before any restored data becomes available, as required by ADR-0013. A recently deleted account must remain unavailable after reconciliation.
5. Record restore duration, migration/integrity results, deletion-reconciliation outcome, and any remediation in `DEVELOPMENT.md` or the restricted operations record. Do not record fitness content.
6. Destroy the isolated restored instance and verify backup/temporary-resource expiry controls.

An unsuccessful drill, unbounded backup retention, missing deletion reconciliation, or an unconfigured alert recipient blocks beta distribution.

## Required external setup

- Create the AWS production environment, ECR repository, ECS task/execution/deploy roles, private task and database networking, ALB target group, WAF/inbound controls, Secrets Manager entries, RDS instance, backup retention, CloudWatch destinations, and alert recipients.
- Create the Sentry iOS project, configure its data controls and retention, add only its public DSN to the mobile build configuration, and store `SENTRY_AUTH_TOKEN` only as an EAS secret for source-map uploads.
- Verify a signed production-like iOS build reports one synthetic crash with symbolication, then remove the test path. Never test with a real user account or fitness data.

No credential, DSN, database endpoint, alert address, account identifier, or backup identifier belongs in this repository.
