import { useRouter } from "expo-router";

import { FlowScreen } from "../../components/FlowScreen";

export default function WorkoutSessionRoute() {
  const router = useRouter();

  return (
    <FlowScreen
      actionLabel="Finish workout"
      description="Sets, repetitions, load, and notes will stay within reach during training."
      eyebrow="Active session"
      onAction={() => router.push("/workout/summary")}
      title="Your workout."
    />
  );
}
