using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Payment.Infrastructure.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Domain.Entities.Payment>
    {
        public void Configure(EntityTypeBuilder<Domain.Entities.Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Currency)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(p => p.PaymentUrl)
                .HasMaxLength(500);

            builder.Property(p => p.ReturnUrl)
                .HasMaxLength(500);

            builder.Property(p => p.CancelUrl)
                .HasMaxLength(500);

            builder.Property(p => p.Metadata)
                .HasColumnType("jsonb");

            builder.HasMany(p => p.Transactions)
                .WithOne(t => t.Payment)
                .HasForeignKey(t => t.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Refunds)
                .WithOne(r => r.Payment)
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(p => p.OrderId);
            builder.HasIndex(p => p.BuyerId);
            builder.HasIndex(p => p.OrderNumber);
            builder.HasIndex(p => p.Status);
        }
    }
}
