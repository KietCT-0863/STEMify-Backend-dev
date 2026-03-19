using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Configurations
{
    public class ContractConfiguration : BaseIntEntityConfiguration<Contract>
    {
        public override void Configure(EntityTypeBuilder<Contract> builder)
        {
            base.Configure(builder);
            builder.ToTable("Contracts");

            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
            //builder.Property(x => x.Description).HasMaxLength(255);

            builder
                .Property(e => e.Status)
                .HasConversion<string>();

            builder.HasOne(x => x.Organization)
                   .WithMany(x => x.Contracts)
                   .HasForeignKey(x => x.OrganizationId);
        }
    }
}
