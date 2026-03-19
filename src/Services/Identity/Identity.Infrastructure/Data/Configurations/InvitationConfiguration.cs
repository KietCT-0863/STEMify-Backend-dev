using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.OrganizationId)
            .IsRequired()
            .HasComment("Organization ID from Order Service");

        builder.OwnsOne(i => i.InviteeEmail, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("InviteeEmail")
                .HasMaxLength(256)
                .IsRequired()
                .HasComment("Email address of the invitee");
        });

        builder.OwnsOne(i => i.Token, token =>
        {
            token.Property(t => t.Value)
                .HasColumnName("Token")
                .HasMaxLength(100)
                .IsRequired()
                .HasComment("Unique invitation token");
        });

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasComment("Invitation status");

        builder.Property(i => i.TargetRole)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasComment("Role to assign to user");

        builder.Property(i => i.LicenseType)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("License type to assign");

        builder.Property(i => i.ExpiresAt)
            .IsRequired()
            .HasComment("Invitation expiration date");

        builder.Property(i => i.SubscriptionOrderId)
            .HasComment("Order ID of the subscription");

        builder.Property(i => i.FullName)
            .HasMaxLength(200)
            .HasComment("Full name from CSV");

        builder.Property(i => i.FirstName)
            .HasMaxLength(100)
            .HasComment("First name from CSV");

        builder.Property(i => i.LastName)
            .HasMaxLength(100)
            .HasComment("Last name from CSV");

        builder.Property(i => i.GroupName)
            .HasMaxLength(50)
            .HasComment("Class ID from CSV");

        builder.Property(i => i.ExternalId)
            .HasMaxLength(100)
            .HasComment("External ID from CSV");

        builder.Property(i => i.SentAt)
            .HasComment("Email sent timestamp");

        builder.Property(i => i.AcceptedAt)
            .HasComment("Invitation accepted timestamp");

        builder.Property(i => i.AcceptedUserId)
            .HasComment("User ID who accepted the invitation");

        builder.Property(i => i.ProcessedByJobId)
            .HasComment("Bulk import job ID that created this invitation");

        builder.Property(i => i.ScheduledSendDate)
            .HasComment("Date when email should be sent (for scheduled invitations). If null, email should be sent immediately");

        builder.Property(i => i.CreatedAt)
            .IsRequired()
            .HasComment("Invitation creation timestamp");

        builder.Property(i => i.UpdatedAt)
            .IsRequired()
            .HasComment("Last update timestamp");

        builder.HasIndex(i => new { i.OrganizationId, i.Status })
            .HasDatabaseName("IX_Invitations_Organization_Status");

        builder.HasIndex(i => i.ProcessedByJobId)
            .HasDatabaseName("IX_Invitations_JobId");

        builder.HasIndex(i => new { i.Status, i.ExpiresAt })
            .HasDatabaseName("IX_Invitations_Status_Expiry");

        builder.HasIndex(i => i.AcceptedUserId)
            .HasDatabaseName("IX_Invitations_AcceptedUserId");

        builder.HasIndex(i => new { i.ScheduledSendDate, i.Status })
            .HasDatabaseName("IX_Invitations_ScheduledSendDate_Status")
            .HasFilter("\"ScheduledSendDate\" IS NOT NULL");
    }
}
