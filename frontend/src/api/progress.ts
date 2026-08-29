import type { components, operations } from "./generated/schema";
import { executeApiRequest, type ApiRequestOptions } from "./request";

export type ProgressOverview =
  components["schemas"]["ProgressOverviewResponse"];
export type RecordedExerciseSummary =
  components["schemas"]["RecordedExerciseSummaryResponse"];
export type ExercisePerformance =
  components["schemas"]["ExercisePerformanceResponse"];
export type ExercisePerformanceFilters = NonNullable<
  operations["GetExercisePerformance"]["parameters"]["query"]
>;

export async function getProgressOverview(
  profileId: string,
  options: ApiRequestOptions = {},
): Promise<ProgressOverview> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.GET("/profiles/{profileId}/progress", {
      params: { path: { profileId } },
      signal,
    });
    if (error || !data) throw new Error("Progress could not be loaded.");
    return data;
  });
}

export async function getExercisePerformance(
  profileId: string,
  exerciseId: string,
  filters: ExercisePerformanceFilters = {},
  options: ApiRequestOptions = {},
): Promise<ExercisePerformance> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.GET(
      "/profiles/{profileId}/progress/exercises/{exerciseId}",
      {
        params: { path: { profileId, exerciseId }, query: filters },
        signal,
      },
    );
    if (error || !data)
      throw new Error("Exercise performance could not be loaded.");
    return data;
  });
}
