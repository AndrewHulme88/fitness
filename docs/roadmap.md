# Product Roadmap

This roadmap describes product outcomes. Detailed technical sequencing and current statuses live in [`PLAN.md`](../PLAN.md).

## Foundation

Outcome: the project has an explicit purpose, safety boundary, architecture, quality standard, and decision history before implementation begins.

Evidence:

- The foundational documents agree with one another.
- Open product and technical choices are visible.
- Work can be divided into small, testable increments.

## Native shell

Outcome: a polished, accessible iOS application shell runs reliably in development and establishes the visual language without creating unused product surfaces.

Evidence:

- Repeatable install, build, lint, type-check, and test commands.
- Verified iOS simulator behavior.
- Deliberate design tokens and native typography.
- Navigation supports only the first product flow.

## Core training loop

Outcome: a user can plan, perform, and review a workout without AI.

Evidence:

- Focused onboarding and a licensed exercise catalogue.
- Reliable workout planning and active-session logging.
- Accurate history and explainable progress.
- Measured interaction and query performance on representative data.

## Durable accounts

Outcome: personal training data can be securely associated with an account and managed through its lifecycle.

Evidence:

- Standards-based native authentication.
- Server-side ownership enforcement with negative tests.
- Clear session expiry and recovery behavior.
- Export and deletion paths designed before beta.

## Contextual coach

Outcome: the coach can explain approved training context and offer bounded, useful suggestions without becoming an authority over the user's data or health.

Evidence:

- The product remains usable when AI is unavailable.
- Context is minimized and traceable.
- Ordinary, adversarial, privacy, and safety evaluations pass defined thresholds.
- Consequential changes are structured, validated, shown clearly, and explicitly accepted.
- Cost and latency are observable.

## Private beta

Outcome: a small invited group can use the product safely and reliably on supported iPhones.

Evidence:

- Accessibility and physical-device QA are complete.
- Security and privacy reviews have no unresolved critical findings.
- Backup/restore and account deletion are verified.
- Performance baselines and alerting are established.
- App-store disclosures and support paths reflect actual behavior.

## Possible later directions

These require evidence from the core product and are not commitments:

- Android release.
- Health-platform or wearable integration.
- Notifications and scheduled coaching.
- Carefully licensed exercise media.
- Broader training modalities.
- Curated evidence retrieval.
- Public launch, subscriptions, or a marketing website.

