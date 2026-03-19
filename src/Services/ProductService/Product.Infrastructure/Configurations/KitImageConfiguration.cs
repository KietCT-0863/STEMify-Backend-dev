using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Entities;

namespace Product.Infrastructure.Configurations
{
    public class KitImageConfiguration : BaseIntEntityConfiguration<KitImage>
    {
        public override void Configure(EntityTypeBuilder<KitImage> builder)
        {
            base.Configure(builder);
            builder.ToTable("KitImages");

            builder.Property(ki => ki.ImageUrl)
               .HasMaxLength(500);

            // AltText: Required
            builder.Property(ki => ki.AltText)
                   .HasMaxLength(255);

            // Foreign key: KitId → KitProduct
            builder.HasOne(ki => ki.Kit)
                   .WithMany(k => k.KitImages)
                   .HasForeignKey(ki => ki.KitId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
