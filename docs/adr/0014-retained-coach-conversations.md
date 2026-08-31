# ADR-0014: Retain one user-deletable coach conversation per profile

- Status: Accepted
- Date: 2026-08-31

## Context

Read-only coaching needs application-owned user-visible history rather than relying on a provider conversation identifier. Coach messages are sensitive fitness data, so retention must be understandable and user-controlled before a live provider is introduced.

## Decision

- Retain one conversation per training profile as application-owned data until the user deletes the conversation or deletes the account.
- Store only user-visible user and coach messages, their timestamps, response outcome, and the user-visible factual-context source labels.
- Do not store provider request objects, raw model responses beyond the user-visible text, chain-of-thought, credentials, or unrestricted fitness context.
- Include conversation data in the existing account-data export and deletion scope when those capabilities are implemented.

## Consequences

The conversation supports bounded application-owned memory and lets users review or remove what is retained. It adds sensitive data to the profile lifecycle, requiring ownership enforcement, cascading account deletion, export coverage, and provider-retention review before live data is sent externally.

## Alternatives considered

### Provider-owned conversation only

Rejected because it makes user-visible history dependent on an external service and weakens deletion and auditability.

### Ephemeral messages only

Rejected because the safety boundary requires an application-owned record of user-visible messages and it would prevent a coherent retained conversation.
