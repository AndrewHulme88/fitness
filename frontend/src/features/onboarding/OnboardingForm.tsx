import { useState, type ReactNode } from "react";
import { ScrollView, StyleSheet, View } from "react-native";

import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { PrimaryButton } from "../../components/PrimaryButton";
import { colors, layout, radii, spacing } from "../../theme/tokens";
import {
  equipmentOptions,
  experienceOptions,
  goalOptions,
  unitOptions,
  type EquipmentType,
  type OnboardingSubmission,
  type TrainingExperience,
  type TrainingGoal,
  type UnitSystem,
} from "./onboarding-options";
import { SelectionControl } from "./SelectionControl";

type OnboardingFormProps = {
  onSubmit: (submission: OnboardingSubmission) => Promise<void>;
};

type FormErrors = Partial<
  Record<
    "availableEquipment" | "experience" | "goals" | "submission" | "unitSystem",
    string
  >
>;

export function OnboardingForm({ onSubmit }: OnboardingFormProps) {
  const [goals, setGoals] = useState<TrainingGoal[]>([]);
  const [experience, setExperience] = useState<TrainingExperience>();
  const [availableEquipment, setAvailableEquipment] = useState<EquipmentType[]>(
    [],
  );
  const [unitSystem, setUnitSystem] = useState<UnitSystem>();
  const [errors, setErrors] = useState<FormErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async () => {
    const validationErrors: FormErrors = {};

    if (goals.length === 0) {
      validationErrors.goals = "Choose at least one goal.";
    }

    if (!experience) {
      validationErrors.experience = "Choose your experience level.";
    }

    if (availableEquipment.length === 0) {
      validationErrors.availableEquipment =
        "Choose at least one equipment option.";
    }

    if (!unitSystem) {
      validationErrors.unitSystem = "Choose your preferred units.";
    }

    if (
      Object.keys(validationErrors).length > 0 ||
      !experience ||
      !unitSystem
    ) {
      setErrors(validationErrors);
      return;
    }

    setErrors({});
    setIsSubmitting(true);

    try {
      await onSubmit({ goals, experience, availableEquipment, unitSystem });
    } catch {
      setErrors({
        submission:
          "We couldn’t save your setup. Check your connection and try again.",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AppScreen>
      <ScrollView
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        showsVerticalScrollIndicator={false}
      >
        <View style={styles.form}>
          <View style={styles.header}>
            <AppText tone="accent" variant="eyebrow">
              Your setup
            </AppText>
            <AppText accessibilityRole="header" variant="display">
              Make training fit your life.
            </AppText>
            <AppText tone="secondary">
              These choices shape your workout options. You can change them
              later.
            </AppText>
          </View>

          <FormSection
            error={errors.goals}
            helper="Choose all that apply."
            title="What are your goals?"
          >
            {goalOptions.map((option) => (
              <SelectionControl
                disabled={isSubmitting}
                key={option.value}
                label={option.label}
                mode="multiple"
                onPress={() => setGoals(toggleSelection(goals, option.value))}
                selected={goals.includes(option.value)}
              />
            ))}
          </FormSection>

          <FormSection
            error={errors.experience}
            helper="Choose the description that best matches you today."
            title="Training experience"
          >
            {experienceOptions.map((option) => (
              <SelectionControl
                description={option.description}
                disabled={isSubmitting}
                key={option.value}
                label={option.label}
                mode="single"
                onPress={() => setExperience(option.value)}
                selected={experience === option.value}
              />
            ))}
          </FormSection>

          <FormSection
            error={errors.availableEquipment}
            helper="Select everything you can normally use."
            title="Available equipment"
          >
            {equipmentOptions.map((option) => (
              <SelectionControl
                disabled={isSubmitting}
                key={option.value}
                label={option.label}
                mode="multiple"
                onPress={() =>
                  setAvailableEquipment(
                    toggleSelection(availableEquipment, option.value),
                  )
                }
                selected={availableEquipment.includes(option.value)}
              />
            ))}
          </FormSection>

          <FormSection
            error={errors.unitSystem}
            helper="Used for load, measurements, and progress."
            title="Preferred units"
          >
            {unitOptions.map((option) => (
              <SelectionControl
                description={option.description}
                disabled={isSubmitting}
                key={option.value}
                label={option.label}
                mode="single"
                onPress={() => setUnitSystem(option.value)}
                selected={unitSystem === option.value}
              />
            ))}
          </FormSection>

          <View style={styles.privacyNote}>
            <AppText variant="label">Keep health details private</AppText>
            <AppText tone="secondary">
              We don’t ask for injuries or medical information. You can exclude
              individual exercises later without explaining why.
            </AppText>
          </View>

          {errors.submission ? (
            <AppText
              accessibilityLiveRegion="polite"
              accessibilityRole="alert"
              style={styles.error}
            >
              {errors.submission}
            </AppText>
          ) : null}

          <PrimaryButton
            disabled={isSubmitting}
            label={isSubmitting ? "Saving setup…" : "Save and continue"}
            onPress={handleSubmit}
          />
        </View>
      </ScrollView>
    </AppScreen>
  );
}

function FormSection({
  children,
  error,
  helper,
  title,
}: {
  children: ReactNode;
  error?: string;
  helper: string;
  title: string;
}) {
  return (
    <View style={styles.section}>
      <View style={styles.sectionHeader}>
        <AppText variant="title">{title}</AppText>
        <AppText tone="secondary">{helper}</AppText>
        {error ? (
          <AppText accessibilityLiveRegion="polite" style={styles.error}>
            {error}
          </AppText>
        ) : null}
      </View>
      <View style={styles.options}>{children}</View>
    </View>
  );
}

function toggleSelection<T>(selection: readonly T[], value: T): T[] {
  return selection.includes(value)
    ? selection.filter((item) => item !== value)
    : [...selection, value];
}

const styles = StyleSheet.create({
  content: {
    alignItems: "center",
    paddingHorizontal: spacing.xl,
    paddingVertical: spacing.xxxl,
  },
  form: {
    width: "100%",
    maxWidth: layout.readableContentWidth,
    gap: spacing.xxxl,
  },
  header: {
    gap: spacing.md,
  },
  section: {
    gap: spacing.lg,
  },
  sectionHeader: {
    gap: spacing.sm,
  },
  options: {
    gap: spacing.sm,
  },
  privacyNote: {
    gap: spacing.sm,
    padding: spacing.lg,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.control,
    backgroundColor: colors.surface,
  },
  error: {
    color: colors.statusDanger,
  },
});
