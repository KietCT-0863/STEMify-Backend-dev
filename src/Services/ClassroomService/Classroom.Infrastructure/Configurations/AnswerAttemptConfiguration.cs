using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations
{
    public class AnswerAttemptConfiguration : BaseIntEntityConfiguration<AnswerAttempt>
    {
        public override void Configure(EntityTypeBuilder<AnswerAttempt> builder)
        {
            base.Configure(builder);

            // Table mapping
            builder.ToTable("AnswerAttempt");

            builder.Property(aa => aa.IsCorrect)
            .IsRequired();

            builder.Property(aa => aa.IsSelected)
                .IsRequired();

            // Relationships
            builder.HasOne(aa => aa.QuestionAttempt)
                .WithMany(qqa => qqa.AnswerAttempts)
                .HasForeignKey(aa => aa.QuestionAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(aa => aa.QuestionAttemptId)
                .HasDatabaseName("IX_AnswerAttempt_QuestionAttempt");

            builder.HasIndex(aa => new { aa.QuestionAttemptId, aa.AnswerId })
                .IsUnique()
                .HasDatabaseName("IX_AnswerAttempt_QuestionAttempt_Answer");
        }
    }
}
