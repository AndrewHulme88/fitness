# ADR-0008: Model workouts as reusable profile-owned templates

- Status: Accepted
- Date: 2026-08-27

## Context

The product needs deterministic workout creation and editing before it defines active-session logging, offline synchronization, progression, or AI-generated proposals. A workout plan must reference the curated catalogue, preserve the user's intended exercise order and targets, respect profile display units, and remain safe to edit without silently overwriting newer state.

## Decision

Represent a workout plan as a profile-owned aggregate with a name, monotonic revision, UTC timestamps, and 1–20 unique curated exercises in explicit zero-based order. Each exercise stores planned sets and only the targets permitted by its catalogue tracking mode.

Persist measurement values canonically as kilograms, metres, and seconds. Convert to or from metric/imperial display values only in the mobile planner. Do not create recommended sets, repetitions, loads, durations, or distances automatically; all initial prescriptions are explicit user input until a qualified deterministic programming policy exists.

Use relational plan and plan-exercise tables with database bounds and foreign keys to profiles and catalogue exercises. Treat the revision as an optimistic concurrency token and return an HTTP `409` problem response for stale updates. Do not add delete or archive behavior until workout-history references have a defined lifecycle.

Expose list, create, detail, and update endpoints only in Development while the prototype has no authentication. Profile scoping is an integrity boundary, not authorization. The mobile route carries the current-session profile identifier temporarily and will be replaced by authenticated account context.

Use a compact mobile layout with restrained dividers. Put catalogue discovery and prescription editing in focused sheets. Support long-press drag reordering and equivalent VoiceOver move actions.

## Consequences

- Active sessions can later snapshot a stable, deterministic template without turning plans into history records.
- Changing display units does not alter stored meaning.
- Concurrent edits fail visibly instead of applying last-write-wins behavior.
- The database can enforce reference and numeric integrity while aggregate logic owns contiguous ordering and tracking-mode rules.
- Authentication, offline edits, template-to-session snapshot rules, progression, archive/delete, and AI proposals remain separate decisions.
- A plan is capped at 20 exercises, keeping mobile rendering and request validation bounded.

## Alternatives considered

- A JSON plan document was rejected because catalogue identity, referential integrity, and future session/history relationships are relational concerns.
- Storing values in the user's preferred units was rejected because a later preference change could reinterpret persisted numbers.
- Automatic target defaults were rejected because they would appear to be coaching without an approved programming policy.
- Last-write-wins updates were rejected because they can silently discard changes.
- A uniqueness constraint on `(workout_plan_id, position)` was deferred because swaps complicate in-place persistence and the aggregate already rebuilds contiguous positions before saving.
- Delete/archive was deferred because template history references and recovery behavior are not yet defined.
