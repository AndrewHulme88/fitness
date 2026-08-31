import { useEffect, useState } from "react";
import {
  Alert,
  Pressable,
  ScrollView,
  StyleSheet,
  TextInput,
  View,
} from "react-native";

import {
  deleteCoachConversation,
  getCoachConversation,
  sendCoachMessage,
  type CoachConversation as CoachConversationDocument,
} from "../../api/coach";
import { AppScreen } from "../../components/AppScreen";
import { AppText } from "../../components/AppText";
import { PrimaryButton } from "../../components/PrimaryButton";
import { RouteStatus } from "../../components/RouteStatus";
import { colors, layout, radii, spacing } from "../../theme/tokens";

export function CoachConversation({ profileId }: { profileId: string }) {
  const [conversation, setConversation] = useState<CoachConversationDocument>();
  const [question, setQuestion] = useState("");
  const [loading, setLoading] = useState(true);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string>();

  useEffect(() => {
    const controller = new AbortController();
    getCoachConversation(profileId, { signal: controller.signal })
      .then(setConversation)
      .catch(
        () =>
          !controller.signal.aborted &&
          setError("Your coach conversation could not be loaded."),
      )
      .finally(() => !controller.signal.aborted && setLoading(false));
    return () => controller.abort();
  }, [profileId]);

  if (loading)
    return (
      <RouteStatus
        busy
        message="Loading your saved coaching conversation."
        title="Preparing coach"
      />
    );
  if (error)
    return (
      <RouteStatus
        actionLabel="Try again"
        message={error}
        onAction={() => setError(undefined)}
        title="Coach unavailable"
      />
    );

  const send = async () => {
    const value = question.trim();
    if (!value || sending) return;
    setSending(true);
    try {
      setConversation(await sendCoachMessage(profileId, value));
      setQuestion("");
    } catch {
      setError(
        "The coach is unavailable right now. Your workouts are still available.",
      );
    } finally {
      setSending(false);
    }
  };

  const clear = () => {
    if (sending) return;

    Alert.alert(
      "Delete conversation?",
      "This permanently deletes the coach messages saved to your account.",
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Delete",
          style: "destructive",
          onPress: () => {
            if (sending) return;
            void deleteCoachConversation(profileId)
              .then(() => setConversation(undefined))
              .catch(() => setError("The conversation could not be deleted."));
          },
        },
      ],
    );
  };

  return (
    <AppScreen>
      <ScrollView
        contentContainerStyle={styles.content}
        contentInsetAdjustmentBehavior="automatic"
        keyboardShouldPersistTaps="handled"
      >
        <View style={styles.intro}>
          <AppText tone="accent" variant="eyebrow">
            AI coach
          </AppText>
          <AppText accessibilityRole="header" variant="display">
            Training questions, not medical advice
          </AppText>
          <AppText tone="secondary">
            The coach uses selected training details you can review below. It
            cannot diagnose injuries or change your plans.
          </AppText>
        </View>
        <View style={styles.messages}>
          {conversation?.messages.map((message) => (
            <View
              key={message.id}
              style={[
                styles.message,
                message.role === "coach" && styles.coachMessage,
              ]}
            >
              <AppText variant="label">
                {message.role === "coach" ? "AI coach" : "You"}
              </AppText>
              <AppText
                tone={message.role === "coach" ? "primary" : "secondary"}
              >
                {message.content}
              </AppText>
              {message.contextSources.length > 0 ? (
                <AppText tone="secondary" style={styles.basis}>
                  Based on: {message.contextSources.join(", ")}
                </AppText>
              ) : null}
            </View>
          ))}
          {!conversation ? (
            <AppText tone="secondary">
              Ask about training terms, your workouts, or your recorded
              progress.
            </AppText>
          ) : null}
        </View>
        <View style={styles.composer}>
          <TextInput
            accessibilityLabel="Question for the AI coach"
            editable={!sending}
            maxLength={1000}
            multiline
            onChangeText={setQuestion}
            placeholder="Ask a training question"
            placeholderTextColor={colors.textSecondary}
            style={styles.input}
            value={question}
          />
          <PrimaryButton
            disabled={sending || question.trim().length === 0}
            label={sending ? "Asking coach…" : "Ask coach"}
            onPress={() => void send()}
          />
        </View>
        {conversation ? (
          <Pressable
            accessibilityRole="button"
            disabled={sending}
            onPress={clear}
            style={styles.delete}
          >
            <AppText tone="secondary" variant="label">
              Delete saved conversation
            </AppText>
          </Pressable>
        ) : null}
      </ScrollView>
    </AppScreen>
  );
}

const styles = StyleSheet.create({
  content: {
    width: "100%",
    maxWidth: layout.readableContentWidth,
    alignSelf: "center",
    gap: spacing.xl,
    padding: spacing.lg,
    paddingBottom: spacing.xxxl,
  },
  intro: { gap: spacing.sm },
  messages: { gap: spacing.md },
  message: {
    gap: spacing.xs,
    borderTopWidth: StyleSheet.hairlineWidth,
    borderColor: colors.border,
    paddingTop: spacing.md,
  },
  coachMessage: {
    backgroundColor: colors.surface,
    borderRadius: radii.panel,
    padding: spacing.md,
  },
  basis: { fontSize: 13 },
  composer: { gap: spacing.md },
  input: {
    minHeight: 108,
    color: colors.textPrimary,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.control,
    padding: spacing.md,
    textAlignVertical: "top",
  },
  delete: { minHeight: 44, alignItems: "center", justifyContent: "center" },
});
