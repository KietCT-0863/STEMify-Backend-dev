using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cart.Infrastructure.Configurations
{
    public class CartConfiguration : BaseIntEntityConfiguration<Domain.Entities.Cart>
    {
        public override void Configure(EntityTypeBuilder<Domain.Entities.Cart> builder)
        {
            base.Configure(builder);

            builder.ToTable("Carts");

            builder.Property(o => o.UserId)
                   .IsRequired();

            builder.Property(o => o.Status)
                .HasDefaultValue(Shared.Enums.CartStatus.Active)
                .HasConversion<string>();

            builder.HasIndex(o => o.UserId);
            builder.HasIndex(o => new { o.UserId, o.Status });

            builder.HasMany(o => o.CartItems)
                   .WithOne(i => i.Cart)
                   .HasForeignKey(i => i.CartId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
