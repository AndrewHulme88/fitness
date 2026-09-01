import { useEffect, useState } from "react";
import { FlatList, Pressable, StyleSheet, View } from "react-native";

import { listWorkouts, type WorkoutSummary } from "../../api/workouts";
import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { PrimaryButton } from "../../components/PrimaryButton";
import { RouteStatus } from "../../components/RouteStatus";
import { TrainingSections } from "../../components/TrainingSections";
import { colors, layout, spacing } from "../../theme/tokens";

type WorkoutListProps = {
  onCreate: () => void;
  onCoach: (workoutId?: string) => void;
  onEdit: (workoutId: string) => void;
  onHistory: () => void;
  onProgress: () => void;
  onStart: (workoutId: string) => void;
  profileId: string;
};

export function WorkoutList({
  onCreate,
  onCoach,
  onEdit,
  onHistory,
  onProgress,
  onStart,
  profileId,
}: WorkoutListProps) {
  const [workouts, setWorkouts] = useState<WorkoutSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string>();
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    listWorkouts(
      profileId,
      { limit: 50, offset: 0 },
      { signal: controller.signal },
    )
      .then((result) => setWorkouts(result.items))
      .catch(() => {
        if (!controller.signal.aborted) {
          setError("Your workouts could not be loaded.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false);
      });

    return () => controller.abort();
  }, [profileId, reloadKey]);

  const retry = () => {
    setError(undefined);
    setIsLoading(true);
    setReloadKey((value) => value + 1);
  };

  if (isLoading) {
    return (
      <RouteStatus
        busy
        message="Loading your saved workout plans."
        title="Preparing your workouts"
      />
    );
  }

  if (error) {
    return (
      <RouteStatus
        actionLabel="Try again"
        message={error}
        onAction={retry}
        title="Workouts unavailable"
      />
    );
  }

  return (
    <AppScreen>
      <FlatList
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        data={workouts}
        keyExtractor={(workout) => workout.id}
        ListHeaderComponent={
          <View style={styles.header}>
            <TrainingSections
              active="plans"
              onHistory={onHistory}
              onPlans={() => undefined}
              onProgress={onProgress}
            />
            <View style={styles.intro}>
              <AppText tone="accent" variant="eyebrow">
                Workout plans
              </AppText>
              <AppText accessibilityRole="header" variant="display">
                Your workouts
              </AppText>
              <AppText tone="secondary">
                Build focused templates and adjust them as your training
                changes.
              </AppText>
            </View>
            <PrimaryButton label="Create workout" onPress={onCreate} />
            <Pressable
              accessibilityRole="button"
              onPress={() => onCoach()}
              style={styles.coachLink}
            >
              <AppText tone="accent" variant="label">
                Ask AI coach
              </AppText>
            </Pressable>
          </View>
        }
        ListEmptyComponent={
          <View style={styles.empty}>
            <AppText variant="title">No workouts yet</AppText>
            <AppText tone="secondary">
              Create a reusable plan from the exercise catalogue when you’re
              ready.
            </AppText>
          </View>
        }
        renderItem={({ item }) => (
          <WorkoutRow
            onCoach={() => onCoach(item.id)}
            onEdit={() => onEdit(item.id)}
            onStart={() => onStart(item.id)}
            workout={item}
          />
        )}
        showsVerticalScrollIndicator={false}
      />
    </AppScreen>
  );
}

function WorkoutRow({
  onCoach,
  onEdit,
  onStart,
  workout,
}: {
  onCoach: () => void;
  onEdit: () => void;
  onStart: () => void;
  workout: WorkoutSummary;
}) {
  const exerciseCount = Number(workout.exerciseCount);
  const setCount = Number(workout.plannedSetCount);

  return (
    <View style={styles.row}>
      <View style={styles.rowCopy}>
        <AppText variant="title" style={styles.rowTitle}>
          {workout.name}
        </AppText>
        <AppText tone="secondary">
          {formatCount(exerciseCount, "exercise")} ·{" "}
          {formatCount(setCount, "set")}
        </AppText>
        <AppText style={styles.updated} tone="secondary">
          Updated {formatUpdatedAt(workout.updatedAt)}
        </AppText>
      </View>
      <View style={styles.rowActions}>
        <Pressable
          accessibilityRole="button"
          onPress={onStart}
          style={styles.startButton}
        >
          <AppText style={styles.startLabel} variant="label">
            Start
          </AppText>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          onPress={onEdit}
          style={({ pressed }) => [
            styles.editButton,
            pressed && styles.pressed,
          ]}
        >
          <AppText tone="secondary" variant="label">
            Edit
          </AppText>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          onPress={onCoach}
          style={styles.coachButton}
        >
          <AppText tone="accent" variant="label">
            Review
          </AppText>
        </Pressable>
      </View>
    </View>
  );
}

function formatCount(value: number, singular: string) {
  return `${value} ${singular}${value === 1 ? "" : "s"}`;
}

function formatUpdatedAt(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "recently";
  return date.toLocaleDateString(undefined, {
    day: "numeric",
    month: "short",
    year:
      date.getFullYear() === new Date().getFullYear() ? undefined : "numeric",
  });
}

const styles = StyleSheet.create({
  content: {
    width: "100%",
    maxWidth: layout.readableContentWidth,
    alignSelf: "center",
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.xxl,
    paddingBottom: spacing.xxxl,
  },
  header: {
    gap: spacing.xl,
    paddingBottom: spacing.xxl,
  },
  intro: {
    gap: spacing.sm,
  },
  empty: {
    gap: spacing.sm,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
    paddingVertical: spacing.xxl,
  },
  coachLink: { minHeight: 44, alignItems: "center", justifyContent: "center" },
  row: {
    minHeight: 104,
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border,
    paddingVertical: spacing.lg,
  },
  rowCopy: {
    flex: 1,
    gap: spacing.xs,
  },
  rowActions: { alignItems: "center", gap: spacing.xs },
  startButton: {
    minHeight: 44,
    minWidth: 72,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 12,
    backgroundColor: colors.accent,
  },
  startLabel: { color: colors.onAccent },
  editButton: {
    minHeight: 44,
    minWidth: 72,
    alignItems: "center",
    justifyContent: "center",
  },
  coachButton: {
    minHeight: 44,
    minWidth: 72,
    alignItems: "center",
    justifyContent: "center",
  },
  rowTitle: {
    fontSize: 20,
  },
  updated: {
    fontSize: 13,
  },
  pressed: {
    opacity: 0.7,
  },
});
