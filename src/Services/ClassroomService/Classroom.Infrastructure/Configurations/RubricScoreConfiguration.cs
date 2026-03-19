using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations;

/// <summary>
/// Entity configuration for RubricScore entity
/// Defines rubric scoring for question attempts with proper constraints and relationships
/// </summary>
public class RubricScoreConfiguration : BaseIntEntityConfiguration<RubricScore>
{
    public override void Configure(EntityTypeBuilder<RubricScore> builder)
    {
        // Apply base entity configuration
        base.Configure(builder);

        // Configure foreign key properties
        ConfigureForeignKeys(builder);

        // Configure numeric properties
        ConfigureNumericProperties(builder);

        // Configure indexes
        ConfigureIndexes(builder);
    }

    /// <summary>
    /// Configure string properties with validation and constraints (none for this entity)
    /// </summary>
    private static void ConfigureStringProperties(EntityTypeBuilder<RubricScore> builder)
    {
        // No string properties in this entity
    }

    /// <summary>
    /// Configure foreign key properties
    /// </summary>
    private static void ConfigureForeignKeys(EntityTypeBuilder<RubricScore> builder)
    {
        builder.Property(rs => rs.AssignmentQuestionAttemptId)
            .IsRequired();

        builder.Property(rs => rs.RubricCriterionId)
            .IsRequired();

        // Foreign key relationship with AssignmentQuestionAttempt
        builder.HasOne(rs => rs.AssignmentQuestionAttempt)
            .WithMany(aqa => aqa.RubricScores)
            .HasForeignKey(rs => rs.AssignmentQuestionAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Configure date and time properties (none for this entity)
    /// </summary>
    private static void ConfigureDateProperties(EntityTypeBuilder<RubricScore> builder)
    {
        // No date properties in this entity
    }

    /// <summary>
    /// Configure numeric properties
    /// </summary>
    private static void ConfigureNumericProperties(EntityTypeBuilder<RubricScore> builder)
    {
        builder.Property(rs => rs.Points)
            .IsRequired()
            .HasPrecision(5, 2)
            .HasDefaultValue(0)
            .HasComment("Points awarded for this rubric criterion");
    }

    /// <summary>
    /// Configure indexes for query performance
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<RubricScore> builder)
    {
        // Index for querying by assignment question attempt
        builder.HasIndex(rs => rs.AssignmentQuestionAttemptId)
            .HasDatabaseName("IX_RubricScore_AssignmentQuestionAttemptId");

        // Index for querying by rubric criterion
        builder.HasIndex(rs => rs.RubricCriterionId)
            .HasDatabaseName("IX_RubricScore_RubricCriterionId");

        // Composite unique index to prevent duplicate rubric scores for same criterion in same question attempt
        builder.HasIndex(rs => new { rs.AssignmentQuestionAttemptId, rs.RubricCriterionId })
            .IsUnique()
            .HasDatabaseName("IX_RubricScore_QuestionAttemptId_CriterionId");
    }
}