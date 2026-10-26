using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ate.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeCode)
            .HasMaxLength(50);

        builder.Property(e => e.Department)
            .HasMaxLength(100);

        builder.Property(e => e.MonthlyAllowance)
            .HasPrecision(12, 2)
            .IsRequired()
            .HasDefaultValue(0m);

        builder.Property(e => e.QrCodeToken)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(e => e.QrCodeToken)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => new { e.CompanyId, e.PersonId })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(e => e.Company)
            .WithMany(c => c.Employees)
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Person)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
