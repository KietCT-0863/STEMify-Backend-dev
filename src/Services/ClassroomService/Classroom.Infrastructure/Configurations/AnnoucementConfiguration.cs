using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations;

/// <summary>
/// Entity configuration for Announcement entity
/// Defines classroom announcements with proper constraints and relationships
/// </summary>
public class AnnoucementConfiguration : BaseIntEntityConfiguration<Annoucement>
{
    public override void Configure(EntityTypeBuilder<Annoucement> builder)
    {
        // Apply base entity configuration
        base.Configure(builder);

        // Table mapping
        builder.ToTable("Announcement");

        // Configure string properties
        ConfigureStringProperties(builder);

        // Configure foreign key properties
        ConfigureForeignKeys(builder);

        // Configure date properties
        ConfigureDateProperties(builder);

        // Configure indexes
        ConfigureIndexes(builder);
    }

    /// <summary>
    /// Configure string properties with validation and constraints
    /// </summary>
    private static void ConfigureStringProperties(EntityTypeBuilder<Annoucement> builder)
    {
        builder
            .Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnType("varchar(255)")
            .HasComment("Announcement title");

        builder
            .Property(a => a.Content)
            .IsRequired()
            .HasMaxLength(2000)
            .HasColumnType("varchar(2000)")
            .HasComment("Announcement content");

        builder
            .Property(a => a.FileUrl)
            .HasMaxLength(500)
            .HasColumnType("varchar(500)")
            .HasComment("Optional file attachment URL");
    }

    /// <summary>
    /// Configure foreign key properties
    /// </summary>
    private static void ConfigureForeignKeys(EntityTypeBuilder<Annoucement> builder)
    {
        builder.Property(a => a.ClassroomId).IsRequired().HasComment("Foreign key to classroom");

        // Note: Author information could be tracked through audit fields or separate table
        // For now, we rely on application layer to track who created the announcement
    }

    /// <summary>
    /// Configure date and time properties
    /// </summary>
    private static void ConfigureDateProperties(EntityTypeBuilder<Annoucement> builder)
    {
        builder
            .Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Announcement creation timestamp");

        builder
            .Property(a => a.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate()
            .HasComment("Announcement last update timestamp");
    }

    /// <summary>
    /// Configure indexes for query performance
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<Annoucement> builder)
    {
        // Index for finding announcements by classroom
        builder.HasIndex(a => a.ClassroomId).HasDatabaseName("IX_Announcements_ClassroomId");

        // Index for ordering by creation date
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("IX_Announcements_CreatedAt");

        // Composite index for classroom announcements ordered by date
        builder
            .HasIndex(a => new { a.ClassroomId, a.CreatedAt })
            .HasDatabaseName("IX_Announcements_ClassroomCreatedAt");
    }
}
