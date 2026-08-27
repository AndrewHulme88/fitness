import { fireEvent, render, screen } from "@testing-library/react-native";

import type { ExerciseSummary } from "../../api/exercises";
import { DraggableExerciseList } from "./DraggableExerciseList";
import { createExerciseDraft } from "./workout-draft";

describe("DraggableExerciseList", () => {
  it("offers accessible move actions in addition to touch dragging", () => {
    const onReorder = jest.fn();
    const drafts = [
      exercise("Back Squat", "01"),
      exercise("Bench Press", "02"),
    ].map(createExerciseDraft);

    render(
      <DraggableExerciseList
        drafts={drafts}
        errors={{}}
        onAutoScroll={jest.fn()}
        onEdit={jest.fn()}
        onReorder={onReorder}
        unitSystem="metric"
      />,
    );

    const benchHandle = screen.getByRole("adjustable", {
      name: "Reorder Bench Press",
    });
    fireEvent(benchHandle, "accessibilityAction", {
      nativeEvent: { actionName: "decrement" },
    });

    expect(onReorder).toHaveBeenCalledWith(1, 0);
    expect(benchHandle).toHaveAccessibilityValue({
      min: 1,
      max: 2,
      now: 2,
      text: "Position 2 of 2",
    });
  });
});

function exercise(name: string, idSuffix: string): ExerciseSummary {
  return {
    id: `00000000-0000-0000-0000-0000000000${idSuffix}`,
    slug: name.toLowerCase().replaceAll(" ", "-"),
    name,
    category: "strength",
    movementPattern: "squat",
    trackingMode: "repetitionsAndLoad",
    requiredEquipment: ["barbell"],
    primaryMuscles: ["quadriceps"],
  };
}
