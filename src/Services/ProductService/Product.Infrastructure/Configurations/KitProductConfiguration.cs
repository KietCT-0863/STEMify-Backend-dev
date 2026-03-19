using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Enums;

namespace Product.Infrastructure.Configurations
{
    public class KitProductConfiguration : BaseIntEntityConfiguration<Domain.Entities.KitProduct>
    {
        public override void Configure(EntityTypeBuilder<Domain.Entities.KitProduct> builder)
        {
            base.Configure(builder);
            builder.ToTable("KitProducts");

            builder
            .Property(c => c.Status)
            .IsRequired()
            .HasConversion(
                status => status.ToString(),
                status => (KitProductStatus)Enum.Parse(typeof(KitProductStatus), status)
            )
            .HasDefaultValue(KitProductStatus.Draft)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

            builder.HasMany(c => c.KitComponents)
               .WithOne(k => k.KitProduct)
               .HasForeignKey(c => c.KitId)
               .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
