using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Enums;

namespace Order.Infrastructure.Configurations
{
    public class OrderHistoryConfiguration : BaseIntEntityConfiguration<Domain.Entities.OrderHistory>
    {
        public override void Configure(EntityTypeBuilder<Domain.Entities.OrderHistory> builder)
        {
            base.Configure(builder);
            builder.ToTable("OrderHistories");

            builder
            .Property(c => c.OldStatus)
            .IsRequired()
            .HasConversion(
                status => status.ToString(),
                status => (OrderStatus)Enum.Parse(typeof(OrderStatus), status)
            )
            .HasDefaultValue(OrderStatus.Pending)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

            builder
            .Property(c => c.NewStatus)
            .IsRequired()
            .HasConversion(
                status => status.ToString(),
                status => (OrderStatus)Enum.Parse(typeof(OrderStatus), status)
            )
            .HasDefaultValue(OrderStatus.Pending)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

            builder.Property(h => h.ChangedById)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(h => h.ChangedByRole)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(h => h.ChangedAt)
                   .IsRequired();
            builder.Property(h => h.Notes)
                   .HasMaxLength(500);

            builder.HasOne(h => h.Order)
                   .WithMany(o => o.OrderHistories)
                   .HasForeignKey(h => h.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
