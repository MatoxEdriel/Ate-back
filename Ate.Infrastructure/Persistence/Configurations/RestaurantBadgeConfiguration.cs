using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ate.Infrastructure.Persistence.Configurations;

public class RestaurantBadgeDefinitionConfiguration : IEntityTypeConfiguration<RestaurantBadgeDefinition>
{
    public void Configure(EntityTypeBuilder<RestaurantBadgeDefinition> builder)
    {
        builder.ToTable("restaurant_badge_definitions");
        builder.HasKey(b => b.Code);
        builder.Property(b => b.Code).HasMaxLength(50);
        builder.Property(b => b.Name).HasMaxLength(80).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Icon).HasMaxLength(30).IsRequired();
        builder.Property(b => b.Category).HasMaxLength(50).IsRequired();
        builder.Property(b => b.EarnMinRatio).HasPrecision(3, 2);
        builder.Property(b => b.LoseMinRatio).HasPrecision(3, 2);
    }
}

public class RestaurantBadgeConfiguration : IEntityTypeConfiguration<RestaurantBadge>
{
    public void Configure(EntityTypeBuilder<RestaurantBadge> builder)
    {
        builder.ToTable("restaurant_badges");
        builder.HasKey(rb => new { rb.RestaurantId, rb.BadgeCode });
        builder.Property(rb => rb.BadgeCode).HasMaxLength(50);

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(rb => rb.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rb => rb.Badge)
            .WithMany()
            .HasForeignKey(rb => rb.BadgeCode)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
