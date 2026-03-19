using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Configurations
{
    public class RefundConfiguration : IEntityTypeConfiguration<Refund>
    {
        public void Configure(EntityTypeBuilder<Refund> builder)
        {
            builder.ToTable("Refunds");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.RefundNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(r => r.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(r => r.Currency)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(r => r.Reason)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(r => r.ProviderRefundId)
                .HasMaxLength(200);

            builder.Property(r => r.ErrorMessage)
                .HasMaxLength(1000);

            builder.HasIndex(r => r.PaymentId);
            builder.HasIndex(r => r.RefundNumber);
            builder.HasIndex(r => r.Status);
        }
    }
}
