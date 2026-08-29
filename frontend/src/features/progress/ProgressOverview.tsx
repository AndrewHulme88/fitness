import { useEffect, useState } from "react";
import { FlatList, Pressable, StyleSheet, View } from "react-native";

import {
  getProgressOverview,
  type ProgressOverview as ProgressOverviewDocument,
  type RecordedExerciseSummary,
} from "../../api/progress";
import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { RouteStatus } from "../../components/RouteStatus";
import { TrainingSections } from "../../components/TrainingSections";
import { colors, layout, spacing } from "../../theme/tokens";

type Props = {
  onHistory: () => void;
  onPlans: () => void;
  onSelectExercise: (exerciseId: string) => void;
  profileId: string;
};

export function ProgressOverview({
  onHistory,
  onPlans,
  onSelectExercise,
  profileId,
}: Props) {
  const [overview, setOverview] = useState<ProgressOverviewDocument>();
  const [error, setError] = useState<string>();
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    getProgressOverview(profileId, { signal: controller.signal })
      .then(setOverview)
      .catch(() => {
        if (!controller.signal.aborted)
          setError("Your progress could not be loaded.");
      });
    return () => controller.abort();
  }, [profileId, reloadKey]);

  if (!overview && !error) {
    return (
      <RouteStatus
        busy
        message="Calculating totals from your recorded workouts."
        title="Preparing progress"
      />
    );
  }
  if (!overview) {
    return (
      <RouteStatus
        actionLabel="Try again"
        message={error ?? "Your progress is unavailable."}
        onAction={() => {
          setError(undefined);
          setReloadKey((value) => value + 1);
        }}
        title="Progress unavailable"
      />
    );
  }

  return (
    <AppScreen>
      <FlatList
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        data={overview.recordedExercises}
        keyExtractor={(item) => item.exerciseId}
        ListHeaderComponent={
          <View style={styles.header}>
            <TrainingSections
              active="progress"
              onHistory={onHistory}
              onPlans={onPlans}
              onProgress={() => undefined}
            />
            <View style={styles.intro}>
              <AppText tone="accent" variant="eyebrow">
                Last four weeks
              </AppText>
              <AppText accessibilityRole="header" variant="display">
                Progress
              </AppText>
              <AppText tone="secondary">
                These totals come directly from completed workouts. They are not
                a score or an estimate of fitness.
              </AppText>
            </View>
            <View style={styles.metrics}>
              <Metric
                label="Workouts"
                value={String(overview.completedWorkoutCount)}
              />
              <Metric label="Sets" value={String(overview.completedSetCount)} />
              <Metric
                label="Training time"
                value={formatTotalTime(overview.totalWorkoutDurationSeconds)}
              />
            </View>
            <View style={styles.listIntro}>
              <AppText variant="title">Recorded performance</AppText>
              <AppText tone="secondary">
                Review up to 12 recent appearances for each exercise.
              </AppText>
            </View>
          </View>
        }
        ListEmptyComponent={
          <View style={styles.empty}>
            <AppText variant="title">No recorded performance yet</AppText>
            <AppText tone="secondary">
              Complete a workout to establish your first training record.
            </AppText>
          </View>
        }
        renderItem={({ item }) => (
          <ExerciseRow
            item={item}
            onPress={() => onSelectExercise(item.exerciseId)}
          />
        )}
        showsVerticalScrollIndicator={false}
      />
    </AppScreen>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <View style={styles.metric}>
      <AppText style={styles.metricValue} variant="title">
        {value}
      </AppText>
      <AppText style={styles.metricLabel} tone="secondary">
        {label}
      </AppText>
    </View>
  );
}

function ExerciseRow({
  item,
  onPress,
}: {
  item: RecordedExerciseSummary;
  onPress: () => void;
}) {
  const appearances = Number(item.appearanceCount);
  return (
    <Pressable
      accessibilityHint="Review recorded values for this exercise"
      accessibilityRole="button"
      onPress={onPress}
      style={({ pressed }) => [styles.row, pressed && styles.pressed]}
    >
      <View style={styles.rowCopy}>
        <AppText style={styles.rowTitle} variant="title">
          {item.exerciseName}
        </AppText>
        <AppText tone="secondary">
          {appearances} recorded {appearances === 1 ? "workout" : "workouts"} ·
          Last {formatShortDate(item.lastPerformedAt)}
        </AppText>
      </View>
      <AppText tone="accent" variant="label">
        View
      </AppText>
    </Pressable>
  );
}

function formatTotalTime(value: number | string) {
  const minutes = Math.round(Number(value) / 60);
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  return `${hours}h ${minutes % 60}m`;
}

function formatShortDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "date unavailable";
  return date.toLocaleDateString(undefined, { day: "numeric", month: "short" });
}

const styles = StyleSheet.create({
  content: {
    width: "100%",
    maxWidth: layout.readableContentWidth,
    alignSelf: "center",
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.lg,
    paddingBottom: spacing.xxxl,
  },
  header: { gap: spacing.xxl, paddingBottom: spacing.lg },
  intro: { gap: spacing.sm },
  metrics: {
    flexDirection: "row",
    gap: spacing.sm,
    paddingVertical: spacing.lg,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
  },
  metric: { flex: 1, gap: spacing.xs },
  metricValue: { fontSize: 22 },
  metricLabel: { fontSize: 13 },
  listIntro: { gap: spacing.sm },
  empty: {
    gap: spacing.sm,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
    paddingVertical: spacing.xxl,
  },
  row: {
    minHeight: 82,
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border,
    paddingVertical: spacing.lg,
  },
  rowCopy: { flex: 1, gap: spacing.xs },
  rowTitle: { fontSize: 19 },
  pressed: { opacity: 0.7 },
});
