using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Product.Domain.Enums;

namespace Product.Infrastructure.Configurations
{
    public class PlanConfiguration : BaseIntEntityConfiguration<Domain.Entities.Plan>
    {
        public override void Configure(EntityTypeBuilder<Domain.Entities.Plan> builder)
        {
            base.Configure(builder);

            builder.ToTable("Plans");

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder
            .Property(c => c.Status)
            .IsRequired()
            .HasDefaultValue(PlanStatus.Draft)
            .HasConversion(
                status => status.ToString(),
                status => (PlanStatus)Enum.Parse(typeof(PlanStatus), status)
            );

            builder.HasMany(p => p.PlanCurriculums)
                .WithOne(pc => pc.Plan)
                .HasForeignKey(pc => pc.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.PlanBillingCycles)
                .WithOne(pbc => pbc.Plan)
                .HasForeignKey(pbc => pbc.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
