# AI Coach Safety

## Purpose

This document defines the product and engineering boundaries for the AI coach. It applies to prompts, tools, model selection, UI copy, data access, logging, tests, and incident handling.

The application is a general fitness and wellness tool. It is not a medical device, clinician, physiotherapist, dietitian, emergency service, or substitute for professional judgment. Product capabilities and marketing must remain consistent with that intended purpose.

## Allowed coaching scope

The coach may:

- Explain a user's recorded workout or general training terminology.
- Summarize approved training history accurately.
- Suggest ordinary exercise substitutions based on available equipment and stated preferences.
- Propose conservative changes to a fitness plan within deterministic limits.
- Help a user reflect on adherence, preferences, perceived difficulty, and scheduling.
- Encourage rest or professional advice when the user's message indicates uncertainty or risk.

Advice should distinguish recorded facts, general information, and model-generated suggestions.

## Disallowed scope

The coach must not:

- Diagnose or rule out an injury, disease, disorder, or medical condition.
- Interpret symptoms as a medical professional.
- Prescribe treatment, rehabilitation, medication, supplements, or clinical care.
- Tell a user to train through acute or unexplained pain.
- Provide aggressive weight-loss, purging, starvation, or other disordered-eating guidance.
- Create exercise guidance for a disclosed pregnancy, significant medical condition, or post-operative recovery as if ordinary fitness coaching were sufficient.
- Discourage a user from following qualified professional advice.
- Claim certainty, credentials, monitoring, or emergency capability it does not have.
- Make consequential account or program changes without explicit confirmation.

## High-risk signals

Safety behavior should be considered when a request includes, among other things:

- Chest pain, fainting, severe breathing difficulty, sudden weakness, or other potentially urgent symptoms.
- Severe, acute, worsening, or unexplained pain.
- Head, neck, or spinal injury concerns.
- Post-operative training, pregnancy, or a medical condition affecting exercise.
- Requests to diagnose an injury from text, a photo, or training data.
- Self-harm, purging, starvation, extreme weight change, or compulsive exercise.
- Medication or supplement dosing.
- A minor attempting to use the adult product.

This list is not a diagnostic classifier. It is a conservative product trigger for limiting the response and directing the user to appropriate human support.

## Response behavior

When a request is outside scope or potentially urgent, the coach should:

1. State the limitation directly and without alarmist language.
2. Avoid guessing at a diagnosis or providing a disguised treatment plan.
3. Recommend stopping or avoiding the potentially harmful activity when immediate continuation could increase risk.
4. Encourage contact with an appropriately qualified professional.
5. For potentially urgent situations, advise the user to contact local emergency services or seek urgent medical help.
6. Keep the response focused; do not bury the safety message under ordinary coaching.

The product must not claim that a generic disclaimer makes otherwise unsafe advice acceptable.

## Architectural controls

- Route all model calls through the backend.
- Send only authorized, task-relevant context.
- Keep a deterministic pre-check and post-validation layer for known safety and product constraints.
- Bound available tools, output length, time, retries, and spend.
- Use strict structured output for proposed actions.
- Validate exercise identifiers, units, progression, ownership, and allowed operations outside the model.
- Show a human-readable diff before a proposed plan change.
- Require explicit user confirmation and execute the accepted change through the ordinary application service.
- Fail closed for malformed action output: return no action rather than guessing.
- Preserve an application-owned record of user-visible messages and applied actions.
- Use a stable privacy-preserving safety identifier when supported by the provider.

## Data use and privacy

- Never expose one user's context to another user.
- Do not send data that is not required for the current request.
- Do not include credentials, internal authorization data, or unrestricted database access in model tools.
- Do not log raw prompts and responses by default.
- Separate operational metadata from sensitive content.
- Document provider storage and retention settings before live user data is sent.
- Provide clear disclosure that selected information is processed by an AI provider.
- Design deletion and retention behavior for both application-owned and provider-held data.

## Prompt and model change control

- Version the system prompt and structured schemas.
- Keep model identifiers configurable.
- Record provider, model, prompt version, safety outcome, latency, and usage metadata for evaluation without retaining unnecessary sensitive content.
- Run the evaluation suite before changing prompts, models, tools, context assembly, or safety logic.
- Roll out material behavior changes gradually once real users exist.
- Retain a rollback path to the prior known configuration.

## Evaluation suite

The suite should include:

- Normal questions about workouts, exercise terminology, scheduling, and progress.
- Ambiguous pain and injury language.
- Explicit requests for diagnosis or rehabilitation.
- Attempts to override system limits or reveal another user's data.
- Unsafe progression and extreme-volume requests.
- Eating-disorder and extreme weight-loss prompts.
- Medication and supplement requests.
- Pregnancy, post-operative, chronic-condition, and minor-user scenarios.
- Malformed, partial, and contradictory user context.
- Provider refusal, timeout, invalid schema, and outage behavior.
- Proposed actions containing nonexistent exercises, invalid units, or unauthorized identifiers.

Evaluation criteria should cover safety, factual use of supplied context, uncertainty, action validity, privacy, usefulness, and tone. A change should not ship merely because an average aggregate score improves; critical safety cases must meet their own release gate.

## User experience requirements

- Identify coach content as AI-generated.
- Make suggestions visually distinct from recorded facts and applied plan data.
- Explain the key basis for a recommendation using user-visible context.
- Let the user reject or edit a proposal.
- Provide a way to report unsafe, incorrect, or irrelevant output.
- Never imply that the coach is continuously monitoring the user.
- Preserve core workout access when the coach is unavailable.

## Incident handling

If unsafe output is discovered:

1. Preserve a privacy-safe reproduction and configuration metadata.
2. Assess severity and whether the behavior is systemic.
3. Disable the affected capability or revert the prompt/model when appropriate.
4. Add a regression evaluation before implementing the correction.
5. Record the issue and response in `DEVELOPMENT.md` without exposing user data.
6. Review whether product copy, policy, context, validation, or the provider—not only the prompt—contributed to the failure.

## Review boundary

Before a public beta, review the intended purpose, privacy disclosures, provider terms, app-store requirements, and applicable law for each launch jurisdiction. Product positioning must be reassessed if future features move toward diagnosis, treatment, clinical monitoring, or rehabilitation.

