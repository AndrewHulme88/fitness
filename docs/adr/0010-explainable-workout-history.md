# ADR-0010: Derive factual history and progress from completed sessions

- Status: Accepted
- Date: 2026-08-29

## Context

Users need to review what they actually performed, correct recording mistakes, and see useful progress without opaque scores or unsupported claims. The active-session model already preserves the original workout snapshot and canonical actuals. History must not weaken that provenance, and its first queries need explicit bounds before the dataset grows.

## Decision

Treat completed workout sessions as the source of truth for both history and basic progress. Return completed history newest first through bounded offset pagination. The client groups UTC finish instants using the device's current local calendar because a persisted user time-zone preference does not yet exist.

Make corrections an explicit revision-checked operation rather than reopening a completed session. A correction may change recorded set completion, supported actual values, exercise skips, and bounded session or exercise notes. It cannot change the immutable plan snapshot, exercise or set identity and order, or session start and finish instants. Record the most recent correction timestamp so corrected data is visible, while deferring a full audit ledger until a concrete product or compliance requirement exists.

Keep progress explainable and derived from recorded facts. Report completed workouts, completed sets, and total recorded duration for a rolling 28-day UTC window. For each exercise, return no more than the latest 12 completed-session appearances and include only its tracking-mode-specific actual values. Do not infer personal records, trends, calories, readiness, scores, or streaks.

Keep history and correction online-only in this increment. Project bounded query results, cap list sizes, and index completed sessions by profile, status, and finish time. Retain the API as the source of truth.

## Consequences

- Users can distinguish original plan intent from corrected recorded results.
- Corrections cannot silently rewrite session identity, chronology, or exercise ordering.
- Progress values can be traced directly to completed sessions and sets.
- Device-local grouping can change if the user travels; account-level time-zone semantics remain a later decision.
- Only the latest correction time is retained, so a complete before-and-after audit trail is not yet available.
- Offline history, correction queuing, personal-record rules, and richer trends remain deliberately deferred.
- Phase 4 must replace route-carried profile identifiers with authenticated ownership checks.

## Alternatives considered

Reopening completed sessions through the active synchronization endpoint was rejected because it blurs lifecycle rules and could alter snapshot structure. An append-only event ledger was rejected as premature without an audit or multi-device requirement. Generic scores, inferred trends, streaks, and personal records were rejected because their rules would be opaque or unapproved. Unbounded history retrieval and client-side aggregation were rejected because their cost would grow with the account.
