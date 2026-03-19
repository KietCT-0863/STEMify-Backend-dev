using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resource.Domain.Entities;

namespace Resource.Infrastructure.Configurations
{
    public class AssignmentConfiguration : BaseIntEntityConfiguration<Assignment>
    {
        public override void Configure(EntityTypeBuilder<Assignment> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.ContentId)
                .IsRequired();

            builder.Property(x => x.Title)
                .IsRequired();

            builder.Property(x => x.TotalScore)
                .IsRequired()
                .HasDefaultValue(100);

            builder.Property(x => x.PassingScore)
                .IsRequired()
                .HasDefaultValue(80);

            builder.Property(x => x.DurationDays)
                .IsRequired(false);

            // Relationships
            builder.HasMany(x => x.AssignmentQuestions)
                .WithOne(q => q.Assignment)
                .HasForeignKey(q => q.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(x => x.ContentId)
                .HasDatabaseName("IX_Assignment_SectionId");
        }
    }
}