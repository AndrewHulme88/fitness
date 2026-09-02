import { useEffect, useState } from "react";
import {
  Alert,
  Pressable,
  ScrollView,
  StyleSheet,
  TextInput,
  View,
} from "react-native";

import {
  deleteCoachConversation,
  confirmCoachWorkoutProposal,
  getCoachConversation,
  sendCoachMessage,
  type CoachConversation as CoachConversationDocument,
} from "../../api/coach";
import {
  getWorkout,
  listWorkouts,
  type WorkoutDetail,
  type WorkoutSummary,
} from "../../api/workouts";
import { getProgressOverview, type ProgressOverview } from "../../api/progress";
import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { PrimaryButton } from "../../components/PrimaryButton";
import { RouteStatus } from "../../components/RouteStatus";
import { colors, layout, radii, spacing } from "../../theme/tokens";

const emptyProposals: NonNullable<CoachConversationDocument["proposals"]> = [];

export function CoachConversation({
  initialWorkoutId,
  profileId,
}: {
  initialWorkoutId?: string;
  profileId: string;
}) {
  const [conversation, setConversation] = useState<CoachConversationDocument>();
  const [question, setQuestion] = useState("");
  const [workouts, setWorkouts] = useState<WorkoutSummary[]>([]);
  const [selectedWorkoutId, setSelectedWorkoutId] = useState<
    string | undefined
  >(initialWorkoutId);
  const [workoutLoading, setWorkoutLoading] = useState(true);
  const [workoutError, setWorkoutError] = useState(false);
  const [progress, setProgress] = useState<ProgressOverview>();
  const [progressExerciseId, setProgressExerciseId] = useState<string>();
  const [progressPeriodDays, setProgressPeriodDays] = useState<7 | 28>();
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [confirmingProposalId, setConfirmingProposalId] = useState<string>();
  const [proposalWorkouts, setProposalWorkouts] = useState<
    Record<string, WorkoutDetail>
  >({});
  const [error, setError] = useState<string>();
  const proposals = conversation?.proposals ?? emptyProposals;

  useEffect(() => {
    const controller = new AbortController();
    getCoachConversation(profileId, { signal: controller.signal })
      .then(setConversation)
      .catch(
        () =>
          !controller.signal.aborted &&
          setError("Your coach conversation could not be loaded."),
      )
      .finally(() => !controller.signal.aborted && setLoading(false));
    return () => controller.abort();
  }, [profileId]);

  useEffect(() => {
    const controller = new AbortController();
    listWorkouts(
      profileId,
      { limit: 50, offset: 0 },
      { signal: controller.signal },
    )
      .then((result) => setWorkouts(result.items))
      .catch(() => !controller.signal.aborted && setWorkoutError(true))
      .finally(() => !controller.signal.aborted && setWorkoutLoading(false));
    return () => controller.abort();
  }, [profileId]);

  useEffect(() => {
    const controller = new AbortController();
    getProgressOverview(profileId, { signal: controller.signal })
      .then(setProgress)
      .catch(() => undefined);
    return () => controller.abort();
  }, [profileId]);

  useEffect(() => {
    if (proposals.length === 0) return;
    let active = true;
    void Promise.all(
      proposals.map(
        async (proposal) =>
          [
            proposal.workoutId,
            await getWorkout(profileId, proposal.workoutId),
          ] as const,
      ),
    ).then((entries) => {
      if (active) setProposalWorkouts(Object.fromEntries(entries));
    });
    return () => {
      active = false;
    };
  }, [profileId, proposals]);

  if (loading)
    return (
      <RouteStatus
        busy
        message="Loading your saved coaching conversation."
        title="Preparing coach"
      />
    );
  if (error)
    return (
      <RouteStatus
        actionLabel="Try again"
        message={error}
        onAction={() => setError(undefined)}
        title="Coach unavailable"
      />
    );

  const send = async () => {
    const value = question.trim();
    if (!value || sending) return;
    setSending(true);
    try {
      setConversation(
        await sendCoachMessage(
          profileId,
          value,
          {},
          selectedWorkoutId,
          progressExerciseId,
          progressPeriodDays,
        ),
      );
      setQuestion("");
    } catch {
      setError(
        "The coach is unavailable right now. Your workouts are still available.",
      );
    } finally {
      setSending(false);
    }
  };

  const clear = () => {
    if (sending) return;

    Alert.alert(
      "Delete conversation?",
      "This permanently deletes the coach messages saved to your account.",
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Delete",
          style: "destructive",
          onPress: () => {
            if (sending) return;
            void deleteCoachConversation(profileId)
              .then(() => setConversation(undefined))
              .catch(() => setError("The conversation could not be deleted."));
          },
        },
      ],
    );
  };

  const confirmProposal = async (proposalId: string) => {
    if (sending || confirmingProposalId) return;
    setConfirmingProposalId(proposalId);
    try {
      await confirmCoachWorkoutProposal(profileId, proposalId);
      setConversation((current) =>
        current
          ? {
              ...current,
              proposals: (current.proposals ?? []).filter(
                (proposal) => proposal.id !== proposalId,
              ),
            }
          : current,
      );
    } catch {
      setError(
        "The proposal could not be applied. Review your current workout and try again.",
      );
    } finally {
      setConfirmingProposalId(undefined);
    }
  };

  return (
    <AppScreen>
      <ScrollView
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        keyboardShouldPersistTaps="handled"
      >
        <View style={styles.intro}>
          <AppText tone="accent" variant="eyebrow">
            AI coach
          </AppText>
          <AppText accessibilityRole="header" variant="display">
            Training questions, not medical advice
          </AppText>
          <AppText tone="secondary">
            The coach uses selected training details you can review below. It
            cannot diagnose injuries or change your plans.
          </AppText>
        </View>
        <View style={styles.messages}>
          {conversation?.messages.map((message) => (
            <View
              key={message.id}
              style={[
                styles.message,
                message.role === "coach" && styles.coachMessage,
              ]}
            >
              <AppText variant="label">
                {message.role === "coach" ? "AI coach" : "You"}
              </AppText>
              <AppText
                tone={message.role === "coach" ? "primary" : "secondary"}
              >
                {message.content}
              </AppText>
              {message.contextSources.length > 0 ? (
                <AppText tone="secondary" style={styles.basis}>
                  Based on: {message.contextSources.join(", ")}
                </AppText>
              ) : null}
            </View>
          ))}
          {!conversation ? (
            <AppText tone="secondary">
              Ask about training terms, your workouts, or your recorded
              progress.
            </AppText>
          ) : null}
        </View>
        {proposals.map((proposal) => {
          const currentWorkout = proposalWorkouts[proposal.workoutId];
          const isConfirming = confirmingProposalId === proposal.id;
          return (
            <View key={proposal.id} style={styles.proposal}>
              <AppText variant="label">Proposed workout change</AppText>
              <AppText>{proposal.rationale}</AppText>
              <AppText tone="secondary">
                Current: {currentWorkout?.name ?? "Loading workout"} · revision{" "}
                {proposal.expectedRevision}
              </AppText>
              <AppText tone="secondary">
                Proposed: {proposal.name} · {proposal.exercises.length}{" "}
                exercises,{" "}
                {proposal.exercises.reduce(
                  (total, exercise) => total + Number(exercise.plannedSets),
                  0,
                )}{" "}
                sets
              </AppText>
              <View
                accessibilityLabel="Exercise-level proposal changes"
                style={styles.changes}
              >
                <AppText variant="label">Exercise-level changes</AppText>
                {proposal.changes.length === 0 ? (
                  <AppText tone="secondary">
                    No exercise changes were proposed.
                  </AppText>
                ) : (
                  proposal.changes.map((change, index) => (
                    <AppText key={`${change.kind}-${index}`} tone="secondary">
                      {formatChange(change)}
                    </AppText>
                  ))
                )}
              </View>
              <PrimaryButton
                disabled={sending || Boolean(confirmingProposalId)}
                label={
                  isConfirming ? "Applying change…" : "Apply proposed change"
                }
                onPress={() => void confirmProposal(proposal.id)}
              />
              <AppText tone="secondary" style={styles.proposalNote}>
                Applying this updates your workout. You can still edit it
                afterwards.
              </AppText>
            </View>
          );
        })}
        <View style={styles.reviewPicker}>
          <AppText variant="label">Review one workout</AppText>
          <AppText tone="secondary">
            Choose the only workout the coach may review or propose changes to.
          </AppText>
          {workoutLoading ? (
            <AppText tone="secondary">Loading workouts…</AppText>
          ) : null}
          {workoutError ? (
            <AppText tone="secondary">
              Workout selection is unavailable. Return to Plans and try again.
            </AppText>
          ) : null}
          {!workoutLoading && !workoutError && workouts.length === 0 ? (
            <AppText tone="secondary">
              Create a workout in Plans before reviewing it.
            </AppText>
          ) : null}
          {workouts.map((workout) => {
            const selected = workout.id === selectedWorkoutId;
            return (
              <Pressable
                accessibilityRole="radio"
                accessibilityState={{ selected }}
                key={workout.id}
                onPress={() => {
                  setSelectedWorkoutId(workout.id);
                  setProgressExerciseId(undefined);
                  setProgressPeriodDays(undefined);
                }}
                style={[
                  styles.workoutChoice,
                  selected && styles.workoutChoiceSelected,
                ]}
              >
                <AppText tone={selected ? "accent" : "primary"} variant="label">
                  {workout.name}
                </AppText>
                <AppText tone="secondary">Revision {workout.revision}</AppText>
              </Pressable>
            );
          })}
        </View>
        <View style={styles.reviewPicker}>
          <AppText variant="label">Review recorded progress</AppText>
          <AppText tone="secondary">
            Select one factual source. The coach will not infer records, scores,
            or readiness.
          </AppText>
          <View style={styles.progressPeriods}>
            {[7, 28].map((days) => {
              const selected = progressPeriodDays === days;
              return (
                <Pressable
                  accessibilityRole="radio"
                  accessibilityState={{ selected }}
                  key={days}
                  onPress={() => {
                    setProgressPeriodDays(days as 7 | 28);
                    setProgressExerciseId(undefined);
                    setSelectedWorkoutId(undefined);
                  }}
                  style={[
                    styles.periodChoice,
                    selected && styles.workoutChoiceSelected,
                  ]}
                >
                  <AppText variant="label">Last {days} days</AppText>
                </Pressable>
              );
            })}
          </View>
          {progress?.recordedExercises.map((exercise) => {
            const selected = progressExerciseId === exercise.exerciseId;
            return (
              <Pressable
                accessibilityRole="radio"
                accessibilityState={{ selected }}
                key={exercise.exerciseId}
                onPress={() => {
                  setProgressExerciseId(exercise.exerciseId);
                  setProgressPeriodDays(undefined);
                  setSelectedWorkoutId(undefined);
                }}
                style={[
                  styles.workoutChoice,
                  selected && styles.workoutChoiceSelected,
                ]}
              >
                <AppText tone={selected ? "accent" : "primary"} variant="label">
                  {exercise.exerciseName}
                </AppText>
                <AppText tone="secondary">
                  Recorded completed-set values
                </AppText>
              </Pressable>
            );
          })}
        </View>
        <View style={styles.composer}>
          <TextInput
            accessibilityLabel="Question for the AI coach"
            editable={!sending}
            maxLength={1000}
            multiline
            onChangeText={setQuestion}
            placeholder={
              selectedWorkoutId
                ? "Ask about the selected workout"
                : "Ask a training question"
            }
            placeholderTextColor={colors.textSecondary}
            style={styles.input}
            value={question}
          />
          <PrimaryButton
            disabled={sending || question.trim().length === 0}
            label={sending ? "Asking coach…" : "Ask coach"}
            onPress={() => void send()}
          />
        </View>
        {conversation ? (
          <Pressable
            accessibilityRole="button"
            disabled={sending}
            onPress={clear}
            style={styles.delete}
          >
            <AppText tone="secondary" variant="label">
              Delete saved conversation
            </AppText>
          </Pressable>
        ) : null}
      </ScrollView>
    </AppScreen>
  );
}

const styles = StyleSheet.create({
  content: {
    width: "100%",
    maxWidth: layout.readableContentWidth,
    alignSelf: "center",
    gap: spacing.xl,
    padding: spacing.lg,
    paddingBottom: spacing.xxxl,
  },
  intro: { gap: spacing.sm },
  messages: { gap: spacing.md },
  message: {
    gap: spacing.xs,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
    paddingTop: spacing.md,
  },
  coachMessage: {
    backgroundColor: colors.surface,
    borderRadius: radii.panel,
    padding: spacing.md,
  },
  proposal: {
    gap: spacing.sm,
    backgroundColor: colors.surface,
    borderRadius: radii.panel,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
  },
  proposalNote: { fontSize: 13 },
  changes: {
    gap: spacing.xs,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
    paddingTop: spacing.sm,
  },
  basis: { fontSize: 13 },
  reviewPicker: { gap: spacing.sm },
  progressPeriods: { flexDirection: "row", gap: spacing.sm },
  periodChoice: {
    minHeight: 44,
    flex: 1,
    borderWidth: 1,
    borderColor: colors.border,
    justifyContent: "center",
    paddingHorizontal: spacing.sm,
  },
  workoutChoice: {
    minHeight: 44,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
    justifyContent: "center",
    paddingVertical: spacing.sm,
  },
  workoutChoiceSelected: {
    borderLeftWidth: 3,
    borderLeftColor: colors.accent,
    paddingLeft: spacing.sm,
  },
  composer: { gap: spacing.md },
  input: {
    minHeight: 108,
    color: colors.textPrimary,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.control,
    padding: spacing.md,
    textAlignVertical: "top",
  },
  delete: { minHeight: 44, alignItems: "center", justifyContent: "center" },
});

function formatChange(
  change: NonNullable<
    CoachConversationDocument["proposals"]
  >[number]["changes"][number],
) {
  const current = change.current?.name;
  const proposed = change.proposed?.name;
  if (change.kind === "substitution")
    return `Substitute ${current} with ${proposed}.`;
  if (change.kind === "addition") return `Add ${proposed}.`;
  if (change.kind === "removal") return `Remove ${current}.`;
  return `Change the prescription for ${current}.`;
}
