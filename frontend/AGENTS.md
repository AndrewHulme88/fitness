# Frontend Instructions

Apply the repository-level instructions first.

- The mobile client is iOS-first Expo, React Native, and strict TypeScript. Do not add browser or Android product scope unless the plan explicitly calls for it.
- Use the iOS system typeface unless a documented design decision changes it. Prefer calm hierarchy, native interaction patterns, and restrained color over generic dashboard styling.
- Keep presentation components focused; put domain rules, API mapping, and durable state transitions outside them. Do not add state or UI libraries without a concrete current need.
- Use generated types from `src/api/generated`; never maintain duplicate API DTOs by hand or edit generated output directly.
- Treat `EXPO_PUBLIC_*` values as public bundle configuration. Store tokens only through approved secure mobile storage when identity work introduces them.
- Test pure behavior, visible component states, and critical navigation paths at the smallest useful layer. Use the simulator for visual, Dynamic Type, VoiceOver, gesture, navigation, or other native behavior that tests cannot establish.
- Measure or benchmark only changes to a known performance-sensitive interaction, especially active workout logging.
