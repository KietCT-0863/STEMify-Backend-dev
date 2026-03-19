using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Enums;

namespace Order.Infrastructure.Configurations
{
    public class OrderConfiguration : BaseIntEntityConfiguration<Domain.Entities.Order>
    {
        public override void Configure(EntityTypeBuilder<Domain.Entities.Order> builder)
        {
            base.Configure(builder);
            builder.ToTable("Orders");

            builder.Property(o => o.OrderNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder
            .Property(c => c.Status)
            .IsRequired()
            .HasConversion(
                status => status.ToString(),
                status => (OrderStatus)Enum.Parse(typeof(OrderStatus), status)
            )
            .HasDefaultValue(OrderStatus.Pending)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

            // PaymentId reference to Payment service
            builder.Property(o => o.PaymentId)
                   .HasColumnType("uuid");

            builder.Property(o => o.SubTotal)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            builder.Property(o => o.DeliveryFee)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(o => o.DiscountAmount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(o => o.Amount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(o => o.Notes)
                   .HasMaxLength(500);

            // Relationships
            builder.HasMany(o => o.OrderHistories)
                   .WithOne(h => h.Order)
                   .HasForeignKey(h => h.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.OrderItems)
                   .WithOne(i => i.Order)
                   .HasForeignKey(i => i.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(o => o.BuyerId);
            builder.HasIndex(o => o.OrderNumber).IsUnique();
            builder.HasIndex(o => o.PaymentId);
        }
    }
}
