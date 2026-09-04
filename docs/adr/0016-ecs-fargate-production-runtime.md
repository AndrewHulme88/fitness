# ADR-0016: Run the beta API on ECS Fargate behind an ALB and AWS WAF

- Status: Superseded by ADR-0017
- Date: 2026-09-04

## Context

The beta API needs a managed AWS runtime that can reach private Amazon RDS PostgreSQL, expose HTTPS without making PostgreSQL public, accept controlled deployments, publish privacy-minimized operational logs, and apply an edge rate limit in addition to the API's per-account limits. The product has no current need for multiple services, direct database access from the mobile app, or a self-managed Kubernetes control plane.

## Decision

- Run the ASP.NET Core API as an Amazon ECS Fargate service in private subnets with no public task IP address.
- Place an internet-facing Application Load Balancer in public subnets. It terminates HTTPS and forwards only to the ECS target group. The target group uses `GET /health/ready`; the existing `GET /health` remains process liveness only.
- Attach an AWS WAF web ACL to the ALB. It includes AWS managed baseline rules and a coarse, per-source-IP rate rule. It complements rather than replaces the API's authenticated per-account rate policies.
- Give the RDS security group one inbound rule: TCP 5432 from the ECS task security group. The ALB security group may reach the task security group only on the container port. No desktop, mobile-client, or internet security group may reach RDS.
- Store database and provider credentials in Secrets Manager, referenced by the ECS task definition. The task execution role can retrieve only the named secrets and write logs; the application task role contains only runtime permissions that the API actually needs. Do not use the RDS master account for the API.
- Use ECR for immutable container images, CloudWatch Logs for the privacy-minimized API output, and a deployment role that explicitly applies EF migrations before a new API task set serves traffic.

## Consequences

- Network, identity, load-balancer, WAF, ECR, ECS, Secrets Manager, and alert configuration are release work and must be provisioned reproducibly before beta.
- Tasks can be replaced without database exposure. Secret rotation requires a new ECS deployment because injected environment-variable secrets are read when a task starts.
- ECS Fargate and an ALB add a recurring operational cost, but avoid maintaining hosts or Kubernetes while retaining predictable private networking and WAF integration.
- At least two task subnets/AZs and two desired tasks are the production availability target once beta traffic and cost justify it. A single task may be used only for an explicitly recorded pre-beta smoke environment.

## Alternatives considered

### AWS App Runner

Rejected for beta production. It reduces initial setup, but private-database networking and the desired ALB/WAF ingress controls are less direct for this boundary.

### EC2-hosted API

Rejected. It adds host patching, capacity, and recovery responsibility without a demonstrated need for host-level control.

### Kubernetes

Rejected. A single modular-monolith API does not justify cluster operations or their security and availability overhead.
