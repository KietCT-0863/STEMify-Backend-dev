using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Order.Infrastructure.Configurations
{
    public class OrderItemConfiguration : BaseIntEntityConfiguration<Domain.Entities.OrderItem>
    {
        public override void Configure(EntityTypeBuilder<Domain.Entities.OrderItem> builder)
        {
            base.Configure(builder);
            builder.ToTable("OrderItems");

            builder.Property(x => x.ProductName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.Sku)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.ProductImageUrl)
                .HasMaxLength(500);
            builder.Property(x => x.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.DiscountAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.HasOne(x => x.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
