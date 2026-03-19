using Identity.Application.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums;

namespace Identity.Infrastructure.Data.Configurations;

public class OrganizationUserLicenseReadModelConfiguration
    : IEntityTypeConfiguration<OrganizationUserLicenseReadModel>
{
    public void Configure(EntityTypeBuilder<OrganizationUserLicenseReadModel> builder)
    {
        builder.ToTable("OrganizationUserLicenseReadModels");


        builder.HasKey(x => x.LicenseAssignmentId);

        builder.HasIndex(x => x.OrganizationUserId)
            .HasDatabaseName("IX_OrganizationUserLicenseReadModels_OrganizationUserId");

        builder.HasIndex(x => x.OrganizationId)
            .HasDatabaseName("IX_OrganizationUserLicenseReadModels_OrganizationId");

       builder.HasIndex(x => x.SubscriptionOrderId)
            .HasDatabaseName("IX_OrganizationUserLicenseReadModels_SubscriptionOrderId");

        builder.HasIndex(x => new { x.OrganizationUserId, x.Status })
            .HasDatabaseName("IX_OrganizationUserLicenseReadModels_OrgUserId_Status");

        builder.Property(x => x.LicenseAssignmentId)
            .IsRequired();

        builder.Property(x => x.OrganizationUserId)
            .IsRequired();

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.SubscriptionOrderId)
            .IsRequired();

        builder.Property(x => x.LicenseType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(LicenseAssignmentStatus.Pending);

        builder.Property(x => x.AssignedAt)
            .IsRequired();

        builder.Property(x => x.LastUpdatedAt)
            .IsRequired();
    }
}


