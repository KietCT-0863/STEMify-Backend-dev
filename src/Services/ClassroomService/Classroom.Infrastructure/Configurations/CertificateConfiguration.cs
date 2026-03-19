using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Classroom.Infrastructure.Configurations
{
    /// <summary>
    /// Entity configuration for Certificate entity
    /// Defines certificate rules and constraints
    /// </summary>
    public class CertificateConfiguration : BaseIntEntityConfiguration<Domain.Entities.Certificate>
    {
        public override void Configure(EntityTypeBuilder<Domain.Entities.Certificate> builder)
        {
            base.Configure(builder);

            builder.ToTable("Certificates");

            builder.Property(c => c.UserId)
                .IsRequired();

            builder.Property(c => c.CertificateType)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(c => c.IssueDate)
                .IsRequired();

            builder.Property(c => c.VerificationCode)
                .IsRequired();

            builder.HasIndex(c => c.VerificationCode)
                .IsUnique();

            // CourseEnrollment 1–1
            builder.HasOne(c => c.CourseEnrollment)
                .WithOne(e => e.Certificate)
                .HasForeignKey<Domain.Entities.Certificate>(c => c.CourseEnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.CourseEnrollmentId).IsUnique();

            // CurriculumEnrollment 1–1
            builder.HasOne(c => c.CurriculumEnrollment)
                .WithOne(e => e.Certificate)
                .HasForeignKey<Domain.Entities.Certificate>(c => c.CurriculumEnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Check constraint to enforce business rules:
            //builder.HasCheckConstraint("CK_Certificate_CourseCurriculum",
            //    "([CertificateType] = 'Course' AND [CourseEnrollmentId] IS NOT NULL AND [CurriculumEnrollmentId] IS NULL) " +
            //    "OR ([CertificateType] = 'Curriculum' AND [CourseEnrollmentId] IS NULL AND [CurriculumEnrollmentId] IS NOT NULL)");
        }
    }
}
