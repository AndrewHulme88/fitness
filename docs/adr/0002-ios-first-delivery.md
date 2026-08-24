# ADR-0002: Deliver iOS first

- Status: Accepted
- Date: 2026-08-24

## Context

Attempting simultaneous iOS, Android, and browser delivery would widen the product and QA surface before the core workout experience exists. The project prioritizes polish, reliability, and small increments.

## Decision

Target iOS for the first prototype, MVP, and private beta. Use normal Expo and React Native practices so Android is not needlessly blocked, but do not include Android-specific implementation, store work, device QA, or release criteria in current increments. Do not build a browser version in the initial roadmap.

Use iOS native conventions as the initial experience baseline, including the system typeface, navigation expectations, accessibility behavior, secure storage, and simulator/physical-device validation.

## Consequences

- Product and design reviews can focus on a smaller set of devices and platform conventions.
- Platform-specific iOS code may be used when it creates meaningful quality, provided the choice is documented and does not gratuitously undermine portability.
- Android assumptions will remain unverified until an Android phase is intentionally opened.
- A later Android effort will require its own accessibility, performance, permission, build, store, and device QA work; React Native does not eliminate that work.
- Web-specific code, dependencies, and responsive layouts are not part of the initial client.

## Alternatives considered

### Simultaneous iOS and Android

Rejected initially because it doubles important testing and release considerations while the product itself is still being discovered.

### Universal Expo web application

Technically feasible, but deferred because the user has chosen to focus on mobile and the browser experience has no current product requirement.

### SwiftUI-only application

Rejected because React Native and Expo remain desired technologies and preserve a future Android path.

