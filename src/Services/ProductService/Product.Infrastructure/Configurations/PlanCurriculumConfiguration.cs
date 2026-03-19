using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Product.Infrastructure.Configurations
{
    public class PlanCurriculumConfiguration : BaseIntEntityConfiguration<Domain.Entities.PlanCurriculum>
    {
        public override void Configure(EntityTypeBuilder<Domain.Entities.PlanCurriculum> builder)
        {
            base.Configure(builder);

            builder.ToTable("PlanCurriculums");

            builder.HasOne(pc => pc.Plan)
                .WithMany(p => p.PlanCurriculums)
                .HasForeignKey(pc => pc.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
