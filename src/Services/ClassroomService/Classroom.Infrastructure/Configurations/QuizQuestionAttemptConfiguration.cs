using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations
{
    public class QuizQuestionAttemptConfiguration : BaseIntEntityConfiguration<QuizQuestionAttempt>
    {
        public override void Configure(EntityTypeBuilder<QuizQuestionAttempt> builder)
        {
            base.Configure(builder);

            // Table mapping
            builder.ToTable("QuizQuestionAttempt");

            builder.Property(qqa => qqa.Score)
                .HasColumnType("numeric(5,2)")
                .IsRequired(false);

            builder.Property(qqa => qqa.IsCorrect)
                .IsRequired();

            // Relationships
            builder.HasOne(qqa => qqa.QuizAttempt)
                .WithMany(qa => qa.QuestionAttempts)
                .HasForeignKey(qqa => qqa.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(qqa => qqa.AnswerAttempts)
                .WithOne(aa => aa.QuestionAttempt)
                .HasForeignKey(aa => aa.QuestionAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(qqa => qqa.QuizAttemptId)
                .HasDatabaseName("IX_QuizQuestionAttempt_QuizAttempt");

            builder.HasIndex(qqa => new { qqa.QuizAttemptId, qqa.QuestionId })
                .IsUnique()
                .HasDatabaseName("IX_QuizQuestionAttempt_QuizAttempt_Question");
        }
    }
}
