import { useLocalSearchParams, useRouter } from "expo-router";
import { useEffect, useState } from "react";

import {
  getTrainingProfile,
  updateTrainingProfile,
  type TrainingProfile,
} from "../api/profiles";
import { RouteStatus } from "../components/RouteStatus";
import { OnboardingForm } from "../features/onboarding/OnboardingForm";
import type { OnboardingSubmission } from "../features/onboarding/onboarding-options";
import { saveStoredProfile } from "../features/onboarding/profile-storage";

export default function ProfileRoute() {
  const router = useRouter();
  const { profileId } = useLocalSearchParams<{ profileId?: string }>();
  const [profile, setProfile] = useState<TrainingProfile>();
  const [loadFailed, setLoadFailed] = useState(false);
  const [loadAttempt, setLoadAttempt] = useState(0);

  useEffect(() => {
    if (!profileId) return;

    const controller = new AbortController();
    getTrainingProfile(profileId, { signal: controller.signal })
      .then((result) => setProfile(result))
      .catch(() => {
        if (!controller.signal.aborted) setLoadFailed(true);
      });

    return () => controller.abort();
  }, [loadAttempt, profileId]);

  if (!profileId) {
    return (
      <RouteStatus
        actionLabel="Return home"
        message="Open your workout plans to edit your training preferences."
        onAction={() => router.replace("/")}
        title="Profile required"
      />
    );
  }

  if (loadFailed) {
    return (
      <RouteStatus
        actionLabel="Try again"
        message="Your training preferences could not be loaded. Check your connection and try again."
        onAction={() => {
          setLoadFailed(false);
          setLoadAttempt((attempt) => attempt + 1);
        }}
        title="Preferences unavailable"
      />
    );
  }

  if (!profile) {
    return (
      <RouteStatus
        busy
        message="Loading your current training preferences."
        title="Training preferences"
      />
    );
  }

  const initialSubmission: OnboardingSubmission = {
    goals: profile.goals,
    experience: profile.experience,
    availableEquipment: profile.availableEquipment,
    unitSystem: profile.unitSystem,
  };

  const handleSubmit = async (submission: OnboardingSubmission) => {
    const updatedProfile = await updateTrainingProfile(profileId, submission);
    await saveStoredProfile({
      schemaVersion: 1,
      profileId: updatedProfile.id,
      unitSystem: updatedProfile.unitSystem,
    });
    router.back();
  };

  return (
    <OnboardingForm
      initialSubmission={initialSubmission}
      onSubmit={handleSubmit}
      submitLabel="Save preferences"
    />
  );
}
