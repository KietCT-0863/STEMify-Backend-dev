using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resource.Domain.Entities;

namespace Resource.Infrastructure.Configurations
{
    public class TagConfiguration : BaseIntEntityConfiguration<Tag>
    {
        public override void Configure(EntityTypeBuilder<Tag> builder)
        {
            base.Configure(builder);
            builder
                .Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("varchar(100)");
            builder
            .HasMany(c => c.LessonAssetTags)
            .WithOne(r => r.Tag)
            .HasForeignKey(r => r.TagId)
            .OnDelete(DeleteBehavior.Cascade) // When tag deleted, delete lesson asset tags
            .HasConstraintName("FK_LessonAssetTags_Tag");
        }
    }
}
