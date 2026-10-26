using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ate.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.BusinessName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.TradeName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.TaxId)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.TaxId)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Phone)
            .HasMaxLength(30);

        builder.Property(c => c.Address)
            .HasMaxLength(300);

        builder.Property(c => c.LogoUrl)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
