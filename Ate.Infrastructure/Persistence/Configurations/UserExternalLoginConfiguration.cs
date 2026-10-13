using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ate.Infrastructure.Persistence.Configurations;

public class UserExternalLoginConfiguration : IEntityTypeConfiguration<UserExternalLogin>
{
    public void Configure(EntityTypeBuilder<UserExternalLogin> builder)
    {
        builder.ToTable("user_external_logins");

        builder.HasKey(uel => uel.Id);

        builder.Property(uel => uel.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(uel => uel.ProviderKey)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(uel => uel.ProviderDisplayName)
            .HasMaxLength(150);

        builder.HasIndex(uel => new { uel.Provider, uel.ProviderKey })
            .IsUnique();

        builder.HasOne(uel => uel.User)
            .WithMany(u => u.ExternalLogins)
            .HasForeignKey(uel => uel.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
