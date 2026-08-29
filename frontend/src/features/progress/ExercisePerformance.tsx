import { useEffect, useState } from "react";
import { ScrollView, StyleSheet, View } from "react-native";

import {
  getExercisePerformance,
  type ExercisePerformance as ExercisePerformanceDocument,
} from "../../api/progress";
import { getTrainingProfile } from "../../api/profiles";
import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { RouteStatus } from "../../components/RouteStatus";
import { colors, layout, spacing } from "../../theme/tokens";
import {
  formatRecordedValues,
  type UnitSystem,
} from "../sessions/session-values";

type Props = { exerciseId: string; profileId: string };

export function ExercisePerformance({ exerciseId, profileId }: Props) {
  const [performance, setPerformance] = useState<ExercisePerformanceDocument>();
  const [unitSystem, setUnitSystem] = useState<UnitSystem>();
  const [error, setError] = useState<string>();
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    Promise.all([
      getExercisePerformance(
        profileId,
        exerciseId,
        { limit: 12 },
        {
          signal: controller.signal,
        },
      ),
      getTrainingProfile(profileId, { signal: controller.signal }),
    ])
      .then(([loadedPerformance, profile]) => {
        setPerformance(loadedPerformance);
        setUnitSystem(profile.unitSystem);
      })
      .catch(() => {
        if (!controller.signal.aborted)
          setError("Recorded performance could not be loaded.");
      });
    return () => controller.abort();
  }, [exerciseId, profileId, reloadKey]);

  if (!performance || !unitSystem) {
    return error ? (
      <RouteStatus
        actionLabel="Try again"
        message={error}
        onAction={() => {
          setError(undefined);
          setReloadKey((value) => value + 1);
        }}
        title="Performance unavailable"
      />
    ) : (
      <RouteStatus
        busy
        message="Loading recent recorded values."
        title="Preparing performance"
      />
    );
  }

  return (
    <AppScreen>
      <ScrollView
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.header}>
          <AppText tone="accent" variant="eyebrow">
            Recorded performance
          </AppText>
          <AppText accessibilityRole="header" variant="display">
            {performance.exerciseName}
          </AppText>
          <AppText tone="secondary">
            Actual completed-set values from your 12 most recent appearances.
          </AppText>
        </View>

        {performance.appearances.length < 2 ? (
          <View style={styles.insufficient}>
            <AppText variant="label">One recorded appearance</AppText>
            <AppText tone="secondary">
              More completed workouts will add recorded comparisons here.
            </AppText>
          </View>
        ) : null}

        <View>
          {performance.appearances.map((appearance) => (
            <View key={appearance.sessionId} style={styles.appearance}>
              <View style={styles.appearanceHeader}>
                <AppText variant="label">{appearance.workoutName}</AppText>
                <AppText style={styles.date} tone="secondary">
                  {formatDate(appearance.performedAt)}
                </AppText>
              </View>
              {appearance.sets.map((set) => (
                <View key={Number(set.position)} style={styles.setRow}>
                  <AppText style={styles.setLabel} tone="secondary">
                    Set {Number(set.position) + 1}
                  </AppText>
                  <AppText>
                    {formatRecordedValues(
                      performance.trackingMode,
                      {
                        actualRepetitions: nullableNumber(
                          set.actualRepetitions,
                        ),
                        actualLoadKilograms: nullableNumber(
                          set.actualLoadKilograms,
                        ),
                        actualDurationSeconds: nullableNumber(
                          set.actualDurationSeconds,
                        ),
                        actualDistanceMetres: nullableNumber(
                          set.actualDistanceMetres,
                        ),
                      },
                      unitSystem,
                    )}
                  </AppText>
                </View>
              ))}
            </View>
          ))}
        </View>
      </ScrollView>
    </AppScreen>
  );
}

function nullableNumber(value: number | string | null) {
  return value === null ? null : Number(value);
}

function formatDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Date unavailable";
  return date.toLocaleDateString(undefined, {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

const styles = StyleSheet.create({
  content: {
    width: "100%",
    maxWidth: layout.readableContentWidth,
    alignSelf: "center",
    padding: spacing.lg,
    paddingBottom: spacing.xxxl,
    gap: spacing.xl,
  },
  header: { gap: spacing.sm },
  insufficient: {
    gap: spacing.xs,
    padding: spacing.lg,
    borderRadius: 14,
    backgroundColor: colors.surface,
  },
  appearance: {
    gap: spacing.sm,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border,
    paddingVertical: spacing.lg,
  },
  appearanceHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    gap: spacing.md,
  },
  date: { fontSize: 14 },
  setRow: {
    flexDirection: "row",
    gap: spacing.md,
    paddingVertical: spacing.xs,
  },
  setLabel: { width: 54 },
});
