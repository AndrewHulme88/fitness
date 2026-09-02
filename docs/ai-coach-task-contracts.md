# AI Coach Task Contracts and Evaluation Gates

This document defines the enabled AI coach tasks. It is a release gate for changes to prompts, schemas, context assembly, provider models, or action validation. The coach is advisory only and never receives database or application write authority.

## Common controls

- The API authenticates and verifies profile ownership before assembling context.
- Context is opt-in, profile-scoped, bounded, and supplied by application queries only.
- The provider receives no tools, credentials, unrestricted identifiers, raw request logs, or another profile's data.
- The safety pre-check runs before context assembly and the provider call.
- Provider output is untrusted. Malformed advice fails closed; malformed proposals are discarded.
- User-visible messages identify their supplied factual context through context-source labels.

## Enabled tasks

### Workout explanation

The user may ask for general explanation of a recorded or selected workout. Context may include their training profile and, only when selected, one owned workout snapshot. The response may explain training terms and give general adult-fitness information. It cannot diagnose, prescribe treatment, claim monitoring, or modify a plan.

### Progress review

The user must choose exactly one source: an owned exercise's 12 most recent completed-set appearances, or factual completed-workout totals for the most recent 7 or 28 days. The response must distinguish recorded facts from general coaching interpretation. It must not claim a personal record, readiness, score, causal conclusion, or trend unless the supplied facts explicitly establish it. The independent Progress screens remain authoritative.

### Exercise substitution

An exercise substitution is available only inside a selected-workout review proposal. The provider may refer only to the selected workout's exercise identifiers and the profile's approved equipment. The API verifies ownership, curated identifiers, uniqueness, tracking modes, prescription bounds, expected revision, and a visible diff before anything can be confirmed.

### Workout-update proposal

The provider may return at most one strict-schema replacement proposal for one selected owned workout. It has no write capability. The API persists only a valid, revision-bound pending proposal. The user reviews named additions, removals, substitutions, and prescription changes, then explicitly confirms through the ordinary workout update path. Stale, malformed, invalid, or cross-workout proposals are discarded or rejected.

## Deterministic evaluation gate

Every enabled task must pass these checks before a relevant change ships:

| Gate | Required evidence |
| --- | --- |
| Safety | High-risk, diagnosis, rehabilitation, pregnancy, minor-user, medication, disordered-eating, and urgent-symptom prompts stop before provider access. |
| Privacy and ownership | A request cannot load another profile's workout or progress facts; provider requests contain only the selected bounded scope. |
| Scope | Combined workout/progress scopes, combined exercise/period progress scopes, and periods other than 7 or 28 days are rejected. |
| Output | Empty or malformed advice fails closed; invalid exercise identifiers, units, bounds, or target-workout revisions do not create an actionable proposal. |
| Confirmation | A proposal does not alter a workout before confirmation; confirmation rejects stale revisions and applies only the persisted validated payload. |

The deterministic API and PostgreSQL integration tests are the minimum gate. Before a live-provider model, prompt, or schema change, run the same synthetic cases against the candidate configuration and retain only privacy-safe configuration and outcome metadata. A critical gate failure blocks rollout regardless of aggregate usefulness.
