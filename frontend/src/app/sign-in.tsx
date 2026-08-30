import * as AuthSession from "expo-auth-session";
import { useRouter } from "expo-router";
import { useState } from "react";

import { RouteStatus } from "../components/RouteStatus";
import {
  createAuthorizationRequest,
  getCognitoConfiguration,
  getDiscovery,
  getRedirectUri,
  saveSession,
} from "../features/auth/cognito";

export default function SignInRoute() {
  const router = useRouter();
  const [error, setError] = useState<string>();
  const [isSigningIn, setIsSigningIn] = useState(false);

  const signIn = async () => {
    setError(undefined);
    setIsSigningIn(true);
    try {
      const configuration = getCognitoConfiguration();
      const discovery = getDiscovery(configuration.domain);
      const request = createAuthorizationRequest(configuration);
      await request.makeAuthUrlAsync(discovery);
      const response = await request.promptAsync(discovery);
      if (response.type === "cancel" || response.type === "dismiss") return;
      if (response.type !== "success" || !response.params.code) {
        throw new Error("Sign-in could not be completed.");
      }

      const session = await AuthSession.exchangeCodeAsync(
        {
          clientId: configuration.appClientId,
          code: response.params.code,
          extraParams: { code_verifier: request.codeVerifier ?? "" },
          redirectUri: getRedirectUri(),
        },
        discovery,
      );
      await saveSession(session);
      router.replace("/");
    } catch {
      setError("We couldn’t sign you in. Check your connection and try again.");
    } finally {
      setIsSigningIn(false);
    }
  };

  return (
    <RouteStatus
      actionLabel={isSigningIn ? "Signing in…" : "Sign in or create account"}
      busy={isSigningIn}
      message={
        error ?? "Sign in to keep your training data tied to your account."
      }
      onAction={() => void signIn()}
      title="Welcome to Fitness Coach"
    />
  );
}
