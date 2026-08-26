# ADR-0006: Keep the initial onboarding profile closed, minimal, and local

- Status: Accepted
- Date: 2026-08-26

## Context

The workout experience needs enough information to offer relevant exercises and units before authentication or AI exists. Collecting free-form goals, injuries, medical history, or speculative preferences would create sensitive data and ambiguous inputs without a current product use.

The local prototype still needs to prove the client, API, contract, and PostgreSQL path. Its profile data is user-owned, but Phase 4 intentionally owns authentication and account lifecycle.

## Decision

The initial profile contains:

- Any non-empty combination of build strength, build muscle, and general fitness goals.
- Exactly one beginner, intermediate, or advanced experience level.
- One or more choices from bodyweight, dumbbells, barbell, bench, squat rack, cable machine, resistance bands, and cardio equipment.
- Exactly one metric or imperial unit preference.

Use closed enum values in the API contract and readable string values in PostgreSQL. Store multi-select goals and equipment as normalized child rows with composite uniqueness constraints. Generate the profile identifier and UTC creation instant on the server.

Do not collect free-form goal detail, injuries, medical information, body measurements, or other self-declared health constraints. Add exercise exclusions only after the catalogue provides stable exercise identifiers, without requiring a reason.

Map the unauthenticated profile endpoints only in Development. The prototype may return stable identifiers, but possession of an identifier is not authorization and the client will not establish a durable account association until the identity design is approved.

## Consequences

- Workout rules can consume a small deterministic vocabulary without parsing user prose.
- The database can enforce supported values and prevent duplicate selections.
- The profile can grow through reviewed migrations and contract changes when a real workout use case requires more information.
- Development data is intentionally local and disposable. Profile routes cannot be deployed as an anonymous production surface.
- Durable device-to-profile association, ownership, cross-device synchronization, and account migration remain Phase 4 concerns and must be resolved before protected workout data is released.

## Alternatives considered

- Free-form goals were rejected because the initial workout flow cannot use them deterministically and they may invite unnecessary sensitive detail.
- Injury history and medical notes were rejected because they are not required for the approved general-fitness scope and would increase privacy and safety obligations.
- A single serialized array column was rejected because relational child rows provide clearer integrity, queryability, and future foreign-key migration to catalogue data.
- Anonymous endpoints in every environment were rejected because random identifiers do not establish ownership.
