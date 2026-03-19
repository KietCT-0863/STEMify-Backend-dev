using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Configurations
{
    public class OrganizationTypeConfiguration : BaseIntEntityConfiguration<OrganizationType>
    {
        public override void Configure(EntityTypeBuilder<OrganizationType> builder)
        {
            base.Configure(builder);
            builder.ToTable("OrganizationTypes");

            builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        }
    }
}
