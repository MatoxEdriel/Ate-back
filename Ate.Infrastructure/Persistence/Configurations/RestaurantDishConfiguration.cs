using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ate.Infrastructure.Persistence.Configurations;

public class RestaurantDishConfiguration : IEntityTypeConfiguration<RestaurantDish>
{
    public void Configure(EntityTypeBuilder<RestaurantDish> builder)
    {
        builder.ToTable("restaurant_dishes");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.MealType)
            .HasConversion(
                mealType => mealType.ToString().ToLowerInvariant(),
                value => Enum.Parse<MealType>(value, true))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(d => d.Protein)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(d => d.DefaultDrink)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Price)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(d => d.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(d => new { d.RestaurantId, d.MealType })
            .HasFilter("is_deleted = false");

        builder.HasOne(d => d.Restaurant)
            .WithMany(r => r.Dishes)
            .HasForeignKey(d => d.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
