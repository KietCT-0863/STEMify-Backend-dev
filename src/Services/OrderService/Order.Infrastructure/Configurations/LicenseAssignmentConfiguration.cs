using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Infrastructure.Configurations
{
    public class LicenseAssignmentConfiguration : BaseIntEntityConfiguration<LicenseAssignment>
    {
        public override void Configure(EntityTypeBuilder<LicenseAssignment> builder)
        {
            base.Configure(builder);
            builder.ToTable("LicenseAssignments");

            builder
                .Property(e => e.Status)
                .HasConversion<string>();

            builder
                .Property(e => e.LicenseType)
                .HasConversion<string>();

            builder.HasOne(x => x.OrganizationSubscriptionOrder)
                   .WithMany(x => x.LicenseAssignments)
                   .HasForeignKey(x => x.OrganizationSubscriptionOrderId);
        }
    }
}
