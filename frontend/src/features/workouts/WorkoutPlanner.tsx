import { useEffect, useRef, useState } from "react";
import {
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  TextInput,
  View,
  useWindowDimensions,
} from "react-native";

import type { ExerciseSummary } from "../../api/exercises";
import { getTrainingProfile, type TrainingProfile } from "../../api/profiles";
import {
  createWorkout,
  getWorkout,
  updateWorkout,
  WorkoutRevisionConflictError,
  type WorkoutDetail,
} from "../../api/workouts";
import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { PrimaryButton } from "../../components/PrimaryButton";
import { RouteStatus } from "../../components/RouteStatus";
import { colors, layout, radii, spacing } from "../../theme/tokens";
import { DraggableExerciseList } from "./DraggableExerciseList";
import { ExercisePicker } from "./ExercisePicker";
import { PrescriptionEditor } from "./PrescriptionEditor";
import {
  buildWorkoutRequest,
  createDraftFromWorkoutExercise,
  createExerciseDraft,
  type WorkoutDraftErrors,
  type WorkoutExerciseDraft,
} from "./workout-draft";

type WorkoutPlannerProps = {
  onSaved: () => void;
  profileId: string;
  workoutId?: string;
};

export function WorkoutPlanner({
  onSaved,
  profileId,
  workoutId,
}: WorkoutPlannerProps) {
  const { height: windowHeight } = useWindowDimensions();
  const scrollRef = useRef<ScrollView>(null);
  const scrollOffset = useRef(0);
  const [profile, setProfile] = useState<TrainingProfile>();
  const [loadedWorkout, setLoadedWorkout] = useState<WorkoutDetail>();
  const [name, setName] = useState("");
  const [drafts, setDrafts] = useState<WorkoutExerciseDraft[]>([]);
  const [selectedExerciseId, setSelectedExerciseId] = useState<string>();
  const [pickerVisible, setPickerVisible] = useState(false);
  const [errors, setErrors] = useState<WorkoutDraftErrors>({ byExercise: {} });
  const [submissionError, setSubmissionError] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    Promise.all([
      getTrainingProfile(profileId, { signal: controller.signal }),
      workoutId
        ? getWorkout(profileId, workoutId, { signal: controller.signal })
        : Promise.resolve(undefined),
    ])
      .then(([loadedProfile, workout]) => {
        setProfile(loadedProfile);
        setLoadedWorkout(workout);
        setName(workout?.name ?? "");
        setDrafts(
          workout
            ? workout.exercises.map((exercise) =>
                createDraftFromWorkoutExercise(
                  exercise,
                  loadedProfile.unitSystem,
                ),
              )
            : [],
        );
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setSubmissionError("The workout planner could not be loaded.");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false);
      });

    return () => controller.abort();
  }, [profileId, reloadKey, workoutId]);

  const retryLoad = () => {
    setSubmissionError(undefined);
    setIsLoading(true);
    setReloadKey((value) => value + 1);
  };

  if (isLoading) {
    return (
      <RouteStatus
        busy
        message="Loading your profile and exercise options."
        title="Preparing your workout"
      />
    );
  }

  if (!profile) {
    return (
      <RouteStatus
        actionLabel="Try again"
        message={submissionError ?? "Your training profile is unavailable."}
        onAction={retryLoad}
        title="Workout unavailable"
      />
    );
  }

  const selectedDraft = drafts.find(
    (draft) => draft.exercise.id === selectedExerciseId,
  );
  const selectedIds = new Set(drafts.map((draft) => draft.exercise.id));
  const plannedSetCount = drafts.reduce(
    (total, draft) => total + (Number(draft.plannedSets) || 0),
    0,
  );

  const handleExerciseSelected = (exercise: ExerciseSummary) => {
    if (selectedIds.has(exercise.id) || drafts.length >= 20) return;
    const draft = createExerciseDraft(exercise);
    setDrafts((current) => [...current, draft]);
    setSelectedExerciseId(exercise.id);
  };

  const handleReorder = (fromIndex: number, toIndex: number) => {
    if (fromIndex === toIndex) return;
    setDrafts((current) => {
      const next = [...current];
      const [moved] = next.splice(fromIndex, 1);
      if (!moved) return current;
      next.splice(toIndex, 0, moved);
      return next;
    });
  };

  const handleAutoScroll = (absoluteY: number) => {
    const edge = 120;
    const increment = 18;
    if (absoluteY < edge) {
      scrollRef.current?.scrollTo({
        y: Math.max(0, scrollOffset.current - increment),
        animated: false,
      });
    } else if (absoluteY > windowHeight - edge) {
      scrollRef.current?.scrollTo({
        y: scrollOffset.current + increment,
        animated: false,
      });
    }
  };

  const handleSave = async () => {
    const result = buildWorkoutRequest(name, drafts, profile.unitSystem);
    if (result.errors) {
      setErrors(result.errors);
      const firstInvalidExercise = drafts.find(
        (draft) => result.errors.byExercise[draft.exercise.id],
      );
      if (firstInvalidExercise) {
        setSelectedExerciseId(firstInvalidExercise.exercise.id);
      }
      return;
    }

    setErrors({ byExercise: {} });
    setSubmissionError(undefined);
    setIsSaving(true);
    try {
      if (workoutId && loadedWorkout) {
        await updateWorkout(profileId, workoutId, {
          ...result.request,
          expectedRevision: loadedWorkout.revision,
        });
      } else {
        await createWorkout(profileId, result.request);
      }
      onSaved();
    } catch (error) {
      setSubmissionError(
        error instanceof WorkoutRevisionConflictError
          ? "This workout changed elsewhere. Reload it before saving again."
          : "We couldn’t save this workout. Check your connection and try again.",
      );
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <AppScreen>
      <KeyboardAvoidingView
        behavior={Platform.OS === "ios" ? "padding" : undefined}
        style={styles.screen}
      >
        <ScrollView
          ref={scrollRef}
          contentContainerStyle={styles.content}
          contentInsetAdjustmentBehavior="automatic"
          keyboardDismissMode="interactive"
          keyboardShouldPersistTaps="handled"
          onScroll={(event) => {
            scrollOffset.current = event.nativeEvent.contentOffset.y;
          }}
          scrollEventThrottle={16}
          showsVerticalScrollIndicator={false}
        >
          <View style={styles.intro}>
            <AppText tone="accent" variant="eyebrow">
              {workoutId ? "Edit workout" : "New workout"}
            </AppText>
            <AppText accessibilityRole="header" variant="title">
              Build a workout you can reuse.
            </AppText>
          </View>

          <View style={styles.nameField}>
            <AppText variant="label">Workout name</AppText>
            <TextInput
              accessibilityLabel="Workout name"
              allowFontScaling
              autoCorrect={false}
              maxLength={80}
              onChangeText={setName}
              placeholder="e.g. Upper strength"
              placeholderTextColor={colors.textSecondary}
              selectionColor={colors.focus}
              style={styles.nameInput}
              value={name}
            />
            {errors.name ? (
              <AppText accessibilityRole="alert" style={styles.error}>
                {errors.name}
              </AppText>
            ) : null}
          </View>

          <View style={styles.summary}>
            <SummaryValue label="Exercises" value={drafts.length} />
            <SummaryValue label="Planned sets" value={plannedSetCount} />
          </View>

          <View>
            <View style={styles.sectionHeading}>
              <View>
                <AppText variant="title">Exercise order</AppText>
                <AppText tone="secondary">
                  Long press the handle and drag to reorder.
                </AppText>
              </View>
            </View>

            {drafts.length > 0 ? (
              <DraggableExerciseList
                drafts={drafts}
                errors={errors.byExercise}
                onAutoScroll={handleAutoScroll}
                onEdit={setSelectedExerciseId}
                onReorder={handleReorder}
                unitSystem={profile.unitSystem}
              />
            ) : (
              <View style={styles.emptyExercises}>
                <AppText variant="label">No exercises added</AppText>
                <AppText tone="secondary">
                  Choose from exercises compatible with your available
                  equipment.
                </AppText>
              </View>
            )}

            {errors.exercises ? (
              <AppText accessibilityRole="alert" style={styles.errorBlock}>
                {errors.exercises}
              </AppText>
            ) : null}

            <Pressable
              accessibilityRole="button"
              accessibilityState={{ disabled: drafts.length >= 20 }}
              disabled={drafts.length >= 20}
              onPress={() => setPickerVisible(true)}
              style={({ pressed }) => [
                styles.addExercise,
                pressed && styles.pressed,
                drafts.length >= 20 && styles.disabled,
              ]}
            >
              <AppText style={styles.addSymbol}>+</AppText>
              <AppText tone="accent" variant="label">
                {drafts.length >= 20
                  ? "Exercise limit reached"
                  : "Add exercise"}
              </AppText>
            </Pressable>
          </View>

          {submissionError ? (
            <AppText
              accessibilityLiveRegion="polite"
              accessibilityRole="alert"
              style={styles.error}
            >
              {submissionError}
            </AppText>
          ) : null}
        </ScrollView>

        <View style={styles.saveBar}>
          <PrimaryButton
            disabled={isSaving}
            label={isSaving ? "Saving workout…" : "Save workout"}
            onPress={handleSave}
          />
        </View>
      </KeyboardAvoidingView>

      <ExercisePicker
        excludedExerciseIds={selectedIds}
        onClose={() => setPickerVisible(false)}
        onSelect={handleExerciseSelected}
        profile={profile}
        visible={pickerVisible}
      />
      <PrescriptionEditor
        draft={selectedDraft ?? null}
        error={
          selectedDraft
            ? errors.byExercise[selectedDraft.exercise.id]
            : undefined
        }
        onClose={() => setSelectedExerciseId(undefined)}
        onRemove={(exerciseId) => {
          setDrafts((current) =>
            current.filter((draft) => draft.exercise.id !== exerciseId),
          );
          setSelectedExerciseId(undefined);
        }}
        onSave={(draft) => {
          setDrafts((current) =>
            current.map((item) =>
              item.exercise.id === draft.exercise.id ? draft : item,
            ),
          );
          setErrors((current) => {
            const nextByExercise = { ...current.byExercise };
            delete nextByExercise[draft.exercise.id];
            return { ...current, byExercise: nextByExercise };
          });
          setSelectedExerciseId(undefined);
        }}
        unitSystem={profile.unitSystem}
      />
    </AppScreen>
  );
}

function SummaryValue({ label, value }: { label: string; value: number }) {
  return (
    <View style={styles.summaryValue}>
      <AppText style={styles.summaryNumber} variant="title">
        {value}
      </AppText>
      <AppText style={styles.summaryLabel} tone="secondary" variant="eyebrow">
        {label}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
  },
  content: {
    width: "100%",
    maxWidth: layout.readableContentWidth,
    alignSelf: "center",
    gap: spacing.xl,
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.xl,
    paddingBottom: spacing.xxl,
  },
  intro: {
    gap: spacing.sm,
  },
  nameField: {
    gap: spacing.sm,
  },
  nameInput: {
    minHeight: 52,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.control,
    paddingHorizontal: spacing.lg,
    color: colors.textPrimary,
    backgroundColor: colors.surface,
    fontSize: 17,
    fontWeight: "600",
  },
  summary: {
    flexDirection: "row",
    borderTopWidth: StyleSheet.hairlineWidth,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
  },
  summaryValue: {
    flex: 1,
    alignItems: "center",
    gap: spacing.xs,
    paddingVertical: spacing.md,
  },
  summaryNumber: {
    fontSize: 21,
  },
  summaryLabel: {
    fontSize: 10,
  },
  sectionHeading: {
    paddingBottom: spacing.md,
  },
  emptyExercises: {
    gap: spacing.sm,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
    paddingVertical: spacing.xl,
  },
  addExercise: {
    minHeight: 58,
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
  },
  addSymbol: {
    width: 30,
    height: 30,
    borderRadius: 9,
    color: colors.onAccent,
    backgroundColor: colors.accent,
    fontSize: 23,
    lineHeight: 29,
    textAlign: "center",
  },
  saveBar: {
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border,
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.md,
    paddingBottom: spacing.lg,
    backgroundColor: colors.canvas,
  },
  error: {
    color: colors.statusDanger,
  },
  errorBlock: {
    paddingTop: spacing.sm,
    color: colors.statusDanger,
  },
  pressed: {
    opacity: 0.7,
  },
  disabled: {
    opacity: 0.48,
  },
});
