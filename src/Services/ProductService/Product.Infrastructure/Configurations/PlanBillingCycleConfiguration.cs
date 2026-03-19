using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Product.Infrastructure.Configurations
{
    public class PlanBillingCycleConfiguration : BaseIntEntityConfiguration<Domain.Entities.PlanBillingCycle>
    {
        public override void Configure(EntityTypeBuilder<Domain.Entities.PlanBillingCycle> builder)
        {
            base.Configure(builder);

            builder.ToTable("PlanBillingCycles");

            builder.Property(p => p.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder
                .Property(e => e.BillingCycle)
                .HasConversion<string>();

            builder.HasOne(p => p.Plan)
                .WithMany(p => p.PlanBillingCycles)
                .HasForeignKey(p => p.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.ParentPlanBillingCycle)
                .WithMany(p => p.AddOnBillingCycles)
                .HasForeignKey(p => p.ParentPlanBillingCycleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
