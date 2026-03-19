using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations
{
    public class StudentQuizConfiguration : BaseIntEntityConfiguration<StudentQuiz>
    {
        public override void Configure(EntityTypeBuilder<StudentQuiz> builder)
        {
            base.Configure(builder);

            // Table mapping
            builder.ToTable("StudentQuiz");

            builder.Property(sq => sq.StudentId)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(sq => sq.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(sq => sq.FinalScore)
                .HasColumnType("numeric(5,2)");

            builder.Property(sq => sq.AssignedAt)
                .IsRequired();

            builder.Property(sq => sq.DueDate)
                .IsRequired(false);

            builder.Property(sq => sq.MaxAttemptAllowed)
                .IsRequired(false);

            builder.Property(sq => sq.TimeLimitMinutes)
                .IsRequired(false);

            builder.Property(sq => sq.AttemptCount)
                .HasDefaultValue(0);

            // Relationships
            builder.HasOne<StudentSectionProgress>()
                .WithOne(s => s.StudentQuiz)
                .HasForeignKey<StudentQuiz>(sq => sq.StudentSectionProgressId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(sq => new { sq.QuizId, sq.StudentSectionProgressId })
                .IsUnique()
                .HasDatabaseName("IX_StudentQuiz_Quiz_SectionProgress");

        }
    }
}
