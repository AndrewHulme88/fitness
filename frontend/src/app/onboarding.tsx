import { useRouter } from "expo-router";

import { FlowScreen } from "../components/FlowScreen";

export default function OnboardingRoute() {
  const router = useRouter();

  return (
    <FlowScreen
      actionLabel="Continue"
      description="Choose your goals, experience, available equipment, and preferred units."
      eyebrow="Your setup"
      onAction={() => router.push("/workout/create")}
      title="Make training fit your life."
    />
  );
}
