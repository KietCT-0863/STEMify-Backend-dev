using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Entities;

namespace Product.Infrastructure.Configurations
{
    public class ComponentConfiguration : BaseIntEntityConfiguration<Domain.Entities.Component>
    {
        public override void Configure(EntityTypeBuilder<Component> builder)
        {
            base.Configure(builder);
            builder.ToTable("Components");

            builder.Property(c => c.Name)
              .IsRequired()
              .HasMaxLength(255);

            builder.HasMany(c => c.KitComponents)
               .WithOne(k => k.Component)
               .HasForeignKey(c => c.ComponentId)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
