import { useLocalSearchParams, useRouter } from "expo-router";
import { StyleSheet, View } from "react-native";

import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { PrimaryButton } from "../../components/PrimaryButton";
import { RouteStatus } from "../../components/RouteStatus";
import { useWorkoutSession } from "../../features/sessions/useWorkoutSession";
import { layout, spacing } from "../../theme/tokens";

export default function WorkoutSummaryRoute() {
  const router = useRouter();
  const { profileId } = useLocalSearchParams<{ profileId?: string }>();
  if (!profileId) {
    return (
      <RouteStatus
        title="Summary unavailable"
        message="The session profile is missing."
      />
    );
  }
  return (
    <WorkoutSummary
      profileId={profileId}
      onDone={() =>
        router.replace({ pathname: "/workouts", params: { profileId } })
      }
    />
  );
}

function WorkoutSummary({
  profileId,
  onDone,
}: {
  profileId: string;
  onDone: () => void;
}) {
  const { session, loadState, message, clearCompleted } =
    useWorkoutSession(profileId);
  if (loadState === "loading") {
    return (
      <RouteStatus
        busy
        title="Preparing summary"
        message="Checking your saved session."
      />
    );
  }
  if (!session) {
    return (
      <RouteStatus
        actionLabel="Return to workouts"
        onAction={onDone}
        title="Summary unavailable"
        message={message ?? "This session is no longer available."}
      />
    );
  }

  const sets = session.exercises.flatMap((exercise) => exercise.sets);
  const completedSets = sets.filter((set) => set.isCompleted).length;
  const skippedExercises = session.exercises.filter(
    (exercise) => exercise.isSkipped,
  ).length;
  const finishedAt = session.finishedAt
    ? Date.parse(session.finishedAt)
    : Date.parse(session.updatedAt);
  const elapsedMinutes = Math.max(
    1,
    Math.round((finishedAt - Date.parse(session.startedAt)) / 60_000),
  );

  return (
    <AppScreen>
      <View style={styles.content}>
        <View style={styles.intro}>
          <AppText tone="accent" variant="eyebrow">
            Session summary
          </AppText>
          <AppText accessibilityRole="header" variant="display">
            {session.workoutName}
          </AppText>
          <AppText tone="secondary">
            {session.syncState === "synced"
              ? "Your completed workout is synchronized."
              : (message ?? "Saved on this device and waiting to synchronize.")}
          </AppText>
        </View>
        <View style={styles.metrics}>
          <Metric label="Completed sets" value={String(completedSets)} />
          <Metric label="Elapsed time" value={`${elapsedMinutes} min`} />
          <Metric label="Skipped exercises" value={String(skippedExercises)} />
        </View>
        {session.notes ? (
          <View style={styles.note}>
            <AppText variant="label">Session note</AppText>
            <AppText tone="secondary">{session.notes}</AppText>
          </View>
        ) : null}
        <PrimaryButton
          disabled={session.syncState !== "synced"}
          label={
            session.syncState === "synced"
              ? "Return to workouts"
              : "Waiting to synchronize…"
          }
          onPress={() =>
            void clearCompleted().then((cleared) => cleared && onDone())
          }
        />
      </View>
    </AppScreen>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <View style={styles.metric}>
      <AppText variant="title">{value}</AppText>
      <AppText tone="secondary">{label}</AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  content: {
    flex: 1,
    width: "100%",
    maxWidth: layout.readableContentWidth,
    alignSelf: "center",
    padding: spacing.xl,
    gap: spacing.xxl,
  },
  intro: { gap: spacing.sm },
  metrics: { gap: spacing.lg },
  metric: { gap: spacing.xs },
  note: { gap: spacing.sm },
});
