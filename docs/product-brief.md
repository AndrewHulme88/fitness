# Product Brief

## Working title

Fitness Coach. The final name and visual identity are undecided.

## Product statement

Fitness Coach is an iOS-first application that helps adults plan, perform, and understand their training. It combines a fast, dependable workout experience with an AI coach that uses approved user context to explain, suggest, and encourage without acting as a medical professional or silently changing the user's program.

## Problem

Many fitness applications are either passive workout logs, rigid program libraries, or chat interfaces with little reliable knowledge of the user's actual training. Users need a product that makes the immediate training task simple while providing contextual help they can inspect and control.

## Initial audience

- Adults aged 18 and over.
- People pursuing general strength and fitness goals.
- Beginner-to-intermediate users who value guidance but retain control over their training.
- Initially, one account represents one person; coach, clinician, team, and household accounts are out of scope.

The exact balance between general strength, hypertrophy, and broader fitness is an open product decision.

## Core jobs

1. Help me decide what I am doing today.
2. Let me log a set quickly without interrupting my training.
3. Show me what I have done and whether I am progressing.
4. Explain my plan and recent training in language I understand.
5. Suggest a reasonable next action while keeping me in control.

## Product principles

- The workout flow comes before the chat experience.
- The application remains useful when AI is unavailable.
- Every recommendation should be understandable and reversible.
- Calm, clear interaction is more valuable than novelty.
- Collect less data and use it well.
- Do not confuse general wellness coaching with health care.
- Reliability and responsiveness during a workout are non-negotiable.

## MVP capabilities

- Focused onboarding for goals, experience, equipment, and preferred units.
- A curated exercise catalogue with clear, licensed content.
- Creation and editing of simple workouts.
- Active-session logging for sets, repetitions, load, completion, and notes.
- Workout history and a small set of explainable progress indicators.
- Contextual coach conversation based on approved profile and training data.
- Structured coach proposals that require validation and explicit acceptance.
- Account privacy controls appropriate to the data collected.

## Explicit non-goals for the MVP

- Injury diagnosis or rehabilitation.
- Disease management or clinical monitoring.
- Medication, supplement, or treatment recommendations.
- Prescriptive meal plans or aggressive weight-loss coaching.
- Support for minors.
- Social feeds, public profiles, competitions, or coach marketplaces.
- Wearables or Apple Health integration.
- Browser and Android releases.
- Computer-vision form assessment.
- Autonomous program changes.
- A vector database or general-purpose agent framework.
- Payments and subscriptions.

## Experience direction

The interface should feel like a well-made native fitness tool, not a themed AI chat product. The default design direction is:

- iOS-native typography and interaction behavior.
- Restrained color with strong semantic states.
- Compact, legible workout controls designed for movement and one-handed use.
- Clear hierarchy without excessive cards or decoration.
- Purposeful motion and haptics only where they improve feedback.
- Complete empty, loading, error, interrupted, and offline experiences.

The coach should appear where context makes it useful, but chat should not dominate the application shell.

## Safety position

The product is for general fitness and wellness. It does not determine whether a user has an injury or medical condition. High-risk or medical requests receive a bounded response that explains the limitation and directs the user toward an appropriate qualified professional or local emergency help when warranted.

See [ai-safety.md](ai-safety.md) for the operational requirements.

## Early success criteria

Before setting numeric targets, establish representative flows and measurement baselines. The MVP should demonstrate that:

- A user can understand and begin today's workout without needing the coach.
- Logging a normal session is fast and reliable on a supported iPhone.
- History and progress derive from recorded data and can be explained.
- Coach answers use correct approved context and distinguish facts from suggestions.
- Unsafe or out-of-scope requests consistently trigger the intended behavior.
- A provider outage does not block core workout functionality.
- The codebase can be built, tested, reviewed, and demonstrated without private infrastructure or real user data.

## Open product questions

- What is the product's working and eventual public name?
- Which training outcome should lead the first experience?
- What is the smallest useful onboarding dataset?
- Is authentication required for the first prototype?
- Are progress photos excluded from the MVP?
- What tone should the coach use, and how configurable should it be?
- Which jurisdictions and app-store markets would a first public beta target?

