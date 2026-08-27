# ADR-0007: Own and explicitly import a curated exercise catalogue

- Status: Accepted
- Date: 2026-08-27

## Context

Workout planning needs stable, searchable exercise identifiers and enough structured metadata to choose exercises compatible with a user's equipment. A third-party catalogue would introduce availability, licensing, attribution, schema, and recurring-dependency risks before the product needs a large library.

Exercise instructions also carry a quality and safety obligation. Automated validation can protect structure and vocabulary, but it cannot replace qualified content review before public release.

## Decision

Own a version-controlled manifest containing 35 original, common strength and cardio entries. Keep it text-only and exclude user-created exercises, rehabilitation content, subjective difficulty ratings, and goal tags.

Use immutable UUIDs, readable slugs, searchable aliases, category, movement pattern, tracking mode, required equipment, primary and secondary muscles, and bounded setup, execution, and safety text. Move equipment into a small shared domain vocabulary because both Profiles and Exercises depend on the same values.

Embed the manifest in the API build and import it only through an explicit command. Validate the entire manifest before opening a transaction. Store the imported version, canonical content hash, review status, and import time. Permit additive and in-place content updates only through a higher catalogue version; refuse rollback, identity reassignment, and removal without a future retirement design.

PostgreSQL is the runtime source of truth. Normalize aliases, equipment, and muscles because they are searched or filtered; keep display instructions as bounded scalar text. Expose bounded search and detail endpoints only in Development while the application remains a local unauthenticated prototype.

The initial content status remains `RequiresQualifiedReview`. A qualified fitness professional must review the catalogue before any public release.

## Consequences

- The application has no runtime exercise-content vendor, API key, recurring catalogue cost, or third-party schema dependency.
- Catalogue changes are readable in source control and deterministic across environments.
- Stable exercise identifiers can support exclusions and workout history without coupling those records to display names.
- The explicit importer adds one local setup step after migrations and deliberately refuses ambiguous destructive changes.
- Search filters are bounded and use the same generated camel-case taxonomy as the mobile client.
- Media, custom exercises, retirement, and public production routing remain separate decisions.

## Alternatives considered

- A third-party exercise API was rejected because it would add licensing, availability, privacy, cost, and schema dependencies without a demonstrated need.
- Scraped or copied content was rejected because provenance and usage rights would be unreliable.
- Automatic startup seeding was rejected because content changes should not mutate a database implicitly when the API starts.
- EF model seeding was rejected because a large content set would obscure schema snapshots and migrations.
- Storing the entire catalogue only as JSON was rejected because runtime search, filtering, integrity, and future workout relationships belong in PostgreSQL.
- Images and video were deferred because no licensed source or complete media product behavior has been approved.
