using FitnessCoach.Api.Features.Profiles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessCoach.Api.Features.AiCoach;

internal sealed class CoachConversationConfiguration : IEntityTypeConfiguration<CoachConversation>
{
    public void Configure(EntityTypeBuilder<CoachConversation> builder)
    {
        builder.ToTable("coach_conversations");
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.ProfileId).IsUnique();
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.ProfileId).HasColumnName("profile_id");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at");
        builder.HasOne<TrainingProfile>()
            .WithMany()
            .HasForeignKey(item => item.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(item => item.Messages)
            .WithOne()
            .HasForeignKey(item => item.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CoachMessageConfiguration : IEntityTypeConfiguration<CoachMessage>
{
    public void Configure(EntityTypeBuilder<CoachMessage> builder)
    {
        builder.ToTable("coach_messages", table =>
        {
            table.HasCheckConstraint(
                "CK_coach_messages_role", "role IN ('User', 'Coach')");
        });
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.ConversationId, item.CreatedAt });
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.ConversationId).HasColumnName("conversation_id");
        builder.Property(item => item.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(16);
        builder.Property(item => item.Content).HasColumnName("content").HasMaxLength(2_000);
        builder.Property(item => item.ResponseKind)
            .HasColumnName("response_kind")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(item => item.ContextSources).HasColumnName("context_sources");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
    }
}
