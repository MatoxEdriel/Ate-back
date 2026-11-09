using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ate.Infrastructure.Persistence.Configurations;

public class PasswordRecoveryConfiguration : IEntityTypeConfiguration<PasswordRecovery>
{
    public void Configure(EntityTypeBuilder<PasswordRecovery> builder)
    {
        builder.ToTable("password_recoveries");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.CodeHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.ResetTokenHash)
            .HasMaxLength(128);

        builder.Property(r => r.Attempts)
            .IsRequired()
            .HasDefaultValue(0)
            .IsConcurrencyToken();

        builder.Property(r => r.UsedAt)
            .IsConcurrencyToken();

        builder.HasIndex(r => new { r.UserId, r.CreatedAt });

        builder.HasIndex(r => r.ResetTokenHash)
            .IsUnique()
            .HasFilter("reset_token_hash IS NOT NULL");

        builder.Ignore(r => r.IsClosed);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
