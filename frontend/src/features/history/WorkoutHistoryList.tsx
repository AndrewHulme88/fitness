import { useCallback, useEffect, useMemo, useState } from "react";
import { Pressable, SectionList, StyleSheet, View } from "react-native";

import {
  listWorkoutHistory,
  type WorkoutHistorySummary,
} from "../../api/sessions";
import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { RouteStatus } from "../../components/RouteStatus";
import { TrainingSections } from "../../components/TrainingSections";
import { colors, layout, spacing } from "../../theme/tokens";

type Props = {
  onPlans: () => void;
  onProgress: () => void;
  onSelect: (sessionId: string) => void;
  profileId: string;
};

const pageSize = 20;

export function WorkoutHistoryList({
  onPlans,
  onProgress,
  onSelect,
  profileId,
}: Props) {
  const [items, setItems] = useState<WorkoutHistorySummary[]>([]);
  const [nextOffset, setNextOffset] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [error, setError] = useState<string>();
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    listWorkoutHistory(
      profileId,
      { limit: pageSize, offset: 0 },
      { signal: controller.signal },
    )
      .then((result) => {
        setItems(result.items);
        setNextOffset(
          result.nextOffset === null ? null : Number(result.nextOffset),
        );
      })
      .catch(() => {
        if (!controller.signal.aborted)
          setError("Your workout history could not be loaded.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false);
      });
    return () => controller.abort();
  }, [profileId, reloadKey]);

  const loadMore = useCallback(async () => {
    if (nextOffset === null || isLoadingMore) return;
    setIsLoadingMore(true);
    setError(undefined);
    try {
      const result = await listWorkoutHistory(profileId, {
        limit: pageSize,
        offset: nextOffset,
      });
      setItems((current) => [...current, ...result.items]);
      setNextOffset(
        result.nextOffset === null ? null : Number(result.nextOffset),
      );
    } catch {
      setError("Earlier workouts could not be loaded.");
    } finally {
      setIsLoadingMore(false);
    }
  }, [isLoadingMore, nextOffset, profileId]);

  const sections = useMemo(() => groupHistoryByLocalDate(items), [items]);
  if (isLoading) {
    return (
      <RouteStatus
        busy
        message="Loading your completed workouts."
        title="Preparing history"
      />
    );
  }
  if (error && items.length === 0) {
    return (
      <RouteStatus
        actionLabel="Try again"
        message={error}
        onAction={() => {
          setError(undefined);
          setIsLoading(true);
          setReloadKey((value) => value + 1);
        }}
        title="History unavailable"
      />
    );
  }

  return (
    <AppScreen>
      <SectionList
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        sections={sections}
        keyExtractor={(item) => item.id}
        ListHeaderComponent={
          <View style={styles.header}>
            <TrainingSections
              active="history"
              onHistory={() => undefined}
              onPlans={onPlans}
              onProgress={onProgress}
            />
            <View style={styles.intro}>
              <AppText tone="accent" variant="eyebrow">
                Completed training
              </AppText>
              <AppText accessibilityRole="header" variant="display">
                History
              </AppText>
              <AppText tone="secondary">
                Recorded sessions are shown by the date on this device.
              </AppText>
            </View>
          </View>
        }
        ListEmptyComponent={
          <View style={styles.empty}>
            <AppText variant="title">No completed workouts</AppText>
            <AppText tone="secondary">
              Finished workouts will appear here with only the values you
              recorded.
            </AppText>
          </View>
        }
        ListFooterComponent={
          nextOffset !== null || error ? (
            <View style={styles.footer}>
              {error ? (
                <AppText accessibilityRole="alert" tone="secondary">
                  {error}
                </AppText>
              ) : null}
              {nextOffset !== null ? (
                <Pressable
                  accessibilityRole="button"
                  disabled={isLoadingMore}
                  onPress={() => void loadMore()}
                  style={styles.loadMore}
                >
                  <AppText tone="accent" variant="label">
                    {isLoadingMore ? "Loading…" : "Load earlier workouts"}
                  </AppText>
                </Pressable>
              ) : null}
            </View>
          ) : null
        }
        renderSectionHeader={({ section }) => (
          <AppText style={styles.sectionTitle} variant="label">
            {section.title}
          </AppText>
        )}
        renderItem={({ item }) => (
          <HistoryRow item={item} onPress={() => onSelect(item.id)} />
        )}
        stickySectionHeadersEnabled={false}
        showsVerticalScrollIndicator={false}
      />
    </AppScreen>
  );
}

function HistoryRow({
  item,
  onPress,
}: {
  item: WorkoutHistorySummary;
  onPress: () => void;
}) {
  const completedSets = Number(item.completedSetCount);
  const totalSets = Number(item.totalSetCount);
  const skipped = Number(item.skippedExerciseCount);
  return (
    <Pressable
      accessibilityHint="Open the completed workout record"
      accessibilityRole="button"
      onPress={onPress}
      style={({ pressed }) => [styles.row, pressed && styles.pressed]}
    >
      <View style={styles.rowCopy}>
        <AppText style={styles.rowTitle} variant="title">
          {item.workoutName}
        </AppText>
        <AppText tone="secondary">
          {completedSets}/{totalSets} sets ·{" "}
          {formatDuration(item.durationSeconds)}
        </AppText>
        {skipped > 0 || item.correctedAt ? (
          <AppText style={styles.supporting} tone="secondary">
            {[
              skipped > 0
                ? `${skipped} skipped ${skipped === 1 ? "exercise" : "exercises"}`
                : null,
              item.correctedAt ? "Corrected" : null,
            ]
              .filter(Boolean)
              .join(" · ")}
          </AppText>
        ) : null}
      </View>
      <AppText tone="accent" variant="label">
        View
      </AppText>
    </Pressable>
  );
}

export function groupHistoryByLocalDate(items: WorkoutHistorySummary[]) {
  const sections: { title: string; data: WorkoutHistorySummary[] }[] = [];
  for (const item of items) {
    const title = formatLocalDay(item.finishedAt);
    const current = sections.at(-1);
    if (current?.title === title) current.data.push(item);
    else sections.push({ title, data: [item] });
  }
  return sections;
}

function formatLocalDay(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Date unavailable";
  return date.toLocaleDateString(undefined, {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

function formatDuration(value: number | string) {
  const totalMinutes = Math.max(1, Math.round(Number(value) / 60));
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes} min`;
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
  header: { gap: spacing.xxl, paddingBottom: spacing.xxl },
  intro: { gap: spacing.sm },
  empty: {
    gap: spacing.sm,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
    paddingVertical: spacing.xxl,
  },
  sectionTitle: {
    paddingTop: spacing.xl,
    paddingBottom: spacing.sm,
    color: colors.textSecondary,
  },
  row: {
    minHeight: 92,
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border,
    paddingVertical: spacing.lg,
  },
  rowCopy: { flex: 1, gap: spacing.xs },
  rowTitle: { fontSize: 20 },
  supporting: { fontSize: 13 },
  footer: { alignItems: "center", gap: spacing.sm, paddingTop: spacing.xl },
  loadMore: {
    minHeight: 48,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: spacing.lg,
  },
  pressed: { opacity: 0.7 },
});
