# Design Foundation

This document defines the initial iOS visual foundation. It is intentionally narrow: it supports the first client screen without claiming to be a final public brand.

## Direction

The selected direction is **Midnight Indigo**: a near-black background with a restrained indigo undertone, warm off-white typography, and a bright rust/orange accent. The interface should feel like a focused fitness tool rather than a chat product or generic dashboard.

The initial client uses a deliberate dark appearance. A light appearance will not be improvised by inverting colors; it should be added only after an equally considered, accessible palette is designed and reviewed.

## Core colors

| Role             | Token             | Value     | Use                                                   |
| ---------------- | ----------------- | --------- | ----------------------------------------------------- |
| Canvas           | `canvas`          | `#111224` | Primary application background                        |
| Surface          | `surface`         | `#191A2C` | Subtle separation when a bounded surface is necessary |
| Raised surface   | `surfaceRaised`   | `#22233A` | Elevated or transient content only                    |
| Primary text     | `textPrimary`     | `#F4F0E7` | Headings and essential content                        |
| Secondary text   | `textSecondary`   | `#BAB9C7` | Supporting content that remains readable              |
| Accent           | `accent`          | `#D46A48` | Primary actions and strong emphasis                   |
| Accent highlight | `accentHighlight` | `#E48B6E` | Accent text and smaller highlights                    |
| On accent        | `onAccent`        | `#1A1310` | Text placed on the bright accent                      |
| Border           | `border`          | `#36374F` | Dividers and restrained boundaries                    |
| Focus            | `focus`           | `#F0A083` | Visible keyboard or accessibility focus indication    |

Color must communicate hierarchy without becoming decoration. Rust is reserved for primary actions, current navigation state, and small meaningful emphasis. It is not a general surface color.

## Typography

- Use the iOS system typeface. Do not set a custom family.
- Product text scales with Dynamic Type; do not disable font scaling or constrain it with a maximum multiplier.
- Prefer a small number of strong levels: display, title, body, label, and eyebrow.
- Avoid fixed-height text containers. Layout must reflow when text grows.
- Use sentence case except for short eyebrow labels where uppercase is deliberate.

## Spacing, radius, and motion

- Spacing follows a compact 4-point-derived scale: 4, 8, 12, 16, 24, 32, and 48 points.
- Corners are restrained: 4 points for subtle details, 14 for controls, and 20 for the occasional necessary panel.
- Do not turn every section into a rounded card.
- Motion durations are 120, 220, and 320 milliseconds. Prefer immediate feedback and short spatial transitions.
- Respect Reduce Motion. Essential state changes must remain understandable without animation.
- Interactive targets must be at least 44 by 44 points.

## Accessibility review

For every affected screen:

- Verify essential text, secondary text, accent text, and action labels meet WCAG AA contrast against their actual surfaces.
- Review Dynamic Type at the default size and at an accessibility size without truncation, overlap, or clipped controls.
- Check VoiceOver names, roles, reading order, values, and actions.
- Confirm information is not communicated by color alone.
- Preserve visible focus and selected states.
- Verify controls remain at least 44 by 44 points and usable one-handed where appropriate.
- Check Reduce Motion behavior before adding non-essential animation.

## Workout planning interaction

- Keep saved plans and ordered exercises compact. Use dividers and spacing before adding bounded cards.
- Keep the workout name, exercise count, planned-set count, order, and save action visible in a clear hierarchy. Put catalogue discovery and prescription editing in focused sheets.
- Never prefill training targets that could be mistaken for a generated recommendation. A newly selected exercise starts with only one structural set and requires the user to provide tracking-specific targets.
- Use a dedicated 44-point drag handle so row selection remains distinct from reordering. Long-press drag must have visible lifted and insertion states.
- Expose VoiceOver adjustable move-up and move-down actions; dragging cannot be the only reorder mechanism.
- Keep the save action stable at the bottom while allowing content and the keyboard to reflow safely above it.
- Filter catalogue choices by the profile's available equipment, show text instructions before selection, and make duplicate choices visibly unavailable.

## Visual review checklist

- Review on at least one compact and one large supported iPhone simulator.
- Switch iOS between light and dark system appearances and confirm the deliberate dark app appearance remains coherent.
- Confirm the status bar and system chrome remain readable.
- Test the longest realistic copy expected for the screen.
- Check loading, empty, error, offline, disabled, and interrupted states when the screen supports them.
- Reject excessive cards, pills, gradients, glows, glass effects, oversized icons, and ornamental motion.
- Confirm the coach is contextual and secondary to the fitness task.
