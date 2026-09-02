import { randomUUID } from "expo-crypto";
import { memo, useEffect, useState } from "react";
import {
  Alert,
  FlatList,
  Modal,
  Pressable,
  StyleSheet,
  TextInput,
  View,
} from "react-native";

import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { PrimaryButton } from "../../components/PrimaryButton";
import { RouteStatus } from "../../components/RouteStatus";
import { colors, layout, radii, spacing } from "../../theme/tokens";
import {
  addSet,
  removeSet,
  type ActualSetValues,
  type LocalWorkoutSession,
  type SessionExercise,
  type SessionSet,
  updateSet,
} from "./session-model";
import { formatPlan, formatSetActual, type UnitSystem } from "./session-values";
import { SetEntrySheet } from "./SetEntrySheet";
import { useWorkoutSession } from "./useWorkoutSession";

type Props = {
  profileId: string;
  workoutPlanId?: string;
  unitSystem: UnitSystem;
  onFinished: () => void;
  onExit: () => void;
};

export function ActiveWorkout({
  profileId,
  workoutPlanId,
  unitSystem,
  onFinished,
  onExit,
}: Props) {
  const {
    session,
    loadState,
    message,
    mutate,
    setRestTimer,
    retry,
    reloadServerVersion,
    discard,
  } = useWorkoutSession(profileId, workoutPlanId);
  const [selected, setSelected] = useState<{
    exercise: SessionExercise;
    set: SessionSet;
  } | null>(null);
  const [noteTarget, setNoteTarget] = useState<"session" | string | null>(null);

  if (loadState === "loading") {
    return (
      <RouteStatus
        busy
        title="Starting workout"
        message="Preparing a recoverable session copy."
      />
    );
  }
  if (loadState === "error" || !session) {
    return (
      <RouteStatus
        actionLabel="Return to workouts"
        message={message ?? "The active workout is unavailable."}
        onAction={onExit}
        title="Workout unavailable"
      />
    );
  }

  const performDiscard = () => {
    Alert.alert(
      "Discard this workout?",
      "This permanently removes the active session and everything logged in it.",
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Discard",
          style: "destructive",
          onPress: () => {
            void discard()
              .then(onExit)
              .catch(() =>
                Alert.alert(
                  "Couldn’t discard workout",
                  "Connect to the API and try again.",
                ),
              );
          },
        },
      ],
    );
  };

  const finish = () => {
    const unfinished = session.exercises.reduce(
      (count, exercise) =>
        count +
        (exercise.isSkipped
          ? 0
          : exercise.sets.filter((set) => !set.isCompleted).length),
      0,
    );
    const completed = session.exercises
      .flatMap((exercise) => exercise.sets)
      .filter((set) => set.isCompleted).length;
    if (completed === 0) {
      Alert.alert(
        "No completed sets",
        "Complete at least one set, or discard this session.",
      );
      return;
    }

    Alert.alert(
      unfinished > 0 ? "End workout early?" : "Finish workout?",
      unfinished > 0
        ? `${unfinished} ${unfinished === 1 ? "set is" : "sets are"} unfinished. Completed work will be preserved.`
        : "You can review the completed session next.",
      [
        { text: "Keep training", style: "cancel" },
        {
          text: unfinished > 0 ? "End early" : "Finish",
          onPress: () => {
            const finishedAt = new Date(
              Math.max(Date.now(), Date.parse(session.startedAt)),
            ).toISOString();
            void mutate((current) => ({
              ...current,
              status: "completed",
              finishedAt,
            }))?.then(onFinished);
          },
        },
      ],
    );
  };

  const noteValue =
    noteTarget === "session"
      ? session.notes
      : session.exercises.find((item) => item.exerciseId === noteTarget)?.notes;

  return (
    <AppScreen>
      <FlatList
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        data={session.exercises}
        keyExtractor={(exercise) => exercise.exerciseId}
        ListHeaderComponent={
          <View style={styles.header}>
            <View style={styles.headerTitleRow}>
              <View style={styles.headerCopy}>
                <AppText tone="accent" variant="eyebrow">
                  Active workout
                </AppText>
                <AppText accessibilityRole="header" variant="title">
                  {session.workoutName}
                </AppText>
                <ElapsedTime startedAt={session.startedAt} />
              </View>
              <Pressable
                accessibilityRole="button"
                onPress={() => setNoteTarget("session")}
                style={styles.textButton}
              >
                <AppText tone="accent" variant="label">
                  Session note
                </AppText>
              </Pressable>
            </View>
            <SyncStatus
              message={message}
              session={session}
              onReload={() => {
                Alert.alert(
                  "Use the server version?",
                  "This replaces the unsynchronized copy currently on this device.",
                  [
                    { text: "Keep device copy", style: "cancel" },
                    {
                      text: "Use server version",
                      style: "destructive",
                      onPress: () => {
                        void reloadServerVersion()
                          .then((serverSession) => {
                            if (serverSession.status === "completed")
                              onFinished();
                          })
                          .catch(() =>
                            Alert.alert(
                              "Couldn’t load server version",
                              "Connect to the API and try again.",
                            ),
                          );
                      },
                    },
                  ],
                );
              }}
              onRetry={retry}
            />
            {session.restTimerEndsAt ? (
              <RestTimer
                endsAt={session.restTimerEndsAt}
                onAdd={() => {
                  const remaining = Math.max(
                    Date.parse(session.restTimerEndsAt ?? ""),
                    Date.now(),
                  );
                  setRestTimer(new Date(remaining + 30_000).toISOString());
                }}
                onDismiss={() => setRestTimer(null)}
              />
            ) : null}
          </View>
        }
        ListFooterComponent={
          <View style={styles.footer}>
            <PrimaryButton label="Finish workout" onPress={finish} />
            <Pressable
              accessibilityRole="button"
              onPress={performDiscard}
              style={styles.discardButton}
            >
              <AppText style={styles.dangerText} variant="label">
                Discard workout
              </AppText>
            </Pressable>
          </View>
        }
        renderItem={({ item }) => (
          <ExerciseLog
            exercise={item}
            unitSystem={unitSystem}
            onAddSet={() =>
              void mutate((current) =>
                addSet(current, item.exerciseId, randomUUID()),
              )
            }
            onEditNote={() => setNoteTarget(item.exerciseId)}
            onRemoveSet={(set) => {
              const remove = () =>
                void mutate((current) =>
                  removeSet(current, item.exerciseId, set.setId),
                );
              if (set.isCompleted) {
                Alert.alert(
                  "Remove completed set?",
                  "This removes its logged result.",
                  [
                    { text: "Cancel", style: "cancel" },
                    { text: "Remove", style: "destructive", onPress: remove },
                  ],
                );
              } else remove();
            }}
            onSelectSet={(set) => setSelected({ exercise: item, set })}
            onToggleSkip={() =>
              void mutate((current) => ({
                ...current,
                exercises: current.exercises.map((exercise) =>
                  exercise.exerciseId === item.exerciseId
                    ? { ...exercise, isSkipped: !exercise.isSkipped }
                    : exercise,
                ),
              }))
            }
          />
        )}
        showsVerticalScrollIndicator={false}
      />
      <SetEntrySheet
        exercise={selected?.exercise ?? null}
        set={selected?.set ?? null}
        unitSystem={unitSystem}
        onClose={() => setSelected(null)}
        onSave={(values: ActualSetValues, complete, startRest) => {
          if (!selected) return;
          void mutate((current) =>
            updateSet(
              current,
              selected.exercise.exerciseId,
              selected.set.setId,
              values,
              complete ? new Date().toISOString() : null,
            ),
          );
          if (startRest)
            setRestTimer(new Date(Date.now() + 90_000).toISOString());
          setSelected(null);
        }}
      />
      <NoteSheet
        key={`${noteTarget ?? "closed"}:${noteValue ?? ""}`}
        title={noteTarget === "session" ? "Session note" : "Exercise note"}
        value={noteValue ?? ""}
        visible={noteTarget !== null}
        onClose={() => setNoteTarget(null)}
        onSave={(value) => {
          void mutate((current) =>
            noteTarget === "session"
              ? { ...current, notes: value || null }
              : {
                  ...current,
                  exercises: current.exercises.map((exercise) =>
                    exercise.exerciseId === noteTarget
                      ? { ...exercise, notes: value || null }
                      : exercise,
                  ),
                },
          );
          setNoteTarget(null);
        }}
      />
    </AppScreen>
  );
}

const ExerciseLog = memo(function ExerciseLog({
  exercise,
  unitSystem,
  onAddSet,
  onEditNote,
  onRemoveSet,
  onSelectSet,
  onToggleSkip,
}: {
  exercise: SessionExercise;
  unitSystem: UnitSystem;
  onAddSet: () => void;
  onEditNote: () => void;
  onRemoveSet: (set: SessionSet) => void;
  onSelectSet: (set: SessionSet) => void;
  onToggleSkip: () => void;
}) {
  return (
    <View style={[styles.exercise, exercise.isSkipped && styles.skipped]}>
      <View style={styles.exerciseHeader}>
        <View style={styles.exerciseCopy}>
          <AppText variant="title" style={styles.exerciseName}>
            {exercise.exerciseName}
          </AppText>
          <AppText tone="secondary">
            Plan · {formatPlan(exercise, unitSystem)}
          </AppText>
        </View>
        <Pressable
          accessibilityRole="button"
          onPress={onToggleSkip}
          style={styles.textButton}
        >
          <AppText
            tone={exercise.isSkipped ? "accent" : "secondary"}
            variant="label"
          >
            {exercise.isSkipped ? "Resume" : "Skip"}
          </AppText>
        </Pressable>
      </View>
      {exercise.notes ? (
        <AppText tone="secondary">Note · {exercise.notes}</AppText>
      ) : null}
      <View style={styles.sets}>
        {exercise.sets.map((set) => (
          <View key={set.setId} style={styles.setRow}>
            <Pressable
              accessibilityHint={
                set.isCompleted ? "Edit logged values" : "Enter actual values"
              }
              accessibilityLabel={`Set ${set.position + 1}, ${formatSetActual(exercise, set, unitSystem)}`}
              accessibilityRole="button"
              onPress={() => onSelectSet(set)}
              style={({ pressed }) => [
                styles.setMain,
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
                  variant="label"
                  style={set.isCompleted ? styles.completeNumber : undefined}
                >
                  {set.isCompleted ? "✓" : set.position + 1}
                </AppText>
              </View>
              <AppText
                style={styles.setValue}
                tone={set.isCompleted ? "primary" : "secondary"}
              >
                {formatSetActual(exercise, set, unitSystem)}
              </AppText>
            </Pressable>
            <Pressable
              accessibilityLabel={`Remove set ${set.position + 1}`}
              accessibilityRole="button"
              hitSlop={8}
              onPress={() => onRemoveSet(set)}
              style={styles.removeSet}
            >
              <AppText tone="secondary" variant="label">
                Remove
              </AppText>
            </Pressable>
          </View>
        ))}
      </View>
      <View style={styles.exerciseActions}>
        <Pressable
          accessibilityRole="button"
          disabled={exercise.sets.length >= 20}
          onPress={onAddSet}
          style={styles.smallAction}
        >
          <AppText tone="accent" variant="label">
            + Add set
          </AppText>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          onPress={onEditNote}
          style={styles.smallAction}
        >
          <AppText tone="secondary" variant="label">
            {exercise.notes ? "Edit note" : "Add note"}
          </AppText>
        </Pressable>
      </View>
    </View>
  );
});

function ElapsedTime({ startedAt }: { startedAt: string }) {
  const [elapsed, setElapsed] = useState(0);
  useEffect(() => {
    const update = () =>
      setElapsed(Math.max(0, Date.now() - Date.parse(startedAt)));
    const initialTimer = setTimeout(update, 0);
    const timer = setInterval(update, 1_000);
    return () => {
      clearTimeout(initialTimer);
      clearInterval(timer);
    };
  }, [startedAt]);
  return <AppText tone="secondary">{formatDuration(elapsed)} elapsed</AppText>;
}

function RestTimer({
  endsAt,
  onAdd,
  onDismiss,
}: {
  endsAt: string;
  onAdd: () => void;
  onDismiss: () => void;
}) {
  const [seconds, setSeconds] = useState(0);
  useEffect(() => {
    const update = () =>
      setSeconds(
        Math.max(0, Math.ceil((Date.parse(endsAt) - Date.now()) / 1_000)),
      );
    const initialTimer = setTimeout(update, 0);
    const timer = setInterval(update, 250);
    return () => {
      clearTimeout(initialTimer);
      clearInterval(timer);
    };
  }, [endsAt]);
  return (
    <View accessibilityLiveRegion="polite" style={styles.restTimer}>
      <View>
        <AppText variant="label">Rest · {seconds}s</AppText>
        <AppText tone="secondary" style={styles.smallCopy}>
          In-app timer only
        </AppText>
      </View>
      <View style={styles.inlineActions}>
        <Pressable
          accessibilityRole="button"
          onPress={onAdd}
          style={styles.textButton}
        >
          <AppText tone="accent" variant="label">
            +30s
          </AppText>
        </Pressable>
        <Pressable
          accessibilityRole="button"
          onPress={onDismiss}
          style={styles.textButton}
        >
          <AppText tone="secondary" variant="label">
            Dismiss
          </AppText>
        </Pressable>
      </View>
    </View>
  );
}

function SyncStatus({
  session,
  message,
  onRetry,
  onReload,
}: {
  session: LocalWorkoutSession;
  message?: string;
  onRetry: () => void;
  onReload: () => void;
}) {
  if (session.syncState === "synced" && !message) return null;
  return (
    <View style={styles.syncStatus}>
      <AppText tone="secondary" style={styles.syncCopy}>
        {message ??
          (session.syncState === "syncing"
            ? "Synchronizing…"
            : "Saved on this device")}
      </AppText>
      {session.syncState === "pending" ? (
        <Pressable accessibilityRole="button" onPress={onRetry}>
          <AppText tone="accent" variant="label">
            Retry
          </AppText>
        </Pressable>
      ) : null}
      {session.syncState === "conflict" ? (
        <Pressable accessibilityRole="button" onPress={onReload}>
          <AppText tone="accent" variant="label">
            Review server copy
          </AppText>
        </Pressable>
      ) : null}
    </View>
  );
}

function NoteSheet({
  title,
  value,
  visible,
  onClose,
  onSave,
}: {
  title: string;
  value: string;
  visible: boolean;
  onClose: () => void;
  onSave: (value: string) => void;
}) {
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
          <Pressable accessibilityRole="button" onPress={onClose}>
            <AppText tone="secondary" variant="label">
              Cancel
            </AppText>
          </Pressable>
          <AppText variant="label">{title}</AppText>
          <Pressable accessibilityRole="button" onPress={() => onSave(draft)}>
            <AppText tone="accent" variant="label">
              Save
            </AppText>
          </Pressable>
        </View>
        <TextInput
          accessibilityLabel={title}
          autoFocus
          maxLength={title === "Session note" ? 2_000 : 1_000}
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

function formatDuration(milliseconds: number) {
  const seconds = Math.floor(milliseconds / 1_000);
  const hours = Math.floor(seconds / 3_600);
  const minutes = Math.floor((seconds % 3_600) / 60);
  const remainder = seconds % 60;
  return hours > 0
    ? `${hours}:${String(minutes).padStart(2, "0")}:${String(remainder).padStart(2, "0")}`
    : `${minutes}:${String(remainder).padStart(2, "0")}`;
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
  header: { gap: spacing.lg, paddingBottom: spacing.xl },
  headerTitleRow: {
    flexDirection: "row",
    gap: spacing.md,
    alignItems: "flex-start",
  },
  headerCopy: { flex: 1, gap: spacing.xs },
  textButton: {
    minHeight: 44,
    justifyContent: "center",
    paddingHorizontal: spacing.sm,
  },
  syncStatus: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    padding: spacing.md,
    borderRadius: radii.control,
    backgroundColor: colors.surface,
  },
  syncCopy: { flex: 1, fontSize: 14 },
  restTimer: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    padding: spacing.md,
    borderRadius: radii.control,
    backgroundColor: colors.surfaceRaised,
  },
  smallCopy: { fontSize: 13 },
  inlineActions: { flexDirection: "row" },
  exercise: {
    gap: spacing.md,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border,
    paddingVertical: spacing.xl,
  },
  skipped: { opacity: 0.62 },
  exerciseHeader: {
    flexDirection: "row",
    alignItems: "flex-start",
    gap: spacing.md,
  },
  exerciseCopy: { flex: 1, gap: spacing.xs },
  exerciseName: { fontSize: 21 },
  sets: { gap: spacing.xs },
  setRow: { minHeight: 54, flexDirection: "row", alignItems: "center" },
  setMain: {
    flex: 1,
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    minHeight: 52,
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
  setValue: { flex: 1 },
  removeSet: {
    minHeight: 44,
    justifyContent: "center",
    paddingLeft: spacing.md,
  },
  pressed: { opacity: 0.7 },
  exerciseActions: { flexDirection: "row", gap: spacing.md },
  smallAction: {
    minHeight: 44,
    justifyContent: "center",
    paddingRight: spacing.sm,
  },
  footer: { gap: spacing.md, paddingTop: spacing.xl },
  discardButton: {
    minHeight: 48,
    alignItems: "center",
    justifyContent: "center",
  },
  dangerText: { color: colors.statusDanger },
  noteScreen: { flex: 1, backgroundColor: colors.canvas },
  noteHeader: {
    minHeight: 56,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: spacing.lg,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
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
