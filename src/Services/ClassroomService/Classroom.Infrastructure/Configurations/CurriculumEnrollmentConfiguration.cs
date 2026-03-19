using Classroom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations
{
    /// <summary>
    /// Entity configuration for CurriculumEnrollment entity
    /// Defines student enrollment in classrooms with proper constraints
    /// </summary>
    public class CurriculumEnrollmentConfiguration : BaseIntEntityConfiguration<CurriculumEnrollment>
    {
        public override void Configure(EntityTypeBuilder<CurriculumEnrollment> builder)
        {
            base.Configure(builder);

            // Table name (optional, can remove if using default conventions)
            builder.ToTable("CurriculumEnrollments");

            // StudentId is required
            builder.Property(e => e.StudentId)
                .IsRequired();

            builder
            .Property(e => e.ProgressPercentage)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Percentage of curriculum completed (0-100)");

            // CurriculumEnrollmentId is required
            builder.Property(e => e.CurriculumId)
                .IsRequired();

            // Enrollment status stored as string or int (your choice)
            builder.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>(); // store enum as string (safer for readability)

            // Relationships
            builder.HasOne(e => e.Certificate)
                .WithOne(c => c.CurriculumEnrollment)
                .HasForeignKey<Certificate>(c => c.CurriculumEnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(e => e.CourseEnrollments)
                .WithOne(c => c.CurriculumEnrollment)
                .HasForeignKey(c => c.CurriculumEnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional: Add index for quick lookups
            builder.HasIndex(e => new { e.StudentId, e.CurriculumId })
                .IsUnique(false); // one student can enroll multiple times if allowed
        }
    }
}
