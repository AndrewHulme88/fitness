import type { components, operations } from "./generated/schema";
import { executeApiRequest, type ApiRequestOptions } from "./request";

export type ExerciseSearchFilters = NonNullable<
  operations["SearchExercises"]["parameters"]["query"]
>;
export type ExerciseSearchResult =
  components["schemas"]["ExerciseSearchResponse"];
export type ExerciseSummary = components["schemas"]["ExerciseSummaryResponse"];
export type ExerciseDetail = components["schemas"]["ExerciseDetailResponse"];

export async function searchExercises(
  filters: ExerciseSearchFilters = {},
  options: ApiRequestOptions = {},
): Promise<ExerciseSearchResult> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.GET("/exercises", {
      params: { query: filters },
      signal,
    });

    if (error || !data) {
      throw new Error("The exercise catalogue could not be loaded.");
    }

    return data;
  });
}

export async function getExercise(
  exerciseId: string,
  options: ApiRequestOptions = {},
): Promise<ExerciseDetail> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.GET("/exercises/{exerciseId}", {
      params: { path: { exerciseId } },
      signal,
    });

    if (error || !data) {
      throw new Error("The exercise could not be loaded.");
    }

    return data;
  });
}
