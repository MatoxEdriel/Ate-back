using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ate.Infrastructure.Persistence.Configurations;

public class AchievementDefinitionConfiguration : IEntityTypeConfiguration<AchievementDefinition>
{
    public void Configure(EntityTypeBuilder<AchievementDefinition> builder)
    {
        builder.ToTable("achievement_definitions");
        builder.HasKey(a => a.Code);
        builder.Property(a => a.Code).HasMaxLength(50);
        builder.Property(a => a.Name).HasMaxLength(80).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Tier).HasMaxLength(10).IsRequired();
        builder.Property(a => a.Icon).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Metric).HasMaxLength(40).IsRequired();
    }
}

public class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("user_achievements");
        builder.HasKey(ua => new { ua.UserId, ua.AchievementCode });
        builder.Property(ua => ua.AchievementCode).HasMaxLength(50);

        builder.HasOne(ua => ua.Achievement)
            .WithMany()
            .HasForeignKey(ua => ua.AchievementCode)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
