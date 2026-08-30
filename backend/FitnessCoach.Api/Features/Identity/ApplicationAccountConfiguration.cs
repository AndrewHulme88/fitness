using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FitnessCoach.Api.Features.Identity;

internal sealed class ApplicationAccountConfiguration : IEntityTypeConfiguration<ApplicationAccount>
{
    public void Configure(EntityTypeBuilder<ApplicationAccount> builder)
    {
        builder.ToTable("application_accounts");
        builder.HasKey(account => account.Id);
        builder.HasIndex(account => new { account.Issuer, account.Subject }).IsUnique();
        builder.Property(account => account.Id).HasColumnName("id");
        builder.Property(account => account.Issuer).HasColumnName("issuer").HasMaxLength(256);
        builder.Property(account => account.Subject).HasColumnName("subject").HasMaxLength(256);
        builder.Property(account => account.CreatedAt).HasColumnName("created_at");
    }
}
