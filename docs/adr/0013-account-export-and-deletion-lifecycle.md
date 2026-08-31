# ADR-0013: Export account data directly and delete it through a durable Cognito-coordinated workflow

- Status: Accepted
- Date: 2026-08-31

## Context

Fitness Coach stores an application account keyed by Cognito issuer and subject, one owned training profile, workout plans, workout sessions (including notes and completed-set actuals), and derived progress. The Expo app also holds an active-workout document and profile association in its sandbox. These are sensitive fitness data.

Before beta, people need a usable copy of their data and a way to permanently delete both application data and their Cognito identity. A PostgreSQL deletion and Cognito user deletion cannot be one distributed transaction, so the design must survive interruption without restoring access or retaining fitness data indefinitely.

## Decision

### Export

- Account settings offers **Export my data** to an authenticated, freshly signed-in user. The client writes the response to a temporary protected cache file, opens the native share/save sheet, and removes its own temporary file when that hand-off finishes. The UI explains that copies saved or shared outside the app are controlled by their destination.
- Return one UTF-8 `application/json` attachment named `fitness-coach-export-YYYY-MM-DD.json`, with `Content-Disposition: attachment` and `Cache-Control: no-store`. Generate it directly from PostgreSQL; do not use object storage, email delivery, or retained server-side export copies.
- Version the document with top-level `format: "fitness-coach-data-export"`, `formatVersion`, `exportedAt`, and `dataCoverage`. Use ISO 8601 UTC timestamps and canonical stored units (kilograms, metres, seconds). Preserve JSON numbers, stable UUIDs, and snapshot values where they explain saved plans or recorded sessions.
- Include profile goals, experience, equipment, and unit preference; reusable plans and prescriptions; all active and completed sessions, snapshots, skips, notes, actuals, and correction timestamps; plus a machine-readable description of the current factual progress window. Sessions remain the source of truth, so the progress section is convenience data only.
- Include the Cognito provider and immutable external subject, plus current Cognito user attributes when the fresh token has the required self-service scope. Exclude tokens, secrets, passwords, browser cookies, internal application-account IDs, IP addresses, request logs, and deletion-operation records.
- Stream ordered, bounded database reads rather than serializing an unbounded EF graph. The endpoint is read-only, ownership-scoped, cancellable, rate limited, and must publish the exact schema and a synthetic example when implemented.

### Deletion experience and authorization

- Deletion is separate from sign out. It explains that plans, workout history, notes, active local workout data, and Cognito sign-in are permanently removed; it offers export first without requiring it.
- Starting deletion requires a new Cognito managed-login authorization with `prompt=login`. The API accepts final confirmation only when the verified Cognito access token has an `auth_time` no older than five minutes, belongs to the current application account, and carries both the Fitness Coach API scope and `aws.cognito.signin.user.admin`. A typed confirmation phrase and accessible final confirmation are required. A refreshed token, device possession, or a local profile UUID is insufficient.
- Before final confirmation, cancellation changes nothing. Afterwards, normal account use is blocked; the app clears its local profile/session data and secure credentials; and it provides no undo or restoration path.

### Cognito coordination, purge, and recovery

1. In a short PostgreSQL transaction, create a deletion operation with an opaque operation ID, request time, a one-way keyed digest of `(issuer, subject)`, and status `requested`. It contains no profile, plan, session, note, token, email, or raw subject.
2. The API passes the already-verified fresh access token only in memory to Cognito's self-service `DeleteUser` operation. The mobile app does not call Cognito directly, so the application can persist and monitor the PostgreSQL purge consistently. Tokens are never logged or persisted.
3. If Cognito rejects or times out before success, mark the operation failed with a bounded provider-error category, retain all application data, and show a retryable error. Retry requires another fresh authorization and confirmation.
4. After Cognito confirms deletion, atomically delete the application account and owned profile. Database cascades remove profile selections, plans and prescriptions, sessions, snapshots, sets, skips, and notes. Mark the operation `application-purged`. The application must not create a replacement account for this terminal operation.
5. If execution stops after Cognito succeeds, a protected background worker retries only the application purge. It uses the keyed identity digest to locate the remaining account and is idempotent. The account is unavailable while pending; operations exceeding 24 hours are monitored release-blocking incidents.

Cognito's self-service `DeleteUser` is permanent and cannot be restored. The native client must request `aws.cognito.signin.user.admin`; its current custom API scope alone is insufficient. The provider call is authorized by the user's token, not by AWS credentials in the mobile app.

### Retention and restoration boundaries

- Primary PostgreSQL account data is deleted in the purge transaction. No fitness data is retained for analytics, support, or a grace period.
- Retain only the operation ID, timestamps, terminal status, and keyed identity digest for 45 days, then permanently delete it. Keep the digest key separately from database backups and rotate it under the production secret-management policy. This supports interrupted purges and prevents recent deletion from being undone by restore.
- Production backups must be encrypted, access-controlled, and expire within 35 days. Restore procedures reconcile deletion operations before restored data becomes available. A backup cannot restore a deleted account on request. Selecting the production backup service, automating expiry, and proving the reconciliation drill are beta gates.
- Current privacy-safe request logs have no bodies, headers, tokens, prompts, or fitness content, and are not an export or deletion source. Any production observability, support, AI-provider, photo, analytics, or storage capability must document its own data inventory, retention, deletion, and restore behavior before it receives live data.
- The app requests Cognito deletion through `DeleteUser`; AWS-controlled legal, security, and service-level retention is outside the application transaction and must be disclosed with the applicable AWS terms and region in launch privacy materials. The app retains no separate Cognito-profile copy beyond its identity key while the account exists.

### Verification required when implemented

- PostgreSQL integration tests: export ownership isolation; every current owned data type; stable schema/versioning; no-store headers; cancellation; and no credentials or internal-only records.
- Deletion tests: stale/fresh-token rejection, wrong confirmation, cross-account attempts, Cognito failure before deletion, idempotent retry, Cognito-success/interrupted-purge recovery, cascaded removal, operation expiry, and no recreation during a pending operation.
- Physical iPhone checks: export share/save and deletion with Dynamic Type, VoiceOver, offline/provider failure, app termination during deletion, secure-cache cleanup, and failed sign-in after Cognito deletion.
- Record a pre-beta threat model for identity, export files, deletion operations, local cache, backups, provider coordination, and restoration.

## Consequences

- Implementation adds account-lifecycle persistence, a protected deletion worker, Cognito self-service deletion integration, account settings, and an explicit mobile cache-cleanup path. It does not add an export-data bucket, email delivery, or provider credentials to the client.
- Application data cannot be recovered after final confirmation. Export-before-deletion and honest failure states are therefore essential.
- The existing cascade/restrict relationships need a deletion-focused migration and integration proof before account deletion is enabled. Future user-owned tables must extend this inventory before live use.

## Alternatives considered

### Delete only application data or only Cognito identity

Rejected. Either leaves a material account component behind, and deleting Cognito first without a durable application-purge path creates an unmanaged orphan.

### Let the client call Cognito `DeleteUser` directly

Rejected. Cognito supports this, but a direct mobile call provides no durable way to complete or observe PostgreSQL purge after interruption.

### Keep deleted accounts for a recovery grace period

Rejected. Retaining sensitive workout history after confirmed deletion conflicts with permanent deletion. A data-minimized operation record is enough for reliability and backup safety.

### Store generated exports for later download

Rejected. That creates another sensitive store, retention obligation, and cleanup failure mode without a demonstrated need for asynchronous large exports.
