using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ate.Infrastructure.Persistence.Configurations;

public class CompanyRestaurantAgreementConfiguration : IEntityTypeConfiguration<CompanyRestaurantAgreement>
{
    public void Configure(EntityTypeBuilder<CompanyRestaurantAgreement> builder)
    {
        builder.ToTable("company_restaurant_agreements");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.DistanceM)
            .IsRequired();

        builder.Property(a => a.StartsOn)
            .IsRequired();

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(a => new { a.CompanyId, a.RestaurantId })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasOne(a => a.Company)
            .WithMany()
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Restaurant)
            .WithMany(r => r.Agreements)
            .HasForeignKey(a => a.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
