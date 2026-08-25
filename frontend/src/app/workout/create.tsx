import { useRouter } from "expo-router";

import { FlowScreen } from "../../components/FlowScreen";

export default function CreateWorkoutRoute() {
  const router = useRouter();

  return (
    <FlowScreen
      actionLabel="Start workout"
      description="Choose exercises and organise them into a session you can follow."
      eyebrow="Workout creation"
      onAction={() => router.push("/workout/session")}
      title="Create your first workout."
    />
  );
}
