import type { components, operations } from "./generated/schema";
import { executeApiRequest, type ApiRequestOptions } from "./request";

export type CreateWorkoutRequest =
  components["schemas"]["CreateWorkoutRequest"];
export type UpdateWorkoutRequest =
  components["schemas"]["UpdateWorkoutRequest"];
export type WorkoutExerciseRequest =
  components["schemas"]["WorkoutExerciseRequest"];
export type WorkoutDetail = components["schemas"]["WorkoutDetailResponse"];
export type WorkoutSummary = components["schemas"]["WorkoutSummaryResponse"];
export type WorkoutListResult = components["schemas"]["WorkoutListResponse"];
export type WorkoutListFilters = NonNullable<
  operations["ListWorkouts"]["parameters"]["query"]
>;

export class WorkoutRevisionConflictError extends Error {
  constructor() {
    super("The workout changed after it was loaded.");
    this.name = "WorkoutRevisionConflictError";
  }
}

export async function listWorkouts(
  profileId: string,
  filters: WorkoutListFilters = {},
  options: ApiRequestOptions = {},
): Promise<WorkoutListResult> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.GET("/profiles/{profileId}/workouts", {
      params: { path: { profileId }, query: filters },
      signal,
    });

    if (error || !data) {
      throw new Error("Your workouts could not be loaded.");
    }

    return data;
  });
}

export async function createWorkout(
  profileId: string,
  request: CreateWorkoutRequest,
  options: ApiRequestOptions = {},
): Promise<WorkoutDetail> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.POST(
      "/profiles/{profileId}/workouts",
      {
        params: { path: { profileId } },
        body: request,
        signal,
      },
    );

    if (error || !data) {
      throw new Error("The workout could not be saved.");
    }

    return data;
  });
}

export async function getWorkout(
  profileId: string,
  workoutId: string,
  options: ApiRequestOptions = {},
): Promise<WorkoutDetail> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.GET(
      "/profiles/{profileId}/workouts/{workoutId}",
      {
        params: { path: { profileId, workoutId } },
        signal,
      },
    );

    if (error || !data) {
      throw new Error("The workout could not be loaded.");
    }

    return data;
  });
}

export async function updateWorkout(
  profileId: string,
  workoutId: string,
  request: UpdateWorkoutRequest,
  options: ApiRequestOptions = {},
): Promise<WorkoutDetail> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error, response } = await client.PUT(
      "/profiles/{profileId}/workouts/{workoutId}",
      {
        params: { path: { profileId, workoutId } },
        body: request,
        signal,
      },
    );

    if (response.status === 409) {
      throw new WorkoutRevisionConflictError();
    }

    if (error || !data) {
      throw new Error("The workout could not be saved.");
    }

    return data;
  });
}
