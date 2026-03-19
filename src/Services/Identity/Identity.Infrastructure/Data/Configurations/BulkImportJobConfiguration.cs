using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class BulkImportJobConfiguration : IEntityTypeConfiguration<BulkImportJob>
{
    public void Configure(EntityTypeBuilder<BulkImportJob> builder)
    {
        builder.ToTable("BulkImportJobs");

        builder.HasKey(j => j.Id);

        // Properties
        builder.Property(j => j.OrganizationId)
            .IsRequired()
            .HasComment("Organization ID from Order Service");

        builder.Property(j => j.SubscriptionOrderId)
            .HasComment("Optional SubscriptionOrderId preferred for this bulk job");

        builder.Property(j => j.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasComment("Job processing status");

        builder.Property(j => j.TotalCount)
            .IsRequired()
            .HasComment("Total number of users to invite");

        builder.Property(j => j.ProcessedCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Number of users processed");

        builder.Property(j => j.SuccessCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Number of successful invitations");

        builder.Property(j => j.FailedCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Number of failed invitations");

        builder.Property(j => j.CsvDataJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasComment("Serialized CSV data for processing");

        builder.Property(j => j.CreatedBy)
            .IsRequired()
            .HasComment("User ID who created the job");

        builder.Property(j => j.CreatedAt)
            .IsRequired()
            .HasComment("Job creation timestamp");

        builder.Property(j => j.UpdatedAt)
            .IsRequired()
            .HasComment("Last update timestamp");

        builder.Property(j => j.StartedAt)
            .HasComment("Job processing start timestamp");

        builder.Property(j => j.CompletedAt)
            .HasComment("Job completion timestamp");

        // Ignore computed properties
        builder.Ignore(j => j.ProgressPercentage);
        builder.Ignore(j => j.ProcessingDuration);

        // Configure owned collection for failures
        builder.OwnsMany(j => j.Failures, failure =>
        {
            failure.ToTable("BulkImportFailures");
            failure.Property(f => f.Email).HasMaxLength(256).IsRequired();
            failure.Property(f => f.Reason).HasMaxLength(500).IsRequired();
            failure.Property(f => f.FailedAt).IsRequired();
        });

        // Indexes for performance
        builder.HasIndex(j => j.OrganizationId)
            .HasDatabaseName("IX_BulkImportJobs_OrganizationId");

        builder.HasIndex(j => j.Status)
            .HasDatabaseName("IX_BulkImportJobs_Status");

        builder.HasIndex(j => j.CreatedBy)
            .HasDatabaseName("IX_BulkImportJobs_CreatedBy");

        builder.HasIndex(j => new { j.OrganizationId, j.CreatedAt })
            .HasDatabaseName("IX_BulkImportJobs_Organization_Created");
    }
}
