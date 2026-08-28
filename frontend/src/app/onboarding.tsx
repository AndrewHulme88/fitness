import { useRouter } from "expo-router";

import { createTrainingProfile } from "../api/profiles";
import type { OnboardingSubmission } from "../features/onboarding/onboarding-options";
import { OnboardingForm } from "../features/onboarding/OnboardingForm";
import { saveStoredProfile } from "../features/onboarding/profile-storage";

export default function OnboardingRoute() {
  const router = useRouter();

  const handleSubmit = async (submission: OnboardingSubmission) => {
    const profile = await createTrainingProfile(submission);
    await saveStoredProfile({
      schemaVersion: 1,
      profileId: profile.id,
      unitSystem: profile.unitSystem,
    });
    router.replace({
      pathname: "/workouts",
      params: { profileId: profile.id },
    });
  };

  return <OnboardingForm onSubmit={handleSubmit} />;
}
