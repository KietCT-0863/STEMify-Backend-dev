using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resource.Domain.Entities;

namespace Resource.Infrastructure.Configurations
{
    public class LessonAssetConfiguration : BaseIntEntityConfiguration<LessonAsset>
    {
        public override void Configure(EntityTypeBuilder<LessonAsset> builder)
        {
            base.Configure(builder);

            builder
            .Property(c => c.LessonId)
            .IsRequired();

            builder
            .Property(c => c.AssetUrl)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");

            builder
            .Property(c => c.Type)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

            builder
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("varchar(200)");

            builder
            .Property(c => c.PublicId)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnType("varchar(255)");

            builder
            .Property(c => c.Size)
            .IsRequired()
            .HasColumnType("bigint");

            builder
            .HasMany(c => c.LessonAssetTags)
            .WithOne(r => r.LessonAsset)
            .HasForeignKey(r => r.LessonAssetId)
            .OnDelete(DeleteBehavior.Cascade) // When lesson asset deleted, delete lesson asset tags
            .HasConstraintName("FK_LessonAssetTags_LessonAsset");

            builder
            .HasOne(r => r.Lesson)
            .WithMany(r => r.LessonAssets)
            .HasForeignKey(r => r.LessonId)
            .OnDelete(DeleteBehavior.Cascade) // When lesson deleted, delete lesson assets
            .HasConstraintName("FK_LessonAssets_Lesson");
        }
    }
}
