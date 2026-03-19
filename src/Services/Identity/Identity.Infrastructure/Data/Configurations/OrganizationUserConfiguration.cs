using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class OrganizationUserConfiguration : IEntityTypeConfiguration<OrganizationUser>
{
    public void Configure(EntityTypeBuilder<OrganizationUser> builder)
    {
        builder.ToTable("OrganizationUsers");

        builder.HasKey(ou => ou.Id);

        builder.Property(ou => ou.OrganizationId)
            .IsRequired()
            .HasComment("Organization ID from Order Service - no FK constraint");

        builder.Property(ou => ou.UserId)
            .IsRequired()
            .HasComment("User ID from Identity Service");

        builder.Property(ou => ou.OrganizationRole)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasComment("User role within the organization");

        builder.Property(ou => ou.GroupId)
            .HasComment("Group ID");

        // Role-specific properties (subscription-scoped)
        builder.Property(ou => ou.Bio)
            .HasMaxLength(500)
            .HasComment("User bio/description within this subscription");

        builder.Property(ou => ou.StudentDateOfBirth)
            .HasComment("Student date of birth (only for Student role)");

        builder.Property(ou => ou.StudentMajor)
            .HasMaxLength(100)
            .HasComment("Student major (only for Student role)");

        builder.Property(ou => ou.TeacherSpecialization)
            .HasMaxLength(100)
            .HasComment("Teacher specialization (only for Teacher role)");



        builder.Property(ou => ou.JoinedAt)
            .IsRequired()
            .HasComment("Timestamp when user joined organization");

        builder.Property(ou => ou.LeftAt)
            .HasComment("Timestamp when user left organization");

        builder.Property(ou => ou.CreatedAt)
            .IsRequired()
            .HasComment("Record creation timestamp");

        builder.Property(ou => ou.UpdatedAt)
            .IsRequired()
            .HasComment("Last update timestamp");

        builder.HasOne(ou => ou.User)
            .WithMany()
            .HasForeignKey(ou => ou.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ou => new { ou.OrganizationId, ou.UserId })
            .IsUnique()
            .HasDatabaseName("IX_OrganizationUsers_Org_User");

        
        builder.HasIndex(ou => ou.OrganizationId)
            .HasDatabaseName("IX_OrganizationUsers_OrganizationId");

        builder.HasIndex(ou => ou.UserId)
            .HasDatabaseName("IX_OrganizationUsers_UserId");

        builder.HasIndex(ou => new { ou.OrganizationId, ou.OrganizationRole })
            .HasDatabaseName("IX_OrganizationUsers_Organization_Role");



        builder.HasIndex(ou => ou.GroupId)
            .HasDatabaseName("IX_OrganizationUsers_GroupId");

        builder.HasOne(ou => ou.Group)
            .WithMany(g => g.Students)
            .HasForeignKey(ou => ou.GroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
