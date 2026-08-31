import type { components } from "./generated/schema";
import { executeApiRequest, type ApiRequestOptions } from "./request";

export type CreateTrainingProfileRequest =
  components["schemas"]["CreateTrainingProfileRequest"];
export type TrainingProfile = components["schemas"]["TrainingProfileResponse"];

export class AuthenticationRequiredError extends Error {
  constructor() {
    super("Sign-in is required to save a training profile.");
    this.name = "AuthenticationRequiredError";
  }
}

export async function createTrainingProfile(
  request: CreateTrainingProfileRequest,
  options: ApiRequestOptions = {},
): Promise<TrainingProfile> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error, response } = await client.POST("/profiles", {
      body: request,
      signal,
    });

    if (response.status === 401) {
      throw new AuthenticationRequiredError();
    }

    if (error || !data) {
      throw new Error("The profile could not be saved.");
    }

    return data;
  });
}

export async function getTrainingProfile(
  profileId: string,
  options: ApiRequestOptions = {},
): Promise<TrainingProfile> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.GET("/profiles/{profileId}", {
      params: { path: { profileId } },
      signal,
    });

    if (error || !data) {
      throw new Error("The training profile could not be loaded.");
    }

    return data;
  });
}
