using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Configurations
{
    public class OrganizationConfiguration : BaseIntEntityConfiguration<Organization>
    {
        public override void Configure(EntityTypeBuilder<Organization> builder)
        {
            base.Configure(builder);
            builder.ToTable("Organizations");

            builder.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();

            builder
                .Property(e => e.Status)
                .HasConversion<string>();

            builder.HasOne(x => x.OrganizationType)
                   .WithMany(x => x.Organizations)
                   .HasForeignKey(x => x.OrganizationTypeId);
        }
    }
}
