using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cart.Infrastructure.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<Domain.Entities.CartItem>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.CartItem> builder)
        {
            builder.ToTable("CartItems");

            builder.HasKey(ci => new { ci.CartId, ci.ProductId });

            builder.Property(x => x.Quantity)
                .IsRequired()
                .HasDefaultValue(1);

            builder.HasOne(x => x.Cart)
                .WithMany(o => o.CartItems)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ProductId);
        }
    }
}
