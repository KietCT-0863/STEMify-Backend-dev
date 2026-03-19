using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations
{
    public class StudentSectionProgressConfiguration
        : BaseIntEntityConfiguration<StudentSectionProgress>
    {
        public override void Configure(EntityTypeBuilder<StudentSectionProgress> builder)
        {
            // Table name
            builder.ToTable("StudentSectionProgress");

            // Foreign key: StudentLessonProgressId
            builder
                .Property(x => x.StudentLessonProgressId)
                .IsRequired()
                .HasComment("FK to StudentLessonProgress");

            builder
                .HasIndex(x => x.StudentLessonProgressId)
                .HasDatabaseName("IX_StudentSectionProgress_LessonProgressId");

            // Foreign key: SectionId (from resource-service)
            builder
                .Property(x => x.SectionId)
                .IsRequired()
                .HasComment("FK to Section (from resource-service)");

            builder
                .HasIndex(x => x.SectionId)
                .HasDatabaseName("IX_StudentSectionProgress_SectionId");

            // Progress status enum
            builder
                .Property(x => x.Status)
                .IsRequired()
                .HasConversion(v => v.ToString(), v => Enum.Parse<ProgressStatus>(v))
                .HasColumnType("varchar(50)")
                .HasComment(
                    "Progress status: NotStarted, InProgress, Completed, Submitted, Failed."
                );

            builder.HasIndex(x => x.Status).HasDatabaseName("IX_StudentSectionProgress_Status");

            // CompletedAt
            builder
                .Property(x => x.CompletedAt)
                .HasComment("Timestamp when the section was completed");

            // Optional: Unique constraint to prevent duplicate tracking
            builder
                .HasIndex(x => new { x.StudentLessonProgressId, x.SectionId })
                .IsUnique()
                .HasDatabaseName("IX_StudentSectionProgress_LessonSection_Unique");
        }
    }
}
