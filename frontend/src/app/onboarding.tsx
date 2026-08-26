import { useRouter } from "expo-router";

import { createTrainingProfile } from "../api/profiles";
import type { OnboardingSubmission } from "../features/onboarding/onboarding-options";
import { OnboardingForm } from "../features/onboarding/OnboardingForm";

export default function OnboardingRoute() {
  const router = useRouter();

  const handleSubmit = async (submission: OnboardingSubmission) => {
    await createTrainingProfile(submission);
    router.replace("/workout/create");
  };

  return <OnboardingForm onSubmit={handleSubmit} />;
}
