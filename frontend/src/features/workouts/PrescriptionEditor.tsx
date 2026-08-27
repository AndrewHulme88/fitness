import { useState } from "react";
import {
  KeyboardAvoidingView,
  Modal,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  TextInput,
  View,
} from "react-native";

import { AppText } from "../../components/AppText";
import { PrimaryButton } from "../../components/PrimaryButton";
import { colors, layout, radii, spacing } from "../../theme/tokens";
import {
  targetLabels,
  type UnitSystem,
  type WorkoutExerciseDraft,
} from "./workout-draft";

type PrescriptionEditorProps = {
  draft: WorkoutExerciseDraft | null;
  error?: string;
  onClose: () => void;
  onRemove: (exerciseId: string) => void;
  onSave: (draft: WorkoutExerciseDraft) => void;
  unitSystem: UnitSystem;
};

export function PrescriptionEditor({
  draft,
  error,
  onClose,
  onRemove,
  onSave,
  unitSystem,
}: PrescriptionEditorProps) {
  if (!draft) return null;

  return (
    <PrescriptionEditorSheet
      key={draft.exercise.id}
      error={error}
      initialDraft={draft}
      onClose={onClose}
      onRemove={onRemove}
      onSave={onSave}
      unitSystem={unitSystem}
    />
  );
}

function PrescriptionEditorSheet({
  error,
  initialDraft,
  onClose,
  onRemove,
  onSave,
  unitSystem,
}: Omit<PrescriptionEditorProps, "draft"> & {
  initialDraft: WorkoutExerciseDraft;
}) {
  const [workingDraft, setWorkingDraft] = useState(initialDraft);

  const { trackingMode } = workingDraft.exercise;
  const usesRepetitions =
    trackingMode === "repetitions" || trackingMode === "repetitionsAndLoad";
  const usesDuration =
    trackingMode === "duration" ||
    trackingMode === "distanceAndDuration" ||
    trackingMode === "distanceDurationAndLoad";
  const usesDistance =
    trackingMode === "distanceAndDuration" ||
    trackingMode === "distanceDurationAndLoad";
  const usesLoad =
    trackingMode === "repetitionsAndLoad" ||
    trackingMode === "distanceDurationAndLoad";
  const labels = targetLabels(trackingMode, unitSystem);

  const update = (values: Partial<WorkoutExerciseDraft>) =>
    setWorkingDraft((current) =>
      current ? { ...current, ...values } : current,
    );

  return (
    <Modal
      animationType="slide"
      onRequestClose={onClose}
      presentationStyle="pageSheet"
      visible
    >
      <KeyboardAvoidingView
        behavior={Platform.OS === "ios" ? "padding" : undefined}
        style={styles.screen}
      >
        <View style={styles.header}>
          <Pressable
            accessibilityRole="button"
            hitSlop={8}
            onPress={onClose}
            style={styles.headerAction}
          >
            <AppText tone="secondary" variant="label">
              Cancel
            </AppText>
          </Pressable>
          <AppText
            accessibilityRole="header"
            style={styles.headerTitle}
            variant="label"
          >
            Exercise targets
          </AppText>
          <View style={styles.headerAction} />
        </View>

        <ScrollView
          contentContainerStyle={styles.content}
          keyboardShouldPersistTaps="handled"
        >
          <View style={styles.intro}>
            <AppText tone="accent" variant="eyebrow">
              Planned exercise
            </AppText>
            <AppText accessibilityRole="header" variant="title">
              {workingDraft.exercise.name}
            </AppText>
            <AppText tone="secondary">
              Set the target you want to follow. These values are not generated
              coaching recommendations.
            </AppText>
          </View>

          <View style={styles.fields}>
            <NumberField
              label="Planned sets"
              onChangeText={(plannedSets) => update({ plannedSets })}
              value={workingDraft.plannedSets}
            />

            {usesRepetitions ? (
              <View style={styles.fieldRow}>
                <NumberField
                  label="Minimum reps"
                  onChangeText={(minimumRepetitions) =>
                    update({ minimumRepetitions })
                  }
                  value={workingDraft.minimumRepetitions}
                />
                <NumberField
                  label="Maximum reps"
                  onChangeText={(maximumRepetitions) =>
                    update({ maximumRepetitions })
                  }
                  value={workingDraft.maximumRepetitions}
                />
              </View>
            ) : null}

            {usesLoad ? (
              <NumberField
                decimal
                label={labels.load}
                onChangeText={(targetLoad) => update({ targetLoad })}
                optional
                value={workingDraft.targetLoad}
              />
            ) : null}

            {usesDuration ? (
              <NumberField
                decimal={trackingMode !== "duration"}
                label={labels.duration}
                onChangeText={(targetDuration) => update({ targetDuration })}
                optional={trackingMode !== "duration"}
                value={workingDraft.targetDuration}
              />
            ) : null}

            {usesDistance ? (
              <NumberField
                decimal
                label={labels.distance}
                onChangeText={(targetDistance) => update({ targetDistance })}
                optional
                value={workingDraft.targetDistance}
              />
            ) : null}
          </View>

          {error ? (
            <AppText
              accessibilityLiveRegion="polite"
              accessibilityRole="alert"
              style={styles.error}
            >
              {error}
            </AppText>
          ) : null}

          <View style={styles.actions}>
            <PrimaryButton
              label="Apply targets"
              onPress={() => onSave(workingDraft)}
            />
            <Pressable
              accessibilityRole="button"
              onPress={() => onRemove(workingDraft.exercise.id)}
              style={styles.removeAction}
            >
              <AppText style={styles.removeLabel} variant="label">
                Remove exercise
              </AppText>
            </Pressable>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </Modal>
  );
}

function NumberField({
  decimal = false,
  label,
  onChangeText,
  optional = false,
  value,
}: {
  decimal?: boolean;
  label: string;
  onChangeText: (value: string) => void;
  optional?: boolean;
  value: string;
}) {
  return (
    <View style={styles.field}>
      <AppText variant="label">
        {label}
        {optional ? <AppText tone="secondary"> · optional</AppText> : null}
      </AppText>
      <TextInput
        accessibilityLabel={label}
        allowFontScaling
        autoCorrect={false}
        keyboardType={decimal ? "decimal-pad" : "number-pad"}
        onChangeText={onChangeText}
        placeholder="—"
        placeholderTextColor={colors.textSecondary}
        selectionColor={colors.focus}
        style={styles.input}
        value={value}
      />
    </View>
  );
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
  content: {
    width: "100%",
    maxWidth: layout.readableContentWidth,
    alignSelf: "center",
    gap: spacing.xl,
    padding: spacing.xl,
    paddingBottom: spacing.xxxl,
  },
  intro: {
    gap: spacing.sm,
  },
  fields: {
    gap: spacing.lg,
  },
  fieldRow: {
    flexDirection: "row",
    gap: spacing.md,
  },
  field: {
    flex: 1,
    gap: spacing.sm,
  },
  input: {
    minHeight: 52,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.control,
    paddingHorizontal: spacing.lg,
    color: colors.textPrimary,
    backgroundColor: colors.surface,
    fontSize: 17,
  },
  error: {
    color: colors.statusDanger,
  },
  actions: {
    gap: spacing.md,
  },
  removeAction: {
    minHeight: layout.minimumTouchTarget,
    alignItems: "center",
    justifyContent: "center",
  },
  removeLabel: {
    color: colors.statusDanger,
  },
});
