# Exercise Catalogue Content Policy

## Scope

The initial catalogue is an internally owned, text-only set of 35 common strength and cardio exercises. It supports the first workout-planning flow without introducing a third-party content API, user-created exercises, rehabilitation content, or highly technical lifts.

The canonical authoring manifest is `backend/FitnessCoach.Api/Features/Exercises/Catalogue/exercise-catalogue.json`. PostgreSQL is the runtime source of truth after an explicit validated import.

## Content ownership and licensing

- Names, aliases, classifications, and instructions are written specifically for this project rather than copied or adapted from an external exercise database.
- Do not scrape websites, copy protected instructions, embed external videos, or add third-party images without a documented licence and attribution review.
- If externally sourced content is proposed later, record its owner, source, licence version, permitted uses, attribution requirements, modification rights, and termination risk before adding it.
- Common exercise facts and names do not remove the need to write original explanatory text.

## Content review

The initial manifest is marked `requiresQualifiedReview`. Code review and automated validation establish structural quality; they do not constitute professional exercise-content review.

Before public release, an appropriately qualified fitness professional must review every entry for:

- Clear and accurate setup and execution.
- Proportionate, non-alarmist general safety cues.
- Appropriate movement, equipment, muscle, and tracking classifications.
- Language that remains within general fitness rather than diagnosis, treatment, rehabilitation, or individual medical advice.

Record the reviewer, date, catalogue version, scope, and material corrections in the development record without storing unnecessary personal data. Change the manifest review status only in the reviewed version.

## Stable identity and lifecycle

- Exercise UUIDs are permanent and must never be reassigned.
- Slugs are readable identifiers and may change only when the importer can prove the UUID remains the same.
- Every content change increments `catalogueVersion` after a catalogue has been imported.
- The importer refuses version rollback, same-version content changes, UUID/slug reassignment, and silent removal.
- Exercise retirement requires an explicit design that preserves existing workout history. Deleting an entry from the manifest is not retirement.

## Taxonomy

Each exercise has:

- A strength or cardio category.
- One primary movement pattern.
- A tracking mode describing the metrics a workout may record.
- One or more required equipment values shared with onboarding.
- One or more primary muscles and optional secondary muscles.
- A unique name, slug, stable UUID, and bounded searchable aliases.
- Bounded setup, execution, and safety text.

Difficulty and goal tags are intentionally absent. Workout planning should interpret the user's experience and goals rather than embedding subjective suitability claims in the exercise record.

The manifest validator has an absolute 500-entry guardrail. Raising it requires review of the import cost, search query behavior, payload limits, and catalogue navigation with a representative dataset.

## Media and custom content

The initial catalogue contains no image, video, animation, or media URL fields. Media remains an open product and licensing decision and should be added only with a real approved source and complete loading, accessibility, storage, and failure behavior.

User-created exercises are outside the current scope. Their naming, ownership, synchronization, moderation, and history behavior require a separate design.
