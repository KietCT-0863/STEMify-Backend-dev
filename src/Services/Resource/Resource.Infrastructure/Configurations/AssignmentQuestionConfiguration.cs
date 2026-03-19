using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resource.Domain.Entities;
using Resource.Domain.Enums;

namespace Resource.Infrastructure.Configurations
{
    public class AssignmentQuestionConfiguration : BaseIntEntityConfiguration<AssignmentQuestion>
    {
        public override void Configure(EntityTypeBuilder<AssignmentQuestion> builder)
        {
            base.Configure(builder);

            // Properties
            builder.Property(x => x.AssignmentId)
                .IsRequired();

            builder.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired()
                .HasDefaultValue(AssignmentQuestionType.Text);

            builder.Property(x => x.Content)
                .IsRequired();

            builder.Property(x => x.OrderIndex)
                .IsRequired();

            builder.Property(x => x.Points)
                .IsRequired();

            // Relationships
            builder.HasOne(x => x.Assignment)
                .WithMany(a => a.AssignmentQuestions)
                .HasForeignKey(x => x.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(x => x.AssignmentId)
                .HasDatabaseName("IX_AssignmentQuestion_AssignmentId");

            builder.HasIndex(x => new { x.AssignmentId, x.OrderIndex })
                .HasDatabaseName("IX_AssignmentQuestion_AssignmentId_OrderIndex");
        }
    }
}