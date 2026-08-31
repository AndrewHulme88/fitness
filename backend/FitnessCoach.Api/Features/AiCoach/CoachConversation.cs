namespace FitnessCoach.Api.Features.AiCoach;

internal enum CoachMessageRole
{
    User,
    Coach,
}

internal sealed class CoachConversation
{
    private CoachConversation()
    {
    }

    private CoachConversation(Guid id, Guid profileId, DateTimeOffset createdAt)
    {
        Id = id;
        ProfileId = profileId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public ICollection<CoachMessage> Messages { get; } = [];

    public static CoachConversation Create(Guid profileId, DateTimeOffset createdAt) =>
        new(Guid.NewGuid(), profileId, createdAt);

    public CoachMessage AddMessage(
        CoachMessageRole role,
        string content,
        AiCoachResponseKind? responseKind,
        IReadOnlyList<string> contextSources,
        DateTimeOffset createdAt)
    {
        var message = new CoachMessage(
            Guid.NewGuid(), Id, role, content, responseKind, contextSources.ToArray(), createdAt);
        Messages.Add(message);
        UpdatedAt = createdAt;
        return message;
    }
}

internal sealed class CoachMessage
{
    private CoachMessage()
    {
    }

    public CoachMessage(
        Guid id,
        Guid conversationId,
        CoachMessageRole role,
        string content,
        AiCoachResponseKind? responseKind,
        string[] contextSources,
        DateTimeOffset createdAt)
    {
        Id = id;
        ConversationId = conversationId;
        Role = role;
        Content = content;
        ResponseKind = responseKind;
        ContextSources = contextSources;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public CoachMessageRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public AiCoachResponseKind? ResponseKind { get; private set; }
    public string[] ContextSources { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
}
