import { useEffect, useState } from "react";
import {
  ActivityIndicator,
  FlatList,
  Modal,
  Pressable,
  StyleSheet,
  TextInput,
  View,
} from "react-native";

import {
  getExercise,
  searchExercises,
  type ExerciseDetail,
  type ExerciseSummary,
} from "../../api/exercises";
import type { TrainingProfile } from "../../api/profiles";
import { AppText } from "../../components/AppText";
import { PrimaryButton } from "../../components/PrimaryButton";
import { colors, layout, radii, spacing } from "../../theme/tokens";

type ExercisePickerProps = {
  excludedExerciseIds: ReadonlySet<string>;
  onClose: () => void;
  onSelect: (exercise: ExerciseSummary) => void;
  profile: TrainingProfile;
  visible: boolean;
};

export function ExercisePicker({
  excludedExerciseIds,
  onClose,
  onSelect,
  profile,
  visible,
}: ExercisePickerProps) {
  const [query, setQuery] = useState("");
  const [items, setItems] = useState<ExerciseSummary[]>([]);
  const [selected, setSelected] = useState<ExerciseSummary>();
  const [detail, setDetail] = useState<ExerciseDetail>();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string>();
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    if (!visible || selected) return;

    const controller = new AbortController();
    const timer = setTimeout(() => {
      setIsLoading(true);
      setError(undefined);
      searchExercises(
        {
          query: query.trim() || undefined,
          availableEquipment: profile.availableEquipment,
          limit: 50,
        },
        { signal: controller.signal },
      )
        .then((result) => setItems(result.items))
        .catch(() => {
          if (!controller.signal.aborted) {
            setError("The exercise catalogue could not be loaded.");
          }
        })
        .finally(() => {
          if (!controller.signal.aborted) setIsLoading(false);
        });
    }, 250);

    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, [profile.availableEquipment, query, reloadKey, selected, visible]);

  useEffect(() => {
    if (!selected) return;

    const controller = new AbortController();
    getExercise(selected.id, { signal: controller.signal })
      .then(setDetail)
      .catch(() => {
        if (!controller.signal.aborted) {
          setError("The exercise details could not be loaded.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false);
      });

    return () => controller.abort();
  }, [selected, reloadKey]);

  const close = () => {
    setQuery("");
    setSelected(undefined);
    setDetail(undefined);
    setError(undefined);
    onClose();
  };

  const showCatalogue = () => {
    setSelected(undefined);
    setDetail(undefined);
    setError(undefined);
    setIsLoading(false);
  };

  const showDetail = (exercise: ExerciseSummary) => {
    setDetail(undefined);
    setError(undefined);
    setIsLoading(true);
    setSelected(exercise);
  };

  const retry = () => {
    setError(undefined);
    setIsLoading(true);
    setReloadKey((value) => value + 1);
  };

  return (
    <Modal
      animationType="slide"
      onRequestClose={selected ? showCatalogue : close}
      presentationStyle="pageSheet"
      visible={visible}
    >
      <View style={styles.screen}>
        <View style={styles.header}>
          <Pressable
            accessibilityRole="button"
            onPress={selected ? showCatalogue : close}
            style={styles.headerAction}
          >
            <AppText tone="secondary" variant="label">
              {selected ? "Back" : "Cancel"}
            </AppText>
          </Pressable>
          <AppText
            accessibilityRole="header"
            style={styles.headerTitle}
            variant="label"
          >
            {selected ? "Exercise details" : "Add exercise"}
          </AppText>
          <View style={styles.headerAction} />
        </View>

        {selected ? (
          <ExerciseDetailView
            detail={detail}
            error={error}
            isLoading={isLoading}
            onAdd={() => {
              onSelect(selected);
              close();
            }}
            onRetry={retry}
          />
        ) : (
          <View style={styles.catalogue}>
            <TextInput
              accessibilityLabel="Search exercises"
              allowFontScaling
              autoCapitalize="none"
              autoCorrect={false}
              onChangeText={setQuery}
              placeholder="Search exercises"
              placeholderTextColor={colors.textSecondary}
              returnKeyType="search"
              selectionColor={colors.focus}
              style={styles.search}
              value={query}
            />

            {error ? (
              <View style={styles.status}>
                <AppText accessibilityRole="alert" tone="secondary">
                  {error}
                </AppText>
                <Pressable
                  accessibilityRole="button"
                  onPress={retry}
                  style={styles.retry}
                >
                  <AppText tone="accent" variant="label">
                    Try again
                  </AppText>
                </Pressable>
              </View>
            ) : isLoading ? (
              <ActivityIndicator
                accessibilityLabel="Loading exercises"
                accessibilityRole="progressbar"
                color={colors.accentHighlight}
                style={styles.status}
              />
            ) : (
              <FlatList
                contentContainerStyle={styles.list}
                data={items}
                keyboardShouldPersistTaps="handled"
                keyExtractor={(item) => item.id}
                ListEmptyComponent={
                  <View style={styles.status}>
                    <AppText tone="secondary">
                      No compatible exercises match this search.
                    </AppText>
                  </View>
                }
                renderItem={({ item }) => {
                  const isAdded = excludedExerciseIds.has(item.id);
                  return (
                    <Pressable
                      accessibilityRole="button"
                      accessibilityState={{ disabled: isAdded }}
                      disabled={isAdded}
                      onPress={() => showDetail(item)}
                      style={({ pressed }) => [
                        styles.exerciseRow,
                        pressed && styles.pressed,
                        isAdded && styles.added,
                      ]}
                    >
                      <View style={styles.exerciseCopy}>
                        <AppText variant="label">{item.name}</AppText>
                        <AppText style={styles.exerciseMeta} tone="secondary">
                          {formatMuscles(item.primaryMuscles)}
                        </AppText>
                      </View>
                      <AppText
                        tone={isAdded ? "secondary" : "accent"}
                        variant="label"
                      >
                        {isAdded ? "Added" : "Details"}
                      </AppText>
                    </Pressable>
                  );
                }}
                showsVerticalScrollIndicator={false}
              />
            )}
          </View>
        )}
      </View>
    </Modal>
  );
}

function ExerciseDetailView({
  detail,
  error,
  isLoading,
  onAdd,
  onRetry,
}: {
  detail?: ExerciseDetail;
  error?: string;
  isLoading: boolean;
  onAdd: () => void;
  onRetry: () => void;
}) {
  if (isLoading) {
    return (
      <ActivityIndicator
        accessibilityLabel="Loading exercise details"
        accessibilityRole="progressbar"
        color={colors.accentHighlight}
        style={styles.detailStatus}
      />
    );
  }

  if (error || !detail) {
    return (
      <View style={styles.detailStatus}>
        <AppText accessibilityRole="alert" tone="secondary">
          {error ?? "The exercise details are unavailable."}
        </AppText>
        <PrimaryButton label="Try again" onPress={onRetry} />
      </View>
    );
  }

  return (
    <FlatList
      contentContainerStyle={styles.detailContent}
      data={
        [
          ["Setup", detail.setup],
          ["Execution", detail.execution],
          ["Safety", detail.safety],
        ] as const
      }
      keyExtractor={([title]) => title}
      ListHeaderComponent={
        <View style={styles.detailIntro}>
          <AppText tone="accent" variant="eyebrow">
            {formatMuscles(detail.primaryMuscles)}
          </AppText>
          <AppText accessibilityRole="header" variant="title">
            {detail.name}
          </AppText>
          <AppText tone="secondary">
            {formatTrackingMode(detail.trackingMode)}
          </AppText>
        </View>
      }
      ListFooterComponent={
        <View style={styles.detailFooter}>
          <PrimaryButton label="Add to workout" onPress={onAdd} />
        </View>
      }
      renderItem={({ item: [title, copy] }) => (
        <View style={styles.detailSection}>
          <AppText variant="label">{title}</AppText>
          <AppText tone="secondary">{copy}</AppText>
        </View>
      )}
    />
  );
}

function formatMuscles(muscles: readonly string[]) {
  return muscles.map(capitalizeWords).join(" · ");
}

function formatTrackingMode(value: string) {
  const labels: Record<string, string> = {
    repetitions: "Track repetitions",
    repetitionsAndLoad: "Track repetitions and load",
    duration: "Track duration",
    distanceAndDuration: "Track distance and duration",
    distanceDurationAndLoad: "Track distance, duration and load",
  };
  return labels[value] ?? value;
}

function capitalizeWords(value: string) {
  return value
    .replace(/([A-Z])/g, " $1")
    .replace(/^./, (letter) => letter.toUpperCase());
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: colors.canvas,
  },
  header: {
    minHeight: 56,
    flexDirection: "row",
    alignItems: "center",
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
    paddingHorizontal: spacing.lg,
  },
  headerAction: {
    minWidth: 72,
    minHeight: layout.minimumTouchTarget,
    justifyContent: "center",
  },
  headerTitle: {
    flex: 1,
    textAlign: "center",
  },
  catalogue: {
    flex: 1,
    width: "100%",
    maxWidth: layout.readableContentWidth,
    alignSelf: "center",
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.lg,
  },
  search: {
    minHeight: 50,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.control,
    paddingHorizontal: spacing.lg,
    color: colors.textPrimary,
    backgroundColor: colors.surface,
    fontSize: 17,
  },
  list: {
    paddingTop: spacing.lg,
    paddingBottom: spacing.xxxl,
  },
  exerciseRow: {
    minHeight: 68,
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
    paddingVertical: spacing.md,
  },
  exerciseCopy: {
    flex: 1,
    gap: spacing.xs,
  },
  exerciseMeta: {
    fontSize: 14,
  },
  pressed: {
    opacity: 0.72,
  },
  added: {
    opacity: 0.48,
  },
  status: {
    minHeight: 160,
    alignItems: "center",
    justifyContent: "center",
    gap: spacing.lg,
    padding: spacing.xl,
  },
  retry: {
    minHeight: layout.minimumTouchTarget,
    justifyContent: "center",
  },
  detailStatus: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    gap: spacing.lg,
    padding: spacing.xl,
  },
  detailContent: {
    width: "100%",
    maxWidth: layout.readableContentWidth,
    alignSelf: "center",
    gap: spacing.xl,
    padding: spacing.xl,
    paddingBottom: spacing.xxxl,
  },
  detailIntro: {
    gap: spacing.sm,
  },
  detailSection: {
    gap: spacing.sm,
  },
  detailFooter: {
    paddingTop: spacing.md,
  },
});
