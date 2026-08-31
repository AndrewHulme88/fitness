# Architectural Decision Records

ADRs capture decisions that have broad or long-lived technical consequences. `DEVELOPMENT.md` remains the chronological journal; an ADR describes the current decision in a compact, reviewable form.

## Status vocabulary

- Proposed — under active consideration.
- Accepted — current direction.
- Superseded — replaced by another ADR.
- Deprecated — retained temporarily but should not guide new work.
- Rejected — considered and intentionally not selected.

## Records

| ADR                                                      | Status     | Decision                                                         |
| -------------------------------------------------------- | ---------- | ---------------------------------------------------------------- |
| [0001](0001-foundational-architecture.md)                | Accepted   | Expo client with an ASP.NET Core modular monolith and PostgreSQL |
| [0002](0002-ios-first-delivery.md)                       | Accepted   | Deliver iOS first and defer browser/Android releases             |
| [0003](0003-ai-coach-boundary.md)                        | Accepted   | Keep the AI coach advisory, backend-mediated, and user-confirmed |
| [0004](0004-expo-router-navigation.md)                   | Accepted   | Use Expo Router's stable native stack for mobile navigation      |
| [0005](0005-api-contract-workflow.md)                    | Accepted   | Generate the mobile API client from committed OpenAPI            |
| [0006](0006-minimum-onboarding-profile.md)               | Accepted   | Keep the initial onboarding profile closed, minimal, and local   |
| [0007](0007-internal-exercise-catalogue.md)              | Accepted   | Own and explicitly import a curated exercise catalogue           |
| [0008](0008-reusable-workout-templates.md)               | Accepted   | Store explicit, revisioned, profile-owned workout templates      |
| [0009](0009-recoverable-workout-sessions.md)             | Accepted   | Use snapshot sessions with a local durable outbox                |
| [0010](0010-explainable-workout-history.md)              | Accepted   | Derive factual history and progress from completed sessions      |
| [0011](0011-managed-identity-and-prototype-migration.md) | Superseded | Auth0 identity with explicit prototype-data migration            |
| [0012](0012-cognito-identity-and-prototype-migration.md) | Accepted   | Use Cognito identity with explicit prototype-data migration      |
| [0013](0013-account-export-and-deletion-lifecycle.md)    | Accepted   | Export directly and coordinate irreversible account deletion     |

## Template

```markdown
# ADR-NNNN: Title

- Status: Proposed
- Date: YYYY-MM-DD

## Context

## Decision

## Consequences

## Alternatives considered
```

Do not edit an accepted ADR to conceal a change in direction. Add a superseding ADR and link both records.
