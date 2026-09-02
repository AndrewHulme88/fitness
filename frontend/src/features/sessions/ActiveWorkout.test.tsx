import { act, fireEvent, render, screen } from "@testing-library/react-native";
import { Alert } from "react-native";

import type { LocalWorkoutSession } from "./session-model";
import { useWorkoutSession } from "./useWorkoutSession";
import { ActiveWorkout } from "./ActiveWorkout";

jest.mock("expo-crypto", () => ({ randomUUID: () => "new-id" }));
jest.mock("./useWorkoutSession", () => ({ useWorkoutSession: jest.fn() }));

const useSessionMock = jest.mocked(useWorkoutSession);

const session: LocalWorkoutSession = {
  schemaVersion: 1,
  id: "session-id",
  profileId: "profile-id",
  workoutPlanId: "plan-id",
  workoutPlanRevision: 1,
  workoutName: "Upper strength",
  revision: 1,
  status: "active",
  startedAt: "2026-08-28T00:00:00Z",
  updatedAt: "2026-08-28T00:00:00Z",
  finishedAt: null,
  notes: null,
  syncState: "synced",
  mutationId: null,
  restTimerEndsAt: null,
  exercises: [
    {
      exerciseId: "exercise-id",
      position: 0,
      exerciseName: "Bench press",
      trackingMode: "repetitionsAndLoad",
      primaryMuscles: ["chest"],
      plannedSets: 1,
      minimumRepetitions: 8,
      maximumRepetitions: 10,
      targetLoadKilograms: 60,
      targetDurationSeconds: null,
      targetDistanceMetres: null,
      isSkipped: false,
      notes: null,
      sets: [
        {
          setId: "set-id",
          position: 0,
          isCompleted: false,
          completedAt: null,
          actualRepetitions: null,
          actualLoadKilograms: null,
          actualDurationSeconds: null,
          actualDistanceMetres: null,
        },
      ],
    },
  ],
};

describe("ActiveWorkout", () => {
  const mutate = jest.fn();
  const setRestTimer = jest.fn();

  beforeEach(() => {
    jest.useFakeTimers();
    mutate.mockReset();
    setRestTimer.mockReset();
    mutate.mockImplementation(async () => undefined);
    useSessionMock.mockReturnValue({
      session,
      loadState: "ready",
      message: undefined,
      mutate,
      setRestTimer,
      retry: jest.fn(),
      reloadServerVersion: jest.fn(),
      discard: jest.fn(),
      clearCompleted: jest.fn(),
    });
  });

  afterEach(() => jest.useRealTimers());

  it("keeps planned values separate until the user completes a set", () => {
    render(
      <ActiveWorkout
        onExit={jest.fn()}
        onFinished={jest.fn()}
        profileId="profile-id"
        unitSystem="metric"
        workoutPlanId="plan-id"
      />,
    );

    expect(screen.getByText("Plan · 8–10 reps · 60 kg")).toBeVisible();
    fireEvent.press(screen.getByLabelText("Set 1, Tap to log"));
    expect(screen.getByRole("header", { name: "Bench press" })).toBeVisible();
    expect(screen.getByLabelText("Repetitions")).toHaveProp("value", "10");
    expect(screen.getByLabelText("Load (kg)")).toHaveProp("value", "60");

    fireEvent.press(
      screen.getByRole("button", { name: "Complete & start 90 sec rest" }),
    );

    expect(mutate).toHaveBeenCalledTimes(1);
    const update = mutate.mock.calls[0]?.[0];
    const updated = update(session);
    expect(updated.exercises[0].sets[0]).toMatchObject({
      isCompleted: true,
      actualRepetitions: 10,
      actualLoadKilograms: 60,
    });
    expect(setRestTimer).toHaveBeenCalledWith(expect.stringMatching(/Z$/));
  });

  it("does not finish an empty session", () => {
    const alert = jest
      .spyOn(Alert, "alert")
      .mockImplementation(() => undefined);
    render(
      <ActiveWorkout
        onExit={jest.fn()}
        onFinished={jest.fn()}
        profileId="profile-id"
        unitSystem="metric"
      />,
    );

    fireEvent.press(screen.getByRole("button", { name: "Finish workout" }));

    expect(alert).toHaveBeenCalledWith(
      "No completed sets",
      "Complete at least one set, or discard this session.",
    );
    expect(mutate).not.toHaveBeenCalled();
    alert.mockRestore();
  });

  it("opens the summary after accepting a completed server copy", async () => {
    const alert = jest
      .spyOn(Alert, "alert")
      .mockImplementation(() => undefined);
    const reloadServerVersion = jest.fn().mockResolvedValue({
      ...session,
      finishedAt: "2026-08-28T01:00:00Z",
      status: "completed" as const,
      syncState: "synced" as const,
    });
    const onFinished = jest.fn();
    useSessionMock.mockReturnValue({
      session: { ...session, syncState: "conflict" },
      loadState: "ready",
      message:
        "This session also changed elsewhere. Your device copy is still safe.",
      mutate,
      setRestTimer,
      retry: jest.fn(),
      reloadServerVersion,
      discard: jest.fn(),
      clearCompleted: jest.fn(),
    });

    render(
      <ActiveWorkout
        onExit={jest.fn()}
        onFinished={onFinished}
        profileId="profile-id"
        unitSystem="metric"
      />,
    );

    fireEvent.press(screen.getByRole("button", { name: "Review server copy" }));
    const buttons = alert.mock.calls[0]?.[2];
    const useServerVersion = buttons?.find(
      (button) => button.text === "Use server version",
    );
    await act(async () => useServerVersion?.onPress?.());

    expect(reloadServerVersion).toHaveBeenCalledTimes(1);
    expect(onFinished).toHaveBeenCalledTimes(1);
    alert.mockRestore();
  });
});
