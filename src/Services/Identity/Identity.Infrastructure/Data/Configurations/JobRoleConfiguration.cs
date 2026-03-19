using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations
{
    public class JobRoleConfiguration : BaseIntEntityConfiguration<JobRole>
    {
        public override void Configure(EntityTypeBuilder<JobRole> builder)
        {
            base.Configure(builder);

            builder.ToTable("JobRoles");

            builder
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");
        }
    }
}
