import { fireEvent, render, screen } from "@testing-library/react-native";
import { PanResponder, type PanResponderGestureState } from "react-native";

import type { ExerciseSummary } from "../../api/exercises";
import { DraggableExerciseList } from "./DraggableExerciseList";
import { createExerciseDraft } from "./workout-draft";

describe("DraggableExerciseList", () => {
  beforeEach(() => {
    jest.spyOn(PanResponder, "create").mockImplementation((config) => {
      return {
        panHandlers: {
          onResponderGrant: config.onPanResponderGrant,
          onResponderMove: config.onPanResponderMove,
          onResponderRelease: config.onPanResponderRelease,
          onResponderTerminate: config.onPanResponderTerminate,
        },
      } as ReturnType<typeof PanResponder.create>;
    });
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

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
        onDragStateChange={jest.fn()}
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

  it("reorders a successfully completed drag", () => {
    const onReorder = jest.fn();
    const onDragStateChange = jest.fn();
    const drafts = [
      exercise("Back Squat", "01"),
      exercise("Bench Press", "02"),
    ].map(createExerciseDraft);

    render(
      <DraggableExerciseList
        drafts={drafts}
        errors={{}}
        onDragStateChange={onDragStateChange}
        onEdit={jest.fn()}
        onReorder={onReorder}
        unitSystem="metric"
      />,
    );
    fireEvent(
      screen.getByTestId(`exercise-layout-${drafts[0].exercise.id}`),
      "layout",
      {
        nativeEvent: { layout: { height: 76, width: 320, x: 0, y: 0 } },
      },
    );
    fireEvent(
      screen.getByTestId(`exercise-layout-${drafts[1].exercise.id}`),
      "layout",
      {
        nativeEvent: { layout: { height: 76, width: 320, x: 0, y: 76 } },
      },
    );

    const handle = screen.getByTestId(`reorder-${drafts[0].exercise.id}`);
    fireEvent(handle, "responderGrant", {}, gestureState());
    fireEvent(handle, "responderMove", {}, gestureState(92, 130));
    fireEvent(handle, "responderRelease", {}, gestureState(92, 130));

    expect(onReorder).toHaveBeenCalledWith(0, 1);
    expect(onDragStateChange.mock.calls).toEqual([[true], [false]]);
  });

  it("does not reorder when an active drag is cancelled", () => {
    const onReorder = jest.fn();
    const onDragStateChange = jest.fn();
    const drafts = [
      exercise("Back Squat", "01"),
      exercise("Bench Press", "02"),
    ].map(createExerciseDraft);

    render(
      <DraggableExerciseList
        drafts={drafts}
        errors={{}}
        onDragStateChange={onDragStateChange}
        onEdit={jest.fn()}
        onReorder={onReorder}
        unitSystem="metric"
      />,
    );
    fireEvent(
      screen.getByTestId(`exercise-layout-${drafts[0].exercise.id}`),
      "layout",
      {
        nativeEvent: { layout: { height: 76, width: 320, x: 0, y: 0 } },
      },
    );
    fireEvent(
      screen.getByTestId(`exercise-layout-${drafts[1].exercise.id}`),
      "layout",
      {
        nativeEvent: { layout: { height: 76, width: 320, x: 0, y: 76 } },
      },
    );

    const handle = screen.getByTestId(`reorder-${drafts[0].exercise.id}`);
    fireEvent(handle, "responderGrant", {}, gestureState());
    fireEvent(handle, "responderMove", {}, gestureState(92, 130));
    fireEvent(handle, "responderTerminate", {}, gestureState(92, 130));

    expect(onReorder).not.toHaveBeenCalled();
    expect(onDragStateChange.mock.calls).toEqual([[true], [false]]);
  });

  it("does not reorder a tap or small pointer movement", () => {
    const onReorder = jest.fn();
    const drafts = [
      exercise("Back Squat", "01"),
      exercise("Bench Press", "02"),
    ].map(createExerciseDraft);

    render(
      <DraggableExerciseList
        drafts={drafts}
        errors={{}}
        onDragStateChange={jest.fn()}
        onEdit={jest.fn()}
        onReorder={onReorder}
        unitSystem="metric"
      />,
    );
    fireEvent(
      screen.getByTestId(`exercise-layout-${drafts[0].exercise.id}`),
      "layout",
      {
        nativeEvent: { layout: { height: 76, width: 320, x: 0, y: 0 } },
      },
    );
    fireEvent(
      screen.getByTestId(`exercise-layout-${drafts[1].exercise.id}`),
      "layout",
      {
        nativeEvent: { layout: { height: 76, width: 320, x: 0, y: 76 } },
      },
    );

    const handle = screen.getByTestId(`reorder-${drafts[0].exercise.id}`);
    fireEvent(handle, "responderGrant", {}, gestureState());
    fireEvent(handle, "responderMove", {}, gestureState(20, 58));
    fireEvent(handle, "responderRelease", {}, gestureState(20, 58));

    expect(onReorder).not.toHaveBeenCalled();
  });
});

function gestureState(dy = 0, moveY = 38): PanResponderGestureState {
  return {
    stateID: 1,
    moveX: 0,
    moveY,
    x0: 0,
    y0: 38,
    dx: 0,
    dy,
    vx: 0,
    vy: 0,
    numberActiveTouches: 1,
    _accountsForMovesUpTo: 0,
  };
}

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
