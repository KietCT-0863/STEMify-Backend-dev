using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations;

/// <summary>
/// Entity configuration for AssignmentAttempt entity
/// Defines assignment attempt submissions with proper constraints and relationships
/// </summary>
public class AssignmentAttemptConfiguration : BaseIntEntityConfiguration<AssignmentAttempt>
{
    public override void Configure(EntityTypeBuilder<AssignmentAttempt> builder)
    {
        // Apply base entity configuration
        base.Configure(builder);

        // Configure string properties
        ConfigureStringProperties(builder);

        // Configure foreign key properties
        ConfigureForeignKeys(builder);

        // Configure date properties
        ConfigureDateProperties(builder);

        // Configure numeric properties
        ConfigureNumericProperties(builder);

        // Configure relationships
        ConfigureRelationships(builder);

        // Configure indexes
        ConfigureIndexes(builder);
    }

    /// <summary>
    /// Configure string properties with validation and constraints
    /// </summary>
    private static void ConfigureStringProperties(EntityTypeBuilder<AssignmentAttempt> builder)
    {
        builder.Property(aa => aa.TeacherId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(aa => aa.Feedback)
            .IsRequired(false)
            .HasMaxLength(2000);
    }

    /// <summary>
    /// Configure foreign key properties
    /// </summary>
    private static void ConfigureForeignKeys(EntityTypeBuilder<AssignmentAttempt> builder)
    {
        builder.Property(aa => aa.StudentAssignmentId)
            .IsRequired();

        // Foreign key relationship with StudentAssignment
        builder.HasOne(aa => aa.StudentAssignment)
            .WithMany(sa => sa.AssignmentAttempts)
            .HasForeignKey(aa => aa.StudentAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Configure date and time properties
    /// </summary>
    private static void ConfigureDateProperties(EntityTypeBuilder<AssignmentAttempt> builder)
    {
    }

    /// <summary>
    /// Configure numeric properties
    /// </summary>
    private static void ConfigureNumericProperties(EntityTypeBuilder<AssignmentAttempt> builder)
    {
        builder.Property(aa => aa.TotalScore)
            .IsRequired()
            .HasPrecision(5, 2)
            .HasDefaultValue(0)
            .HasComment("Total score as percentage (0-100)");

        builder.Property(aa => aa.AttemptNumber)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(aa => aa.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(Classroom.Domain.Enums.AssignmentAttemptStatus.UnderReview);
    }

    /// <summary>
    /// Configure relationships
    /// </summary>
    private static void ConfigureRelationships(EntityTypeBuilder<AssignmentAttempt> builder)
    {
        // One-to-many relationship with AssignmentQuestionAttempt
        builder.HasMany(aa => aa.AssignmentQuestionAttempts)
            .WithOne(aqa => aqa.AssignmentAttempt)
            .HasForeignKey(aqa => aqa.AssignmentAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Configure indexes for query performance
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<AssignmentAttempt> builder)
    {
        // Index for querying by student assignment
        builder.HasIndex(aa => aa.StudentAssignmentId)
            .HasDatabaseName("IX_AssignmentAttempt_StudentAssignmentId");

        // Index for querying by teacher
        builder.HasIndex(aa => aa.TeacherId)
            .HasDatabaseName("IX_AssignmentAttempt_TeacherId");

        // Index for querying by status
        builder.HasIndex(aa => aa.Status)
            .HasDatabaseName("IX_AssignmentAttempt_Status");

        // Index for querying by submitted date
        builder.HasIndex(aa => aa.SubmittedAt)
            .HasDatabaseName("IX_AssignmentAttempt_SubmittedAt");

        // Composite index for student assignment and attempt number (unique constraint)
        builder.HasIndex(aa => new { aa.StudentAssignmentId, aa.AttemptNumber })
            .IsUnique()
            .HasDatabaseName("IX_AssignmentAttempt_StudentAssignmentId_AttemptNumber");

        // Composite index for teacher and status (common query pattern)
        builder.HasIndex(aa => new { aa.TeacherId, aa.Status })
            .HasDatabaseName("IX_AssignmentAttempt_TeacherId_Status");
    }
}