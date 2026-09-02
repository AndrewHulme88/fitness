import type { components } from "./generated/schema";
import { executeApiRequest, type ApiRequestOptions } from "./request";

export type CoachConversation =
  components["schemas"]["CoachConversationResponse"];

export type CoachWorkoutProposal =
  components["schemas"]["AiCoachProposalResponse"];

export async function getCoachConversation(
  profileId: string,
  options: ApiRequestOptions = {},
): Promise<CoachConversation | undefined> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, response } = await client.GET(
      "/profiles/{profileId}/coach/conversation",
      { params: { path: { profileId } }, signal },
    );
    if (response.status === 404) return undefined;
    if (!data) throw new Error("The coach conversation could not be loaded.");
    return data;
  });
}

export async function sendCoachMessage(
  profileId: string,
  question: string,
  options: ApiRequestOptions = {},
  workoutId?: string,
  progressExerciseId?: string,
  progressPeriodDays?: 7 | 28,
): Promise<CoachConversation> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.POST(
      "/profiles/{profileId}/coach/conversation/messages",
      {
        params: { path: { profileId } },
        body: { question, workoutId, progressExerciseId, progressPeriodDays },
        signal,
      },
    );
    if (error || !data) throw new Error("The coach is unavailable right now.");
    return data;
  });
}

export async function deleteCoachConversation(
  profileId: string,
  options: ApiRequestOptions = {},
): Promise<void> {
  return executeApiRequest(options, async (client, signal) => {
    const { error } = await client.DELETE(
      "/profiles/{profileId}/coach/conversation",
      { params: { path: { profileId } }, signal },
    );
    if (error) throw new Error("The coach conversation could not be deleted.");
  });
}

export async function confirmCoachWorkoutProposal(
  profileId: string,
  proposalId: string,
  options: ApiRequestOptions = {},
): Promise<void> {
  return executeApiRequest(options, async (client, signal) => {
    const { error } = await client.POST(
      "/profiles/{profileId}/coach/conversation/proposals/{proposalId}/confirm",
      { params: { path: { profileId, proposalId } }, signal },
    );
    if (error) throw new Error("The proposal could not be applied.");
  });
}
