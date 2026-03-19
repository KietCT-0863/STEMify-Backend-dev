using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations
{
    public class StudentLessonProgressConfiguration
        : BaseIntEntityConfiguration<StudentLessonProgress>
    {
        public override void Configure(EntityTypeBuilder<StudentLessonProgress> builder)
        {
            // Apply base entity configuration
            base.Configure(builder);

            // Table mapping
            builder.ToTable("StudentLessonProgress");

            // Required: SectionId (FK từ resource-service)
            builder
                .Property(x => x.LessonId)
                .IsRequired()
                .HasComment("Reference to Lesson (from resource-service)");

            // Enum conversion
            builder
                .Property(x => x.Status)
                .IsRequired()
                .HasConversion(s => s.ToString(), s => Enum.Parse<ProgressStatus>(s))
                .HasMaxLength(50)
                .HasColumnType("varchar(50)")
                .HasComment(
                    "Progress status: InProgress, Completed, Failed."
                );

            // CompletedAt (nullable)
            builder
                .Property(x => x.CompletedAt)
                .HasComment("Timestamp when student completed the lesson");

            // Navigation: SectionProgress
            builder
                .HasMany(x => x.SectionProgress)
                .WithOne(x => x.LessonProgress)
                .HasForeignKey(x => x.StudentLessonProgressId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index on Status for filtering
            builder.HasIndex(x => x.Status).HasDatabaseName("IX_StudentLessonProgress_Status");
        }
    }
}
