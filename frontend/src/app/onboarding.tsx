import { useRouter } from "expo-router";

import {
  AuthenticationRequiredError,
  createTrainingProfile,
} from "../api/profiles";
import { clearSession } from "../features/auth/cognito";
import type { OnboardingSubmission } from "../features/onboarding/onboarding-options";
import { OnboardingForm } from "../features/onboarding/OnboardingForm";
import { saveStoredProfile } from "../features/onboarding/profile-storage";

export default function OnboardingRoute() {
  const router = useRouter();

  const handleSubmit = async (submission: OnboardingSubmission) => {
    try {
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
    } catch (error) {
      if (error instanceof AuthenticationRequiredError) {
        await clearSession();
        router.replace("/sign-in");
        return;
      }

      throw error;
    }
  };

  return <OnboardingForm onSubmit={handleSubmit} />;
}
