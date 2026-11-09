using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ate.Infrastructure.Persistence.Configurations;

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("restaurants");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(r => r.Code)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.Property(r => r.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(r => r.Cuisine)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(r => r.Address)
            .HasMaxLength(300);

        builder.Property(r => r.Phone)
            .HasMaxLength(30);

        builder.Property(r => r.Latitude)
            .HasColumnType("numeric(9,6)");

        builder.Property(r => r.Longitude)
            .HasColumnType("numeric(9,6)");

        builder.Property(r => r.BrandColor)
            .HasMaxLength(7)
            .IsRequired();

        // TEMPORAL: text porque el logo va serializado en base64. Con el servidor de imágenes
        // volverá a HasMaxLength(500) con la URL del archivo.
        builder.Property(r => r.LogoUrl)
            .HasColumnType("text");

        builder.Property(r => r.Icon)
            .HasMaxLength(30);

        builder.Property(r => r.ReadyMinutes)
            .IsRequired()
            .HasDefaultValue(15);

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(r => r.MerchantUser)
            .WithMany()
            .HasForeignKey(r => r.MerchantUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
