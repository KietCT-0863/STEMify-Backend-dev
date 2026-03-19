using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resource.Domain.Entities;
using System.Reflection.Emit;

namespace Resource.Infrastructure.Configurations
{
    public class CurriculumEmulationConfiguration : BaseIntEntityConfiguration<CurriculumEmulation>
    {
        public override void Configure(EntityTypeBuilder<CurriculumEmulation> builder)
        {
            base.Configure(builder);

            builder
                .HasOne(e => e.Curriculum)
                    .WithMany(a => a.CurriculumEmulations)
                    .HasForeignKey(e => e.CurriculumId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => new { e.EmulationId, e.CurriculumId }).IsUnique();
        }
    }
}