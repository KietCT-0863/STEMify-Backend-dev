using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.OrganizationId)
            .IsRequired()
            .HasComment("Organization ID");

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Group name");

        builder.Property(g => g.Description)
            .HasMaxLength(500)
            .HasComment("Group description");

        builder.Property(g => g.Code)
            .HasMaxLength(50)
            .HasComment("Group code (unique within organization)");

        builder.Property(g => g.Grade)
            .HasConversion<int?>()
            .HasComment("Group grade level (1-5)");

        builder.Property(g => g.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(GroupStatus.Active)
            .HasComment("Group status: Active or Archived");

        builder.Property(g => g.CreatedByUserId)
            .IsRequired()
            .HasComment("User ID who created the group");

        builder.Property(g => g.CreatedAt)
            .IsRequired()
            .HasComment("Record creation timestamp");

        builder.Property(g => g.UpdatedAt)
            .IsRequired()
            .HasComment("Last update timestamp");

        builder.HasMany(g => g.Students)
            .WithOne(ou => ou.Group)
            .HasForeignKey(ou => ou.GroupId)
            .OnDelete(DeleteBehavior.SetNull); 

        builder.HasIndex(g => g.OrganizationId)
            .HasDatabaseName("IX_Groups_OrganizationId");

        builder.HasIndex(g => new { g.OrganizationId, g.Code })
            .IsUnique()
            .HasFilter("\"Code\" IS NOT NULL")
            .HasDatabaseName("IX_Groups_OrganizationId_Code");

        builder.HasIndex(g => new { g.OrganizationId, g.Status })
            .HasDatabaseName("IX_Groups_OrganizationId_Status");
    }
}

