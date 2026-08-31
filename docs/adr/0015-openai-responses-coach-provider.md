# ADR-0015: Use OpenAI Responses with GPT-5.6 Terra for read-only coaching

- Status: Accepted
- Date: 2026-08-31

## Context

The completed coach boundary needs a live provider while preserving server-side credentials, bounded context, application-owned conversation history, and deterministic safety controls.

## Decision

Use the OpenAI Responses API with `gpt-5.6-terra`, low reasoning effort, a 600-token output cap, no tools, `store: false`, and a hashed stable safety identifier. The API key is server-side configuration only. Development without a configured key keeps the deterministic fake; non-development without a key fails safely as unavailable.

## Consequences

The provider adapter remains replaceable behind the product interface. Provider response state is not used for conversation retention. Before real user traffic, evaluate Terra against Sol with synthetic cases, verify account/project data-retention controls, apply rate and spend limits, and record the active model and prompt version.
