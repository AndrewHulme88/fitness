# ADR-0009: Use snapshot sessions with a local durable outbox

- Status: Accepted
- Date: 2026-08-28

## Context

Active workout logging must remain responsive during ordinary network loss and app interruption without changing the reusable workout template or silently losing completed sets. The API is the source of truth, but an iPhone cannot depend on a successful request after every interaction. Phase 4 identity and multi-device ownership are not yet available, so this decision must remain explicit about its prototype boundary.

## Decision

Starting a workout is an online operation that creates a session from one plan revision. The session copies the plan name, ordered exercises, tracking modes, muscles, and prescriptions into immutable snapshot fields. Later plan edits cannot change that session.

Allow one active session per profile, enforced by a partial unique PostgreSQL index. The client generates stable UUIDs for the session, mutations, and added sets with Expo Crypto. It persists the complete current session in Expo SQLite after every edit, then synchronizes the complete mutable state with an optimistic revision. The API stores the last mutation UUID, so a retry after a lost response is idempotent. A newer local edit is rebased onto the acknowledged server revision and sent next.

Temporary transport failures retain a pending device copy and retry on demand or when the app becomes active. A revision conflict never silently chooses a winner: the device copy remains intact until the user explicitly elects to load the server copy. Completion may happen offline and is synchronized later. Starting and discarding remain online operations. A completed session becomes immutable in this phase, and the device retains it until completion is acknowledged.

The local prototype stores only its profile identifier, unit preference, and recoverable session payload in the app sandbox. It does not store credentials or AI provider keys. This is interruption recovery, not an assertion that application-level encryption, authenticated ownership, cross-device merge, backup policy, or account deletion is complete.

## Consequences

- Workout logging remains immediately usable after a session has started, even when connectivity is temporarily unavailable.
- The API receives bounded full-session writes of at most 20 exercises and 20 sets per exercise. This is deliberately simpler and safer than a premature operation log.
- Plan intent and completed actuals have separate persistence semantics.
- Conflicts are rare in the current one-device prototype but have a non-destructive recovery path.
- Users cannot start offline, discard offline, edit a completed session, or start another workout while a completion is still pending.
- Phase 4 must bind the local profile and every session route to authenticated ownership. Before beta, local cache protection, backup behavior, account deletion, multi-device policy, and telemetry must receive explicit review.

## Alternatives considered

Writing every set directly to the API was rejected because connectivity would sit on the critical logging path. Keeping only React state was rejected because process termination would lose work. Mutating the workout template was rejected because planned intent and completed history have different lifecycles. A per-operation event log or CRDT was rejected because the current one-profile, one-active-session requirement does not justify its merge and migration complexity. Last-write-wins was rejected because it can silently discard fitness data.
