import { useMemo, useState } from "react";
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
import { colors, radii, spacing } from "../../theme/tokens";
import type {
  ActualSetValues,
  SessionExercise,
  SessionSet,
} from "./session-model";
import { suggestedValues } from "./session-model";
import {
  fieldsFor,
  fromDisplayValue,
  toDisplayValue,
  type UnitSystem,
} from "./session-values";

type Props = {
  purpose?: "logging" | "correction";
  exercise: SessionExercise | null;
  set: SessionSet | null;
  unitSystem: UnitSystem;
  onClose: () => void;
  onSave: (
    values: ActualSetValues,
    complete: boolean,
    startRest: boolean,
  ) => void;
};

type ContentProps = Omit<Props, "exercise" | "set"> & {
  exercise: SessionExercise;
  set: SessionSet;
};

export function SetEntrySheet(props: Props) {
  if (!props.exercise || !props.set) return null;
  return (
    <SetEntrySheetContent
      key={props.set.setId}
      {...props}
      exercise={props.exercise}
      set={props.set}
    />
  );
}

function SetEntrySheetContent({
  exercise,
  set,
  unitSystem,
  onClose,
  onSave,
  purpose = "logging",
}: ContentProps) {
  const fields = fieldsFor(exercise, unitSystem);
  const suggested = suggestedValues(exercise, set);
  const [values, setValues] = useState<Record<keyof ActualSetValues, string>>(
    () => ({
      actualRepetitions: toDisplayValue(
        exercise,
        "actualRepetitions",
        suggested.actualRepetitions,
        unitSystem,
      ),
      actualLoadKilograms: toDisplayValue(
        exercise,
        "actualLoadKilograms",
        suggested.actualLoadKilograms,
        unitSystem,
      ),
      actualDurationSeconds: toDisplayValue(
        exercise,
        "actualDurationSeconds",
        suggested.actualDurationSeconds,
        unitSystem,
      ),
      actualDistanceMetres: toDisplayValue(
        exercise,
        "actualDistanceMetres",
        suggested.actualDistanceMetres,
        unitSystem,
      ),
    }),
  );
  const [error, setError] = useState<string>();

  const parsed = useMemo<ActualSetValues>(
    () => ({
      actualRepetitions: fromDisplayValue(
        exercise,
        "actualRepetitions",
        values.actualRepetitions,
        unitSystem,
      ),
      actualLoadKilograms: fromDisplayValue(
        exercise,
        "actualLoadKilograms",
        values.actualLoadKilograms,
        unitSystem,
      ),
      actualDurationSeconds: fromDisplayValue(
        exercise,
        "actualDurationSeconds",
        values.actualDurationSeconds,
        unitSystem,
      ),
      actualDistanceMetres: fromDisplayValue(
        exercise,
        "actualDistanceMetres",
        values.actualDistanceMetres,
        unitSystem,
      ),
    }),
    [exercise, unitSystem, values],
  );

  const save = (complete: boolean, startRest: boolean) => {
    if (complete && fields.some((field) => parsed[field.key] === null)) {
      setError("Enter every value before completing the set.");
      return;
    }
    onSave(parsed, complete, startRest);
  };

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
          <Pressable accessibilityRole="button" hitSlop={8} onPress={onClose}>
            <AppText tone="secondary" variant="label">
              Cancel
            </AppText>
          </Pressable>
          <AppText variant="label">Set {set.position + 1}</AppText>
          <View style={styles.headerSpacer} />
        </View>
        <ScrollView
          contentContainerStyle={styles.content}
          keyboardShouldPersistTaps="handled"
        >
          <View style={styles.intro}>
            <AppText tone="accent" variant="eyebrow">
              {purpose === "correction"
                ? "Correct record"
                : "Log actual result"}
            </AppText>
            <AppText accessibilityRole="header" variant="title">
              {exercise.exerciseName}
            </AppText>
            <AppText tone="secondary">
              {purpose === "correction"
                ? "Update only what was recorded for this set."
                : "Planned values are suggestions for entry only. Nothing is recorded until you save."}
            </AppText>
          </View>
          <View style={styles.fields}>
            {fields.map((field) => (
              <View key={field.key} style={styles.field}>
                <AppText variant="label">{field.label}</AppText>
                <TextInput
                  accessibilityLabel={field.label}
                  keyboardType="decimal-pad"
                  onChangeText={(value) =>
                    setValues((current) => ({ ...current, [field.key]: value }))
                  }
                  selectTextOnFocus
                  style={styles.input}
                  value={values[field.key]}
                />
              </View>
            ))}
          </View>
          {error ? (
            <AppText accessibilityRole="alert" style={styles.error}>
              {error}
            </AppText>
          ) : null}
          <View style={styles.actions}>
            <PrimaryButton
              label={set.isCompleted ? "Save correction" : "Complete set"}
              onPress={() => save(true, false)}
            />
            {!set.isCompleted && purpose === "logging" ? (
              <Pressable
                accessibilityRole="button"
                onPress={() => save(true, true)}
                style={styles.secondaryAction}
              >
                <AppText tone="accent" variant="label">
                  Complete & start 90 sec rest
                </AppText>
              </Pressable>
            ) : set.isCompleted ? (
              <Pressable
                accessibilityRole="button"
                onPress={() => save(false, false)}
                style={styles.secondaryAction}
              >
                <AppText tone="secondary" variant="label">
                  Mark incomplete
                </AppText>
              </Pressable>
            ) : null}
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </Modal>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.canvas },
  header: {
    minHeight: 56,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: spacing.lg,
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
  },
  headerSpacer: { width: 48 },
  content: { padding: spacing.xl, gap: spacing.xxl },
  intro: { gap: spacing.sm },
  fields: { gap: spacing.lg },
  field: { gap: spacing.sm },
  input: {
    minHeight: 52,
    borderRadius: radii.control,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: 20,
    paddingHorizontal: spacing.lg,
  },
  actions: { gap: spacing.md },
  secondaryAction: {
    minHeight: 48,
    alignItems: "center",
    justifyContent: "center",
  },
  error: { color: colors.statusDanger },
});
