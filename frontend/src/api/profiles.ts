import type { components } from "./generated/schema";
import { executeApiRequest, type ApiRequestOptions } from "./request";

export type CreateTrainingProfileRequest =
  components["schemas"]["CreateTrainingProfileRequest"];
export type TrainingProfile = components["schemas"]["TrainingProfileResponse"];

export async function createTrainingProfile(
  request: CreateTrainingProfileRequest,
  options: ApiRequestOptions = {},
): Promise<TrainingProfile> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.POST("/profiles", {
      body: request,
      signal,
    });

    if (error || !data) {
      throw new Error("The profile could not be saved.");
    }

    return data;
  });
}
