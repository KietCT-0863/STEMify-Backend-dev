using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations;

/// <summary>
/// Entity configuration for CourseEnrollment entity
/// Defines student enrollment in classrooms with proper constraints
/// </summary>
public class CourseEnrollmentConfiguration : BaseIntEntityConfiguration<CourseEnrollment>
{
    public override void Configure(EntityTypeBuilder<CourseEnrollment> builder)
    {
        // Apply base entity configuration
        base.Configure(builder);

        // Table mapping
        builder.ToTable("CourseEnrollments");

        // Configure foreign key properties
        ConfigureForeignKeys(builder);

        // Configure enum properties
        ConfigureEnums(builder);

        builder
        .Property(e => e.ProgressPercentage)
        .IsRequired()
        .HasDefaultValue(0)
        .HasComment("Percentage of course completed (0-100)");

        // Configure indexes for performance
        ConfigureIndexes(builder);

        // Configure navigation property: LessonProgress
        builder
            .HasMany(e => e.LessonProgress)
            .WithOne(e => e.CourseEnrollment)
            .HasForeignKey(lp => lp.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Certificate)
                .WithOne(c => c.CourseEnrollment)
                .HasForeignKey<Domain.Entities.Certificate>(c => c.CourseEnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Configure foreign key properties
    /// </summary>
    private static void ConfigureForeignKeys(EntityTypeBuilder<CourseEnrollment> builder)
    {
        builder.Property(e => e.CourseId).IsRequired().HasComment("Foreign key to course");

        builder
            .Property(e => e.StudentId)
            .IsRequired()
            .HasComment("Foreign key to student (Identity service)");

        // Note: StudentId references Identity service, so no direct FK constraint
    }

    /// <summary>
    /// Configure enum properties with proper conversion
    /// </summary>
    private static void ConfigureEnums(EntityTypeBuilder<CourseEnrollment> builder)
    {
        builder
            .Property(e => e.Status)
            .IsRequired()
            .HasConversion(
                status => status.ToString(),
                status => (EnrollmentStatus)Enum.Parse(typeof(EnrollmentStatus), status)
            )
            .HasColumnType("varchar(50)")
            .HasComment("Enrollment status (InProgress, Compeleted)");
    }

    /// <summary>
    /// Configure indexes for query performance
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<CourseEnrollment> builder)
    {
        // Index for finding enrollments by classroom
        builder.HasIndex(e => e.CourseId).HasDatabaseName("IX_Enrollments_CourseId");

        // Index for finding enrollments by student
        builder.HasIndex(e => e.StudentId).HasDatabaseName("IX_Enrollments_StudentId");

        // Index for filtering by status
        builder.HasIndex(e => e.Status).HasDatabaseName("IX_Enrollments_Status");
    }
}
