using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Configurations
{
    public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.ToTable("PaymentTransactions");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.ProviderTransactionId)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(t => t.Currency)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(t => t.GatewayResponseCode)
                .HasMaxLength(50);

            builder.Property(t => t.GatewayResponseMessage)
                .HasMaxLength(500);

            builder.Property(t => t.RawResponse)
                .HasColumnType("text");

            builder.HasIndex(t => t.PaymentId);
            builder.HasIndex(t => t.ProviderTransactionId);
            builder.HasIndex(t => t.Status);
        }
    }
}
