using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations;

/// <summary>
/// Entity configuration for AssignmentQuestionAttempt entity
/// Defines individual question attempts with proper constraints and relationships
/// </summary>
public class AssignmentQuestionAttemptConfiguration : BaseIntEntityConfiguration<AssignmentQuestionAttempt>
{
    public override void Configure(EntityTypeBuilder<AssignmentQuestionAttempt> builder)
    {
        // Apply base entity configuration
        base.Configure(builder);

        // Configure string properties
        ConfigureStringProperties(builder);

        // Configure foreign key properties
        ConfigureForeignKeys(builder);

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
    private static void ConfigureStringProperties(EntityTypeBuilder<AssignmentQuestionAttempt> builder)
    {
        builder.Property(aqa => aqa.AnswerText)
            .IsRequired(false)
            .HasMaxLength(4000)
            .HasComment("Text answer provided by student");

        builder.Property(aqa => aqa.AnswerFileUrl)
            .IsRequired(false)
            .HasMaxLength(500)
            .HasComment("URL to uploaded answer file");
    }

    /// <summary>
    /// Configure foreign key properties
    /// </summary>
    private static void ConfigureForeignKeys(EntityTypeBuilder<AssignmentQuestionAttempt> builder)
    {
        builder.Property(aqa => aqa.AssignmentAttemptId)
            .IsRequired();

        builder.Property(aqa => aqa.AssignmentQuestionId)
            .IsRequired();

        // Foreign key relationship with AssignmentAttempt
        builder.HasOne(aqa => aqa.AssignmentAttempt)
            .WithMany(aa => aa.AssignmentQuestionAttempts)
            .HasForeignKey(aqa => aqa.AssignmentAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Configure numeric properties
    /// </summary>
    private static void ConfigureNumericProperties(EntityTypeBuilder<AssignmentQuestionAttempt> builder)
    {
        builder.Property(aqa => aqa.Points)
            .IsRequired()
            .HasPrecision(5, 2)
            .HasDefaultValue(0)
            .HasComment("Points earned for this question");
    }

    /// <summary>
    /// Configure relationships
    /// </summary>
    private static void ConfigureRelationships(EntityTypeBuilder<AssignmentQuestionAttempt> builder)
    {
        // One-to-many relationship with RubricScore
        builder.HasMany(aqa => aqa.RubricScores)
            .WithOne(rs => rs.AssignmentQuestionAttempt)
            .HasForeignKey(rs => rs.AssignmentQuestionAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Configure date and time properties (none for this entity)
    /// </summary>
    private static void ConfigureDateProperties(EntityTypeBuilder<AssignmentQuestionAttempt> builder)
    {
        // No date properties in this entity
    }

    /// <summary>
    /// Configure indexes for query performance
    /// </summary>
    private static void ConfigureIndexes(EntityTypeBuilder<AssignmentQuestionAttempt> builder)
    {
        // Index for querying by assignment attempt
        builder.HasIndex(aqa => aqa.AssignmentAttemptId)
            .HasDatabaseName("IX_AssignmentQuestionAttempt_AssignmentAttemptId");

        // Index for querying by assignment question
        builder.HasIndex(aqa => aqa.AssignmentQuestionId)
            .HasDatabaseName("IX_AssignmentQuestionAttempt_AssignmentQuestionId");

        // Composite unique index to prevent duplicate question attempts in same assignment attempt
        builder.HasIndex(aqa => new { aqa.AssignmentAttemptId, aqa.AssignmentQuestionId })
            .IsUnique()
            .HasDatabaseName("IX_AssignmentQuestionAttempt_AssignmentAttemptId_QuestionId");
    }
}