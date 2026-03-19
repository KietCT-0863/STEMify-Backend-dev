using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations
{
    public class QuizAttemptConfiguration : BaseIntEntityConfiguration<QuizAttempt>
    {
        public override void Configure(EntityTypeBuilder<QuizAttempt> builder)
        {
            base.Configure(builder);
            // Table mapping
            builder.ToTable("QuizAttempt");

            builder.Property(qa => qa.StudentQuizId)
                .IsRequired();
            builder.Property(qa => qa.AttemptNumber)
                .IsRequired();
            builder.Property(qa => qa.StartedAt)
                .IsRequired();
            builder.Property(qa => qa.CompletedAt)
                .IsRequired(false);
            builder.Property(qa => qa.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(qa => qa.TotalScore)
                .HasColumnType("numeric(5,2)")
                .IsRequired();

            // Relationships
            builder.HasOne(qa => qa.StudentQuiz)
                .WithMany(sq => sq.QuizAttempts)
                .HasForeignKey(qa => qa.StudentQuizId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(qa => qa.QuestionAttempts)
                .WithOne(qqa => qqa.QuizAttempt)
                .HasForeignKey(qqa => qqa.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(qa => qa.StudentQuizId)
                .HasDatabaseName("IX_QuizAttempt_StudentQuiz");

            builder.HasIndex(qa => new { qa.StudentQuizId, qa.AttemptNumber })
                .IsUnique()
                .HasDatabaseName("IX_QuizAttempt_StudentQuiz_AttemptNumber");
        }
    }
}
