using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations;

/// <summary>
/// Entity configuration for StudentAssignment entity
/// Defines student assignment tracking with proper constraints and relationships
/// </summary>
public class StudentAssignmentConfiguration : BaseIntEntityConfiguration<StudentAssignment>
{
    public override void Configure(EntityTypeBuilder<StudentAssignment> builder)
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
    private static void ConfigureStringProperties(EntityTypeBuilder<StudentAssignment> builder)
    {
        builder.Property(sa => sa.StudentId)
            .IsRequired()
            .HasMaxLength(450);
    }

    /// <summary>
    /// Configure foreign key properties
    /// </summary>
    private static void ConfigureForeignKeys(EntityTypeBuilder<StudentAssignment> builder)
    {
        builder.Property(sa => sa.StudentSectionProgressId)
            .IsRequired();

        builder.Property(sa => sa.AssignmentId)
            .IsRequired();

        // Foreign key relationship with StudentSectionProgress
        builder.HasOne(sa => sa.StudentSectionProgress)
            .WithOne(s => s.StudentAssignment)
            .HasForeignKey<StudentAssignment>(sa => sa.StudentSectionProgressId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Configure date and time properties
    /// </summary>
    private static void ConfigureDateProperties(EntityTypeBuilder<StudentAssignment> builder)
    {
        builder.Property(sa => sa.DueDate)
            .IsRequired(false);
    }

    /// <summary>
    /// Configure numeric properties
    /// </summary>
    private static void ConfigureNumericProperties(EntityTypeBuilder<StudentAssignment> builder)
    {
        builder.Property(sa => sa.FinalScore)
            .IsRequired(false)
            .HasPrecision(5, 2)
            .HasComment("Final score as percentage (0-100)");

        builder.Property(sa => sa.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(sa => sa.MaxAttemptAllowed)
            .IsRequired(false);

        builder.Property(sa => sa.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
    }

    /// <summary>
    /// Configure relationships
    /// </summary>
    private static void ConfigureRelationships(EntityTypeBuilder<StudentAssignment> builder)
    {
        // One-to-many relationship with AssignmentAttempt
        builder.HasMany(sa => sa.AssignmentAttempts)
            .WithOne(aa => aa.StudentAssignment)
            .HasForeignKey(aa => aa.StudentAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Configure indexes for query performance
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<StudentAssignment> builder)
    {
        // Index for querying by student
        builder.HasIndex(sa => sa.StudentId)
            .HasDatabaseName("IX_StudentAssignment_StudentId");

        // Index for querying by assignment
        builder.HasIndex(sa => sa.AssignmentId)
            .HasDatabaseName("IX_StudentAssignment_AssignmentId");

        // Composite index for student and assignment (unique constraint)
        builder.HasIndex(sa => new { sa.StudentId, sa.AssignmentId })
            .IsUnique()
            .HasDatabaseName("IX_StudentAssignment_StudentId_AssignmentId");

        // Index for querying by status
        builder.HasIndex(sa => sa.Status)
            .HasDatabaseName("IX_StudentAssignment_Status");

        // Index for querying by due date
        builder.HasIndex(sa => sa.DueDate)
            .HasDatabaseName("IX_StudentAssignment_DueDate");

        // Composite index for student and status (common query pattern)
        builder.HasIndex(sa => new { sa.StudentId, sa.Status })
            .HasDatabaseName("IX_StudentAssignment_StudentId_Status");

        // Index for StudentSectionProgressId
        builder.HasIndex(sa => sa.StudentSectionProgressId)
            .HasDatabaseName("IX_StudentAssignment_StudentSectionProgressId");
    }
}