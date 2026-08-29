import { useEffect, useState } from "react";
import {
  Modal,
  Pressable,
  ScrollView,
  StyleSheet,
  TextInput,
  View,
} from "react-native";

import { getTrainingProfile } from "../../api/profiles";
import {
  correctWorkoutSession,
  getWorkoutSession,
  WorkoutSessionConflictError,
  type WorkoutSession,
} from "../../api/sessions";
import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { PrimaryButton } from "../../components/PrimaryButton";
import { RouteStatus } from "../../components/RouteStatus";
import { colors, layout, radii, spacing } from "../../theme/tokens";
import {
  sessionFromApi,
  type ActualSetValues,
  type LocalWorkoutSession,
  type SessionExercise,
  type SessionSet,
  updateSet,
} from "../sessions/session-model";
import {
  formatPlan,
  formatSetActual,
  type UnitSystem,
} from "../sessions/session-values";
import { SetEntrySheet } from "../sessions/SetEntrySheet";

type Props = {
  profileId: string;
  sessionId: string;
};

export function WorkoutHistoryDetail({ profileId, sessionId }: Props) {
  const [session, setSession] = useState<WorkoutSession>();
  const [unitSystem, setUnitSystem] = useState<UnitSystem>();
  const [draft, setDraft] = useState<LocalWorkoutSession | null>(null);
  const [selected, setSelected] = useState<{
    exercise: SessionExercise;
    set: SessionSet;
  } | null>(null);
  const [noteTarget, setNoteTarget] = useState<"session" | string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState<string>();
  const [isLoading, setIsLoading] = useState(true);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    Promise.all([
      getWorkoutSession(profileId, sessionId, { signal: controller.signal }),
      getTrainingProfile(profileId, { signal: controller.signal }),
    ])
      .then(([loadedSession, profile]) => {
        if (loadedSession.status !== "completed") {
          setMessage("Only completed workouts are available in history.");
          return;
        }
        setSession(loadedSession);
        setUnitSystem(profile.unitSystem);
      })
      .catch(() => {
        if (!controller.signal.aborted)
          setMessage("The workout record could not be loaded.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setIsLoading(false);
      });
    return () => controller.abort();
  }, [profileId, reloadKey, sessionId]);

  if (isLoading) {
    return (
      <RouteStatus
        busy
        message="Loading the recorded sets and notes."
        title="Preparing workout record"
      />
    );
  }
  if (!session || !unitSystem) {
    return (
      <RouteStatus
        actionLabel="Try again"
        message={message ?? "The workout record is unavailable."}
        onAction={() => {
          setMessage(undefined);
          setIsLoading(true);
          setReloadKey((value) => value + 1);
        }}
        title="Record unavailable"
      />
    );
  }

  const displayed = draft ?? sessionFromApi(session);
  const completedSets = displayed.exercises
    .flatMap((exercise) => exercise.sets)
    .filter((set) => set.isCompleted).length;
  const totalSets = displayed.exercises.flatMap(
    (exercise) => exercise.sets,
  ).length;
  const skippedExercises = displayed.exercises.filter(
    (exercise) => exercise.isSkipped,
  ).length;
  const noteValue =
    noteTarget === "session"
      ? displayed.notes
      : displayed.exercises.find((item) => item.exerciseId === noteTarget)
          ?.notes;

  const saveCorrection = async () => {
    if (!draft) return;
    setIsSaving(true);
    setMessage(undefined);
    try {
      const corrected = await correctWorkoutSession(profileId, session.id, {
        expectedRevision: session.revision,
        notes: draft.notes,
        exercises: draft.exercises.map((exercise) => ({
          exerciseId: exercise.exerciseId,
          isSkipped: exercise.isSkipped,
          notes: exercise.notes,
          sets: exercise.sets.map((set) => ({
            setId: set.setId,
            isCompleted: set.isCompleted,
            completedAt: set.completedAt,
            actualRepetitions: set.actualRepetitions,
            actualLoadKilograms: set.actualLoadKilograms,
            actualDurationSeconds: set.actualDurationSeconds,
            actualDistanceMetres: set.actualDistanceMetres,
          })),
        })),
      });
      setSession(corrected);
      setDraft(null);
      setMessage("Correction saved.");
    } catch (error) {
      setMessage(
        error instanceof WorkoutSessionConflictError
          ? "This record changed elsewhere. Reload it before correcting again."
          : "The correction could not be saved. Check your connection and try again.",
      );
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <AppScreen>
      <ScrollView
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.header}>
          <AppText tone="accent" variant="eyebrow">
            {draft ? "Correction mode" : "Completed workout"}
          </AppText>
          <AppText accessibilityRole="header" variant="display">
            {session.workoutName}
          </AppText>
          <AppText tone="secondary">
            {formatCompletedDate(session.finishedAt)}
            {session.correctedAt ? " · Corrected" : ""}
          </AppText>
        </View>

        <View style={styles.metrics}>
          <Metric
            label="Completed sets"
            value={`${completedSets}/${totalSets}`}
          />
          <Metric
            label="Duration"
            value={formatDuration(session.startedAt, session.finishedAt)}
          />
          <Metric label="Skipped exercises" value={String(skippedExercises)} />
        </View>

        {message ? (
          <AppText accessibilityLiveRegion="polite" tone="secondary">
            {message}
          </AppText>
        ) : null}

        <View style={styles.noteBlock}>
          <View style={styles.titleRow}>
            <AppText variant="label">Session note</AppText>
            {draft ? (
              <TextButton
                label="Edit"
                onPress={() => setNoteTarget("session")}
              />
            ) : null}
          </View>
          <AppText tone="secondary">
            {displayed.notes || "No session note recorded."}
          </AppText>
        </View>

        <View>
          {displayed.exercises.map((exercise) => (
            <View
              key={exercise.exerciseId}
              style={[styles.exercise, exercise.isSkipped && styles.skipped]}
            >
              <View style={styles.titleRow}>
                <View style={styles.exerciseCopy}>
                  <AppText style={styles.exerciseName} variant="title">
                    {exercise.exerciseName}
                  </AppText>
                  <AppText tone="secondary">
                    Plan · {formatPlan(exercise, unitSystem)}
                  </AppText>
                </View>
                {draft ? (
                  <TextButton
                    label={exercise.isSkipped ? "Resume" : "Mark skipped"}
                    onPress={() =>
                      setDraft((current) =>
                        current
                          ? {
                              ...current,
                              exercises: current.exercises.map((item) =>
                                item.exerciseId === exercise.exerciseId
                                  ? { ...item, isSkipped: !item.isSkipped }
                                  : item,
                              ),
                            }
                          : current,
                      )
                    }
                  />
                ) : null}
              </View>
              {exercise.sets.map((set) => (
                <Pressable
                  key={set.setId}
                  accessibilityRole={draft ? "button" : undefined}
                  disabled={!draft}
                  onPress={() => setSelected({ exercise, set })}
                  style={({ pressed }) => [
                    styles.setRow,
                    pressed && styles.pressed,
                  ]}
                >
                  <View
                    style={[
                      styles.setNumber,
                      set.isCompleted && styles.setNumberComplete,
                    ]}
                  >
                    <AppText
                      style={
                        set.isCompleted ? styles.completeNumber : undefined
                      }
                      variant="label"
                    >
                      {set.isCompleted ? "✓" : set.position + 1}
                    </AppText>
                  </View>
                  <AppText tone={set.isCompleted ? "primary" : "secondary"}>
                    {set.isCompleted
                      ? formatSetActual(exercise, set, unitSystem)
                      : "Not completed"}
                  </AppText>
                </Pressable>
              ))}
              <View style={styles.exerciseNote}>
                <AppText tone="secondary">
                  {exercise.notes
                    ? `Note · ${exercise.notes}`
                    : "No exercise note recorded."}
                </AppText>
                {draft ? (
                  <TextButton
                    label={exercise.notes ? "Edit note" : "Add note"}
                    onPress={() => setNoteTarget(exercise.exerciseId)}
                  />
                ) : null}
              </View>
            </View>
          ))}
        </View>

        {draft ? (
          <View style={styles.actions}>
            <PrimaryButton
              disabled={isSaving}
              label={isSaving ? "Saving correction…" : "Save correction"}
              onPress={() => void saveCorrection()}
            />
            <TextButton
              label="Cancel correction"
              onPress={() => setDraft(null)}
            />
          </View>
        ) : (
          <PrimaryButton
            label="Correct this record"
            onPress={() => {
              setMessage(undefined);
              setDraft(sessionFromApi(session));
            }}
          />
        )}
      </ScrollView>

      <SetEntrySheet
        exercise={selected?.exercise ?? null}
        purpose="correction"
        set={selected?.set ?? null}
        unitSystem={unitSystem}
        onClose={() => setSelected(null)}
        onSave={(values: ActualSetValues, complete) => {
          if (!selected) return;
          setDraft((current) =>
            current
              ? updateSet(
                  current,
                  selected.exercise.exerciseId,
                  selected.set.setId,
                  values,
                  complete
                    ? (selected.set.completedAt ?? new Date().toISOString())
                    : null,
                )
              : current,
          );
          setSelected(null);
        }}
      />
      <CorrectionNoteSheet
        key={`${noteTarget ?? "closed"}:${noteValue ?? ""}`}
        isSession={noteTarget === "session"}
        value={noteValue ?? ""}
        visible={noteTarget !== null}
        onClose={() => setNoteTarget(null)}
        onSave={(value) => {
          setDraft((current) => {
            if (!current) return current;
            if (noteTarget === "session")
              return { ...current, notes: value || null };
            return {
              ...current,
              exercises: current.exercises.map((exercise) =>
                exercise.exerciseId === noteTarget
                  ? { ...exercise, notes: value || null }
                  : exercise,
              ),
            };
          });
          setNoteTarget(null);
        }}
      />
    </AppScreen>
  );
}

function CorrectionNoteSheet({
  isSession,
  value,
  visible,
  onClose,
  onSave,
}: {
  isSession: boolean;
  value: string;
  visible: boolean;
  onClose: () => void;
  onSave: (value: string) => void;
}) {
  const title = isSession ? "Session note" : "Exercise note";
  const [draft, setDraft] = useState(value);
  return (
    <Modal
      animationType="slide"
      onRequestClose={onClose}
      presentationStyle="pageSheet"
      visible={visible}
    >
      <View style={styles.noteScreen}>
        <View style={styles.noteHeader}>
          <TextButton label="Cancel" onPress={onClose} />
          <AppText variant="label">{title}</AppText>
          <TextButton label="Save" onPress={() => onSave(draft)} />
        </View>
        <TextInput
          accessibilityLabel={title}
          autoFocus
          maxLength={isSession ? 2_000 : 1_000}
          multiline
          onChangeText={setDraft}
          placeholder="Optional notes"
          placeholderTextColor={colors.textSecondary}
          style={styles.noteInput}
          textAlignVertical="top"
          value={draft}
        />
      </View>
    </Modal>
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

function TextButton({
  label,
  onPress,
}: {
  label: string;
  onPress: () => void;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      style={styles.textButton}
    >
      <AppText tone="accent" variant="label">
        {label}
      </AppText>
    </Pressable>
  );
}

function formatCompletedDate(value: string | null) {
  if (!value) return "Completion time unavailable";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Completion time unavailable";
  return date.toLocaleString(undefined, {
    weekday: "short",
    day: "numeric",
    month: "long",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

function formatDuration(startedAt: string, finishedAt: string | null) {
  const seconds = Math.max(
    0,
    Math.round(
      (Date.parse(finishedAt ?? startedAt) - Date.parse(startedAt)) / 1_000,
    ),
  );
  const minutes = Math.max(1, Math.round(seconds / 60));
  const hours = Math.floor(minutes / 60);
  return hours > 0 ? `${hours}h ${minutes % 60}m` : `${minutes} min`;
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
  noteBlock: { gap: spacing.sm },
  titleRow: {
    flexDirection: "row",
    alignItems: "flex-start",
    justifyContent: "space-between",
    gap: spacing.md,
  },
  exercise: {
    gap: spacing.md,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border,
    paddingVertical: spacing.xl,
  },
  skipped: { opacity: 0.62 },
  exerciseCopy: { flex: 1, gap: spacing.xs },
  exerciseName: { fontSize: 21 },
  setRow: {
    minHeight: 48,
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
  },
  setNumber: {
    width: 34,
    height: 34,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: 17,
    backgroundColor: colors.surfaceRaised,
  },
  setNumberComplete: { backgroundColor: colors.accent },
  completeNumber: { color: colors.onAccent },
  exerciseNote: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: spacing.md,
  },
  actions: { gap: spacing.sm },
  textButton: {
    minHeight: 44,
    justifyContent: "center",
    paddingHorizontal: spacing.sm,
  },
  pressed: { opacity: 0.7 },
  noteScreen: { flex: 1, backgroundColor: colors.canvas },
  noteHeader: {
    minHeight: 56,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
    paddingHorizontal: spacing.sm,
  },
  noteInput: {
    flex: 1,
    margin: spacing.lg,
    padding: spacing.lg,
    borderRadius: radii.control,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: 17,
  },
});
