# ADR-0004: Use Expo Router for mobile navigation

- Status: Accepted
- Date: 2026-08-25

## Context

The iOS client needs a small navigation graph now and should retain a straightforward path to Android later. Navigation must support deep links, typed destinations, route-level recovery, and integration testing without introducing a separate browser application.

## Decision

Use Expo Router with its stable native stack and typed routes. Keep route modules under `apps/client/src/app` and add destinations only when an active product increment needs them. The root layout owns shared stack presentation plus loading and error fallbacks; `+not-found` owns unavailable-route recovery.

## Consequences

- Navigation follows Expo's file-based conventions and remains aligned with the Expo toolchain.
- Route paths are checked by TypeScript and can support native deep links later.
- The route tree remains visible from the filesystem and can be exercised with integration tests.
- Expo Router and its SDK-aligned native dependencies become part of the client dependency surface.
- Route groups, tabs, and additional navigators should not be added until a concrete journey requires them.

## Alternatives considered

- Manually configured React Navigation was not selected because Expo Router already provides the required native navigation, deep-link, and test integration while reducing duplicate route configuration.
- Expo Router's experimental stack was not selected because Phase 1.3 requires a dependable foundation, not experimental navigation behavior.
- A custom navigation abstraction was rejected because it would add indirection before the product has navigation requirements that justify it.
