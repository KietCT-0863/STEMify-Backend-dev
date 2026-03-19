using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations
{
    public class ContactConfiguration : BaseIntEntityConfiguration<Contact>
    {
        public override void Configure(EntityTypeBuilder<Contact> builder)
        {
            base.Configure(builder);

            builder.ToTable("Contacts");

            builder.Property(c => c.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Email)
               .IsRequired()
               .HasMaxLength(200);

            builder.Property(c => c.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(c => c.OrganizationName)
                .HasMaxLength(200);

            builder.Property(c => c.JobRoleId)
                .IsRequired();

            builder.Property(c => c.HandledByUserId)
                .IsRequired(false);

            builder
            .Property(c => c.Status)
            .IsRequired()
            .HasConversion(
                status => status.ToString(),
                status => (ContactStatus)Enum.Parse(typeof(ContactStatus), status));

            // Relationships
            builder.HasOne(c => c.JobRole)
                .WithMany()
                .HasForeignKey(c => c.JobRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.HandledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
