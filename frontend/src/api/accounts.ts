import type { components } from "./generated/schema";
import { executeApiRequest, type ApiRequestOptions } from "./request";

export type CurrentAccount = components["schemas"]["CurrentAccountResponse"];

export async function getCurrentAccount(
  options: ApiRequestOptions = {},
): Promise<CurrentAccount> {
  return executeApiRequest(options, async (client, signal) => {
    const { data, error } = await client.GET("/account", { signal });

    if (error || !data) {
      throw new Error("Your account could not be restored.");
    }

    return data;
  });
}
