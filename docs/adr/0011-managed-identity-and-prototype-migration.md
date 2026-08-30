# ADR-0011: Use Auth0-managed identity and explicitly migrate prototype data

- Status: Superseded by [ADR-0012](0012-cognito-identity-and-prototype-migration.md)
- Date: 2026-08-30

## Context

The completed local prototype represents a person only by a client-held profile UUID. That identifier is not authentication and provides no ownership boundary, secure cross-device access, account lifecycle, or safe basis for AI-context authorization.

The iOS client needs a standards-based native sign-in flow, secure credential handling, and an ASP.NET Core API that validates access tokens without custom cryptography. The product must retain its PostgreSQL domain data and its cloud-neutral deployment direction. Existing prototype users must be able to keep their local profile, plans, sessions, and history without allowing possession of a UUID to claim another person's data.

## Supersession

This record selected Auth0 before the project owner chose AWS as the existing cloud provider. It is superseded by [ADR-0012](0012-cognito-identity-and-prototype-migration.md), which retains the same OAuth/OIDC, ownership, and explicit-migration constraints using Amazon Cognito.

## Historical decision

- Use Auth0 as the managed OpenID Connect and OAuth 2.0 identity provider.
- Use Auth0 Universal Login in the system authentication browser with authorization code flow and PKCE. Do not collect identity-provider credentials in the app or use an embedded web view.
- Configure a native Auth0 application for the iOS bundle identifier and a distinct API audience. The client sends only access tokens for that audience to the API.
- Validate issuer, audience, lifetime, signature, and required subject claims through ASP.NET Core JWT bearer middleware and the provider's JWKS. The API treats the stable OIDC `sub` claim as the external identity key; email and display name are neither authorization keys nor required fitness-domain fields.
- Add Sign in with Apple before release alongside any third-party or social sign-in option. Do not require the client to disclose email beyond the provider flow.
- Store authentication material only through the provider SDK's supported secure platform storage. No Auth0 secret is embedded in the mobile bundle; domain, client ID, and API audience are public configuration.
- Introduce an application-owned account record keyed by `(issuer, subject)`, with one owned training profile. Future user-owned routes derive the profile from the authenticated account rather than trusting a profile ID supplied by the client.
- Migrate prototype data only after a user signs in and explicitly confirms the one-time transfer. The authenticated API validates that the nominated unclaimed prototype profile exists, atomically links it to the account, and makes the transfer idempotent. It never accepts a raw profile UUID as ongoing authorization.
- Keep account export and deletion as a separately designed Phase 4 increment before beta. Deletion must include the Auth0 identity and application-owned data, with a documented recovery and retention policy before release.

## Consequences

- Authentication work requires an Auth0 tenant, a native application, an API resource, Apple provider configuration, and local development values. These are external setup requirements, not committed source configuration.
- The Expo app must use an iOS development build for Auth0's native module; Expo Go is not sufficient for authentication verification.
- The API will receive a real authorization boundary and must update every profile-owned endpoint and query to derive ownership from claims. Comprehensive negative ownership tests are required.
- Existing local prototype data remains usable only through the explicit migration flow. A user who declines keeps no authenticated claim on that data, and a fresh account starts with a new profile.
- Auth0 becomes a recurring vendor and privacy dependency. Its pricing, data-processing terms, supported regions, retention settings, and incident posture require review before live-user launch.

## Alternatives considered

### Clerk

Clerk has an Expo integration and can support hosted authentication, but it would make the ASP.NET Core API's verification path more provider-specific for no current product advantage. Auth0 has direct Expo and ASP.NET Core JWT guidance and keeps the API aligned with standard issuer/audience/JWKS validation.

### Supabase Auth

Supabase Auth is capable of issuing JWTs, but choosing it would add a second managed PostgreSQL-centered platform alongside the application's existing EF Core/PostgreSQL ownership. Its React Native quickstart also uses general app storage for persisted sessions, which is not the chosen storage boundary for sensitive fitness data.

### Self-hosted or custom authentication

Rejected. Password handling, MFA, account recovery, token issuance, key rotation, breach response, and native OAuth details create a security and operational burden that is unjustified at this stage.
