using Classroom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations;

/// <summary>
/// Entity configuration for Classroom entity
/// Defines database mapping and constraints following Clean Architecture principles
/// </summary>
public class ClassroomConfiguration : BaseIntEntityConfiguration<Domain.Entities.Classroom>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.Classroom> builder)
    {
        // Apply base entity configuration (ID, common properties)
        base.Configure(builder);

        // Table mapping
        builder.ToTable("Classroom");

        // Configure string properties with appropriate constraints
        ConfigureStringProperties(builder);

        // Configure date properties
        ConfigureDateProperties(builder);

        // Configure teacher relationship
        ConfigureTeacherRelationship(builder);

        // Configure enum properties
        ConfigureEnumProperties(builder);

        // Configure navigation properties and relationships
        ConfigureRelationships(builder);

        // Configure unique constraints and indexes
        ConfigureIndexes(builder);
    }

    /// <summary>
    /// Configure string properties with validation and constraints
    /// </summary>
    private static void ConfigureStringProperties(
        EntityTypeBuilder<Domain.Entities.Classroom> builder
    )
    {
        builder
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnType("varchar(255)")
            .HasComment("Classroom display name");

        builder
            .Property(c => c.Grade)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("varchar(50)")
            .HasComment("Grade level (e.g., Grade 1, Grade 2)");

        builder
            .Property(c => c.Description)
            .HasMaxLength(1000)
            .HasColumnType("varchar(1000)")
            .HasComment("Classroom description");

        builder
            .Property(c => c.ClassCode)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("varchar(100)")
            .HasComment("Unique classroom code for joining");

        builder
            .Property(c => c.CoverImageUrl)
            .HasMaxLength(200)
            .HasColumnType("varchar(200)")
            .HasComment("URL to classroom cover image");
    }

    /// <summary>
    /// Configure date and time properties
    /// </summary>
    private static void ConfigureDateProperties(
        EntityTypeBuilder<Domain.Entities.Classroom> builder
    )
    {
        builder.Property(c => c.StartDate).IsRequired().HasComment("Classroom start date");

        builder.Property(c => c.EndDate).IsRequired().HasComment("Classroom end date");

        builder
            .Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Record creation timestamp");

        builder
            .Property(c => c.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate()
            .HasComment("Record last update timestamp");
    }

    /// <summary>
    /// Configure teacher relationship
    /// </summary>
    private static void ConfigureTeacherRelationship(
        EntityTypeBuilder<Domain.Entities.Classroom> builder
    )
    {
        builder
            .Property(c => c.TeacherId)
            .IsRequired()
            .HasComment("Foreign key to teacher (Identity service)");

        // Note: Teacher is in Identity service, so no direct FK constraint
        // This follows microservices architecture principles
    }

    /// <summary>
    /// Configure enum properties with proper conversion
    /// </summary>
    private static void ConfigureEnumProperties(
        EntityTypeBuilder<Domain.Entities.Classroom> builder
    )
    {
        builder
            .Property(c => c.Status)
            .IsRequired()
            .HasConversion(
                status => status.ToString(),
                status => (ClassroomStatus)Enum.Parse(typeof(ClassroomStatus), status)
            )
            .HasColumnType("varchar(50)")
            .HasComment("Classroom status (Active, Inactive, Archived)");
    }

    /// <summary>
    /// Configure relationships with other entities
    /// </summary>
    private static void ConfigureRelationships(EntityTypeBuilder<Domain.Entities.Classroom> builder)
    {
        // One-to-many: Classroom -> Announcements
        builder
            .HasMany(c => c.Annoucements)
            .WithOne(a => a.Classroom)
            .HasForeignKey(a => a.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade) // When classroom deleted, delete announcements
            .HasConstraintName("FK_Announcements_Classroom");

        // one-to-many: classroom -> classroomStudents
        builder
            .HasMany(c => c.ClassroomStudents)
            .WithOne(cs => cs.Classroom)
            .HasForeignKey(cs => cs.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade) // When classroom deleted, delete classroomStudents
            .HasConstraintName("FK_ClassroomStudents_Classroom");
    }

    /// <summary>
    /// Configure indexes for performance optimization
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<Domain.Entities.Classroom> builder)
    {
        // Unique index on ClassCode for fast lookups and uniqueness
        builder
            .HasIndex(c => c.ClassCode)
            .IsUnique()
            .HasDatabaseName("IX_Classrooms_ClassCode_Unique");

        // Index on TeacherId for teacher's classrooms queries
        builder.HasIndex(c => c.TeacherId).HasDatabaseName("IX_Classrooms_TeacherId");

        // Index on Status for filtering active/inactive classrooms
        builder.HasIndex(c => c.Status).HasDatabaseName("IX_Classrooms_Status");

        // Composite index for date range queries
        builder
            .HasIndex(c => new { c.StartDate, c.EndDate })
            .HasDatabaseName("IX_Classrooms_DateRange");
    }
}
