# ADR-0012: Use Amazon Cognito identity and explicitly migrate prototype data

- Status: Accepted
- Date: 2026-08-30
- Supersedes: [ADR-0011](0011-managed-identity-and-prototype-migration.md)

## Context

The completed local prototype represents a person only by a client-held profile UUID. That identifier is not authentication and provides no ownership boundary, secure cross-device access, account lifecycle, or safe basis for AI-context authorization.

The iOS client needs a standards-based native sign-in flow, secure credential handling, and an ASP.NET Core API that validates access tokens without custom cryptography. The product retains PostgreSQL as its domain-data source of truth. The project owner already uses AWS and prefers to consolidate managed infrastructure there. Existing prototype users must be able to keep their local profile, plans, sessions, and history without allowing possession of a UUID to claim another person's data.

## Decision

- Use an Amazon Cognito User Pool as the managed OpenID Connect and OAuth 2.0 identity provider.
- Use Cognito managed login in the system authentication browser with authorization code flow and PKCE. Do not collect credentials in the app or use an embedded web view.
- Configure a public native app client with no client secret, a Cognito-managed domain, and only authorization-code OAuth flow. Configure a Cognito resource server and a minimal API scope for the ASP.NET Core API.
- The client requests the configured API scope, sends the resulting access token to the API, and stores refresh/session material only in secure platform storage. It contains no AWS credentials or app-client secret.
- Validate tokens through ASP.NET Core JWT bearer middleware against Cognito's regional User Pool issuer and JWKS. Require a valid signature, issuer, expiry, `token_use` of `access`, client identifier, and the required API scope. Do not validate an ID token as an API credential.
- Treat the stable Cognito `sub` claim plus User Pool issuer as the external identity key. Email and display name are neither authorization keys nor required fitness-domain fields.
- Add Sign in with Apple through Cognito before release alongside any third-party or social sign-in option. Do not require the client to disclose email beyond the provider flow.
- Introduce an application-owned account record keyed by `(issuer, subject)`, with one owned training profile. Future user-owned routes derive the profile from the authenticated account rather than trusting a profile ID supplied by the client.
- Migrate prototype data only after a user signs in and explicitly confirms the one-time transfer. The authenticated API validates that the nominated unclaimed prototype profile exists, atomically links it to the account, and makes the transfer idempotent. It never accepts a raw profile UUID as ongoing authorization.
- Keep account export and deletion as a separately designed Phase 4 increment before beta. Deletion must include the Cognito user and application-owned data, with a documented recovery and retention policy before release.

## Consequences

- Authentication work requires an AWS account, a Cognito User Pool, a public app client, managed-login domain, resource server, callback/logout URLs, and uncommitted local values. These are external setup requirements, not committed source configuration.
- Cognito consolidates identity billing, IAM administration, regional controls, and operational ownership with the project's existing AWS account. It does not make the mobile client an AWS-credential holder.
- The API receives a real authorization boundary and must update every profile-owned endpoint and query to derive ownership from claims. Comprehensive negative ownership tests are required.
- Existing local prototype data remains usable only through the explicit migration flow. A user who declines keeps no authenticated claim on that data, and a fresh account starts with a new profile.
- Cognito configuration, pricing plan, data-processing terms, user-pool region, retention, and incident posture require review before live-user launch.

## Alternatives considered

### Auth0

Auth0 was initially selected for its direct Expo and ASP.NET Core quickstarts. It was superseded because the project owner already uses AWS and prefers Cognito's standards-based managed login while avoiding an additional identity vendor. See [ADR-0011](0011-managed-identity-and-prototype-migration.md).

### AWS Amplify Auth

Deferred. The first implementation should use Cognito's standard OIDC endpoints with Expo's supported authentication and secure-storage facilities, keeping the mobile dependency surface smaller. Amplify can be reconsidered only if a demonstrated Cognito integration need exceeds those facilities.

### Self-hosted or custom authentication

Rejected. Password handling, MFA, account recovery, token issuance, key rotation, breach response, and native OAuth details create a security and operational burden that is unjustified at this stage.
