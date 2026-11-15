using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ate.Infrastructure.Persistence.Configurations;

public class LoginAttemptWindowConfiguration : IEntityTypeConfiguration<LoginAttemptWindow>
{
    public void Configure(EntityTypeBuilder<LoginAttemptWindow> builder)
    {
        builder.ToTable("login_attempt_windows");
        builder.HasKey(w => w.Key);
        builder.Property(w => w.Key).HasMaxLength(64).IsFixedLength();
    }
}
