using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Entities;

namespace Product.Infrastructure.Configurations
{
    public class KitComponentConfiguration : BaseIntEntityConfiguration<Domain.Entities.KitComponent>
    {
        public override void Configure(EntityTypeBuilder<KitComponent> builder)
        {
            base.Configure(builder);
            builder.ToTable("KitComponents");

            builder.Property(kc => kc.KitId)
                .IsRequired();
            builder.Property(kc => kc.ComponentId)
                .IsRequired();

            builder.Property(kc => kc.Quantity)
                .IsRequired();
            // Foreign key: ComponentId → Component
            builder.HasOne(kc => kc.KitProduct)
                .WithMany(k => k.KitComponents)
                .HasForeignKey(kc => kc.KitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(kc => kc.Component)
                .WithMany(c => c.KitComponents)
                .HasForeignKey(kc => kc.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
