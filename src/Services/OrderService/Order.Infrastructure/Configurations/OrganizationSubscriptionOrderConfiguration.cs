using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Configurations
{
    public class OrganizationSubscriptionOrderConfiguration : BaseIntEntityConfiguration<OrganizationSubscriptionOrder>
    {
        public override void Configure(EntityTypeBuilder<OrganizationSubscriptionOrder> builder)
        {
            base.Configure(builder);
            builder.ToTable("OrganizationSubscriptionOrders");

            builder.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.Property(x => x.PlanName).HasMaxLength(100).IsRequired();

            builder
                .Property(e => e.Status)
                .HasConversion<string>();

            builder.HasOne(x => x.Organization)
                   .WithMany(x => x.SubscriptionOrders)
                   .HasForeignKey(x => x.OrganizationId);

            builder.HasOne(x => x.Contract)
                   .WithMany(x => x.SubscriptionOrders)
                   .HasForeignKey(x => x.ContractId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.ParentSubscription)
                   .WithMany(x => x.ChildSubscriptions)
                   .HasForeignKey(x => x.ParentSubscriptionId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
